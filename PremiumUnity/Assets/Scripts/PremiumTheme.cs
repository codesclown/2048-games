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

    // Ink, parchment and amber: one restrained palette across every native screen.
    public static UnityEngine.Color Background => ParseColor("192327");    // Deepest midnight obsidian
    public static UnityEngine.Color Surface => ParseColor("223035");       // Glass card surface
    public static UnityEngine.Color SurfaceElevated => ParseColor("2B3A3F"); // Active/Elevated card
    public static UnityEngine.Color SurfaceHover => ParseColor("35474C");   // Highlighted container
    public static UnityEngine.Color Border => ParseColor("3A4B50");         // Subtle card stroke
    public static UnityEngine.Color BorderLight => ParseColor("667A7C");    // Glowing active stroke
    public static UnityEngine.Color BorderElevated => ParseColor("52676A"); // Active elevated stroke
    public static UnityEngine.Color DialogBackground => ParseColor("223035"); // Modal dialog body
    public static UnityEngine.Color GoldBorder => ParseColor("E5BB7A");     // Radiant gold modal stroke
    
    public static UnityEngine.Color Text => ParseColor("F4F0E8");           // Crisp pure white
    public static UnityEngine.Color TextSecondary => ParseColor("DFE6E2");  // High-legibility light silver
    public static UnityEngine.Color Muted => ParseColor("B1BEBA");          // Elegant slate/lavender
    public static UnityEngine.Color Subdued => ParseColor("82938F");        // Subtle hints/dividers

    public static UnityEngine.Color Gold => ParseColor("E5BB7A");           // Royal gold gradient top
    public static UnityEngine.Color GoldDark => ParseColor("CFA064");       // Rich amber gradient bottom
    public static UnityEngine.Color Accent => ParseColor("E5BB7A");         // Gold brand accent
    public static UnityEngine.Color AccentGlow => ParseColor("E5BB7A44");    // Glow highlight
    public static UnityEngine.Color Ink => ParseColor("080A12");            // Deep contrast text on gold

    public static UnityEngine.Color Cyan => ParseColor("9DBBB0");           // Undo / Action blue
    public static UnityEngine.Color Purple => ParseColor("B4A38E");         // Swap / Magic violet
    public static UnityEngine.Color Coral => ParseColor("D38F79");          // Delete / Warning red
    public static UnityEngine.Color Emerald => ParseColor("9DBBB0");        // Victory green

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

                float borderFactor = borderWidth > 0 ? Mathf.Clamp01((dist - (r - borderWidth)) / borderWidth) : 0f;
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
        string tone;
        switch (value) {
            case 0: tone = "314247"; break;
            case 2: tone = "F0EADF"; break;
            case 4: tone = "C7D7C5"; break;
            case 8: tone = "D3BC96"; break;
            case 16: tone = "E4B870"; break;
            case 32: tone = "D99B83"; break;
            case 64: tone = "B9684F"; break;
            case 128: tone = "A6536B"; break;
            case 256: tone = "785777"; break;
            case 512: tone = "476D8D"; break;
            case 1024: tone = "347C77"; break;
            case 2048: tone = "E7BD71"; break;
            case 4096: tone = "514B79"; break;
            case 8192: tone = "284D69"; break;
            default:
                // Keep-going tiles retain a distinct colour beyond the named targets.
                float exponent = Mathf.Log(Mathf.Max(2, value), 2f);
                first = second = UnityEngine.Color.HSVToRGB(Mathf.Repeat(exponent * .61803398875f, 1f), .46f, .66f);
                return;
        }
        first = second = ParseColor(tone);
    }

    public static UnityEngine.Color TileText(int value, bool contrast = false) {
        TileColors(value, out var first, out var second);
        var lightText = contrast ? UnityEngine.Color.white : Text;
        var darkText = contrast ? UnityEngine.Color.black : Ink;
        float light = Mathf.Min(Contrast(lightText, first), Contrast(lightText, second));
        float dark = Mathf.Min(Contrast(darkText, first), Contrast(darkText, second));
        if (Mathf.Max(dark, light) < 4.5f) {
            lightText = UnityEngine.Color.white;
            darkText = UnityEngine.Color.black;
            light = Contrast(lightText, first);
            dark = Contrast(darkText, first);
        }
        return dark >= light ? darkText : lightText;
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
