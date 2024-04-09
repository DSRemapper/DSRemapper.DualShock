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
        public static sbyte ToSByteAxis(this byte b) => (sbyte)(b - 128);
        public static byte ToByteAxis(this sbyte b) => (byte)(b + 128);
    }
}
