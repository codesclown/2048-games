using System.Collections.Generic;
using UnityEngine;

namespace Premium2048 {
public static class PremiumTheme {
    public static UnityEngine.Color ParseColor(string hex) {
        ColorUtility.TryParseHtmlString("#" + hex, out var value);
        return value;
    }
    public static UnityEngine.Color Color(string hex) => ParseColor(hex);
    public static UnityEngine.Color Hex(string hex) => ParseColor(hex);

    // AAA Color Palette
    public static UnityEngine.Color Background => ParseColor("0A0C16");    // Deepest midnight obsidian
    public static UnityEngine.Color Surface => ParseColor("14182D");       // Glass card surface
    public static UnityEngine.Color SurfaceElevated => ParseColor("1C213D"); // Active/Elevated card
    public static UnityEngine.Color SurfaceHover => ParseColor("242B4E");   // Highlighted container
    public static UnityEngine.Color Border => ParseColor("2C355E");         // Subtle card stroke
    public static UnityEngine.Color BorderLight => ParseColor("48558A");    // Glowing active stroke
    
    public static UnityEngine.Color Text => ParseColor("FFFFFF");           // Crisp pure white
    public static UnityEngine.Color TextSecondary => ParseColor("E2E7F4");  // High-legibility light silver
    public static UnityEngine.Color Muted => ParseColor("8E9BB5");          // Elegant slate/lavender
    public static UnityEngine.Color Subdued => ParseColor("586282");        // Subtle hints/dividers

    public static UnityEngine.Color Gold => ParseColor("FFC837");           // Royal gold gradient top
    public static UnityEngine.Color GoldDark => ParseColor("FF8008");       // Rich amber gradient bottom
    public static UnityEngine.Color Accent => ParseColor("FFC837");         // Gold brand accent
    public static UnityEngine.Color AccentGlow => ParseColor("FFD70088");    // Glow highlight
    public static UnityEngine.Color Ink => ParseColor("080A12");            // Deep contrast text on gold

    public static UnityEngine.Color Cyan => ParseColor("00D2FF");           // Undo / Action blue
    public static UnityEngine.Color Purple => ParseColor("A855F7");         // Swap / Magic violet
    public static UnityEngine.Color Coral => ParseColor("FF4757");          // Delete / Warning red
    public static UnityEngine.Color Emerald => ParseColor("2ED573");        // Victory green

    static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    public static Sprite CreateRoundedCard(int size, int radius, UnityEngine.Color fill, UnityEngine.Color border, float borderWidth = 1.5f) {
        string key = $"card_{size}_{radius}_{ColorUtility.ToHtmlStringRGBA(fill)}_{ColorUtility.ToHtmlStringRGBA(border)}_{borderWidth}";
        if (spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float r = radius;
        float half = size * 0.5f;

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dx = Mathf.Max(0, Mathf.Abs(x - half + 0.5f) - (half - r));
                float dy = Mathf.Max(0, Mathf.Abs(y - half + 0.5f) - (half - r));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float outerDist = dist - (r - 0.5f);
                float alpha = Mathf.Clamp01(1f - outerDist);

                if (alpha <= 0f) {
                    tex.SetPixel(x, y, UnityEngine.Color.clear);
                    continue;
                }

                float borderFactor = Mathf.Clamp01((dist - (r - borderWidth)) / borderWidth);
                float grad = Mathf.Lerp(1.08f, 0.94f, (float)y / size);
                UnityEngine.Color inner = new UnityEngine.Color(fill.r * grad, fill.g * grad, fill.b * grad, fill.a);
                UnityEngine.Color pixel = UnityEngine.Color.Lerp(inner, border, borderFactor);
                pixel.a *= alpha;
                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        var spr = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        spriteCache[key] = spr;
        return spr;
    }

    public static Sprite CreateGoldButton(int size, int radius) {
        string key = $"gold_btn_{size}_{radius}";
        if (spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float r = radius;
        float half = size * 0.5f;

        UnityEngine.Color top = Gold;
        UnityEngine.Color btm = GoldDark;
        UnityEngine.Color border = ParseColor("FFE082");

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dx = Mathf.Max(0, Mathf.Abs(x - half + 0.5f) - (half - r));
                float dy = Mathf.Max(0, Mathf.Abs(y - half + 0.5f) - (half - r));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - (dist - (r - 0.5f)));

                if (alpha <= 0f) {
                    tex.SetPixel(x, y, UnityEngine.Color.clear);
                    continue;
                }

                float t = (float)y / size;
                UnityEngine.Color baseCol = UnityEngine.Color.Lerp(btm, top, t);
                if (y > size - 6 && dist < r - 1f) {
                    baseCol = UnityEngine.Color.Lerp(baseCol, UnityEngine.Color.white, 0.35f);
                }
                float borderFactor = Mathf.Clamp01((dist - (r - 2f)) / 2f);
                UnityEngine.Color pixel = UnityEngine.Color.Lerp(baseCol, border, borderFactor * 0.6f);
                pixel.a *= alpha;
                tex.SetPixel(x, y, pixel);
            }
        }
        tex.Apply();
        var spr = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        spriteCache[key] = spr;
        return spr;
    }

    public static void TileColors(int value, out UnityEngine.Color first, out UnityEngine.Color second) {
        string a, b;
        switch (value) {
            case 0: a = "1E233F"; b = "161A30"; break; // Sleek empty slot with subtle depth
            case 2: a = "667EEA"; b = "764BA2"; break; // 2: Cosmic violet-blue
            case 4: a = "F093FB"; b = "F5576C"; break; // 4: Pink-Coral glow
            case 8: a = "4FACFE"; b = "00F2FE"; break; // 8: Electric Cyan
            case 16: a = "43E97B"; b = "38F9D7"; break; // 16: Emerald Mint
            case 32: a = "FA709A"; b = "FEE140"; break; // 32: Sunset Amber
            case 64: a = "A8EDEA"; b = "FED6E3"; break; // 64: Crystal Aqua-Pink
            case 128: a = "FFECD2"; b = "FCB69F"; break; // 128: Peach Sunburst
            case 256: a = "FFD89B"; b = "19547B"; break; // 256: Golden Sand-Navy
            case 512: a = "8A2387"; b = "E94057"; break; // 512: Royal Magenta
            case 1024: a = "FFD700"; b = "FF9800"; break; // 1024: Radiant Sun
            case 2048: a = "FF416C"; b = "FF4B2B"; break; // 2048: Legendary Sunfire Flame
            case 4096: a = "12C2E9"; b = "C471ED"; break; // 4096: Supernova Violet-Cyan
            case 8192: a = "F12711"; b = "F5AF19"; break; // 8192: Solar Flare
            default: a = "00F260"; b = "0575E6"; break;
        }
        first = ParseColor(a);
        second = ParseColor(b);
    }

    public static UnityEngine.Color TileText(int value, bool contrast = false) {
        if (contrast) return Ink;
        if (value == 64 || value == 128) return ParseColor("181C30");
        if (value == 1024 || value == 2048) return ParseColor("FFFFFF");
        if (value >= 4096) return ParseColor("FFFFFF");
        return ParseColor("FFFFFF");
    }

    public static float Contrast(UnityEngine.Color a, UnityEngine.Color b) {
        float x = Luminance(a), y = Luminance(b);
        return (Mathf.Max(x, y) + .05f) / (Mathf.Min(x, y) + .05f);
    }

    static float Luminance(UnityEngine.Color c) {
        return .2126f * Linear(c.r) + .7152f * Linear(c.g) + .0722f * Linear(c.b);
    }

    static float Linear(float value) {
        return value <= .04045f ? value / 12.92f : Mathf.Pow((value + .055f) / 1.055f, 2.4f);
    }
}
}
