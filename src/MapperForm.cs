using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

internal sealed class MapperForm : Form {
    private const double PollIntervalMs = 1.0;
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

    public MapperForm(Config config, ControllerProfile controllerProfile) {
        _config = config;
        _controllerProfile = controllerProfile;
        _hid = new DirectHidController(controllerProfile);
        _debugSources = false;
        _enabled = config.Enabled;
        _injector = new InputInjector(config.UseScanCode);
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Opacity = 0;
    }

    protected override void OnLoad(EventArgs e) {
        base.OnLoad(e);
        _hid.StateUpdated += OnStateUpdated;
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
                        } else if (key.Key == ConsoleKey.Escape) {
                            if (_manualVisible) {
                                try { BeginInvoke((MethodInvoker)delegate { Close(); }); } catch { try { Close(); } catch { } }
                            } else {
                                RequestControllerSelectionRestart();
                            }
                            break;
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
        double nextTick = _clock.Elapsed.TotalMilliseconds;
        while (_pollRunning) {
            lock (_tickLock) {
                try { OnTick(); } catch { }
            }
            
            double now = _clock.Elapsed.TotalMilliseconds;
            if (nextTick < now) {
                nextTick = now;
            }
            nextTick += PollIntervalMs;
            
            while (_pollRunning) {
                now = _clock.Elapsed.TotalMilliseconds;
                double diff = nextTick - now;
                if (diff <= 0) break;
                
                if (diff > 2.0) {
                    Thread.Sleep(1);
                } else if (diff > 0.1) {
                    Thread.Sleep(0);
                } else {
                    Thread.SpinWait(10);
                }
            }
        }
    }



    protected override void OnFormClosing(FormClosingEventArgs e) {
        _pollRunning = false;
        if (_pollThread != null) {
            _pollThread.Join(500);
        }
        NativeMethods.timeEndPeriod(1);
        _hid.StateUpdated -= OnStateUpdated;
        _hid.Stop();
        ReleaseRuntimeHolds();

        base.OnFormClosing(e);
    }

    private bool _prevL1, _prevR1;
    private double _l1DownMs, _r1DownMs, _l2DownMs, _r2DownMs;
    private double _l1UpMs, _r1UpMs, _l2UpMs, _r2UpMs;
    private Layer _previousActionLayer = Layer.Base;
    private Layer _lastReleasedActionLayer = Layer.Base;
    private double _lastReleasedActionLayerUpMs;
    private double _lastReleasedActionLayerDownMs;
    private bool _l1ConsumedByCombo, _r1ConsumedByCombo, _l2ConsumedByCombo, _r2ConsumedByCombo;

    private void OnStateUpdated(ControllerState s) {
        if (!_pollRunning) return;
        lock (_tickLock) {
            try { OnTick(); } catch { }
        }
    }

    private void OnTick() {
        ControllerState s = _hid.State;
        double now = NowMs();
        double deltaSec = Clamp((now - _lastTickMs) / 1000.0, 0.0, MaxMouseFrameSeconds);
        _lastTickMs = now;

        bool preL1 = _prevL1;
        bool preR1 = _prevR1;
        bool l1JustDown = s.L1 && !preL1;
        bool r1JustDown = s.R1 && !preR1;
        if (l1JustDown) { _l1DownMs = now; _l1UpMs = 0; _l1ConsumedByCombo = false; }
        if (r1JustDown) { _r1DownMs = now; _r1UpMs = 0; _r1ConsumedByCombo = false; }
        if (!s.L1 && preL1) { _l1UpMs = now; _l1ConsumedByCombo = false; }
        if (!s.R1 && preR1) { _r1UpMs = now; _r1ConsumedByCombo = false; }
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
        UpdateSystemButtons(s, now);
    }

    private void UpdateTriggers(ControllerState s, double now) {
        if (!_l2Pressed && IsTriggerPressed(s.L2, _config.TriggerPressThreshold)) {
            _l2Pressed = true;
            _l2DownMs = now;
            _l2UpMs = 0;
            _l2ConsumedByCombo = false;
        } else if (_l2Pressed && IsTriggerReleased(s.L2, _config.TriggerReleaseThreshold)) {
            _l2Pressed = false;
            _l2UpMs = now;
            _l2ConsumedByCombo = false;
        }

        if (!_r2Pressed && IsTriggerPressed(s.R2, _config.TriggerPressThreshold)) {
            _r2Pressed = true;
            _r2DownMs = now;
            _r2UpMs = 0;
            _r2ConsumedByCombo = false;
        } else if (_r2Pressed && IsTriggerReleased(s.R2, _config.TriggerReleaseThreshold)) {
            _r2Pressed = false;
            _r2UpMs = now;
            _r2ConsumedByCombo = false;
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

        if (radius < _config.LeftStickExitDeadzone) {
            next = StickDirection.None;
        } else if (radius >= _config.LeftStickEnterDeadzone) {
            next = Sector(s.LX, s.LY);
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
        Layer rawLayer = _mapping.Resolve(s.L1, s.R1, _l2Pressed, _r2Pressed, _l1DownMs, _r1DownMs, _l2DownMs, _r2DownMs, _config.ComboLayerWindowMs);
        Layer layer = rawLayer;
        ConsumeComboComponents(layer);
        layer = FilterConsumedSingleLayer(layer, s.L1, s.R1, _l2Pressed, _r2Pressed);
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
                UpdatePendingLayer(ref hold, layer, layerMs, now);

                bool shouldFlushPending = now - hold.PendingSinceMs >= _config.ActionLayerGraceMs &&
                    !ShouldWaitForPendingSingleLayerToSettle(hold, now);
                if (!shouldFlushPending) {
                    _holds[i] = hold;
                    _prevDown[i] = curr;
                    continue;
                }

                bool releasedPending = hold.PendingReleased || !curr;
                Layer resolvedLayer = hold.PendingLayer != Layer.Base && hold.PendingLayer != Layer.Reserved
                    ? hold.PendingLayer
                    : ((releasedPending || hold.PendingLayerSettled) ? hold.PendingLayer : layer);
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
                    InitializePendingTrace(ref hold, layer, layerMs, now);
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

    private bool ShouldWaitForPendingSingleLayerToSettle(ButtonHold hold, double now) {
        if (hold.PendingLayer == Layer.Base || hold.PendingLayer == Layer.Reserved || IsComboLayer(hold.PendingLayer)) return false;
        if (hold.PendingLayerMs <= hold.PendingSinceMs) return false;
        return now - hold.PendingLayerMs <= _config.ComboLayerWindowMs;
    }

    private void ConsumeComboComponents(Layer layer) {
        switch (layer) {
            case Layer.R1L1:
                _r1ConsumedByCombo = true;
                _l1ConsumedByCombo = true;
                break;
            case Layer.R2L2:
                _r2ConsumedByCombo = true;
                _l2ConsumedByCombo = true;
                break;
            case Layer.L1R2:
                _l1ConsumedByCombo = true;
                _r2ConsumedByCombo = true;
                break;
            case Layer.R1L2:
                _r1ConsumedByCombo = true;
                _l2ConsumedByCombo = true;
                break;
        }
    }

    private Layer FilterConsumedSingleLayer(Layer layer, bool l1, bool r1, bool l2, bool r2) {
        if (IsComboLayer(layer) || layer == Layer.Base || layer == Layer.Reserved) return layer;
        if (!IsConsumedSingleLayer(layer)) return layer;
        return LatestUnconsumedSingleLayer(l1, r1, l2, r2);
    }

    private bool IsConsumedSingleLayer(Layer layer) {
        switch (layer) {
            case Layer.L1: return _l1ConsumedByCombo;
            case Layer.R1: return _r1ConsumedByCombo;
            case Layer.L2: return _l2ConsumedByCombo;
            case Layer.R2: return _r2ConsumedByCombo;
            default: return false;
        }
    }

    private Layer LatestUnconsumedSingleLayer(bool l1, bool r1, bool l2, bool r2) {
        Layer layer = Layer.Base;
        double bestMs = double.NegativeInfinity;
        ConsiderUnconsumedSingle(l1 && !_l1ConsumedByCombo, Layer.L1, _l1DownMs, ref layer, ref bestMs);
        ConsiderUnconsumedSingle(r1 && !_r1ConsumedByCombo, Layer.R1, _r1DownMs, ref layer, ref bestMs);
        ConsiderUnconsumedSingle(l2 && !_l2ConsumedByCombo, Layer.L2, _l2DownMs, ref layer, ref bestMs);
        ConsiderUnconsumedSingle(r2 && !_r2ConsumedByCombo, Layer.R2, _r2DownMs, ref layer, ref bestMs);
        return layer;
    }

    private static void ConsiderUnconsumedSingle(bool active, Layer candidate, double timestampMs, ref Layer layer, ref double bestMs) {
        if (!active) return;
        if (timestampMs >= bestMs) {
            layer = candidate;
            bestMs = timestampMs;
        }
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
            if (upMs <= 0.0) {
                _previousActionLayer = layer;
                return;
            }
            
            bool shouldOverwrite = true;
            if (IsComboLayer(_lastReleasedActionLayer)) {
                if (IsSubsetOf(_previousActionLayer, _lastReleasedActionLayer)) {
                    if (LayerTimestamp(_previousActionLayer) <= _lastReleasedActionLayerUpMs) {
                        shouldOverwrite = false;
                    }
                }
            }

            if (shouldOverwrite && now - upMs < 100.0) {
                _lastReleasedActionLayer = _previousActionLayer;
                _lastReleasedActionLayerUpMs = upMs;
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

    private double ComboUpTimestamp(double t1, double t2) {
        if (t1 == 0 && t2 == 0) return 0.0;
        if (t1 == 0) return t2;
        if (t2 == 0) return t1;
        return Math.Min(t1, t2);
    }

    private double LayerUpTimestamp(Layer layer) {
        switch (layer) {
            case Layer.L1: return _l1UpMs;
            case Layer.R1: return _r1UpMs;
            case Layer.L2: return _l2UpMs;
            case Layer.R2: return _r2UpMs;
            case Layer.R1L1: return ComboUpTimestamp(_r1UpMs, _l1UpMs);
            case Layer.R2L2: return ComboUpTimestamp(_r2UpMs, _l2UpMs);
            case Layer.L1R2: return ComboUpTimestamp(_l1UpMs, _r2UpMs);
            case Layer.R1L2: return ComboUpTimestamp(_r1UpMs, _l2UpMs);
            default: return 0.0;
        }
    }

    private void UpdatePendingLayer(ref ButtonHold hold, Layer layer, double layerMs, double now) {
        List<PendingLayerOccupancySegment> occupiedSegments = PendingTraceSegmentsThrough(hold, layer, layerMs, now);
        PendingLayerOccupancyTrace trace = TracePendingLayerOccupancy(occupiedSegments, layer, _config.ActionLayerGraceMs);
        Layer next = ResolvePendingLayer(
            hold.PendingLayer,
            hold.OriginalPendingLayer,
            hold.PendingSinceMs,
            layer,
            layerMs,
            trace.ReachesPendingStart,
            _config.ActionLayerGraceMs);

        CommitPendingTraceTransition(ref hold, layer, layerMs, now);

        if (next == hold.PendingLayer) return;
        hold.PendingLayerSettled = next == Layer.Base && hold.PendingLayer != Layer.Base;
        hold.PendingLayer = next;
        hold.PendingLayerMs = next == layer ? layerMs : LayerTimestamp(next);
    }

    private void InitializePendingTrace(ref ButtonHold hold, Layer layer, double layerMs, double now) {
        ClearPendingLayerOccupancy(ref hold);
        hold.PendingTraceLayer = layer;
        hold.PendingTraceLayerMs = layerMs;
        hold.PendingTraceStartMs = now;
    }

    private List<PendingLayerOccupancySegment> PendingTraceSegmentsThrough(ButtonHold hold, Layer layer, double layerMs, double now) {
        List<PendingLayerOccupancySegment> segments = new List<PendingLayerOccupancySegment>();
        if (hold.PendingLayerOccupancySegments != null) {
            segments.AddRange(hold.PendingLayerOccupancySegments);
        }

        if (layer != hold.PendingTraceLayer) {
            AddPendingTraceSegment(segments, hold.PendingTraceLayer, hold.PendingTraceLayerMs, hold.PendingTraceStartMs, PendingTraceTransitionMs(hold, layer, layerMs, now));
        }

        return segments;
    }

    private void CommitPendingTraceTransition(ref ButtonHold hold, Layer layer, double layerMs, double now) {
        if (layer == hold.PendingTraceLayer) return;

        List<PendingLayerOccupancySegment> segments = PendingTraceSegmentsThrough(hold, layer, layerMs, now);
        SetPendingLayerOccupancySegments(ref hold, segments, SumPendingLayerOccupancy(segments));
        double transitionMs = PendingTraceTransitionMs(hold, layer, layerMs, now);
        hold.PendingTraceLayer = layer;
        hold.PendingTraceLayerMs = layerMs;
        hold.PendingTraceStartMs = transitionMs;
    }

    private double PendingTraceTransitionMs(ButtonHold hold, Layer layer, double layerMs, double now) {
        if (layer != Layer.Base && layer != Layer.Reserved && layerMs > 0.0) return layerMs;

        double traceLayerUpMs = LayerUpTimestamp(hold.PendingTraceLayer);
        if (traceLayerUpMs > 0.0) return traceLayerUpMs;
        return now;
    }

    private static void AddPendingTraceSegment(List<PendingLayerOccupancySegment> segments, Layer layer, double layerDownMs, double startMs, double endMs) {
        if (endMs <= startMs) return;
        bool isLayerBody = layer != Layer.Base && layer != Layer.Reserved;
        segments.Add(new PendingLayerOccupancySegment(endMs - startMs, isLayerBody, layer, layerDownMs));
    }

    private static void ClearPendingLayerOccupancy(ref ButtonHold hold) {
        hold.PendingLayerOccupancyMs = 0.0;
        if (hold.PendingLayerOccupancySegments != null) {
            hold.PendingLayerOccupancySegments.Clear();
        }
    }

    private static void AddPendingLayerOccupancySegment(ref ButtonHold hold, double durationMs, bool isLayerBody, Layer layer, double layerDownMs) {
        if (durationMs <= 0.0) return;
        if (hold.PendingLayerOccupancySegments == null) {
            hold.PendingLayerOccupancySegments = new List<PendingLayerOccupancySegment>();
        }
        hold.PendingLayerOccupancySegments.Add(new PendingLayerOccupancySegment(durationMs, isLayerBody, layer, layerDownMs));
        hold.PendingLayerOccupancyMs += durationMs;
    }

    private void NormalizePendingLayerOccupancy(ref ButtonHold hold) {
        if (hold.PendingLayerOccupancySegments == null) return;
        List<PendingLayerOccupancySegment> segments = new List<PendingLayerOccupancySegment>(hold.PendingLayerOccupancySegments);
        SetPendingLayerOccupancySegments(ref hold, segments, SumPendingLayerOccupancy(segments));
    }

    private void SetPendingLayerOccupancySegments(ref ButtonHold hold, List<PendingLayerOccupancySegment> segments, double occupiedMs) {
        if (segments == null || segments.Count == 0 || occupiedMs <= 0.0) {
            ClearPendingLayerOccupancy(ref hold);
            return;
        }
        hold.PendingLayerOccupancySegments = segments;
        hold.PendingLayerOccupancyMs = occupiedMs;
    }

    private static double SumPendingLayerOccupancy(List<PendingLayerOccupancySegment> segments) {
        double total = 0.0;
        for (int i = 0; i < segments.Count; i++) {
            total += segments[i].DurationMs;
        }
        return total;
    }

    private PendingLayerOccupancyTrace TracePendingLayerOccupancy(List<PendingLayerOccupancySegment> segments, Layer targetLayer, double actionLayerGraceMs) {
        double cutoffMs = Math.Max(0.0, (double)_config.LayerOccupancyCarryCutoffMs);
        double bodyTakeoverMs = Math.Max(0.0, (double)_config.LayerTakeoverWindowMs);
        double totalWindowMs = Math.Max(0.0, actionLayerGraceMs);
        if (totalWindowMs <= 0.0) return new PendingLayerOccupancyTrace(0.0, segments.Count == 0);

        double actualMs = 0.0;
        double cumulativeBodyMs = 0.0;
        for (int i = segments.Count - 1; i >= 0; i--) {
            PendingLayerOccupancySegment segment = segments[i];
            double remainingMs = totalWindowMs - actualMs;
            if (remainingMs <= 0.0) return new PendingLayerOccupancyTrace(actualMs, false);

            bool countsAsLayerBody = segment.IsLayerBody && !IsComboComponentBodyForTarget(targetLayer, segment);
            if (countsAsLayerBody && cutoffMs > 0.0 && cumulativeBodyMs + segment.DurationMs >= cutoffMs) {
                double cumulativeBodyBudgetMs = bodyTakeoverMs > cumulativeBodyMs ? bodyTakeoverMs - cumulativeBodyMs : 0.0;
                double readableBoundaryBodyMs = Math.Min(segment.DurationMs, cumulativeBodyBudgetMs);
                double takenMs = Math.Min(readableBoundaryBodyMs, remainingMs);
                actualMs += takenMs;
                bool reachesPendingStart = i == 0 && takenMs >= segment.DurationMs;
                return new PendingLayerOccupancyTrace(actualMs, reachesPendingStart);
            }

            if (segment.DurationMs > remainingMs) {
                actualMs += remainingMs;
                return new PendingLayerOccupancyTrace(actualMs, false);
            }

            actualMs += segment.DurationMs;
            if (countsAsLayerBody) cumulativeBodyMs += segment.DurationMs;
        }

        return new PendingLayerOccupancyTrace(actualMs, true);
    }

    private bool IsComboComponentBodyForTarget(Layer targetLayer, PendingLayerOccupancySegment segment) {
        if (!IsComboLayer(targetLayer) || IsComboLayer(segment.Layer) || !IsComboComponent(targetLayer, segment.Layer)) return false;
        double currentComponentDownMs = LayerTimestamp(segment.Layer);
        return currentComponentDownMs > 0.0 && currentComponentDownMs == segment.LayerDownMs;
    }

    private static bool IsComboComponent(Layer combo, Layer single) {
        if (combo == Layer.R1L1 && (single == Layer.R1 || single == Layer.L1)) return true;
        if (combo == Layer.R2L2 && (single == Layer.R2 || single == Layer.L2)) return true;
        if (combo == Layer.L1R2 && (single == Layer.L1 || single == Layer.R2)) return true;
        if (combo == Layer.R1L2 && (single == Layer.R1 || single == Layer.L2)) return true;
        return false;
    }

    internal static Layer ResolvePendingLayer(Layer pendingLayer, Layer originalLayer, double pendingSinceMs, Layer layer, double layerMs, bool reachesPendingStart, double actionLayerGraceMs) {
        if (layer == Layer.Base || layer == Layer.Reserved) return pendingLayer;
        if (layer == pendingLayer) return pendingLayer;
        if (layerMs < pendingSinceMs) return pendingLayer;

        bool layerCombo = IsComboLayer(layer);

        // 如果当前层是组合层，并且之前的 pendingLayer 是这个组合层的一个组件（即它的单层意图被组合层覆盖了）
        // 那么必须剥夺 pendingLayer 的资格，将其回退到 originalLayer 进行判定。
        Layer effectivePendingLayer = pendingLayer;
        if (layerCombo && IsComboComponent(layer, pendingLayer)) {
            if (layerMs - pendingSinceMs > actionLayerGraceMs) {
                return originalLayer;
            }
            if (!IsComboLayer(originalLayer)) {
                return layer;
            }
            effectivePendingLayer = originalLayer;
        }

        if (layerMs - pendingSinceMs > actionLayerGraceMs) return pendingLayer;

        if (!reachesPendingStart) {
            return effectivePendingLayer;
        }

        return layer;
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

    private bool _prevMute;

    private void UpdateSystemButtons(ControllerState s, double now) {
        if (!IsSonyController()) return;

        UpdateMappedSystemButton("Share/Create", s.Create, PhysicalKey.RAlt, ref _prevCreate, ref _createKeyDown);
        UpdateMappedSystemButton("Options/Menu", s.Options, PhysicalKey.RCtrl, ref _prevOptions, ref _optionsKeyDown);
        UpdateMappedSystemButton("Home", s.Home, PhysicalKey.RShift, ref _prevHome, ref _homeKeyDown);
        
        if (s.Mute && !_prevMute) {
            _injector.CurrentSource = "Mute";
            _injector.CurrentReason = "Mute press CapsLock toggle";
            _injector.KeyDown(PhysicalKey.CapsLock);
            _injector.KeyUp(PhysicalKey.CapsLock);
        }
        _prevMute = s.Mute;
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
        _lastReleasedActionLayerDownMs = 0;
        _l1ConsumedByCombo = false;
        _r1ConsumedByCombo = false;
        _l2ConsumedByCombo = false;
        _r2ConsumedByCombo = false;
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
        public Layer PendingLayer;
        public double PendingLayerMs;
        public bool PendingLayerSettled;
        public double PendingLayerOccupancyMs;
        public List<PendingLayerOccupancySegment> PendingLayerOccupancySegments;
        public Layer PendingTraceLayer;
        public double PendingTraceLayerMs;
        public double PendingTraceStartMs;
        public double KeyDownMs;
        public bool RepeatEnabled;
        public double RepeatStartedMs;
        public double NextRepeatMs;
    }

    private struct PendingLayerOccupancySegment {
        public readonly double DurationMs;
        public readonly bool IsLayerBody;
        public readonly Layer Layer;
        public readonly double LayerDownMs;

        public PendingLayerOccupancySegment(double durationMs, bool isLayerBody, Layer layer, double layerDownMs) {
            DurationMs = durationMs;
            IsLayerBody = isLayerBody;
            Layer = layer;
            LayerDownMs = layerDownMs;
        }
    }

    private struct PendingLayerOccupancyTrace {
        public readonly double DurationMs;
        public readonly bool ReachesPendingStart;

        public PendingLayerOccupancyTrace(double durationMs, bool reachesPendingStart) {
            DurationMs = durationMs;
            ReachesPendingStart = reachesPendingStart;
        }
    }
}
