using System;
using System.Collections.Generic;
using UnityEngine;

namespace Premium2048 {
public static class PremiumAudio {
 const int SampleRate = 44100;

 static float TriangleWave(float phase) {
  float p = (phase / (2f * Mathf.PI)) % 1f;
  if (p < 0) p += 1f;
  return 2f * Mathf.Abs(2f * p - 1f) - 1f;
 }

 static float SineWave(float phase) {
  return Mathf.Sin(phase);
 }

 static void AddTone(float[] buffer, float startSec, float durationSec, float[] frequencies, string type, float volume, string envelope) {
  int startSample = Mathf.Clamp((int)(startSec * SampleRate), 0, buffer.Length);
  int toneSamples = (int)(durationSec * SampleRate);
  int endSample = Mathf.Min(startSample + toneSamples, buffer.Length);
  int totalSamples = endSample - startSample;
  if (totalSamples <= 0) return;

  for (int f = 0; f < frequencies.Length; f++) {
   float freq = frequencies[f];
   float oscVol = (volume / (f + 1)) * 0.45f;
   float phase = 0f;
   float phaseInc = 2f * Mathf.PI * freq / SampleRate;

   for (int i = 0; i < totalSamples; i++) {
    int idx = startSample + i;
    float t = (float)i / SampleRate;
    float env;

    if (envelope == "sharp") {
     env = Mathf.Exp(-5.5f * (t / durationSec));
    } else {
     float attack = Mathf.Clamp01(t / 0.02f);
     float decay = Mathf.Exp(-4.2f * (t / durationSec));
     env = attack * decay;
    }

    float sample = (type == "triangle" ? TriangleWave(phase) : SineWave(phase)) * oscVol * env;
    buffer[idx] += sample;
    phase += phaseInc;
   }
  }
 }

 static AudioClip MakeClip(string name, float duration, Action<float[]> build) {
  int samples = (int)(duration * SampleRate);
  var data = new float[samples];
  build(data);

  // Normalize / Soft clip to prevent any distortion
  for (int i = 0; i < samples; i++) {
   data[i] = Mathf.Clamp(data[i], -0.95f, 0.95f);
  }

  var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
  clip.SetData(data, 0);
  return clip;
 }

 // Smooth air whoosh with gentle frequency sweep (matches index.html playSlide)
 public static AudioClip Slide() {
  return MakeClip("Slide", 0.15f, data => {
   float phase = 0f;
   int total = data.Length;
   for (int i = 0; i < total; i++) {
    float t = (float)i / SampleRate;
    float progress = (float)i / total;
    float freq = 300f * Mathf.Pow(150f / 300f, progress);
    float attack = Mathf.Clamp01(t / 0.02f);
    float decay = Mathf.Exp(-4.0f * progress);
    float env = attack * decay * 0.35f;

    data[i] = SineWave(phase) * env;
    phase += 2f * Mathf.PI * freq / SampleRate;
   }
  });
 }

 // Tile merge sound matching index.html playMerge
 public static AudioClip Merge(int tileValue) {
  if (tileValue == 4) {
   return MakeClip("Merge4", 0.3f, data => {
    AddTone(data, 0f, 0.3f, new[] { 523f, 659f }, "sine", 0.35f, "smooth");
   });
  }
  if (tileValue == 8) {
   return MakeClip("Merge8", 0.35f, data => {
    AddTone(data, 0f, 0.35f, new[] { 659f, 784f, 988f }, "sine", 0.38f, "smooth");
   });
  }
  if (tileValue == 16) {
   return MakeClip("Merge16", 0.4f, data => {
    AddTone(data, 0f, 0.4f, new[] { 784f, 988f, 1175f }, "triangle", 0.42f, "smooth");
   });
  }
  if (tileValue == 32) {
   return MakeClip("Merge32", 0.6f, data => {
    float[] sparkle = { 1047f, 1319f, 1568f, 1865f };
    for (int i = 0; i < sparkle.Length; i++) {
     AddTone(data, i * 0.08f, 0.2f, new[] { sparkle[i], sparkle[i] * 1.5f }, "sine", 0.4f, "sharp");
    }
    AddTone(data, 0.1f, 0.5f, new[] { 523f, 659f, 784f }, "triangle", 0.45f, "smooth");
   });
  }
  if (tileValue == 64) {
   return MakeClip("Merge64", 0.8f, data => {
    float[] arpeggio = { 523f, 659f, 784f, 988f, 1175f, 1397f };
    for (int i = 0; i < arpeggio.Length; i++) {
     AddTone(data, i * 0.1f, 0.3f, new[] { arpeggio[i], arpeggio[i] * 0.5f }, "sine", 0.42f, "smooth");
    }
    AddTone(data, 0.65f, 0.8f, new[] { 523f, 659f, 784f, 1047f, 1319f }, "triangle", 0.5f, "smooth");
   });
  }
  if (tileValue == 128) {
   return MakeClip("Merge128", 0.65f, data => {
    AddTone(data, 0f, 0.6f, new[] { 523f, 659f, 784f, 1047f }, "sine", 0.45f, "smooth");
    AddTone(data, 0.2f, 0.4f, new[] { 587f, 740f, 880f, 1175f }, "sine", 0.4f, "smooth");
   });
  }
  if (tileValue >= 256) {
   return MakeClip("MergeEpic", 0.9f, data => {
    AddTone(data, 0f, 0.8f, new[] { 392f, 523f, 659f, 784f }, "sine", 0.48f, "smooth");
    AddTone(data, 0.3f, 0.6f, new[] { 440f, 587f, 740f, 880f }, "triangle", 0.45f, "smooth");
    AddTone(data, 0.6f, 0.4f, new[] { 523f, 659f, 784f, 1047f }, "sine", 0.4f, "smooth");
   });
  }

  float baseFreq = Mathf.Min(600f + tileValue / 8f, 1200f);
  return MakeClip("MergeDefault", 0.25f, data => {
   AddTone(data, 0f, 0.25f, new[] { baseFreq, baseFreq * 1.25f }, "sine", 0.35f, "smooth");
  });
 }

 // Gentle reverse swoosh with rising pitch + magical chime (matches index.html playUndo)
 public static AudioClip Undo() {
  return MakeClip("Undo", 0.45f, data => {
   int sweepSamples = (int)(0.25f * SampleRate);
   float phase = 0f;
   for (int i = 0; i < sweepSamples; i++) {
    float t = (float)i / SampleRate;
    float progress = (float)i / sweepSamples;
    float freq = 250f * Mathf.Pow(450f / 250f, progress);
    float attack = Mathf.Clamp01(t / 0.05f);
    float decay = Mathf.Exp(-3.2f * progress);
    float env = attack * decay * 0.38f;

    data[i] = TriangleWave(phase) * env;
    phase += 2f * Mathf.PI * freq / SampleRate;
   }
   AddTone(data, 0.2f, 0.25f, new[] { 659f, 784f, 988f }, "sine", 0.35f, "smooth");
  });
 }

 // Exchange two-tone + confirmation chime (matches index.html playSwap)
 public static AudioClip Swap() {
  return MakeClip("Swap", 0.55f, data => {
   AddTone(data, 0f, 0.15f, new[] { 440f, 880f }, "triangle", 0.4f, "sharp");
   AddTone(data, 0.15f, 0.15f, new[] { 880f, 440f }, "triangle", 0.4f, "sharp");
   AddTone(data, 0.3f, 0.25f, new[] { 659f, 784f, 988f }, "sine", 0.38f, "smooth");
  });
 }

 // Smooth energy dissipation + sparkle (matches index.html playDelete)
 public static AudioClip Delete() {
  return MakeClip("Delete", 0.28f, data => {
   int sweepSamples = (int)(0.2f * SampleRate);
   float phase = 0f;
   for (int i = 0; i < sweepSamples; i++) {
    float t = (float)i / SampleRate;
    float progress = (float)i / sweepSamples;
    float freq = 600f * Mathf.Pow(200f / 600f, progress);
    float attack = Mathf.Clamp01(t / 0.01f);
    float decay = Mathf.Exp(-4.0f * progress);
    float env = attack * decay * 0.4f;

    data[i] = TriangleWave(phase) * env;
    phase += 2f * Mathf.PI * freq / SampleRate;
   }

   AddTone(data, 0.05f, 0.08f, new[] { 920f, 1340f }, "sine", 0.22f, "sharp");
   AddTone(data, 0.09f, 0.08f, new[] { 1080f, 1490f }, "sine", 0.22f, "sharp");
   AddTone(data, 0.13f, 0.08f, new[] { 1150f, 1620f }, "sine", 0.22f, "sharp");
  });
 }

 // Epic victory fanfare (matches index.html playWin)
 public static AudioClip Win() {
  return MakeClip("Win", 2.0f, data => {
   float[][] fanfare = {
    new[] { 523f, 659f, 784f },
    new[] { 587f, 740f, 880f },
    new[] { 659f, 831f, 988f },
    new[] { 784f, 988f, 1175f }
   };

   for (int i = 0; i < fanfare.Length; i++) {
    AddTone(data, i * 0.2f, 0.4f, fanfare[i], "sine", 0.5f, "smooth");
   }
   AddTone(data, 1.0f, 1.0f, new[] { 523f, 659f, 784f, 1047f, 1319f }, "triangle", 0.58f, "smooth");
  });
 }

 // Dramatic descending sequence (matches index.html playLoss)
 public static AudioClip Loss() {
  return MakeClip("Loss", 1.8f, data => {
   float[][] descent = {
    new[] { 523f, 659f, 784f },
    new[] { 466f, 587f, 698f },
    new[] { 415f, 523f, 622f },
    new[] { 349f, 440f, 523f },
    new[] { 294f, 370f, 440f }
   };

   for (int i = 0; i < descent.Length; i++) {
    AddTone(data, i * 0.3f, 0.5f, descent[i], "sine", 0.45f, "smooth");
   }
  });
 }

 // Magical appearance pop (matches index.html playSpawn)
 public static AudioClip Spawn() {
  return MakeClip("Spawn", 0.2f, data => {
   AddTone(data, 0f, 0.1f, new[] { 1047f, 1319f }, "sine", 0.32f, "sharp");
   AddTone(data, 0.08f, 0.08f, new[] { 1319f, 1568f }, "triangle", 0.28f, "sharp");
  });
 }

 // Crisp UI button click
 public static AudioClip Click() {
  return MakeClip("Click", 0.06f, data => {
   AddTone(data, 0f, 0.06f, new[] { 660f, 880f }, "sine", 0.3f, "sharp");
  });
 }
}
}
