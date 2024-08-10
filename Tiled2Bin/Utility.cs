using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tiled2Bin
{
    internal class Utility
    {
        public static T ToObject<T>(byte[] data, int offset) where T : struct
        {
            T ret = default;
            int objSize = Marshal.SizeOf(typeof(T));
            nint ptr = Marshal.AllocHGlobal(objSize);
            Marshal.Copy(data, offset, ptr, objSize);
            object? obj = Marshal.PtrToStructure(ptr, typeof(T));
            if (obj != null)
                ret = (T)obj;
            Marshal.FreeHGlobal(ptr);
            return ret;
        }
    }
}
