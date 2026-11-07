using System.Buffers.Binary;
using System.Text;
using Glosa.Core.Sng;

namespace Glosa.Tests;

public sealed class SngLayoutReaderTests
{
    [Theory]
    [InlineData("1.00", 10)]
    [InlineData("2.00", 10)]
    [InlineData("3.00", 10)]
    [InlineData("4.00", 16)]
    public void RejectsTruncatedPartBlockTable(string version, int parts)
    {
        byte[] data = new byte[256];
        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);
        Encoding.ASCII.GetBytes(version).CopyTo(data, 18);
        int table = 0x46;
        for (int i = 0; i <= parts; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(table + 4 * i), 100);

        Assert.Throws<InvalidDataException>(() => SngLayoutReader.Read(data));
    }

    [Theory]
    [InlineData("2.00", 10)]
    [InlineData("3.00", 10)]
    [InlineData("4.00", 16)]
    public void RejectsTruncatedLastLaterVersionPartBlock(string version, int parts)
    {
        int tableEnd = 0x46 + 4 * (parts + 1);
        int firstCandidate = tableEnd + 94;
        int declaredPartSize = 168;
        byte[] data = new byte[firstCandidate + parts * declaredPartSize - 1];
        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);
        Encoding.ASCII.GetBytes(version).CopyTo(data, 18);

        // The first later-version layout candidate has complete part headers,
        // but its final declared block is one byte short. Invalidate the two
        // earlier-start candidates so they cannot mask this truncation.
        for (int i = 0; i <= parts; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(
                data.AsSpan(0x46 + 4 * i), i == 0 ? 0u : 87u);

        data[firstCandidate - 92 + 112] = 1;
        data[firstCandidate - 94 + 112] = 1;

        Assert.Throws<InvalidDataException>(() => SngLayoutReader.Read(data));
    }

    [Fact]
    public void RejectsInvalidVersionBeforeReadingBlockTable()
    {
        byte[] data = new byte[512];
        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);
        Encoding.ASCII.GetBytes("9.99").CopyTo(data, 18);

        Assert.Throws<InvalidDataException>(() => SngLayoutReader.Read(data));
    }
}
