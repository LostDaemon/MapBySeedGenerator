using System;
using System.IO;

namespace MapSeedTestApp;

public static class Bmp24Writer
{
    public static void Write(string path, int width, int height, Func<int, int, (byte r, byte g, byte b)> pixel)
    {
        const int bytesPerPixel = 3;
        var rowSizeNoPad = width * bytesPerPixel;
        var rowPadding = (4 - (rowSizeNoPad % 4)) % 4;
        var rowSize = rowSizeNoPad + rowPadding;
        var dataSize = rowSize * height;
        var fileSize = 14 + 40 + dataSize;

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        bw.Write((ushort)0x4D42);
        bw.Write(fileSize);
        bw.Write((ushort)0);
        bw.Write((ushort)0);
        bw.Write(14 + 40);

        bw.Write(40);
        bw.Write(width);
        bw.Write(-height);
        bw.Write((ushort)1);
        bw.Write((ushort)24);
        bw.Write(0);
        bw.Write(dataSize);
        bw.Write(0);
        bw.Write(0);
        bw.Write(0);
        bw.Write(0);

        var row = new byte[rowSize];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var (r, g, b) = pixel(x, y);
                var idx = x * bytesPerPixel;
                row[idx + 0] = b;
                row[idx + 1] = g;
                row[idx + 2] = r;
            }

            if (rowPadding != 0)
            {
                Array.Clear(row, rowSizeNoPad, rowPadding);
            }

            bw.Write(row);
        }
    }
}

public sealed class Bmp24FileWriter : IDisposable
{
    private readonly BinaryWriter _bw;
    private readonly byte[] _row;
    private readonly int _rowSizeNoPad;
    private readonly int _rowPadding;

    public Bmp24FileWriter(string path, int width, int height)
    {
        const int bytesPerPixel = 3;
        _rowSizeNoPad = width * bytesPerPixel;
        _rowPadding = (4 - (_rowSizeNoPad % 4)) % 4;
        var rowSize = _rowSizeNoPad + _rowPadding;
        var dataSize = rowSize * height;
        var fileSize = 14 + 40 + dataSize;

        var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        _bw = new BinaryWriter(fs);

        _bw.Write((ushort)0x4D42);
        _bw.Write(fileSize);
        _bw.Write((ushort)0);
        _bw.Write((ushort)0);
        _bw.Write(14 + 40);

        _bw.Write(40);
        _bw.Write(width);
        _bw.Write(-height);
        _bw.Write((ushort)1);
        _bw.Write((ushort)24);
        _bw.Write(0);
        _bw.Write(dataSize);
        _bw.Write(0);
        _bw.Write(0);
        _bw.Write(0);
        _bw.Write(0);

        _row = new byte[rowSize];
    }

    public void SetPixel(int x, byte r, byte g, byte b)
    {
        const int bytesPerPixel = 3;
        var idx = x * bytesPerPixel;
        _row[idx + 0] = b;
        _row[idx + 1] = g;
        _row[idx + 2] = r;
    }

    public void WriteRow()
    {
        if (_rowPadding != 0)
        {
            Array.Clear(_row, _rowSizeNoPad, _rowPadding);
        }

        _bw.Write(_row);
    }

    public void Dispose()
    {
        _bw.Dispose();
    }
}
