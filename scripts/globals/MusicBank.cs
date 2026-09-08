using System;
using Godot;

namespace CactusTown;

/// <summary>
/// Procedurally synthesised looping music beds (soft chiptune, built in code).
/// A real <c>assets/audio/music/&lt;name&gt;.ogg</c> overrides the matching track.
/// </summary>
public static class MusicBank
{
	private const int Rate = 32000;

	private enum Wave { Sine, Square, Triangle }

	// Chords as MIDI note numbers.
	private static readonly int[][] Home =
	{
		new[] { 57, 61, 64, 68 }, new[] { 50, 54, 57, 61 },
		new[] { 52, 56, 59, 64 }, new[] { 54, 57, 61, 64 },
	};

	private static readonly int[][] Town =
	{
		new[] { 55, 59, 62, 65 }, new[] { 57, 60, 64, 67 },
		new[] { 53, 57, 60, 64 }, new[] { 50, 54, 57, 62 },
	};

	private static readonly int[][] Region =
	{
		new[] { 52, 55, 59, 62 }, new[] { 48, 52, 55, 59 },
		new[] { 50, 53, 57, 60 }, new[] { 55, 59, 62, 66 },
	};

	private static readonly int[][] Arcade =
	{
		new[] { 57, 60, 64 }, new[] { 53, 57, 60 },
		new[] { 55, 59, 62 }, new[] { 52, 55, 59 },
	};

	public static AudioStream? Track(string name) => name switch
	{
		"home" => Loop(68, Home, Wave.Sine, 0.09f, 4),
		"town" => Loop(84, Town, Wave.Triangle, 0.11f, 8),
		"region" => Loop(78, Region, Wave.Triangle, 0.10f, 6),
		"arcade" => Loop(116, Arcade, Wave.Square, 0.08f, 8),
		_ => null,
	};

	private static float NoteHz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

	private static float Osc(Wave wave, double phase)
	{
		var p = phase - Math.Floor(phase);
		return wave switch
		{
			Wave.Sine => Mathf.Sin((float)(p * Mathf.Tau)),
			Wave.Square => p < 0.5 ? 1f : -1f,
			_ => (float)(4.0 * Math.Abs(p - 0.5) - 1.0),
		};
	}

	private static AudioStreamWav Loop(int bpm, int[][] chords, Wave arpWave, float arpGain, int arpSub)
	{
		var beat = 60f / bpm;
		var bar = beat * 4f;
		var total = bar * chords.Length;
		var count = (int)(total * Rate);
		var left = new float[count];
		var right = new float[count];

		for (var c = 0; c < chords.Length; c++)
		{
			var chord = chords[c];
			var barStart = (int)(c * bar * Rate);
			var barLen = (int)(bar * Rate);

			// Sustained pad.
			for (var i = 0; i < barLen && barStart + i < count; i++)
			{
				var t = (float)i / Rate;
				var env = Mathf.Clamp(Mathf.Min(t / 0.4f, (bar - t) / 0.5f), 0f, 1f);
				var s = 0f;
				foreach (var note in chord)
					s += Mathf.Sin((float)((barStart + i) * (NoteHz(note) / Rate) * Mathf.Tau));
				s = s / chord.Length * 0.13f * env;
				left[barStart + i] += s;
				right[barStart + i] += s;
			}

			// Plucked bass on beats 1 and 3.
			var rootLow = chord[0] - 12;
			for (var b = 0; b < 4; b += 2)
			{
				var bs = barStart + (int)(b * beat * Rate);
				var bl = (int)(beat * 1.6f * Rate);
				double phase = 0;
				var inc = NoteHz(rootLow) / Rate;
				for (var i = 0; i < bl && bs + i < count; i++)
				{
					var t = (float)i / Rate;
					var env = t < 0.01f ? t / 0.01f : Mathf.Max(0f, 1f - t / (beat * 1.4f));
					var s = Osc(Wave.Triangle, phase) * env * 0.15f;
					left[bs + i] += s;
					right[bs + i] += s;
					phase += inc;
				}
			}

			// Gentle arpeggio, alternating pan.
			var noteLen = bar / arpSub;
			for (var k = 0; k < arpSub; k++)
			{
				var note = chord[k % chord.Length] + 12;
				var ns = barStart + (int)(k * noteLen * Rate);
				var nl = (int)(noteLen * Rate);
				double phase = 0;
				var inc = NoteHz(note) / Rate;
				var pan = k % 2 == 0 ? 0.36f : 0.64f;
				for (var i = 0; i < nl && ns + i < count; i++)
				{
					var t = (float)i / Rate;
					var env = t < 0.005f ? t / 0.005f : Mathf.Max(0f, 1f - t / (noteLen * 0.9f));
					var s = Osc(arpWave, phase) * env * arpGain;
					left[ns + i] += s * (1f - pan);
					right[ns + i] += s * pan;
					phase += inc;
				}
			}
		}

		return BakeStereo(left, right);
	}

	private static AudioStreamWav BakeStereo(float[] left, float[] right)
	{
		var count = left.Length;
		var data = new byte[count * 4];
		for (var i = 0; i < count; i++)
		{
			var sl = (short)(Mathf.Clamp((float)Math.Tanh(left[i]), -1f, 1f) * 29000f);
			var sr = (short)(Mathf.Clamp((float)Math.Tanh(right[i]), -1f, 1f) * 29000f);
			data[i * 4] = (byte)(sl & 0xFF);
			data[i * 4 + 1] = (byte)((sl >> 8) & 0xFF);
			data[i * 4 + 2] = (byte)(sr & 0xFF);
			data[i * 4 + 3] = (byte)((sr >> 8) & 0xFF);
		}
		return new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = Rate,
			Stereo = true,
			Data = data,
			LoopMode = AudioStreamWav.LoopModeEnum.Forward,
			LoopBegin = 0,
			LoopEnd = count,
		};
	}
}
