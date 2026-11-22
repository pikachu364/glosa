namespace Glosa.Core.Sng;

/// <summary>
/// Optional SNG-to-SMF conversion settings.
/// </summary>
public sealed class SngConversionOptions
{
    /// <summary>
    /// Optional MIDI channel overrides keyed by one-based SNG part number.
    /// MIDI channels use the zero-based range 0 through 15. Parts without an
    /// override retain the converter's historical part-number mapping.
    /// </summary>
    public IReadOnlyDictionary<int, int> PartMidiChannels { get; init; }
        = new Dictionary<int, int>();

    internal void Validate(int partCount)
    {
        foreach (var (partNumber, channel) in PartMidiChannels)
        {
            if (partNumber < 1 || partNumber > partCount)
                throw new ArgumentOutOfRangeException(
                    nameof(PartMidiChannels),
                    partNumber,
                    $"SNG part number must be between 1 and {partCount}.");

            if (channel is < 0 or > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(PartMidiChannels),
                    channel,
                    "MIDI channel must be between 0 and 15.");
        }
    }

    internal int GetMidiChannel(int partNumber, int defaultChannel)
        => PartMidiChannels.TryGetValue(partNumber, out int channel)
            ? channel
            : defaultChannel;
}
