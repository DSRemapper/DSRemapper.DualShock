using HidApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DSRemapper.DualShock
{
    internal static class HidApiHack
    {
        private static readonly DSRLogger logger = DSRLogger.GetLogger("HidApi");
        private static IntPtr GetNativePointer(Device dev)
        {
            SafeHandle? sh = (SafeHandle?)typeof(Device).GetField("handle", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(dev);
            if (sh != null)
            {
                //logger.LogInformation("No null SafeHandle");
                if (!sh.IsInvalid)
                {
                    //logger.LogInformation("Valid SafeHandle");
                    return Marshal.ReadIntPtr(sh.DangerousGetHandle());
                }
            }

            logger.LogWarning("No pointer found");
            return IntPtr.Zero;
        }

        internal static int GetInputBufferCount(this Device device)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr ptr = GetNativePointer(device);
                int bufferCount = 0;
                if (ptr != IntPtr.Zero)
                    return NativeMethods.HidD_GetNumInputBuffers(ptr, ref bufferCount) ? bufferCount : 0;

                logger.LogWarning("Null pointer");
            }
            return 0;
        }
        internal static bool SetInputBufferCount(this Device device, int bufferCount)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogInformation("Windows OS detected, setting input buffer count");
                IntPtr ptr = GetNativePointer(device);
                if (ptr != IntPtr.Zero)
                    return NativeMethods.HidD_SetNumInputBuffers(ptr, bufferCount);

                logger.LogWarning("Null pointer");
                return false;
            }
            return true;
        }
    }
    internal static class BTHandle
    {
        public static bool DisconnectBT(long mac)
        {
            IntPtr btHandle = IntPtr.Zero;
            uint IOCTL_BTH_DISCONNECT_DEVICE = 0x41000C;

            NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS p = new();
            p.dwSize = Marshal.SizeOf(typeof(NativeMethods.BLUETOOTH_FIND_RADIO_PARAMS));
            IntPtr searchHandle = NativeMethods.BluetoothFindFirstRadio(ref p, ref btHandle);
            int bytesReturned = 0;

            bool success = false;

            while (!success && btHandle != IntPtr.Zero)
            {
                Console.WriteLine("Disconnecting");
                success = NativeMethods.DeviceIoControl(btHandle, IOCTL_BTH_DISCONNECT_DEVICE, ref mac, 8, IntPtr.Zero, 0, ref bytesReturned, IntPtr.Zero);
                NativeMethods.CloseHandle(btHandle);
                if (!success)
                {
                    if (!NativeMethods.BluetoothFindNextRadio(searchHandle, ref btHandle))
                        btHandle = IntPtr.Zero;
                }
            }

            NativeMethods.BluetoothFindRadioClose(searchHandle);
            return success;
        }
    }
    internal static partial class NativeMethods
    {
        [LibraryImport("hid.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool HidD_GetNumInputBuffers(IntPtr hidDeviceObject, ref int numberBuffers);
        [LibraryImport("hid.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool HidD_SetNumInputBuffers(IntPtr hidDeviceObject, int numberBuffers);

        [StructLayout(LayoutKind.Sequential)]
        internal struct BLUETOOTH_FIND_RADIO_PARAMS
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwSize;
        }
        [LibraryImport("bthprops.cpl")]
        internal static partial IntPtr BluetoothFindFirstRadio(ref BLUETOOTH_FIND_RADIO_PARAMS pbtfrp, ref IntPtr phRadio);
        [LibraryImport("bthprops.cpl")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BluetoothFindNextRadio(IntPtr hFind, ref IntPtr phRadio);

        [LibraryImport("bthprops.cpl")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool BluetoothFindRadioClose(IntPtr hFind);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DeviceIoControl(IntPtr DeviceHandle, uint IoControlCode, ref long InBuffer, int InBufferSize, IntPtr OutBuffer, int OutBufferSize, ref int BytesReturned, IntPtr Overlapped);

        [LibraryImport("kernel32.dll", EntryPoint = "DeviceIoControl", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DeviceIoControl(IntPtr DeviceHandle, uint IoControlCode, IntPtr InBuffer, int InBufferSize, IntPtr OutBuffer, int OutBufferSize, ref int BytesReturned, IntPtr Overlapped);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CloseHandle(IntPtr hObject);
    }
}
