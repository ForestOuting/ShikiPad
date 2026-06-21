using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

internal static class NativeMethods {
        [DllImport("hid.dll", SetLastError = true)] public static extern void HidD_GetHidGuid(out Guid hidGuid);
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);
        [DllImport("setupapi.dll", SetLastError = true)] public static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref NativeMethods.SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref NativeMethods.SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, uint DeviceInterfaceDetailDataSize, out uint RequiredSize, IntPtr DeviceInfoData);
        [DllImport("setupapi.dll", SetLastError = true)] public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
        [DllImport("kernel32.dll", SetLastError = true)] public static extern bool CloseHandle(IntPtr hObject);
        [DllImport("hid.dll", SetLastError = true)] public static extern bool HidD_GetAttributes(IntPtr device, ref NativeMethods.HIDD_ATTRIBUTES attributes);
        [DllImport("hid.dll", SetLastError = true, CharSet = CharSet.Auto)] public static extern bool HidD_GetProductString(IntPtr hidDeviceObject, IntPtr buffer, uint bufferLength);
        [DllImport("hid.dll", SetLastError = true)] public static extern bool HidD_GetPreparsedData(IntPtr HidDeviceObject, out IntPtr PreparsedData);
        [DllImport("hid.dll", SetLastError = true)] public static extern bool HidD_FreePreparsedData(IntPtr PreparsedData);
        [DllImport("hid.dll", SetLastError = true)] public static extern int HidP_GetCaps(IntPtr PreparsedData, out HIDP_CAPS Capabilities);
        [DllImport("winmm.dll")] public static extern uint timeBeginPeriod(uint uMilliseconds);
        [DllImport("winmm.dll")] public static extern uint timeEndPeriod(uint uMilliseconds);

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_CAPS {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;
        }

        public const ushort XINPUT_GAMEPAD_DPAD_UP = 0x0001;
        public const ushort XINPUT_GAMEPAD_DPAD_DOWN = 0x0002;
        public const ushort XINPUT_GAMEPAD_DPAD_LEFT = 0x0004;
        public const ushort XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008;
        public const ushort XINPUT_GAMEPAD_START = 0x0010;
        public const ushort XINPUT_GAMEPAD_BACK = 0x0020;
        public const ushort XINPUT_GAMEPAD_LEFT_THUMB = 0x0040;
        public const ushort XINPUT_GAMEPAD_RIGHT_THUMB = 0x0080;
        public const ushort XINPUT_GAMEPAD_LEFT_SHOULDER = 0x0100;
        public const ushort XINPUT_GAMEPAD_RIGHT_SHOULDER = 0x0200;
        public const ushort XINPUT_GAMEPAD_A = 0x1000;
        public const ushort XINPUT_GAMEPAD_B = 0x2000;
        public const ushort XINPUT_GAMEPAD_X = 0x4000;
        public const ushort XINPUT_GAMEPAD_Y = 0x8000;

        public static int XInputGetStateAny(int userIndex, out XINPUT_STATE state) {
            try {
                return XInputGetState14(userIndex, out state);
            } catch (DllNotFoundException) {
                try { return XInputGetState910(userIndex, out state); }
                catch (DllNotFoundException) { state = new XINPUT_STATE(); return 1167; }
                catch (EntryPointNotFoundException) { state = new XINPUT_STATE(); return 1167; }
            } catch (EntryPointNotFoundException) {
                try { return XInputGetState910(userIndex, out state); }
                catch (DllNotFoundException) { state = new XINPUT_STATE(); return 1167; }
                catch (EntryPointNotFoundException) { state = new XINPUT_STATE(); return 1167; }
            }
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern int XInputGetState14(int dwUserIndex, out XINPUT_STATE pState);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        private static extern int XInputGetState910(int dwUserIndex, out XINPUT_STATE pState);

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA {
            public uint cbSize;
            public Guid interfaceClassGuid;
            public uint flags;
            public IntPtr reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDD_ATTRIBUTES {
            public uint Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XINPUT_STATE {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XINPUT_GAMEPAD {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }
}
