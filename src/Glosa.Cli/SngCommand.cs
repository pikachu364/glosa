namespace Glosa.Cli;

/// <summary>Converts a Roland/Boss Ballade SNG file to a Standard MIDI File.</summary>
internal static class SngCommand
{
    public static int Run(string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            Console.Error.WriteLine("usage: glosa convert-sng <file.sng> [output.mid]");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args.Length == 3
            ? args[2]
            : Path.ChangeExtension(inputPath, ".mid");

        StringComparison pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (Path.GetFullPath(inputPath).Equals(Path.GetFullPath(outputPath), pathComparison))
        {
            Console.Error.WriteLine("error: output path must be different from the input SNG path");
            return 1;
        }

        byte[] source = File.ReadAllBytes(inputPath);
        byte[] midi = Glosa.Core.Smf.SngConverter.ToSmf(source);
        File.WriteAllBytes(outputPath, midi);

        Console.WriteLine($"{inputPath} -> {outputPath} ({midi.Length} bytes)");
        return 0;
    }
}
