# Glosa development notes

English | [日本語](DEVELOPMENT.ja.md)

For those working on Glosa: how it is laid out, how to build it, the tools for trying it
out, and how the releases are made. The design decisions and the reasons for them are in
[IMPLEMENTATION.md](IMPLEMENTATION.md) (in Japanese); how to use Glosa is in
[README.md](README.md).

---

## Layout

| Project | Contents |
|---|---|
| `src/Glosa.Core` | SMF parsing, the sequencer, moving through the playlist, DEF parsing, emulation (resolving, conversion during playback, panel state), CP932 |
| `src/Glosa.Midi` | The MIDI output and input abstractions (`IMidiOutput`, `IMidiInput` and their factories), `DeviceName`, which points to a device by name, and `UmpEncoder`, shared by the backends that send UMP |
| `src/Glosa.Midi.Windows` | The Windows MIDI Services backend (called through raw vtables), the WinMM backend (P/Invoke) and a high-resolution timer |
| `src/Glosa.Midi.MacOS` | The CoreMIDI backend (P/Invoke) |
| `src/Glosa.Midi.Linux` | The ALSA sequencer backend (P/Invoke) |
| `src/Glosa.Midi.Brack` | The backend that makes the audio plugins Brack hosts outputs, on any system; empty in a build without Brack |
| `src/Glosa.App` | The Avalonia GUI |
| `src/Glosa.Cli` | A console harness for trying things out |
| `tests/Glosa.Tests` | xUnit |

The only packages depended on are **Avalonia** (the GUI), **CommunityToolkit.Mvvm** (view
models), **YamlDotNet** (the format settings and playlists are saved in), and those for the
tests.

## Building and running

```bash
dotnet build
dotnet test
```

On every push and pull request, `.github/workflows/ci.yml` builds the whole solution and runs
the tests on Windows, macOS and Linux. Each MIDI backend goes only into its own system's
build, so a change that breaks the build on another system shows up here too.

A build on any system takes Brack, the audio plugin host, from the first of these it finds,
and then defines `BRACK` (`Directory.Build.props`). What refers to it is
`src/Glosa.Midi.Brack`, which the app refers to only when there is Brack. A checkout of
Brack beside this repository (`../brack`) has its .NET binding built from source, and the
native library Brack has built (on Windows, `brack.dll` in `build\bin` and
`build-x86\bin`) copied to the output. Otherwise it takes the NuGet package of the version
in `BrackVersion` from nuget.org, as CI and the releases do, having no checkout beside. With
`BrackVersion` empty it takes neither, and builds without Brack. To try the package with a
checkout beside, add `-p:UseBrackPackage=true`. Built from source,
Brack is also played by the tests through its test instrument and effect
(`build/bin/brack-test-synth.clap`, `build/bin/brack-test-gain.clap`), so build Brack with its
tests. On macOS, where Brack
works on the main thread, the tests' own entry point (`tests/Glosa.Tests/Program.cs`) runs them
on another thread and keeps the main thread's run loop running.

GUI:

```bash
dotnet run --project src/Glosa.App
```

```bash
dotnet run --project src/Glosa.App -- --def path/to/default.DEF --use SC-88PRO --target MU2000 --device "loopMIDI Port 1" song.mid
```

The options are in [README.md](README.md). For a run that should not touch your everyday
settings, such as when trying something out, `--config <folder>` changes the folder the
settings and playlists are kept in (for the default, see
[設定の保存](IMPLEMENTATION.md#設定の保存), in Japanese).

```bash
# List the output devices
dotnet run --project src/Glosa.Cli -- devices

# File information (tracks, length, SysEx, title)
dotnet run --project src/Glosa.Cli -- info path/to/song.mid

# Play
dotnet run --project src/Glosa.Cli -- play path/to/song.mid --device 0

# Play through emulation
dotnet run --project src/Glosa.Cli -- play song.mid --device 0 --def path/to/default.DEF --use MU2000 --target SC-88PRO

# How closely the sequencer keeps time (measured with a built-in metronome)
dotnet run --project src/Glosa.Cli -- jitter
dotnet run --project src/Glosa.Cli -- jitter --spacing 1 --seconds 5
```

The console harness lists its options when run with no arguments.

> `dotnet run` reads the options before `--` itself,
> so the app's own arguments must always come after `--`.

### Packages

On Windows, the published folder is made into a zip (`publish/glosa-<version>-<rid>.zip`).
It runs as it is from the unpacked folder.

```powershell
tools\windows\package.ps1                      # for this machine
tools\windows\package.ps1 -Rid win-arm64       # for ARM
```

- Made on Windows, so that `glosa.exe` carries its icon.
- `install.cmd` and `uninstall.cmd` (`tools/windows/dist/`) go into the unpacked folder with
  it.

On macOS, it is made into an app bundle (`publish/Glosa.app`): what was published goes into
`Contents/MacOS`, with `Info.plist` and the icon beside it, and the bundle is signed ad hoc,
since Macs with Apple silicon do not run executables that are not signed.

```bash
tools/macos/build-app.sh                      # for this Mac
tools/macos/build-app.sh -r osx-x64           # for Intel Macs
```

- `Info.plist` and `glosa.icns` are in `tools/macos/`. The icon is made from the same SVG as
  the Windows `.ico` by `tools/icon/build-icns.sh` (which needs only `sips` and `iconutil`,
  both part of macOS).

On Linux, it is made into a tar.gz (`publish/glosa-<version>-<rid>.tar.gz`).
It runs as it is from the unpacked folder.

```bash
tools/linux/package.sh                        # for this machine
tools/linux/package.sh -r linux-arm64         # for ARM
```

- Made on Linux or macOS, so that the archive keeps the permission to execute.
- `install.sh` and `uninstall.sh` (`tools/linux/dist/`) go into the unpacked folder with it.

`THIRD_PARTY_NOTICES.md` lists each library that goes into a release, with its license.
When a library is added or updated, update it there too.

#### Releases

Publishing a release on GitHub makes `.github/workflows/release.yml` build the packages and
attach them to the release. The version is the release's tag without its leading `v` (1.2.0
for `v1.2.0`; a suffix such as `v1.2.0-beta.1` can be added too). A tag that is not in the
form of a version fails.

| File | Made on |
|---|---|
| `glosa-<version>-win-x64.zip`, `-win-arm64.zip` | the Windows runner (`tools/windows/package.ps1`) |
| `glosa-<version>-osx-arm64.zip`, `-osx-x64.zip` | the macOS runner (`tools/macos/build-app.sh`) |
| `glosa-<version>-linux-x64.tar.gz`, `-linux-arm64.tar.gz` | the Linux runner (`tools/linux/package.sh`) |

- The tests are run first; if they do not pass, nothing is attached.
- Passing a tag to **Release → Run workflow** under Actions builds the packages of a release
  that already exists again and replaces them.

`build-app.sh` and `package.sh` take the version with `--version`, and `package.ps1` with
`-Version`. Without it, the version is the project's `Version` (1.0.0, as it is not set).


## Windows audio-device investigation notes (2026-10)

There was a report that S-MU2000 sometimes fails to open its audio device after `make`, and that rebooting Windows restores operation. The suspected relationship with Norton scans has not been established; Norton should not be treated as the confirmed cause. Direct audio testing is paused until an environment where audio can be played is available.

### Observations so far

- The Glosa CLI `devices` command ran using the WinMM backend.
- Initialization of Windows MIDI Services failed while activating `Windows.Devices.Midi2.MidiApi`, with error `0x80040154`. This is a separate observation about availability of the MIDI 2.0 API; there is not yet evidence linking it to S-MU2000's audio-output error.
- The MIDI output list contained `Microsoft GS Wavetable Synth`, `Roland port A`, `Roland port B`, `YAMAHA port A`, and `YAMAHA port B`. The user reports that `Roland port A` is connected to 88emuPlayer.
- S-MU2000's log listed `YAMAHA port A/B` for MIDI IN A/B, with C/D/E disconnected.
- During investigation, Windows services `Audiosrv` and `AudioEndpointBuilder` were both `Running`.
- `Win32_SoundDevice` listed devices including Realtek, NVIDIA, Intel Smart Sound and ASUS Utility, all with status `OK`.
- `Get-PnpDevice -Class AudioEndpoint` showed `Speakers (Realtek(R) Audio)`, `AI Noise-cancelling Output (ASUS Utility)`, and input endpoints with status `OK`.
- S-MU2000 startup output included two warnings that a button image could not be opened from line 59 of `panel.txt`. Their relationship to the audio-device issue is unknown.
- When `--audio "スピーカー"` was specified, the application reported that a playback device with a garbled-looking name could not be found. It is not yet known whether this is due to argument encoding, an exact device-name mismatch, or argument handling in the application.

### Steps when resuming

1. Reboot Windows and resume only when audio can be played. Do not try to verify test audio in a place where sound cannot be played.
2. Check whether the issue reproduces without explicitly naming a playback device: `./build/gui.exe /r/roms --audio`.
3. If it reproduces, before rebooting again, open the Windows Playback tab (`Win + R` → `mmsys.cpl`) and test the speakers.
4. Record the result and the exact S-MU2000 error message. If useful, also record the output of `Get-Service Audiosrv,AudioEndpointBuilder` and `Get-PnpDevice -Class AudioEndpoint`.
5. Do not assume Norton scanning is the cause. Compare the reproduction time with build and scan times. Disabling protection should not be the first troubleshooting step.

Current conclusion: Windows reported the audio services and endpoints as present during the investigation, but actual test audio has not yet been checked. The reason S-MU2000 sometimes fails to open the playback device remains unknown.
