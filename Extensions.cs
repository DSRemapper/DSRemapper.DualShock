using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DSRemapper.DualShock
{
    internal static class Extensions
    {
        public static sbyte AxisConvertion(this byte b) => (sbyte)(b ^ 0x80);
        public static byte AxisConvertion(this sbyte b) => (byte)AxisConvertion((byte)b);
    }
}
