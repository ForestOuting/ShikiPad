using System;
using System.Runtime.InteropServices;

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
        [DllImport("user32.dll")] public static extern short GetKeyState(int nVirtKey);

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

}
