using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

internal sealed class MapperForm : Form {
    private const int PollSleepMs = 1;
    private const double MaxMouseFrameSeconds = 0.05;
    private readonly DirectHidController _hid;
    private readonly Config _config;
    private readonly ControllerProfile _controllerProfile;
    private readonly InputInjector _injector;
    private readonly MappingEngine _mapping = new MappingEngine();

    private Thread _pollThread;
    private volatile bool _pollRunning;
    private readonly object _tickLock = new object();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly ButtonHold[] _holds = new ButtonHold[8];
    private readonly bool[] _prevDown = new bool[8];
    private bool _debugSources;
    private bool _enabled;
    private bool _runtimeReleased = true;
    private bool _printedConnectedGuide;
    private bool _l2Pressed;
    private bool _r2Pressed;
    private StickDirection _leftDirection = StickDirection.None;
    private readonly LeftStickScrollIntegrator _leftStickScroll = new LeftStickScrollIntegrator();
    private readonly RightStickMouseIntegrator _rightStickMouse = new RightStickMouseIntegrator();
    private double _mouseFreezeUntilMs;
    private bool _fnArmed;
    private bool _leftMouseDown;
    private bool _rightMouseDown;
    private List<PhysicalKey> _accumulatedModifiers = new List<PhysicalKey>();
    private List<PhysicalKey> _heldLeftStickKeys = new List<PhysicalKey>();
    private readonly ClutchButtonStateMachine _clutchButton = new ClutchButtonStateMachine();
    private bool _prevClutchActive;
    private bool _prevCreate;
    private bool _prevOptions;
    private bool _prevHome;
    private bool _createKeyDown;
    private bool _optionsKeyDown;
    private bool _homeKeyDown;
    private double _disableStartMs;
    private bool _disableArmed = true;
    private volatile bool _manualVisible;
    private double _lastTickMs;

    public bool RestartControllerSelectionRequested { get; private set; }

    public MapperForm(Config config, ControllerProfile controllerProfile, bool debugSources, bool traceInput, bool traceSendinput) {
        _config = config;
        _controllerProfile = controllerProfile;
        _hid = new DirectHidController(controllerProfile);
        _debugSources = debugSources;
        _enabled = config.Enabled;
        _injector = new InputInjector(config.UseScanCode, config.UseInterception);
        _injector.TraceInput = traceInput;
        _injector.TraceSendinput = traceSendinput;
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Opacity = 0;
    }

    protected override void OnLoad(EventArgs e) {
        base.OnLoad(e);
        _hid.Start();
        int parentId = 0;
        try {
            var pc = new System.Diagnostics.PerformanceCounter("Process", "Creating Process ID", Process.GetCurrentProcess().ProcessName);
            parentId = (int)pc.NextValue();
        } catch { }

        _lastTickMs = NowMs();
        _pollRunning = true;
        NativeMethods.timeBeginPeriod(1);
        _pollThread = new Thread(PollLoop);
        _pollThread.IsBackground = true;
        _pollThread.Priority = ThreadPriority.AboveNormal;
        _pollThread.Start();

        Thread guideThread = new Thread(() => {
            while (true) {
                try {
                    if (Console.KeyAvailable) {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Enter) {
                            if (_manualVisible) {
                                Program.PrintRunningHome(_controllerProfile, _config, _hid.DisplayName, _hid.State.Connected);
                                _manualVisible = false;
                            } else {
                                Program.PrintDetailedManual(_controllerProfile, _config);
                                _manualVisible = true;
                            }
                        } else if (key.Key == ConsoleKey.Escape && !_manualVisible) {
                            if (Program.ClearSavedDefaultControllerForRuntime()) {
                                Logger.Info("default launch cleared from connected home");
                                RequestControllerSelectionRestart();
                                break;
                            }
                        }
                    }
                } catch { }
                Thread.Sleep(100);
            }
        });
        guideThread.IsBackground = true;
        guideThread.Start();
    }

    private void RequestControllerSelectionRestart() {
        RestartControllerSelectionRequested = true;
        _manualVisible = false;
        try {
            if (IsHandleCreated) {
                BeginInvoke((MethodInvoker)delegate { Close(); });
            } else {
                Close();
            }
        } catch {
            try { Application.ExitThread(); } catch { }
        }
    }

    private void PollLoop() {
        while (_pollRunning) {
            lock (_tickLock) {
                try { OnTick(); } catch (Exception ex) { Logger.Error("Tick error: " + ex.Message); }
            }
            Thread.Sleep(PollSleepMs);
        }
    }



    protected override void OnFormClosing(FormClosingEventArgs e) {
        _pollRunning = false;
        if (_pollThread != null) {
            _pollThread.Join(500);
        }
        NativeMethods.timeEndPeriod(1);
        _hid.Stop();
        ReleaseRuntimeHolds();

        Logger.Info("shutdown");
        base.OnFormClosing(e);
    }

    private bool _prevL1, _prevR1;
    private double _l1DownMs, _r1DownMs, _l2DownMs, _r2DownMs;
    private double _l1UpMs, _r1UpMs, _l2UpMs, _r2UpMs;
    private Layer _previousActionLayer = Layer.Base;
    private Layer _lastReleasedActionLayer = Layer.Base;
    private double _lastReleasedActionLayerUpMs;
    private double _lastReleasedActionLayerDownMs;

    private void OnTick() {
        ControllerState s = _hid.State;
        double now = NowMs();
        double deltaSec = Clamp((now - _lastTickMs) / 1000.0, 0.0, MaxMouseFrameSeconds);
        _lastTickMs = now;

        bool preL1 = _prevL1;
        bool preR1 = _prevR1;
        bool l1JustDown = s.L1 && !preL1;
        bool r1JustDown = s.R1 && !preR1;
        if (l1JustDown) { _l1DownMs = now; _l1UpMs = 0; }
        if (r1JustDown) { _r1DownMs = now; _r1UpMs = 0; }
        if (!s.L1 && preL1) _l1UpMs = now;
        if (!s.R1 && preR1) _r1UpMs = now;
        _prevL1 = s.L1;
        _prevR1 = s.R1;

        UpdateEmergency(s, now);
        if (!s.Connected || !_enabled) {
            if (!s.Connected) _printedConnectedGuide = false;
            if (!_runtimeReleased) {
                ReleaseRuntimeHolds();
                _runtimeReleased = true;
            }
            return;
        }
        _runtimeReleased = false;
        if (!_printedConnectedGuide) {
            Program.PrintConnectedWelcome(_controllerProfile, _config, _hid.DisplayName);
            _manualVisible = false;
            _printedConnectedGuide = true;
        }
        UpdateTriggers(s, now);
        UpdateClutchButton(s, now);

        UpdateLeftStick(s, deltaSec);
        UpdateActionButtons(s, now);
        UpdateMouseButtons(s, now);
        UpdateRightStick(s, now, deltaSec);
        UpdateSystemButtons(s);
    }

    private void UpdateTriggers(ControllerState s, double now) {
        if (!_l2Pressed && IsTriggerPressed(s.L2, _config.TriggerPressThreshold)) {
            _l2Pressed = true;
            _l2DownMs = now;
            _l2UpMs = 0;
        } else if (_l2Pressed && IsTriggerReleased(s.L2, _config.TriggerReleaseThreshold)) {
            _l2Pressed = false;
            _l2UpMs = now;
        }

        if (!_r2Pressed && IsTriggerPressed(s.R2, _config.TriggerPressThreshold)) {
            _r2Pressed = true;
            _r2DownMs = now;
            _r2UpMs = 0;
        } else if (_r2Pressed && IsTriggerReleased(s.R2, _config.TriggerReleaseThreshold)) {
            _r2Pressed = false;
            _r2UpMs = now;
        }
    }

    internal static bool IsTriggerPressed(double value, double threshold) {
        double press = Math.Max(0.0, threshold);
        return value > press;
    }

    internal static bool IsTriggerReleased(double value, double threshold) {
        double release = Math.Max(0.0, threshold);
        return value <= release;
    }

    private void UpdateClutchButton(ControllerState s, double now) {
        bool down = IsXboxController()
            ? (s.Create || s.Options)
            : s.TouchClick;
        _clutchButton.Update(down, now, _config.ClutchLongPressMs);
    }

    private bool IsClutchActive() {
        return _clutchButton.Active;
    }

    private bool IsSonyController() {
        return _controllerProfile == ControllerProfile.DualSense ||
               _controllerProfile == ControllerProfile.DualSenseBT ||
               _controllerProfile == ControllerProfile.DualShock4 ||
               _controllerProfile == ControllerProfile.DualShock4BT;
    }

    private bool IsXboxController() {
        return _controllerProfile == ControllerProfile.Xbox360 ||
               _controllerProfile == ControllerProfile.Xbox360BT ||
               _controllerProfile == ControllerProfile.XboxSeries ||
               _controllerProfile == ControllerProfile.XboxSeriesBT;
    }

    private PhysicalKey GetLeftStickKey(StickDirection dir) {
        switch (dir) {
            case StickDirection.Up: return PhysicalKey.None; // Wheel Up
            case StickDirection.UpRight: return PhysicalKey.None; // Fn Layer
            case StickDirection.Right: return PhysicalKey.LWin;
            case StickDirection.DownRight: return PhysicalKey.LAlt;
            case StickDirection.Down: return PhysicalKey.None; // Wheel Down
            case StickDirection.DownLeft: return PhysicalKey.LCtrl;
            case StickDirection.Left: return PhysicalKey.LShift;
            case StickDirection.UpLeft: return PhysicalKey.Escape;
            default: return PhysicalKey.None;
        }
    }

    private void UpdateLeftStick(ControllerState s, double deltaSec) {
        double radius = Math.Sqrt(s.LX * s.LX + s.LY * s.LY);
        StickDirection previous = _leftDirection;
        StickDirection next = previous;

        if (previous == StickDirection.None) {
            if (radius >= _config.LeftStickEnterDeadzone) {
                next = Sector(s.LX, s.LY);
            }
        } else if (radius < _config.LeftStickExitDeadzone) {
            next = StickDirection.None;
        } else {
            next = previous;
        }

        if (next != previous) {
            _leftDirection = next;
            _leftStickScroll.Reset();
            if (_leftDirection == StickDirection.UpRight) {
                _fnArmed = true;
            }
        }

        bool clutch = IsClutchActive();
        bool clutchJustPressed = clutch && !_prevClutchActive;
        bool clutchJustReleased = !clutch && _prevClutchActive;
        _prevClutchActive = clutch;

        List<PhysicalKey> desiredKeys = new List<PhysicalKey>();

        if (clutchJustReleased) {
            _accumulatedModifiers.Clear();
        }

        if (clutchJustPressed) {
            foreach (var key in _heldLeftStickKeys) {
                AccumulateLeftStickKey(key);
            }
        }

        if (_leftDirection != StickDirection.None && _leftDirection != StickDirection.Up && _leftDirection != StickDirection.Down) {
            PhysicalKey rawStickKey = GetLeftStickKey(_leftDirection);
            if (clutch) {
                AccumulateLeftStickKey(rawStickKey);
                desiredKeys.AddRange(_accumulatedModifiers);
            } else {
                AddUnique(desiredKeys, rawStickKey);
            }
        } else {
            if (clutch) {
                desiredKeys.AddRange(_accumulatedModifiers);
            } else {
                _accumulatedModifiers.Clear();
            }
        }

        foreach (var key in _heldLeftStickKeys) {
            if (!desiredKeys.Contains(key)) {
                _injector.CurrentSource = "LeftStick";
                _injector.CurrentReason = "ModifierUp " + key;
                _injector.KeyUp(key);
            }
        }
        foreach (var key in desiredKeys) {
            if (!_heldLeftStickKeys.Contains(key)) {
                _injector.CurrentSource = "LeftStick";
                _injector.CurrentReason = "ModifierDown " + key;
                _injector.KeyDown(key);
            }
        }

        _heldLeftStickKeys.Clear();
        _heldLeftStickKeys.AddRange(desiredKeys);

        if (_leftDirection != StickDirection.Up && _leftDirection != StickDirection.Down) {
            _leftStickScroll.Reset();
            return;
        }

        int wheelDelta;
        int direction = _leftDirection == StickDirection.Up ? 1 : -1;
        if (_leftStickScroll.TryUpdate(radius, deltaSec, _config, direction, out wheelDelta)) {
            _injector.CurrentSource = "LeftStick";
            _injector.CurrentReason = "AnalogScroll " + _leftDirection;
            _injector.MouseWheelDelta(wheelDelta);
        }
    }

    private PhysicalKey TranslateToFKey(PhysicalKey numberKey) {
        switch (numberKey) {
            case PhysicalKey.Num1: return PhysicalKey.F1;
            case PhysicalKey.Num2: return PhysicalKey.F2;
            case PhysicalKey.Num3: return PhysicalKey.F3;
            case PhysicalKey.Num4: return PhysicalKey.F4;
            case PhysicalKey.Num5: return PhysicalKey.F5;
            case PhysicalKey.Num6: return PhysicalKey.F6;
            case PhysicalKey.Num7: return PhysicalKey.F7;
            case PhysicalKey.Num8: return PhysicalKey.F8;
            case PhysicalKey.Num9: return PhysicalKey.F9;
            case PhysicalKey.Num0: return PhysicalKey.F10;
            case PhysicalKey.Minus: return PhysicalKey.F11;
            case PhysicalKey.Equals: return PhysicalKey.F12;
            default: return PhysicalKey.None;
        }
    }

    private KeyStroke ApplyFnLayer(KeyStroke stroke) {
        if (!_fnArmed || stroke.Shift) return stroke;
        PhysicalKey fKey = TranslateToFKey(stroke.Key);
        return fKey != PhysicalKey.None ? KeyStroke.Of(fKey) : stroke;
    }

    private static bool IsFunctionKey(KeyStroke stroke) {
        return !stroke.Shift && stroke.Key >= PhysicalKey.F1 && stroke.Key <= PhysicalKey.F12;
    }

    private static void AddUnique(List<PhysicalKey> keys, PhysicalKey key) {
        if (key != PhysicalKey.None && !keys.Contains(key)) {
            keys.Add(key);
        }
    }

    private void AccumulateLeftStickKey(PhysicalKey key) {
        AddUnique(_accumulatedModifiers, key);
    }

    private void UpdateActionButtons(ControllerState s, double now) {
        bool[] currentDown = new bool[] { s.Up, s.Right, s.Square, s.Triangle, s.Left, s.Down, s.Cross, s.Circle };
        Layer layer = _mapping.Resolve(s.L1, s.R1, _l2Pressed, _r2Pressed, _l1DownMs, _r1DownMs, _l2DownMs, _r2DownMs, _config.ComboLayerWindowMs);
        double layerMs = LayerTimestamp(layer);
        RememberReleasedActionLayer(layer, now);

        for (int i = 0; i < 8; i++) {
            bool prev = _prevDown[i];
            bool curr = currentDown[i];
            ButtonHold hold = _holds[i];
            KeyStroke layerKey = ApplyFnLayer(_mapping.Lookup(layer, (ActionButton)i));

            if (hold.Pending) {
                if (!curr && !hold.PendingReleased) {
                    hold.PendingReleased = true;
                }
                UpdatePendingLayer(ref hold, layer, layerMs);

                bool shouldFlushPending = now - hold.PendingSinceMs >= _config.ActionLayerGraceMs;
                if (!shouldFlushPending) {
                    _holds[i] = hold;
                    _prevDown[i] = curr;
                    continue;
                }

                bool releasedPending = hold.PendingReleased || !curr;
                Layer resolvedLayer = hold.PendingLayer != Layer.Base && hold.PendingLayer != Layer.Reserved
                    ? hold.PendingLayer
                    : (releasedPending ? hold.PendingLayer : layer);
                KeyStroke resolvedLayerKey = ApplyFnLayer(_mapping.Lookup(resolvedLayer, (ActionButton)i));
                if (!resolvedLayerKey.IsNone) {
                    _fnArmed = false;
                    if (resolvedLayer != Layer.Base || IsFunctionKey(resolvedLayerKey)) {
                        TapActionKey(i, resolvedLayerKey, "Button " + ActionButtonName(i) + " virtual tap");
                        if (!releasedPending) {
                            hold.Pending = false;
                            hold.PendingReleased = false;
                            hold.Key = resolvedLayerKey;
                            hold.KeyLayer = resolvedLayer;
                            hold.SuppressUntilRelease = true;
                            _holds[i] = hold;
                        } else {
                            _holds[i] = new ButtonHold();
                        }
                        _prevDown[i] = curr;
                        continue;
                    }

                    if (releasedPending) {
                        hold.Pending = false;
                        hold.PendingReleased = false;
                        PressActionKey(i, resolvedLayerKey, "Button " + ActionButtonName(i), ref hold, resolvedLayer, false, now);
                        ReleaseActionKey(i, resolvedLayerKey, "Button " + ActionButtonName(i) + " release after layer settle");
                        _holds[i] = new ButtonHold();
                        _prevDown[i] = curr;
                        continue;
                    }

                    hold.Pending = false;
                    hold.PendingReleased = false;
                    PressActionKey(i, resolvedLayerKey, "Button " + ActionButtonName(i), ref hold, resolvedLayer, resolvedLayer == Layer.Base, now);
                    _holds[i] = hold;
                    _prevDown[i] = curr;
                    continue;
                }

                _holds[i] = new ButtonHold();
                _prevDown[i] = curr;
                continue;
            }

            if (!prev && curr) {
                Layer initialLayer = InitialActionLayer(layer, now, layerMs);
                KeyStroke key = ApplyFnLayer(_mapping.Lookup(initialLayer, (ActionButton)i));
                if (ShouldDeferInitialAction(initialLayer)) {
                    hold = new ButtonHold();
                    hold.Pending = true;
                    hold.OriginalPendingLayer = initialLayer;
                    hold.PendingLayer = initialLayer;
                    hold.PendingLayerMs = initialLayer == layer ? layerMs : LayerTimestamp(initialLayer);
                    hold.PendingSinceMs = now;
                    _holds[i] = hold;
                    _prevDown[i] = curr;
                    continue;
                }

                hold.Key = key;
                hold.KeyIsDown = false;

                if (!key.IsNone) {
                    _fnArmed = false;
                    if (layer != Layer.Base || IsFunctionKey(key)) {
                        TapActionKey(i, key, "Button " + ActionButtonName(i) + " virtual tap");
                        hold.SuppressUntilRelease = true;
                    } else {
                        PressActionKey(i, key, "Button " + ActionButtonName(i), ref hold, layer, layer == Layer.Base, now);
                    }
                }

                _holds[i] = hold;
            } else if (prev && !curr) {
                // 1 -> 0: KeyUp edge
                if (hold.KeyIsDown) {
                    ReleaseActionKey(i, hold.Key, "Button " + ActionButtonName(i) + " release");
                }
                _holds[i] = new ButtonHold();
            } else if (prev && curr) {
                if (hold.SuppressUntilRelease) {
                    _holds[i] = hold;
                    _prevDown[i] = curr;
                    continue;
                }

                KeyStroke currentLayerKey = layerKey;

                if (hold.Key != currentLayerKey) {
                    if (hold.KeyLayer == Layer.Base && layer != Layer.Base) {
                        if (hold.KeyIsDown) {
                            ReleaseActionKey(i, hold.Key, "Button " + ActionButtonName(i) + " base release before layer change");
                        }
                        if (!currentLayerKey.IsNone) {
                            _fnArmed = false;
                            TapActionKey(i, currentLayerKey, "Button " + ActionButtonName(i) + " base-to-layer virtual tap");
                        }
                        hold.Key = currentLayerKey;
                        hold.KeyLayer = layer;
                        hold.KeyIsDown = false;
                        hold.RepeatEnabled = false;
                        hold.SuppressUntilRelease = true;
                        _holds[i] = hold;
                        _prevDown[i] = curr;
                        continue;
                    }

                    if (hold.KeyIsDown && ShouldSuppressLayerChangeDuringCharacterTap(hold, layer, now)) {
                        ReleaseActionKey(i, hold.Key, "Button " + ActionButtonName(i) + " layer change suppress tap residue");
                        hold.Key = KeyStroke.None;
                        hold.KeyIsDown = false;
                        hold.RepeatEnabled = false;
                        hold.SuppressUntilRelease = true;
                        _holds[i] = hold;
                        _prevDown[i] = curr;
                        continue;
                    }

                    if (hold.KeyIsDown) {
                        ReleaseActionKey(i, hold.Key, "Button " + ActionButtonName(i) + " layer change release");
                        hold.KeyIsDown = false;
                    }

                    if (layer != Layer.Base || IsFunctionKey(currentLayerKey)) {
                        if (!currentLayerKey.IsNone) {
                            _fnArmed = false;
                            TapActionKey(i, currentLayerKey, "Button " + ActionButtonName(i) + " layer change virtual tap");
                            hold.Key = currentLayerKey;
                            hold.KeyLayer = layer;
                            hold.KeyIsDown = false;
                            hold.RepeatEnabled = false;
                            hold.SuppressUntilRelease = true;
                            _holds[i] = hold;
                            _prevDown[i] = curr;
                            continue;
                        }
                    }

                    if (!currentLayerKey.IsNone) {
                        _fnArmed = false;
                        PressActionKey(i, currentLayerKey, "Button " + ActionButtonName(i) + " layer change press", ref hold, layer, layer == Layer.Base, now);
                    }

                    hold.Key = currentLayerKey;
                    _holds[i] = hold;
                } else {
                    UpdateBaseRepeat(i, ref hold, now);
                    _holds[i] = hold;
                }
            }

            _prevDown[i] = curr;
        }
    }

    private bool ShouldDeferInitialAction(Layer initialLayer) {
        return _config.ActionLayerGraceMs > 0;
    }

    internal static bool IsSubsetOf(Layer sub, Layer super) {
        if (sub == super) return true;
        if (sub == Layer.Base || sub == Layer.Reserved) return true;
        if (super == Layer.Base || super == Layer.Reserved) return false;

        if (super == Layer.R1L1) return sub == Layer.R1 || sub == Layer.L1;
        if (super == Layer.R2L2) return sub == Layer.R2 || sub == Layer.L2;
        if (super == Layer.L1R2) return sub == Layer.L1 || sub == Layer.R2;
        if (super == Layer.R1L2) return sub == Layer.R1 || sub == Layer.L2;

        return false;
    }

    private void RememberReleasedActionLayer(Layer layer, double now) {
        if (_previousActionLayer != layer && _previousActionLayer != Layer.Base && _previousActionLayer != Layer.Reserved) {
            double upMs = LayerUpTimestamp(_previousActionLayer);
            if (now - upMs < 100.0) {
                _lastReleasedActionLayer = _previousActionLayer;
                _lastReleasedActionLayerUpMs = upMs > 0.0 ? upMs : now;
                _lastReleasedActionLayerDownMs = LayerTimestamp(_previousActionLayer);
            }
        }
        _previousActionLayer = layer;
    }

    private Layer InitialActionLayer(Layer layer, double now, double layerMs) {
        return ResolveInitialActionLayer(layer, layerMs, _lastReleasedActionLayer, _lastReleasedActionLayerDownMs, now, _lastReleasedActionLayerUpMs, _config.ActionLayerPostGraceMs);
    }

    internal static Layer ResolveInitialActionLayer(Layer layer, double layerDownMs, Layer lastReleasedLayer, double lastReleasedLayerDownMs, double now, double lastReleasedLayerUpMs, double postGraceMs) {
        if (lastReleasedLayer != Layer.Base && lastReleasedLayer != Layer.Reserved && (now - lastReleasedLayerUpMs <= postGraceMs)) {
            if (IsSubsetOf(layer, lastReleasedLayer)) {
                return lastReleasedLayer;
            }
            if (lastReleasedLayerDownMs > layerDownMs) {
                return lastReleasedLayer;
            }
        }
        return layer;
    }

    private double LayerTimestamp(Layer layer) {
        switch (layer) {
            case Layer.L1: return _l1DownMs;
            case Layer.R1: return _r1DownMs;
            case Layer.L2: return _l2DownMs;
            case Layer.R2: return _r2DownMs;
            case Layer.R1L1: return Math.Max(_r1DownMs, _l1DownMs);
            case Layer.R2L2: return Math.Max(_r2DownMs, _l2DownMs);
            case Layer.L1R2: return Math.Max(_l1DownMs, _r2DownMs);
            case Layer.R1L2: return Math.Max(_r1DownMs, _l2DownMs);
            default: return 0.0;
        }
    }

    private double LayerUpTimestamp(Layer layer) {
        switch (layer) {
            case Layer.L1: return _l1UpMs;
            case Layer.R1: return _r1UpMs;
            case Layer.L2: return _l2UpMs;
            case Layer.R2: return _r2UpMs;
            case Layer.R1L1: return Math.Max(_r1UpMs, _l1UpMs);
            case Layer.R2L2: return Math.Max(_r2UpMs, _l2UpMs);
            case Layer.L1R2: return Math.Max(_l1UpMs, _r2UpMs);
            case Layer.R1L2: return Math.Max(_r1UpMs, _l2UpMs);
            default: return 0.0;
        }
    }

    private void UpdatePendingLayer(ref ButtonHold hold, Layer layer, double layerMs) {
        if (hold.OriginalPendingLayerUpMs == 0 && hold.OriginalPendingLayer != hold.PendingLayer) {
            hold.OriginalPendingLayerUpMs = LayerUpTimestamp(hold.OriginalPendingLayer);
        }
        double originalLayerUpMs = hold.OriginalPendingLayerUpMs == 0 ? LayerUpTimestamp(hold.OriginalPendingLayer) : hold.OriginalPendingLayerUpMs;
        double pendingLayerUpMs = LayerUpTimestamp(hold.PendingLayer);
        Layer next = ResolvePendingLayer(
            hold.PendingLayer,
            hold.OriginalPendingLayer,
            hold.PendingSinceMs,
            layer,
            layerMs,
            pendingLayerUpMs,
            originalLayerUpMs,
            _config.ActionLayerGraceMs,
            _config.LayerTakeoverWindowMs);

        if (next == hold.PendingLayer) return;
        hold.PendingLayer = next;
        hold.PendingLayerMs = layerMs;
    }

    private static bool IsComboComponent(Layer combo, Layer single) {
        if (combo == Layer.R1L1 && (single == Layer.R1 || single == Layer.L1)) return true;
        if (combo == Layer.R2L2 && (single == Layer.R2 || single == Layer.L2)) return true;
        if (combo == Layer.L1R2 && (single == Layer.L1 || single == Layer.R2)) return true;
        if (combo == Layer.R1L2 && (single == Layer.R1 || single == Layer.L2)) return true;
        return false;
    }

    internal static Layer ResolvePendingLayer(Layer pendingLayer, Layer originalLayer, double pendingSinceMs, Layer layer, double layerMs, double pendingLayerUpMs, double originalLayerUpMs, double actionLayerGraceMs, double takeoverWindowMs) {
        if (layer == Layer.Base || layer == Layer.Reserved) return pendingLayer;
        if (layer == pendingLayer) return pendingLayer;
        if (layerMs < pendingSinceMs) return pendingLayer;

        if (layerMs - pendingSinceMs > actionLayerGraceMs) return pendingLayer;

        bool layerCombo = IsComboLayer(layer);

        // 如果当前层是组合层，并且之前的 pendingLayer 是这个组合层的一个组件（即它的单层意图被组合层覆盖了）
        // 那么必须剥夺 pendingLayer 的资格，将其回退到 originalLayer 进行判定。
        Layer effectivePendingLayer = pendingLayer;
        double effectivePendingLayerUpMs = pendingLayerUpMs;
        if (layerCombo && IsComboComponent(layer, pendingLayer)) {
            effectivePendingLayer = originalLayer;
            effectivePendingLayerUpMs = originalLayerUpMs;
        }

        // 组合层的特殊之处：如果回退后的 effectivePendingLayer（或者原始层）与组合层毫无关联，则组合层无法接管它！
        // （这防止了毫无关联的组合层强行劫持旧动作键）
        if (layerCombo && !IsComboComponent(layer, effectivePendingLayer) && effectivePendingLayer != layer) {
            return effectivePendingLayer;
        }

        double overlap = LayerOverlapAfterActionMs(pendingSinceMs, layerMs, effectivePendingLayerUpMs, effectivePendingLayer);

        if (overlap > takeoverWindowMs) {
            return effectivePendingLayer;
        }

        bool pendingCombo = IsComboLayer(effectivePendingLayer);
        if (pendingCombo && !layerCombo) return effectivePendingLayer;

        return layer;
    }

    private static double LayerOverlapAfterActionMs(double pendingSinceMs, double layerMs, double layerUpMs, Layer heldLayer) {
        if (heldLayer == Layer.Base || heldLayer == Layer.Reserved) return 0.0;
        if (layerUpMs > pendingSinceMs) return Math.Max(0.0, layerUpMs - pendingSinceMs);
        if (layerUpMs == 0.0) return Math.Max(0.0, layerMs - pendingSinceMs);
        return 0.0;
    }

    private bool ShouldSuppressLayerChangeDuringCharacterTap(ButtonHold hold, Layer newLayer, double now) {
        if (hold.KeyLayer == Layer.Base) return false;
        if (IsComboLayer(hold.KeyLayer) && hold.KeyLayer != newLayer) return true;
        if (newLayer == Layer.Base) return true;
        if (hold.KeyLayer == newLayer) return false;
        return now - hold.KeyDownMs <= _config.ActionLayerSwitchGuardMs;
    }

    private static bool IsComboLayer(Layer layer) {
        return layer == Layer.R1L1 || layer == Layer.R2L2 || layer == Layer.L1R2 || layer == Layer.R1L2;
    }

    private void PressActionKey(int index, KeyStroke key, string reason, ref ButtonHold hold, Layer keyLayer, bool repeatable, double now) {
        string source = ActionSource(index);
        string btn = ActionButtonName(index);
        _injector.CurrentSource = source;
        _injector.CurrentReason = reason;
        DebugSources("Source=" + source + " Button=" + btn + " Mode=Held -> " + MappingEngine.KeyName(key) + "Down");
        if (key.Shift) _injector.KeyDown(PhysicalKey.LShift);
        _injector.KeyDown(key.Key);
        hold.Key = key;
        hold.KeyLayer = keyLayer;
        hold.KeyIsDown = true;
        hold.RepeatEnabled = repeatable;
        hold.KeyDownMs = now;
        hold.RepeatStartedMs = now;
        hold.NextRepeatMs = now + Math.Max(1, _config.RepeatDelayMs);
    }

    private void TapActionKey(int index, KeyStroke key, string reason) {
        string source = ActionSource(index);
        string btn = ActionButtonName(index);
        _injector.CurrentSource = source;
        _injector.CurrentReason = reason;
        DebugSources("Source=" + source + " Button=" + btn + " Mode=Tap -> " + MappingEngine.KeyName(key));
        _injector.KeyTap(key.Key, key.Shift, false, false, false);
    }

    private void ReleaseActionKey(int index, KeyStroke key, string reason) {
        string source = ActionSource(index);
        string btn = ActionButtonName(index);
        DebugSources("Source=" + source + " Button=" + btn + " Mode=Held -> " + MappingEngine.KeyName(key) + "Up");
        _injector.CurrentSource = source;
        _injector.CurrentReason = reason;
        _injector.KeyUp(key.Key);
        if (key.Shift) _injector.KeyUp(PhysicalKey.LShift);
    }

    private void UpdateBaseRepeat(int index, ref ButtonHold hold, double now) {
        if (!hold.RepeatEnabled || !hold.KeyIsDown || hold.Key.IsNone) return;
        if (now < hold.NextRepeatMs) return;

        string source = ActionSource(index);
        string btn = ActionButtonName(index);
        _injector.CurrentSource = source;
        _injector.CurrentReason = "Button " + btn + " progressive repeat";
        DebugSources("Source=" + source + " Button=" + btn + " Mode=Repeat -> " + MappingEngine.KeyName(hold.Key) + "Down");
        _injector.KeyDown(hold.Key.Key);

        double heldMs = Math.Max(0.0, now - hold.RepeatStartedMs);
        double interval = BaseRepeatIntervalMs(heldMs);
        hold.NextRepeatMs = now + interval;
    }

    private double BaseRepeatIntervalMs(double heldMs) {
        double fastFreq = 1000.0 / Math.Max(5.0, (double)_config.RepeatIntervalMs);
        double slowFreq = 1000.0 / Math.Max(5.0, (double)_config.BaseRepeatSlowIntervalMs);
        double ramp = Math.Max(1.0, (double)_config.BaseRepeatRampMs);
        double t = Clamp((heldMs - _config.RepeatDelayMs) / ramp, 0.0, 1.0);
        double freq = slowFreq + (fastFreq - slowFreq) * Math.Pow(t, 3.0);
        return 1000.0 / Math.Max(0.1, freq);
    }

    private static string ActionSource(int index) {
        return (index < 2 || index == 4 || index == 5) ? "DPad" : "FaceButton";
    }

    private static string ActionButtonName(int index) {
        return ((ActionButton)index).ToString();
    }

    private void UpdateMouseButtons(ControllerState s, double now) {
        if (s.L3 && !_leftMouseDown) {
            _injector.CurrentSource = "StickClick";
            _injector.CurrentReason = "L3";
            _injector.MouseButton(0, true);
            _leftMouseDown = true;
        } else if (!s.L3 && _leftMouseDown) {
            _injector.CurrentSource = "StickClick";
            _injector.CurrentReason = "L3 release";
            _injector.MouseButton(0, false);
            _leftMouseDown = false;
        }
        if (s.R3 && !_rightMouseDown) {
            _injector.CurrentSource = "StickClick";
            _injector.CurrentReason = "R3";
            _injector.MouseButton(1, true);
            _rightMouseDown = true;
            _mouseFreezeUntilMs = now + _config.R3FreezeMs;
        } else if (!s.R3 && _rightMouseDown) {
            _injector.CurrentSource = "StickClick";
            _injector.CurrentReason = "R3 release";
            _injector.MouseButton(1, false);
            _rightMouseDown = false;
        }
    }

    private void UpdateRightStick(ControllerState s, double now, double deltaSec) {
        if (now < _mouseFreezeUntilMs) {
            _rightStickMouse.Reset();
            return;
        }

        int ix;
        int iy;
        if (_rightStickMouse.TryUpdate(s.RX, s.RY, deltaSec, _config, out ix, out iy)) {
            _injector.CurrentSource = "RightStick";
            _injector.CurrentReason = "Mouse Move";
            _injector.MouseMove(ix, iy);
        }
    }

    private void UpdateSystemButtons(ControllerState s) {
        if (!IsSonyController()) return;

        UpdateMappedSystemButton("Share/Create", s.Create, PhysicalKey.RAlt, ref _prevCreate, ref _createKeyDown);
        UpdateMappedSystemButton("Options/Menu", s.Options, PhysicalKey.RCtrl, ref _prevOptions, ref _optionsKeyDown);
        UpdateMappedSystemButton("Home", s.Home, PhysicalKey.RShift, ref _prevHome, ref _homeKeyDown);
    }

    private void UpdateMappedSystemButton(string source, bool down, PhysicalKey key, ref bool prev, ref bool keyDown) {
        if (down && !prev) {
            _injector.CurrentSource = source;
            _injector.CurrentReason = source + " press";
            _injector.KeyDown(key);
            keyDown = true;
        } else if (!down && prev && keyDown) {
            _injector.CurrentSource = source;
            _injector.CurrentReason = source + " release";
            _injector.KeyUp(key);
            keyDown = false;
        }
        prev = down;
    }

    private void UpdateEmergency(ControllerState s, double now) {
        bool held = s.Options && s.Create;
        if (!held) {
            _disableStartMs = 0;
            _disableArmed = true;
            return;
        }
        if (_disableStartMs <= 0) {
            _disableStartMs = now;
            return;
        }
        if (_disableArmed && now - _disableStartMs >= 1000.0) {
            _enabled = !_enabled;
            _disableArmed = false;
            if (!_enabled) {
                ReleaseRuntimeHolds();
                _runtimeReleased = true;
            }
            Logger.Info(_enabled ? "enabled" : "disabled");
        }
    }

    private void ReleaseRuntimeHolds() {
        ReleaseHeldActionKeys();
        _injector.ReleaseAll();
        _leftMouseDown = false;
        _rightMouseDown = false;
        _leftDirection = StickDirection.None;
        _leftStickScroll.Reset();
        _heldLeftStickKeys.Clear();
        _accumulatedModifiers.Clear();
        _fnArmed = false;
        for (int i = 0; i < _holds.Length; i++) _holds[i] = new ButtonHold();
        for (int i = 0; i < _prevDown.Length; i++) _prevDown[i] = false;
        _prevL1 = false;
        _prevR1 = false;
        _l1DownMs = 0;
        _r1DownMs = 0;
        _l2DownMs = 0;
        _r2DownMs = 0;
        _l1UpMs = 0;
        _r1UpMs = 0;
        _l2UpMs = 0;
        _r2UpMs = 0;
        _previousActionLayer = Layer.Base;
        _lastReleasedActionLayer = Layer.Base;
        _lastReleasedActionLayerUpMs = 0;
        _l2Pressed = false;
        _r2Pressed = false;
        _clutchButton.Reset();
        _prevClutchActive = false;
        _prevCreate = false;
        _prevOptions = false;
        _mouseFreezeUntilMs = 0;
        _rightStickMouse.Reset();
        _prevHome = false;
        _createKeyDown = false;
        _optionsKeyDown = false;
        _homeKeyDown = false;
    }

    private void ReleaseHeldActionKeys() {
        for (int i = 0; i < _holds.Length; i++) {
            if (_holds[i].KeyIsDown) {
                ReleaseActionKey(i, _holds[i].Key, "Runtime release " + ActionButtonName(i));
            }
            _holds[i] = new ButtonHold();
        }
    }

    private double NowMs() { return _clock.Elapsed.TotalMilliseconds; }

    public static StickDirection Sector(double x, double y) {
        double angle = Math.Atan2(-y, x);
        int sector = (int)Math.Floor((angle + Math.PI / 8.0) / (Math.PI / 4.0));
        sector = ((sector % 8) + 8) % 8;
        switch (sector) {
            case 0: return StickDirection.Right;
            case 1: return StickDirection.UpRight;
            case 2: return StickDirection.Up;
            case 3: return StickDirection.UpLeft;
            case 4: return StickDirection.Left;
            case 5: return StickDirection.DownLeft;
            case 6: return StickDirection.Down;
            case 7: return StickDirection.DownRight;
            default: return StickDirection.None;
        }
    }


    private void DebugSources(string message) {
        if (!_debugSources) return;
        Logger.Info(message);
        Console.WriteLine(message);
    }


    private static double Clamp(double value, double min, double max) { return value < min ? min : (value > max ? max : value); }

    private struct ButtonHold {
        public KeyStroke Key;
        public Layer KeyLayer;
        public bool KeyIsDown;
        public bool SuppressUntilRelease;
        public bool Pending;
        public bool PendingReleased;
        public double PendingSinceMs;
        public Layer OriginalPendingLayer;
        public double OriginalPendingLayerUpMs;
        public Layer PendingLayer;
        public double PendingLayerMs;
        public double KeyDownMs;
        public bool RepeatEnabled;
        public double RepeatStartedMs;
        public double NextRepeatMs;
    }
}
