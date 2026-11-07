using System.Text;
using Glosa.Core.Sng;

namespace Glosa.Tests;

public sealed class SngHeaderTests
{
    [Theory]
    [InlineData("1.00", SngVersion.V1)]
    [InlineData("2.00", SngVersion.V2)]
    [InlineData("3.00", SngVersion.V3)]
    [InlineData("4.00", SngVersion.V4)]
    public void ReadsSupportedBalladeVersions(string version, SngVersion expected)
    {
        byte[] data = CreateHeader(version);

        Assert.True(SngHeader.IsSng(data));
        Assert.Equal(expected, SngHeader.Read(data).Version);
    }

    [Fact]
    public void RejectsUnknownVersion()
    {
        byte[] data = CreateHeader("9.99");

        Assert.Throws<InvalidDataException>(() => SngHeader.Read(data));
    }

    [Fact]
    public void RejectsNonSngAndShortHeaders()
    {
        Assert.False(SngHeader.IsSng("MThd"u8));
        Assert.Throws<InvalidDataException>(() => SngHeader.Read("MThd"u8));
        Assert.False(SngHeader.IsSng(CreateHeader("3.00")[..31]));
    }

    private static byte[] CreateHeader(string version)
    {
        byte[] data = new byte[32];
        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);
        Encoding.ASCII.GetBytes(version).CopyTo(data, 18);
        return data;
    }
}
