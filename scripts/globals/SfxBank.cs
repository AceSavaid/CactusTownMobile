using System;
using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// Procedurally synthesised placeholder sound effects (16-bit mono WAV, built in
/// code — no asset files). A real <c>assets/audio/sfx/&lt;name&gt;.wav</c> overrides
/// the matching entry at play time.
/// </summary>
public static class SfxBank
{
	private const int Rate = 22050;

	private enum Wave { Sine, Square, Triangle }

	public static Dictionary<string, AudioStream> Build() => new()
	{
		{ "click", Tone(720f, 0.05f, Wave.Square, 0.30f, 0.001f, 0.035f) },
		{ "toggle", Tone(520f, 0.07f, Wave.Triangle, 0.30f, 0.001f, 0.05f) },
		{ "coin", Arp(new[] { 84, 91 }, 0.16f, Wave.Square, 0.28f) },
		{ "confirm", Arp(new[] { 64, 68, 71 }, 0.26f, Wave.Sine, 0.34f) },
		{ "repair", Arp(new[] { 60, 64, 67, 72 }, 0.44f, Wave.Triangle, 0.34f) },
		{ "fanfare", Arp(new[] { 60, 64, 67, 72, 76, 79 }, 0.8f, Wave.Square, 0.26f) },
		{ "win", Arp(new[] { 67, 72, 76, 79 }, 0.5f, Wave.Square, 0.30f) },
		{ "lose", Arp(new[] { 65, 60, 55 }, 0.42f, Wave.Square, 0.30f) },
		{ "gather", Arp(new[] { 79, 84, 88 }, 0.22f, Wave.Triangle, 0.30f) },
		{ "page", Swish() },
	};

	private static float Osc(Wave w, double phase)
	{
		var p = phase - Math.Floor(phase);
		return w switch
		{
			Wave.Sine => Mathf.Sin((float)(p * Mathf.Tau)),
			Wave.Square => p < 0.5 ? 1f : -1f,
			_ => (float)(4.0 * Math.Abs(p - 0.5) - 1.0),
		};
	}

	private static float NoteHz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

	private static AudioStreamWav Tone(float hz, float seconds, Wave wave, float gain, float attack, float release)
	{
		var count = (int)(seconds * Rate);
		var buffer = new float[count];
		double phase = 0;
		var inc = hz / Rate;
		for (var i = 0; i < count; i++)
		{
			var t = (float)i / Rate;
			var env = t < attack ? t / attack
				: seconds - t < release ? Mathf.Max(0f, (seconds - t) / release)
				: 1f;
			buffer[i] = Osc(wave, phase) * env * gain;
			phase += inc;
		}
		return Bake(buffer);
	}

	private static AudioStreamWav Arp(int[] notes, float total, Wave wave, float gain)
	{
		var step = total / notes.Length;
		var count = (int)(total * Rate);
		var buffer = new float[count];
		for (var k = 0; k < notes.Length; k++)
		{
			var inc = NoteHz(notes[k]) / Rate;
			var start = (int)(k * step * Rate);
			var len = (int)(step * Rate);
			double phase = 0;
			for (var i = 0; i < len && start + i < count; i++)
			{
				var t = (float)i / Rate;
				var env = t < 0.004f ? t / 0.004f : Mathf.Max(0f, 1f - t / (step * 0.95f));
				buffer[start + i] += Osc(wave, phase) * env * gain;
				phase += inc;
			}
		}
		return Bake(buffer);
	}

	private static AudioStreamWav Swish()
	{
		const float seconds = 0.18f;
		var count = (int)(seconds * Rate);
		var buffer = new float[count];
		var last = 0f;
		for (var i = 0; i < count; i++)
		{
			var t = (float)i / Rate;
			var env = Mathf.Sin(Mathf.Pi * t / seconds);
			var noise = (float)GD.RandRange(-1.0, 1.0);
			last = last * 0.85f + noise * 0.15f;
			buffer[i] = last * env * 0.28f;
		}
		return Bake(buffer);
	}

	private static AudioStreamWav Bake(float[] samples)
	{
		var data = new byte[samples.Length * 2];
		for (var i = 0; i < samples.Length; i++)
		{
			var s = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 30000f);
			data[i * 2] = (byte)(s & 0xFF);
			data[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
		}
		return new AudioStreamWav
		{
			Format = AudioStreamWav.FormatEnum.Format16Bits,
			MixRate = Rate,
			Stereo = false,
			Data = data,
		};
	}
}
