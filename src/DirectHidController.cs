using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

internal sealed class DirectHidController {
    public volatile ControllerState State = new ControllerState();
    private readonly ControllerProfile _profile;
    private Thread _thread;
    private volatile bool _running;
    private IntPtr _handle = IntPtr.Zero;
    private string _deviceName = "Sony Controller";
    private int _xinputUserIndex = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

    public DirectHidController(ControllerProfile profile) {
        _profile = profile;
        _deviceName = DisplayName;
    }

    public string DisplayName {
        get {
            switch (_profile) {
                case ControllerProfile.Xbox360: return "Xbox 360 Controller / XInput";
                case ControllerProfile.Xbox360BT: return "Xbox 360 Controller / XInput (BT)";
                case ControllerProfile.XboxSeries: return "Xbox Series X|S Controller / XInput";
                case ControllerProfile.XboxSeriesBT: return "Xbox Series X|S Controller / XInput (BT)";
                case ControllerProfile.DualSenseBT: return "DualSense / Direct HID (BT)";
                case ControllerProfile.DualShock4: return "DualShock 4 / Direct HID";
                case ControllerProfile.DualShock4BT: return "DualShock 4 / Direct HID (BT)";
                default: return "DualSense / Direct HID";
            }
        }
    }

    public void Start() {
        _running = true;
        _thread = new Thread(Loop);
        _thread.IsBackground = true;
        _thread.Priority = ThreadPriority.AboveNormal;
        _thread.Start();
    }

    public void Stop() {
        _running = false;
        if (_handle != IntPtr.Zero && _handle != new IntPtr(-1)) {
            NativeMethods.CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
        if (_thread != null) {
            _thread.Join(500);
        }
    }

    private void Loop() {
        bool isSonyHid = _profile == ControllerProfile.DualSense || _profile == ControllerProfile.DualSenseBT ||
                         _profile == ControllerProfile.DualShock4 || _profile == ControllerProfile.DualShock4BT;
        if (!isSonyHid) {
            XInputLoop();
            return;
        }

        byte[] buffer = new byte[1024];
        while (_running) {
            if (_handle == IntPtr.Zero || _handle == new IntPtr(-1)) {
                State = new ControllerState();
                _handle = FindAndOpenDevice();
                if (_handle != IntPtr.Zero && _handle != new IntPtr(-1)) {
                    ControllerState cs = new ControllerState();
                    cs.Connected = true;
                    State = cs;
                    Logger.Info("Direct HID device connected: " + _deviceName);
                } else {
                    Thread.Sleep(1000);
                    continue;
                }
            }

            uint bytesRead = 0;
            if (ReadFile(_handle, buffer, (uint)buffer.Length, out bytesRead, IntPtr.Zero)) {
                if (bytesRead > 0) {
                    byte[] report = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, report, 0, (int)bytesRead);
                    try {
                        ParseReport(report);
                    } catch (Exception ex) {
                        Logger.Error("Parse error: " + ex.Message);
                    }
                }
            } else {
                Logger.Warn("ReadFile failed, disconnecting...");
                NativeMethods.CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }

    private void XInputLoop() {
        bool wasConnected = false;
        while (_running) {
            NativeMethods.XINPUT_STATE state;
            int result = XInputGetState(ref _xinputUserIndex, out state);
            if (result == 0) {
                if (!wasConnected) {
                    wasConnected = true;
                    Logger.Info("XInput controller connected: " + DisplayName + " slot " + _xinputUserIndex.ToString(CultureInfo.InvariantCulture));
                }
                ParseXInput(state.Gamepad);
                Thread.Sleep(1);
            } else {
                if (wasConnected) {
                    Logger.Warn("XInput controller disconnected");
                    wasConnected = false;
                }
                State = new ControllerState();
                _xinputUserIndex = -1;
                Thread.Sleep(1000);
            }
        }
    }

    private static int XInputGetState(ref int userIndex, out NativeMethods.XINPUT_STATE state) {
        state = new NativeMethods.XINPUT_STATE();
        if (userIndex >= 0) {
            int result = NativeMethods.XInputGetStateAny(userIndex, out state);
            if (result == 0) return 0;
            userIndex = -1;
        }

        for (int i = 0; i < 4; i++) {
            int result = NativeMethods.XInputGetStateAny(i, out state);
            if (result == 0) {
                userIndex = i;
                return 0;
            }
        }
        return 1167;
    }

    private void ParseXInput(NativeMethods.XINPUT_GAMEPAD gamepad) {
        State = ParseXInputState(gamepad);
    }

    internal static ControllerState ParseXInputState(NativeMethods.XINPUT_GAMEPAD gamepad) {
        ControllerState s = new ControllerState();
        s.Connected = true;
        ushort b = gamepad.wButtons;
        s.LX = Axis(gamepad.sThumbLX);
        s.LY = -Axis(gamepad.sThumbLY);
        s.RX = Axis(gamepad.sThumbRX);
        s.RY = -Axis(gamepad.sThumbRY);
        s.L2 = Trigger(gamepad.bLeftTrigger);
        s.R2 = Trigger(gamepad.bRightTrigger);

        s.Up = (b & NativeMethods.XINPUT_GAMEPAD_DPAD_UP) != 0;
        s.Down = (b & NativeMethods.XINPUT_GAMEPAD_DPAD_DOWN) != 0;
        s.Left = (b & NativeMethods.XINPUT_GAMEPAD_DPAD_LEFT) != 0;
        s.Right = (b & NativeMethods.XINPUT_GAMEPAD_DPAD_RIGHT) != 0;
        s.Square = (b & NativeMethods.XINPUT_GAMEPAD_X) != 0;
        s.Cross = (b & NativeMethods.XINPUT_GAMEPAD_A) != 0;
        s.Circle = (b & NativeMethods.XINPUT_GAMEPAD_B) != 0;
        s.Triangle = (b & NativeMethods.XINPUT_GAMEPAD_Y) != 0;
        s.L1 = (b & NativeMethods.XINPUT_GAMEPAD_LEFT_SHOULDER) != 0;
        s.R1 = (b & NativeMethods.XINPUT_GAMEPAD_RIGHT_SHOULDER) != 0;
        s.L3 = (b & NativeMethods.XINPUT_GAMEPAD_LEFT_THUMB) != 0;
        s.R3 = (b & NativeMethods.XINPUT_GAMEPAD_RIGHT_THUMB) != 0;
        s.Create = (b & NativeMethods.XINPUT_GAMEPAD_BACK) != 0;
        s.Options = (b & NativeMethods.XINPUT_GAMEPAD_START) != 0;

        // Xbox has no physical touchpad; clutch toggle is handled in MapperForm
        // Xbox Home/Guide button is intercepted by Windows for Xbox Game Bar and cannot be read via XInput
        return s;
    }


    private IntPtr FindAndOpenDevice() {
        Guid hidGuid;
        NativeMethods.HidD_GetHidGuid(out hidGuid);

        IntPtr deviceInfoSet = NativeMethods.SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, 0x12);
        if (deviceInfoSet == new IntPtr(-1)) return IntPtr.Zero;

        NativeMethods.SP_DEVICE_INTERFACE_DATA interfaceData = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
        interfaceData.cbSize = (uint)Marshal.SizeOf(interfaceData);

        IntPtr foundHandle = IntPtr.Zero;
        uint index = 0;

        while (NativeMethods.SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref hidGuid, index, ref interfaceData)) {
            index++;
            uint requiredSize = 0;
            NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, IntPtr.Zero, 0, out requiredSize, IntPtr.Zero);

            if (requiredSize == 0) continue;

            IntPtr detailData = Marshal.AllocHGlobal((int)requiredSize);
            Marshal.WriteInt32(detailData, (IntPtr.Size == 8) ? 8 : (Marshal.SystemDefaultCharSize == 1 ? 5 : 6));

            if (NativeMethods.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref interfaceData, detailData, requiredSize, out requiredSize, IntPtr.Zero)) {
                string devicePath = Marshal.PtrToStringAuto(new IntPtr(detailData.ToInt64() + 4));

                IntPtr handle = NativeMethods.CreateFile(devicePath, 0x80000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero); // GENERIC_READ, FILE_SHARE_READ|WRITE, OPEN_EXISTING
                if (handle != new IntPtr(-1)) {
                    NativeMethods.HIDD_ATTRIBUTES attrs = new NativeMethods.HIDD_ATTRIBUTES();
                    attrs.Size = (uint)Marshal.SizeOf(attrs);
                    if (NativeMethods.HidD_GetAttributes(handle, ref attrs)) {
                        if (attrs.VendorID == 0x054C) { // Sony
                            bool isGamepad = false;
                            IntPtr preparsedData;
                            if (NativeMethods.HidD_GetPreparsedData(handle, out preparsedData)) {
                                NativeMethods.HIDP_CAPS caps;
                                if (NativeMethods.HidP_GetCaps(preparsedData, out caps) == 0x110000) { // HIDP_STATUS_SUCCESS
                                    Logger.Info("Found Sony HID device: UsagePage=" + caps.UsagePage + ", Usage=" + caps.Usage);
                                    if (caps.UsagePage == 1 && (caps.Usage == 4 || caps.Usage == 5)) {
                                        isGamepad = true;
                                    }
                                }
                                NativeMethods.HidD_FreePreparsedData(preparsedData);
                            }

                            if (!isGamepad) {
                                NativeMethods.CloseHandle(handle);
                                continue;
                            }

                            IntPtr prodStr = Marshal.AllocHGlobal(254);
                            string productName = "";
                            if (NativeMethods.HidD_GetProductString(handle, prodStr, 254)) {
                                productName = Marshal.PtrToStringAuto(prodStr);
                            }
                            Marshal.FreeHGlobal(prodStr);
                            _deviceName = String.IsNullOrEmpty(productName)
                                ? "Sony Controller"
                                : productName + " (PID 0x" + attrs.ProductID.ToString("X4", CultureInfo.InvariantCulture) + ")";
                            foundHandle = handle;
                            Marshal.FreeHGlobal(detailData);
                            break;
                        }
                    }
                    NativeMethods.CloseHandle(handle);
                }
            }
            Marshal.FreeHGlobal(detailData);
        }

        NativeMethods.SetupDiDestroyDeviceInfoList(deviceInfoSet);
        return foundHandle;
    }


    private void ParseReport(byte[] r) {
        ControllerState s;
        if (!TryParseDualSenseReport(r, _profile, out s)) return;
        State = s;  // Volatile publish: reference swap ensures state is visible atomically
    }

    internal static bool TryParseDualSenseReport(byte[] r, ControllerProfile profile, out ControllerState s) {
        s = null;
        if (r == null || r.Length < 10) return false;

        bool isUsbProfile = (profile == ControllerProfile.DualSense || profile == ControllerProfile.DualShock4);
        bool isBtProfile = (profile == ControllerProfile.DualSenseBT || profile == ControllerProfile.DualShock4BT);
        bool isAdvancedBt = (r[0] == 0x31 || r[0] == 0x11);

        if (isUsbProfile) {
            if (r[0] != 0x01) {
                Logger.Warn("Sony (USB) mode rejected report: ID=" + r[0] + ", Length=" + r.Length);
                return false;
            }
        } else if (isBtProfile) {
            if (r[0] != 0x01 && !isAdvancedBt) {
                Logger.Warn("Sony (BT) mode rejected report: ID=" + r[0] + ", Length=" + r.Length);
                return false;
            }
        } else {
            return false;
        }

        s = new ControllerState();
        s.Connected = true;

        if (isBtProfile && !isAdvancedBt) {
            s.LX = Axis(r[1]);
            s.LY = Axis(r[2]);
            s.RX = Axis(r[3]);
            s.RY = Axis(r[4]);

            FillDpadAndFace(s, r[5]);

            byte b2 = r[6];
            s.L1 = (b2 & 0x01) != 0;
            s.R1 = (b2 & 0x02) != 0;
            s.L2 = Trigger(r[8], (b2 & 0x04) != 0);
            s.R2 = Trigger(r[9], (b2 & 0x08) != 0);
            s.Create = (b2 & 0x10) != 0;
            s.Options = (b2 & 0x20) != 0;
            s.L3 = (b2 & 0x40) != 0;
            s.R3 = (b2 & 0x80) != 0;

            byte b3 = r[7];
            s.Home = (b3 & 0x01) != 0;
            s.TouchClick = (b3 & 0x02) != 0;
        } else {
            int offset = isUsbProfile ? 1 : (r[0] == 0x11 ? 3 : 2);
            bool isDs4 = (profile == ControllerProfile.DualShock4 || profile == ControllerProfile.DualShock4BT);

            if (r.Length < offset + 9) return false;

            s.LX = Axis(r[offset + 0]);
            s.LY = Axis(r[offset + 1]);
            s.RX = Axis(r[offset + 2]);
            s.RY = Axis(r[offset + 3]);

            if (isDs4) {
                // DS4 layout: sticks, dpad+face, shoulders, ps+touch, L2, R2
                FillDpadAndFace(s, r[offset + 4]);

                byte b2 = r[offset + 5];
                s.L1 = (b2 & 0x01) != 0;
                s.R1 = (b2 & 0x02) != 0;
                s.Create = (b2 & 0x10) != 0;
                s.Options = (b2 & 0x20) != 0;
                s.L3 = (b2 & 0x40) != 0;
                s.R3 = (b2 & 0x80) != 0;

                if (r.Length > offset + 6) {
                    s.Home = (r[offset + 6] & 0x01) != 0;
                    s.TouchClick = (r[offset + 6] & 0x02) != 0;
                }

                s.L2 = Trigger(r[offset + 7], (b2 & 0x04) != 0);
                s.R2 = Trigger(r[offset + 8], (b2 & 0x08) != 0);
            } else {
                // DS5 layout: sticks, L2, R2, counter, dpad+face, shoulders, ps+touch
                FillDpadAndFace(s, r[offset + 7]);

                byte b2 = r[offset + 8];
                s.L1 = (b2 & 0x01) != 0;
                s.R1 = (b2 & 0x02) != 0;
                s.L2 = Trigger(r[offset + 4], (b2 & 0x04) != 0);
                s.R2 = Trigger(r[offset + 5], (b2 & 0x08) != 0);
                s.Create = (b2 & 0x10) != 0;
                s.Options = (b2 & 0x20) != 0;
                s.L3 = (b2 & 0x40) != 0;
                s.R3 = (b2 & 0x80) != 0;

                if (r.Length > offset + 9) {
                    s.Home = (r[offset + 9] & 0x01) != 0;
                    s.TouchClick = (r[offset + 9] & 0x02) != 0;
                }
            }
        }

        return true;
    }

    private static void FillDpadAndFace(ControllerState s, byte b) {
        int d = b & 0x0F;
        s.Up = d == 0 || d == 1 || d == 7;
        s.Right = d == 1 || d == 2 || d == 3;
        s.Down = d == 3 || d == 4 || d == 5;
        s.Left = d == 5 || d == 6 || d == 7;
        s.Square = (b & 0x10) != 0;
        s.Cross = (b & 0x20) != 0;
        s.Circle = (b & 0x40) != 0;
        s.Triangle = (b & 0x80) != 0;
    }

    private static double Axis(byte value) { return Clamp(((double)value - 127.5) / 127.5, -1.0, 1.0); }
    private static double Axis(short value) {
        return value < 0
            ? Clamp((double)value / 32768.0, -1.0, 0.0)
            : Clamp((double)value / 32767.0, 0.0, 1.0);
    }
    private static double Trigger(byte value) { return Clamp((double)value / 255.0, 0.0, 1.0); }
    private static double Trigger(byte value, bool digitalPressed) {
        return digitalPressed ? 1.0 : Trigger(value);
    }
    private static double Clamp(double value, double min, double max) { return value < min ? min : (value > max ? max : value); }

}
