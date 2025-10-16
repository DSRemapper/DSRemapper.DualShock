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
    internal static class NativeMethods
    {
        [DllImport("hid.dll")]
        static internal extern bool HidD_GetNumInputBuffers(IntPtr hidDeviceObject, ref int numberBuffers);
        [DllImport("hid.dll")]
        static internal extern bool HidD_SetNumInputBuffers(IntPtr hidDeviceObject, int numberBuffers);
    }
}
