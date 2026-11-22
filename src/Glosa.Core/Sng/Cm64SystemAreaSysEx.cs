namespace Glosa.Core.Sng;

/// <summary>
/// Builds Roland DT1 messages for the CM-64 LA and PCM System Areas.
/// MIDI channels are supplied as zero-based values (0 through 15).
/// </summary>
/// <remarks>
/// The LA and PCM System Areas share Roland model ID 0x16, but use different
/// address ranges and different part-channel offsets. This class only builds
/// messages; it does not infer an SNG part-to-sound-part mapping.
/// </remarks>
public static class Cm64SystemAreaSysEx
{
    private const byte RolandId = 0x41;
    private const byte DeviceId = 0x10;
    private const byte ModelId = 0x16;
    private const byte DataSet1 = 0x12;

    /// <summary>LA Part 3 through Part 8 MIDI CHANNEL parameter.</summary>
    public static byte[] CreateLaPartChannel(int partNumber, int midiChannel)
    {
        if (partNumber is < 3 or > 8)
            throw new ArgumentOutOfRangeException(nameof(partNumber), "CM-64 LA part must be between 3 and 8.");
        ValidateChannel(midiChannel);
        return CreateDt1(0x10, 0x00, (byte)(0x0C + partNumber), (byte)midiChannel);
    }

    /// <summary>PCM Part 1 through Part 6 MIDI CHANNEL parameter.</summary>
    public static byte[] CreatePcmPartChannel(int partNumber, int midiChannel)
    {
        if (partNumber is < 1 or > 6)
            throw new ArgumentOutOfRangeException(nameof(partNumber), "CM-64 PCM part must be between 1 and 6.");
        ValidateChannel(midiChannel);
        return CreateDt1(0x52, 0x00, (byte)(0x09 + partNumber), (byte)midiChannel);
    }

    /// <summary>Sets LA Reverb Mode, Time, and Level.</summary>
    public static byte[] CreateLaReverb(byte mode, byte time, byte level)
        => CreateDt1(0x10, 0x00, 0x01, mode, time, level);

    /// <summary>Sets PCM Reverb Mode, Time, and Level.</summary>
    public static byte[] CreatePcmReverb(byte mode, byte time, byte level)
        => CreateDt1(0x52, 0x00, 0x01, mode, time, level);

    /// <summary>Sets LA Master Volume (0 through 100).</summary>
    public static byte[] CreateLaMasterVolume(int volume)
    {
        ValidateMasterVolume(volume);
        return CreateDt1(0x10, 0x00, 0x16, (byte)volume);
    }

    /// <summary>Sets PCM Master Volume (0 through 100).</summary>
    public static byte[] CreatePcmMasterVolume(int volume)
    {
        ValidateMasterVolume(volume);
        return CreateDt1(0x52, 0x00, 0x10, (byte)volume);
    }

    /// <summary>Builds one complete Roland DT1 frame, including its checksum and F7.</summary>
    public static byte[] CreateDt1(byte addressHigh, byte addressMiddle, byte addressLow, params byte[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Validate7Bit(addressHigh, nameof(addressHigh));
        Validate7Bit(addressMiddle, nameof(addressMiddle));
        Validate7Bit(addressLow, nameof(addressLow));
        foreach (byte value in values)
            Validate7Bit(value, nameof(values));

        int sum = addressHigh + addressMiddle + addressLow;
        foreach (byte value in values) sum += value;
        byte checksum = (byte)((128 - (sum & 0x7F)) & 0x7F);

        byte[] message = new byte[9 + values.Length];
        message[0] = 0xF0;
        message[1] = RolandId;
        message[2] = DeviceId;
        message[3] = ModelId;
        message[4] = DataSet1;
        message[5] = addressHigh;
        message[6] = addressMiddle;
        message[7] = addressLow;
        values.CopyTo(message, 8);
        message[^2] = checksum;
        message[^1] = 0xF7;
        return message;
    }

    private static void ValidateChannel(int channel)
    {
        if (channel is < 0 or > 15)
            throw new ArgumentOutOfRangeException(nameof(channel), "MIDI channel must be between 0 and 15.");
    }

    private static void ValidateMasterVolume(int volume)
    {
        if (volume is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(volume), "CM-64 master volume must be between 0 and 100.");
    }

    private static void Validate7Bit(byte value, string parameterName)
    {
        if (value > 0x7F)
            throw new ArgumentOutOfRangeException(parameterName, "Roland DT1 address and data bytes must be 7-bit values.");
    }
}
