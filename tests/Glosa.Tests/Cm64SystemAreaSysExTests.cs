using Glosa.Core.Sng;

namespace Glosa.Tests;

public sealed class Cm64SystemAreaSysExTests
{
    [Fact]
    public void BuildsLaAndPcmPartChannelMessages()
    {
        Assert.Equal(
            new byte[] { 0xF0, 0x41, 0x10, 0x16, 0x12, 0x10, 0x00, 0x0F, 0x03, 0x5E, 0xF7 },
            Cm64SystemAreaSysEx.CreateLaPartChannel(3, 3));

        Assert.Equal(
            new byte[] { 0xF0, 0x41, 0x10, 0x16, 0x12, 0x52, 0x00, 0x0E, 0x0E, 0x12, 0xF7 },
            Cm64SystemAreaSysEx.CreatePcmPartChannel(5, 14));
    }

    [Fact]
    public void BuildsReverbAndMasterVolumeMessages()
    {
        Assert.Equal(
            new byte[] { 0xF0, 0x41, 0x10, 0x16, 0x12, 0x10, 0x00, 0x01, 0x00, 0x04, 0x07, 0x64, 0xF7 },
            Cm64SystemAreaSysEx.CreateLaReverb(0, 4, 7));

        Assert.Equal(
            new byte[] { 0xF0, 0x41, 0x10, 0x16, 0x12, 0x52, 0x00, 0x01, 0x00, 0x04, 0x07, 0x22, 0xF7 },
            Cm64SystemAreaSysEx.CreatePcmReverb(0, 4, 7));

        Assert.Equal(
            new byte[] { 0xF0, 0x41, 0x10, 0x16, 0x12, 0x10, 0x00, 0x16, 0x4B, 0x0F, 0xF7 },
            Cm64SystemAreaSysEx.CreateLaMasterVolume(75));

        Assert.Equal(
            new byte[] { 0xF0, 0x41, 0x10, 0x16, 0x12, 0x52, 0x00, 0x10, 0x4B, 0x53, 0xF7 },
            Cm64SystemAreaSysEx.CreatePcmMasterVolume(75));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(9, 0)]
    public void RejectsLaPartNumbersOutsideSystemArea(int partNumber, int channel)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => Cm64SystemAreaSysEx.CreateLaPartChannel(partNumber, channel));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(7, 0)]
    public void RejectsPcmPartNumbersOutsideSystemArea(int partNumber, int channel)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => Cm64SystemAreaSysEx.CreatePcmPartChannel(partNumber, channel));

    [Fact]
    public void RejectsInvalidMidiChannelsAndMasterVolume()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cm64SystemAreaSysEx.CreateLaPartChannel(3, 16));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cm64SystemAreaSysEx.CreatePcmPartChannel(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cm64SystemAreaSysEx.CreateLaMasterVolume(101));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cm64SystemAreaSysEx.CreatePcmMasterVolume(-1));
    }

    [Fact]
    public void RejectsNonSevenBitDt1AddressAndData()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cm64SystemAreaSysEx.CreateDt1(0x80, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cm64SystemAreaSysEx.CreateDt1(0, 0, 0, 0x80));
    }

    [Fact]
    public void EveryGeneratedMessageHasAValidRolandChecksum()
    {
        byte[][] messages =
        [
            Cm64SystemAreaSysEx.CreateLaPartChannel(3, 3),
            Cm64SystemAreaSysEx.CreatePcmPartChannel(5, 14),
            Cm64SystemAreaSysEx.CreateLaReverb(0, 4, 7),
            Cm64SystemAreaSysEx.CreatePcmReverb(0, 4, 7),
            Cm64SystemAreaSysEx.CreateLaMasterVolume(75),
            Cm64SystemAreaSysEx.CreatePcmMasterVolume(75)
        ];

        foreach (byte[] message in messages)
        {
            Assert.Equal((byte)0xF0, message[0]);
            Assert.Equal((byte)0xF7, message[^1]);
            Assert.Equal(0, message.Skip(5).Take(message.Length - 7).Sum(value => value) + message[^2] & 0x7F);
        }
    }
    [Fact]
    public void BuildsAValidFrameForAnEmptyDataBlock()
    {
        Assert.Equal(
            new byte[] { 0xF0, 0x41, 0x10, 0x16, 0x12, 0x10, 0x00, 0x00, 0x70, 0xF7 },
            Cm64SystemAreaSysEx.CreateDt1(0x10, 0x00, 0x00));
    }

}
