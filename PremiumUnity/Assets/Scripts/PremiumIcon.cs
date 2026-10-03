using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Premium2048 {
public enum IconKind {Menu,Sound,Mute,Restart,Undo,Swap,Delete,Settings,Play,Check,Lock,Trophy,Star,Map}

public sealed class PremiumIcon : Image {
 static readonly Dictionary<IconKind,Sprite> cache = new Dictionary<IconKind,Sprite>();
 IconKind kind;

 public IconKind Kind {
  get => kind;
  set {
   kind = value;
   sprite = GetSprite(value);
   type = Type.Simple;
   preserveAspect = true;
   SetAllDirty();
  }
 }

 public static Sprite GetSprite(IconKind kind) {
  if (cache.TryGetValue(kind, out var s) && s != null) return s;
  var spr = GenerateSprite(kind);
  cache[kind] = spr;
  return spr;
 }

 static Sprite GenerateSprite(IconKind kind) {
  const int S = 128;
  var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
  tex.wrapMode = TextureWrapMode.Clamp;
  tex.filterMode = FilterMode.Bilinear;
  Color32[] pixels = new Color32[S * S];

  float stroke = 0.075f;
  float aa = 2.5f / S;

  for (int y = 0; y < S; y++) {
   float py = (y + 0.5f) / (S * 0.5f) - 1f;
   for (int x = 0; x < S; x++) {
    float px = (x + 0.5f) / (S * 0.5f) - 1f;
    Vector2 p = new Vector2(px, py);
    float d = DistToIcon(p, kind);
    float alpha;
    if (kind == IconKind.Play) {
     alpha = Mathf.Clamp01(0.5f - d / aa);
    } else {
     alpha = Mathf.Clamp01(0.5f - (d - stroke) / aa);
    }
    byte a = (byte)(alpha * 255f);
    pixels[y * S + x] = new Color32(255, 255, 255, a);
   }
  }

  tex.SetPixels32(pixels);
  tex.Apply();
  return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
 }

 static float DistToIcon(Vector2 p, IconKind kind) {
  float d = 999f;
  switch (kind) {
   case IconKind.Menu:
    d = Mathf.Min(d, Seg(p, new Vector2(-0.7f, 0.45f), new Vector2(0.7f, 0.45f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.7f, 0f), new Vector2(0.7f, 0f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.7f, -0.45f), new Vector2(0.7f, -0.45f)));
    break;

   case IconKind.Sound:
   case IconKind.Mute:
    d = Mathf.Min(d, Seg(p, new Vector2(-0.75f, -0.28f), new Vector2(-0.75f, 0.28f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.75f, 0.28f), new Vector2(-0.35f, 0.28f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.35f, 0.28f), new Vector2(0.08f, 0.68f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.08f, 0.68f), new Vector2(0.08f, -0.68f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.08f, -0.68f), new Vector2(-0.35f, -0.28f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.35f, -0.28f), new Vector2(-0.75f, -0.28f)));
    if (kind == IconKind.Mute) {
     d = Mathf.Min(d, Seg(p, new Vector2(0.32f, 0.35f), new Vector2(0.85f, -0.35f)));
     d = Mathf.Min(d, Seg(p, new Vector2(0.32f, -0.35f), new Vector2(0.85f, 0.35f)));
    } else {
     d = Mathf.Min(d, Arc(p, new Vector2(0.08f, 0f), 0.38f, -50f, 50f));
     d = Mathf.Min(d, Arc(p, new Vector2(0.08f, 0f), 0.70f, -50f, 50f));
    }
    break;

   case IconKind.Restart:
    d = Mathf.Min(d, Arc(p, Vector2.zero, 0.62f, -60f, 220f));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.45f, 0.42f), new Vector2(-0.45f, 0.88f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.45f, 0.42f), new Vector2(-0.02f, 0.42f)));
    break;

   case IconKind.Undo:
    d = Mathf.Min(d, Seg(p, new Vector2(-0.68f, 0.38f), new Vector2(0.15f, 0.38f)));
    d = Mathf.Min(d, Arc(p, new Vector2(0.15f, -0.06f), 0.44f, -90f, 90f));
    d = Mathf.Min(d, Seg(p, new Vector2(0.15f, -0.50f), new Vector2(-0.52f, -0.50f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.68f, 0.38f), new Vector2(-0.32f, 0.72f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.68f, 0.38f), new Vector2(-0.32f, 0.04f)));
    break;

   case IconKind.Swap:
    d = Mathf.Min(d, Seg(p, new Vector2(-0.72f, 0.35f), new Vector2(0.55f, 0.35f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.55f, 0.35f), new Vector2(0.20f, 0.68f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.55f, 0.35f), new Vector2(0.20f, 0.02f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.72f, -0.35f), new Vector2(-0.55f, -0.35f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.55f, -0.35f), new Vector2(-0.20f, -0.02f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.55f, -0.35f), new Vector2(-0.20f, -0.68f)));
    break;

   case IconKind.Delete:
    d = Mathf.Min(d, Seg(p, new Vector2(-0.72f, 0.48f), new Vector2(0.72f, 0.48f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.26f, 0.72f), new Vector2(0.26f, 0.72f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.26f, 0.72f), new Vector2(-0.26f, 0.48f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.26f, 0.72f), new Vector2(0.26f, 0.48f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.52f, 0.48f), new Vector2(-0.40f, -0.72f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.40f, -0.72f), new Vector2(0.40f, -0.72f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.40f, -0.72f), new Vector2(0.52f, 0.48f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.20f, 0.22f), new Vector2(-0.16f, -0.48f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0f, 0.22f), new Vector2(0f, -0.48f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.20f, 0.22f), new Vector2(0.16f, -0.48f)));
    break;

   case IconKind.Settings:
    d = Mathf.Min(d, Mathf.Abs(p.magnitude - 0.35f));
    d = Mathf.Min(d, Mathf.Abs(p.magnitude - 0.72f));
    for (int i = 0; i < 8; i++) {
     float a = i * Mathf.PI / 4f;
     Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
     d = Mathf.Min(d, Seg(p, dir * 0.70f, dir * 0.95f));
    }
    break;

   case IconKind.Play:
    return Tri(p, new Vector2(-0.45f, -0.68f), new Vector2(-0.45f, 0.68f), new Vector2(0.68f, 0f));

   case IconKind.Check:
    d = Mathf.Min(d, Seg(p, new Vector2(-0.68f, -0.02f), new Vector2(-0.18f, -0.52f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.18f, -0.52f), new Vector2(0.72f, 0.62f)));
    break;

   case IconKind.Lock:
    d = Mathf.Min(d, Seg(p, new Vector2(-0.58f, -0.72f), new Vector2(0.58f, -0.72f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.58f, -0.72f), new Vector2(0.58f, 0.18f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.58f, 0.18f), new Vector2(-0.58f, 0.18f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.58f, 0.18f), new Vector2(-0.58f, -0.72f)));
    d = Mathf.Min(d, Arc(p, new Vector2(0f, 0.18f), 0.38f, 0f, 180f));
    break;

   case IconKind.Trophy:
    d = Mathf.Min(d, Seg(p, new Vector2(-0.60f, 0.68f), new Vector2(0.60f, 0.68f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.60f, 0.68f), new Vector2(-0.38f, -0.10f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.38f, -0.10f), new Vector2(0.38f, -0.10f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.38f, -0.10f), new Vector2(0.60f, 0.68f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0f, -0.10f), new Vector2(0f, -0.68f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.40f, -0.68f), new Vector2(0.40f, -0.68f)));
    break;

   case IconKind.Star:
    // 5-pointed star segments
    Vector2[] pts = new Vector2[10];
    for(int i=0; i<10; i++) {
     float a = i * Mathf.PI / 5f - Mathf.PI / 2f;
     float r = (i % 2 == 0) ? 0.75f : 0.35f;
     pts[i] = new Vector2(Mathf.Cos(a) * r, -Mathf.Sin(a) * r);
    }
    for(int i=0; i<10; i++) {
     d = Mathf.Min(d, Seg(p, pts[i], pts[(i + 1) % 10]));
    }
    break;

   case IconKind.Map:
    d = Mathf.Min(d, Seg(p, new Vector2(-0.65f, -0.55f), new Vector2(-0.65f, 0.65f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.65f, 0.65f), new Vector2(-0.20f, 0.45f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.20f, 0.45f), new Vector2(-0.20f, -0.75f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.20f, -0.75f), new Vector2(-0.65f, -0.55f)));
    d = Mathf.Min(d, Seg(p, new Vector2(-0.20f, 0.45f), new Vector2(0.20f, 0.65f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.20f, 0.65f), new Vector2(0.20f, -0.55f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.20f, -0.55f), new Vector2(-0.20f, -0.75f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.20f, 0.65f), new Vector2(0.65f, 0.45f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.65f, 0.45f), new Vector2(0.65f, -0.75f)));
    d = Mathf.Min(d, Seg(p, new Vector2(0.65f, -0.75f), new Vector2(0.20f, -0.55f)));
    break;
  }
  return d;
 }

 static float Seg(Vector2 p, Vector2 a, Vector2 b) {
  Vector2 pa = p - a, ba = b - a;
  float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
  return (pa - ba * h).magnitude;
 }

 static float Arc(Vector2 p, Vector2 c, float r, float sDeg, float eDeg) {
  Vector2 d = p - c;
  float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
  if (ang < 0f) ang += 360f;
  float s = (sDeg % 360f + 360f) % 360f;
  float e = (eDeg % 360f + 360f) % 360f;
  bool inArc = (s <= e) ? (ang >= s && ang <= e) : (ang >= s || ang <= e);
  if (inArc) return Mathf.Abs(d.magnitude - r);
  float sR = sDeg * Mathf.Deg2Rad, eR = eDeg * Mathf.Deg2Rad;
  Vector2 pA = c + new Vector2(Mathf.Cos(sR), Mathf.Sin(sR)) * r;
  Vector2 pB = c + new Vector2(Mathf.Cos(eR), Mathf.Sin(eR)) * r;
  return Mathf.Min((p - pA).magnitude, (p - pB).magnitude);
 }

 static float Tri(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2) {
  Vector2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
  Vector2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
  Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
  Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
  Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
  float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
  float dX = Mathf.Min(Mathf.Min(pq0.sqrMagnitude, pq1.sqrMagnitude), pq2.sqrMagnitude);
  float dY = Mathf.Min(Mathf.Min(s * (v0.x * e0.y - v0.y * e0.x), s * (v1.x * e1.y - v1.y * e1.x)), s * (v2.x * e2.y - v2.y * e2.x));
  return -Mathf.Sqrt(dX) * Mathf.Sign(dY);
 }

 protected override void OnEnable() {
  // Prepare the sprite before uGUI starts its rebuild, never inside OnPopulateMesh.
  if(sprite==null)sprite=GetSprite(kind);
  base.OnEnable();
 }
}
}
