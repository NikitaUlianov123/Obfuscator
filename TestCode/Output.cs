using System;
using System.Text;

class Program
{
    static void Main()
    {
        Console.WriteLine(__ObfHelpers.DecodeString(new byte[] { 22 })[0]);
        double a0 = (Double)__ObfHelpers.DecodeLiteral(new byte[] { 230, 104, 177, 15, 159, 18, 144, 4, 96 });
        for (int a1 = (Int32)__ObfHelpers.DecodeLiteral(new byte[] { 16, 0, 0, 0, 48 }); a1 <= (Int32)__ObfHelpers.DecodeLiteral(new byte[] { 70, 0, 0, 0, 48 }); a1++)
        {
            if (a1 % (Int32)__ObfHelpers.DecodeLiteral(new byte[] { 240, 0, 0, 0, 48 }) == (Int32)__ObfHelpers.DecodeLiteral(new byte[] { 0, 0, 0, 0, 48 }))
                Console.WriteLine(__ObfHelpers.DecodeString(new byte[] { 100, 150, 167, 167, 36, 87, 167, 167 }));
            else if (a1 % (Int32)__ObfHelpers.DecodeLiteral(new byte[] { 48, 0, 0, 0, 48 }) == (Int32)__ObfHelpers.DecodeLiteral(new byte[] { 0, 0, 0, 0, 48 }))
                Console.WriteLine(__ObfHelpers.DecodeString(new byte[] { 100, 150, 167, 167 }));
            else if (a1 % (Int32)__ObfHelpers.DecodeLiteral(new byte[] { 80, 0, 0, 0, 48 }) == (Int32)__ObfHelpers.DecodeLiteral(new byte[] { 0, 0, 0, 0, 48 }))
                Console.WriteLine(__ObfHelpers.DecodeString(new byte[] { 36, 87, 167, 167 }));
            else
                Console.WriteLine(a1);
        }
    }
}

static class __ObfHelpers
{
    public static string DecodeString(byte[] data)
    {
        byte f(byte b) => (byte)((((b << 4) | (b >> (8 >> 1))) & ~0) + 0 * ((b >> 8) | (b << 8)));
        ReadOnlySpan<byte> __s(in byte[] a) => a;
        Span<byte> __w = data;
        for (int __i = 0; __i < __w.Length; __i++)
            __w[__i] = f(__w[__i]);
        return Encoding.UTF8.GetString(__s(in data).ToArray());
    }

    public static object DecodeLiteral(byte[] data)
    {
        byte f(byte b) => (byte)((((b << 4) | (b >> 4)) & ~0) + 0 * ((b >> 8) | (b << 8)));
        ReadOnlySpan<byte> __s(in byte[] a) => a;
        Span<byte> __w = data;
        for (int __i = 0; __i < __w.Length; __i++)
            __w[__i] = f(__w[__i]);
        byte marker = __w[^1];
        byte[] content = __w.Slice(0, __w.Length - 1).ToArray();
        return marker switch
        {
            0x06 => BitConverter.ToDouble(content, 0),
            0x05 => BitConverter.ToSingle(content, 0),
            0x04 => BitConverter.ToInt64(content, 0),
            0x03 => BitConverter.ToInt32(content, 0),
            0x02 => BitConverter.ToInt16(content, 0),
            0x01 => content[0],
            _ => throw new NotSupportedException($"Unsupported literal marker: {marker}")};
    }
}