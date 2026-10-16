using System.Text;
using Glosa.Core.Smf;

namespace Glosa.Tests;

public class SngConverterTests
{
    [Fact]
    public void RecognizesBalladeSignature()
    {
        byte[] data = new byte[32];
        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);

        Assert.True(SngConverter.IsSng(data));
        Assert.False(SngConverter.IsSng("MThd"u8));
    }

    [Fact]
    public void RejectsUnknownBalladeVersion()
    {
        byte[] data = new byte[256];
        Encoding.ASCII.GetBytes("BALLADE SONG").CopyTo(data, 0);
        Encoding.ASCII.GetBytes("9.99").CopyTo(data, 18);

        Assert.Throws<InvalidDataException>(() => SngConverter.ToSmf(data));
    }
}
