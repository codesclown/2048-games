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
    public static UnityEngine.Color Background => ParseColor("101B2A");    // Deepest midnight obsidian
    public static UnityEngine.Color Surface => ParseColor("1B2C3B");       // Glass card surface
    public static UnityEngine.Color SurfaceElevated => ParseColor("263A4B"); // Active/Elevated card
    public static UnityEngine.Color SurfaceHover => ParseColor("304A5C");   // Highlighted container
    public static UnityEngine.Color Border => ParseColor("3C5363");         // Subtle card stroke
    public static UnityEngine.Color BorderLight => ParseColor("7E9AA8");    // Glowing active stroke
    public static UnityEngine.Color BorderElevated => ParseColor("567485"); // Active elevated stroke
    public static UnityEngine.Color DialogBackground => ParseColor("1B2C3B"); // Modal dialog body
    public static UnityEngine.Color GoldBorder => ParseColor("EDC487");     // Radiant gold modal stroke
    
    public static UnityEngine.Color Text => ParseColor("F4F0E8");           // Crisp pure white
    public static UnityEngine.Color TextSecondary => ParseColor("DFE6E2");  // High-legibility light silver
    public static UnityEngine.Color Muted => ParseColor("A8B9C7");          // Elegant slate/lavender
    public static UnityEngine.Color Subdued => ParseColor("748C9D");        // Subtle hints/dividers

    public static UnityEngine.Color Gold => ParseColor("EDC487");           // Royal gold gradient top
    public static UnityEngine.Color GoldDark => ParseColor("CFA064");       // Rich amber gradient bottom
    public static UnityEngine.Color Accent => ParseColor("EDC487");         // Gold brand accent
    public static UnityEngine.Color AccentGlow => ParseColor("E5BB7A44");    // Glow highlight
    public static UnityEngine.Color Ink => ParseColor("101722");            // Deep contrast text on gold

    public static UnityEngine.Color Cyan => ParseColor("9DBBB0");           // Undo / Action blue
    public static UnityEngine.Color Purple => ParseColor("B4A38E");         // Swap / Magic violet
    public static UnityEngine.Color Coral => ParseColor("D38F79");          // Delete / Warning red
    public static UnityEngine.Color Emerald => ParseColor("9DBBB0");        // Victory green

    static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    // Cached white material is tinted by each surface: bevels and rims survive state changes.
    public static Sprite SurfaceSprite(bool primary = false) {
        string key = primary ? "surface-primary-v2" : "surface-v2";
        if (spriteCache.TryGetValue(key, out var cached) && cached != null) return cached;
        const int size = 96, radius = 18;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear;
        float half = size * .5f;
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
            float px = x + .5f - half, py = y + .5f - half;
            float qx = Mathf.Abs(px) - (half - radius), qy = Mathf.Abs(py) - (half - radius);
            float distance = Mathf.Sqrt(Mathf.Max(qx, 0) * Mathf.Max(qx, 0) + Mathf.Max(qy, 0) * Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
            float alpha = Mathf.Clamp01(.5f - distance);
            float tone = Mathf.Lerp(primary ? .86f : .91f, 1f, (float)y / (size - 1));
            if (distance > -1.4f) tone = py > 0 ? 1f : (primary ? .72f : .68f);
            else if (primary && py > half - 3) tone = 1f;
            texture.SetPixel(x, y, new UnityEngine.Color(tone, tone, tone, alpha));
        }
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        spriteCache[key] = sprite; return sprite;
    }

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
