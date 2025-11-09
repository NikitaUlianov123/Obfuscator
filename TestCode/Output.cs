using System;
using System.Text;

class Program
{
    static void Main()
    {
        Console.WriteLine(_O.D(new byte[] { 22 })[0]);
        double a0 = (Double)_O.L(new byte[] { 230, 104, 177, 15, 159, 18, 144, 4, 96 });
        for (int a1 = (Int32)_O.L(new byte[] { 16, 0, 0, 0, 48 }); a1 <= (Int32)_O.L(new byte[] { 70, 0, 0, 0, 48 }); a1++)
        {
            if (a1 % (Int32)_O.L(new byte[] { 240, 0, 0, 0, 48 }) == (Int32)_O.L(new byte[] { 0, 0, 0, 0, 48 }))
                Console.WriteLine(_O.D(new byte[] { 100, 150, 167, 167, 36, 87, 167, 167 }));
            else if (a1 % (Int32)_O.L(new byte[] { 48, 0, 0, 0, 48 }) == (Int32)_O.L(new byte[] { 0, 0, 0, 0, 48 }))
                Console.WriteLine(_O.D(new byte[] { 100, 150, 167, 167 }));
            else if (a1 % (Int32)_O.L(new byte[] { 80, 0, 0, 0, 48 }) == (Int32)_O.L(new byte[] { 0, 0, 0, 0, 48 }))
                Console.WriteLine(_O.D(new byte[] { 36, 87, 167, 167 }));
            else
                Console.WriteLine(a1);
        }
    }
}

static class _O
{
    private static byte R(byte x)
    {
        int t = x;
        return (byte)(((t << 4) | (t >> 4)) & 0xFF);
    }

    public static string D(byte[] a)
    {
        var b = (byte[])a.Clone();
        for (int i = 0; i < b.Length; i++)
            b[i] = R(b[i]);
        return Encoding.UTF8.GetString(b);
    }

    public static object L(byte[] z)
    {
        var q = (byte[])z.Clone();
        Func<byte, byte> f = x => R(x);
        for (int i = 0; i < q.Length; i++)
            q[i] = f(q[i]);
        if (q.Length == 0)
            throw new ArgumentException(new string (new[] { 'E', 'm', 'p', 't', 'y', ' ', 'L', 'i', 't', 'e', 'r', 'a', 'l' }));
        byte k = q[^1];
        byte[] c = new byte[q.Length - 1];
        if (c.Length > 0)
            Array.Copy(q, c, c.Length);
        return ((Func<byte, object>)(m => m switch
        {
            0x06 => BitConverter.ToDouble(c, 0),
            0x05 => BitConverter.ToSingle(c, 0),
            0x04 => BitConverter.ToInt64(c, 0),
            0x03 => BitConverter.ToInt32(c, 0),
            0x02 => BitConverter.ToInt16(c, 0),
            0x01 => c.Length > 0 ? (object)c[0] : (object)0,
            0x30 => BitConverter.ToInt32(c, 0),
            0x60 => c.Length >= 8 ? BitConverter.ToDouble(c, 0) : (c.Length >= 4 ? BitConverter.ToInt32(c, 0) : (object)c),
            _ => ((Func<int, object>)(len => len switch
            {
                8 => BitConverter.ToDouble(c, 0),
                4 => BitConverter.ToInt32(c, 0),
                2 => BitConverter.ToInt16(c, 0),
                1 => c[0],
                0 => 0,
                _ => throw new NotSupportedException($"??? {len}")}))(c.Length)}))(k);
    }
}