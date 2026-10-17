using System;
using System.IO;
using System.Text;

namespace JipperResourcePackX;

public static class ByteTool {
    public static int ReadInt(this Stream stream) {
        Span<byte> buf = stackalloc byte[4];
        stream.Read(buf);
        return BitConverter.ToInt32(buf);
    }

    public static float ReadFloat(this Stream stream) {
        Span<byte> buf = stackalloc byte[4];
        stream.Read(buf);
        return BitConverter.ToSingle(buf);
    }

    public static byte[] ReadBytes(this Stream stream, int count) {
        byte[] buf = new byte[count];
        stream.Read(buf, 0, count);
        return buf;
    }

    public static void WriteInt(this Stream stream, int value) {
        Span<byte> buf = stackalloc byte[4];
        BitConverter.TryWriteBytes(buf, value);
        stream.Write(buf);
    }

    public static void WriteFloat(this Stream stream, float value) {
        Span<byte> buf = stackalloc byte[4];
        BitConverter.TryWriteBytes(buf, value);
        stream.Write(buf);
    }

    public static void WriteBoolean(this Stream stream, bool value) {
        stream.WriteByte((byte)(value ? 1 : 0));
    }

    public static void WriteUTF(this Stream stream, string str) {
        if (str == null) {
            stream.WriteInt(-1);
            return;
        }
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        stream.WriteInt(bytes.Length);
        stream.Write(bytes);
    }

    public static void WriteObject(this Stream stream, object obj) {
        if (obj == null) return;
        if (obj is System.Collections.IEnumerable enumerable && obj is not string) {
            foreach (var item in enumerable) {
                if (item is float f) stream.WriteFloat(f);
                else if (item is int i) stream.WriteInt(i);
                else if (item is double d) stream.WriteFloat((float)d);
                else if (item is byte b) stream.WriteByte(b);
                else if (item != null) {
                    byte[] bytes = Encoding.UTF8.GetBytes(item.ToString());
                    stream.Write(bytes);
                }
            }
        } else {
            byte[] bytes = Encoding.UTF8.GetBytes(obj.ToString());
            stream.Write(bytes);
        }
    }
}
