import sys
import tempfile
import unittest
from pathlib import Path

import mido

sys.path.insert(0, str(Path(__file__).parent))
import midi_compare


def make_midi(path: Path, pitch: int = 60, tempo: int = 500000) -> None:
    midi = mido.MidiFile(type=1, ticks_per_beat=480)
    conductor = mido.MidiTrack()
    conductor.append(mido.MetaMessage("set_tempo", tempo=tempo, time=0))
    conductor.append(mido.MetaMessage("end_of_track", time=0))
    track = mido.MidiTrack()
    track.append(mido.Message("note_on", channel=0, note=pitch, velocity=90, time=0))
    track.append(mido.Message("note_off", channel=0, note=pitch, velocity=0, time=480))
    track.append(mido.MetaMessage("end_of_track", time=0))
    midi.tracks.extend([conductor, track])
    midi.save(path)


class MidiCompareTests(unittest.TestCase):
    def test_identical_files_pass(self):
        with tempfile.TemporaryDirectory() as directory:
            a, b = Path(directory) / "a.mid", Path(directory) / "b.mid"
            make_midi(a)
            make_midi(b)
            result = midi_compare.compare(a, b, 100)
            self.assertEqual(result["result"], "PASS")

    def test_pitch_difference_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            a, b = Path(directory) / "a.mid", Path(directory) / "b.mid"
            make_midi(a, pitch=60)
            make_midi(b, pitch=61)
            result = midi_compare.compare(a, b, 100)
            self.assertEqual(result["result"], "FAIL")

    def test_tempo_difference_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            a, b = Path(directory) / "a.mid", Path(directory) / "b.mid"
            make_midi(a, tempo=500000)
            make_midi(b, tempo=600000)
            self.assertEqual(midi_compare.compare(a, b, 100)["result"], "FAIL")

    def test_report_does_not_include_input_paths(self):
        with tempfile.TemporaryDirectory() as directory:
            a, b = Path(directory) / "private_a.mid", Path(directory) / "private_b.mid"
            make_midi(a)
            make_midi(b)
            rendered = str(midi_compare.compare(a, b, 100))
            self.assertNotIn(str(a), rendered)
            self.assertNotIn(str(b), rendered)


if __name__ == "__main__":
    unittest.main()
