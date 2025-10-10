using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace tGameServer.NetworkDefine
{
    public class ConverterPack
    {
        public static byte[] StructureToByteArray(object pack)
        {
            int dataSize = Marshal.SizeOf(pack);
            IntPtr buff = Marshal.AllocHGlobal(dataSize);
            Marshal.StructureToPtr(pack, buff, false);
            byte[] data = new byte[dataSize];
            Marshal.Copy(buff, data, 0, dataSize);
            Marshal.FreeHGlobal(buff);

            return data;
        }

        public static object ByteArrayToStructure(byte[] data, Type type, int size)
        {
            IntPtr buff = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, buff, data.Length);
            object obj = Marshal.PtrToStructure(buff, type);
            Marshal.FreeHGlobal(buff);

            if (Marshal.SizeOf(obj) != size)
                return null;

            return obj;
        }

        public static Packet CreatePack(uint protocol, uint size, byte[] data)
        {
            Packet pack = new Packet();
            pack._protocol = protocol;
            pack._totalSize = size;
            if (data != null)
            {
                pack._data = new byte[1016];

                for (int i = 0; i < data.Length; i++)
                    pack._data[i] = data[i];
            }

            return pack;
        }
    }
}
