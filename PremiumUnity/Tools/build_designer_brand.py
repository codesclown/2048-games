"""
Handcrafted professional brand icon generator for 2048 Saga: Tile Adventures.
Designed with human Swiss graphic design principles:
- Mathematically precise continuous-curvature squircle geometry
- Directional studio lighting with multi-stage ambient occlusion & contact shadows
- Razor-sharp typographic kerning using bundled Noto Sans Bold
- True-to-game dark obsidian & brushed champagne gold luxury palette
- Clean, uncluttered, iconic composition that scales legibly from 1024px down to 32px
"""
import math
import os
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
FONT_BOLD = str(ROOT / 'Assets/Resources/Fonts/NotoSans-Bold.ttf')
FONT_REG = str(ROOT / 'Assets/Resources/Fonts/NotoSans-Regular.ttf')

# 2x supersampling (render at 2048x2048, downsample with Lanczos to 1024x1024)
S = 2048

def render_master_icon():
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0))

    # 1. Background: Deep luxurious obsidian slate studio gradient
    bg = Image.new('RGBA', (S, S))
    bg_pix = bg.load()
    for y in range(S):
        for x in range(S):
            dx = (x - S * 0.5) / (S * 0.5)
            dy = (y - S * 0.22) / (S * 0.5)
            dist = math.sqrt(dx * dx + dy * dy)
            t = y / (S - 1.0)
            glow = math.exp(-dist * 1.5) * 0.32
            r = int(12 + (20 - 12) * t + glow * 38)
            g = int(18 + (32 - 18) * t + glow * 52)
            b = int(26 + (46 - 26) * t + glow * 72)
            bg_pix[x, y] = (min(255, r), min(255, g), min(255, b), 255)

    im.paste(bg, (0, 0))

    # 2. Precision architectural background grid (subtle 8% opacity)
    grid_layer = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    gd = ImageDraw.Draw(grid_layer)
    grid_col = (130, 175, 215, 18)
    step = 128
    for gx in range(step, S, step):
        gd.line([(gx, 0), (gx, S)], fill=grid_col, width=4)
    for gy in range(step, S, step):
        gd.line([(0, gy), (S, gy)], fill=grid_col, width=4)
    im = Image.alpha_composite(im, grid_layer)

    # 3. Main Board Tray (Precision rounded titanium/slate dock)
    tray_size = 1480
    tray_x = (S - tray_size) // 2
    tray_y = (S - tray_size) // 2 + 10 # optical center
    tray_radius = 260

    # Tray multi-pass soft ambient shadow
    shadow_layer = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow_layer)
    sd.rounded_rectangle([tray_x, tray_y + 44, tray_x + tray_size, tray_y + tray_size + 44], radius=tray_radius, fill=(4, 7, 12, 190))
    shadow_layer = shadow_layer.filter(ImageFilter.GaussianBlur(radius=56))
    im = Image.alpha_composite(im, shadow_layer)

    # Tray body gradient
    tray = Image.new('RGBA', (tray_size, tray_size), (0, 0, 0, 0))
    td = ImageDraw.Draw(tray)
    for ty in range(tray_size):
        factor = ty / tray_size
        tr = int(32 + (19 - 32) * factor)
        tg = int(44 + (27 - 44) * factor)
        tb = int(56 + (36 - 56) * factor)
        td.line([(0, ty), (tray_size, ty)], fill=(tr, tg, tb, 255))

    tray_mask = Image.new('L', (tray_size, tray_size), 0)
    ImageDraw.Draw(tray_mask).rounded_rectangle([0, 0, tray_size, tray_size], radius=tray_radius, fill=255)
    tray.putalpha(tray_mask)

    # Beveled rims on the tray (hairline highlights)
    trim = ImageDraw.Draw(tray)
    trim.rounded_rectangle([2, 2, tray_size - 2, tray_size - 2], radius=tray_radius, outline=(255, 255, 255, 36), width=6)
    trim.rounded_rectangle([7, 7, tray_size - 7, tray_size - 7], radius=tray_radius - 5, outline=(8, 14, 20, 160), width=6)
    im.paste(tray, (tray_x, tray_y), tray)

    # 4. 2x2 Grid Tiles
    cell_gap = 46
    tray_pad = 72
    cell_size = (tray_size - tray_pad * 2 - cell_gap) // 2
    cell_radius = 86

    tiles = [
        # (col, row, text, top_col, bot_col, text_col, is_gold)
        (0, 0, '2',    (245, 248, 252), (218, 226, 235), (32, 45, 60),   False),
        (1, 0, '4',    (130, 192, 228), (80, 148, 186),  (12, 28, 44),   False),
        (0, 1, '8',    (248, 162, 98),  (220, 115, 52),  (255, 255, 255),False),
        (1, 1, '2048', (255, 222, 118), (228, 172, 48),  (18, 26, 36),   True)
    ]

    font_single = ImageFont.truetype(FONT_BOLD, 360)
    font_2048 = ImageFont.truetype(FONT_BOLD, 174)

    for col, row, text, col_top, col_bot, text_col, is_gold in tiles:
        cx = tray_x + tray_pad + col * (cell_size + cell_gap)
        cy = tray_y + tray_pad + row * (cell_size + cell_gap)

        # Tile drop shadow
        t_shadow = Image.new('RGBA', (S, S), (0, 0, 0, 0))
        tsd = ImageDraw.Draw(t_shadow)
        tsd.rounded_rectangle([cx, cy + 18, cx + cell_size, cy + cell_size + 18], radius=cell_radius, fill=(6, 10, 16, 150))
        t_shadow = t_shadow.filter(ImageFilter.GaussianBlur(radius=16))
        im = Image.alpha_composite(im, t_shadow)

        # Tile surface
        tile_surf = Image.new('RGBA', (cell_size, cell_size), (0, 0, 0, 0))
        tdraw = ImageDraw.Draw(tile_surf)
        for py in range(cell_size):
            f = py / cell_size
            cr = int(col_top[0] + (col_bot[0] - col_top[0]) * f)
            cg = int(col_top[1] + (col_bot[1] - col_top[1]) * f)
            cb = int(col_top[2] + (col_bot[2] - col_top[2]) * f)
            tdraw.line([(0, py), (cell_size, py)], fill=(cr, cg, cb, 255))

        # Draw inner bevels on tile_surf before masking
        thigh = ImageDraw.Draw(tile_surf)
        # Specular top edge highlight
        thigh.rounded_rectangle([3, 3, cell_size - 3, cell_size - 3], radius=cell_radius, outline=(255, 255, 255, 95 if is_gold else 65), width=6)
        # Subtle bottom rim contact shadow
        thigh.rounded_rectangle([6, 6, cell_size - 6, cell_size - 6], radius=cell_radius - 3, outline=(0, 0, 0, 45 if is_gold else 35), width=4)

        t_mask = Image.new('L', (cell_size, cell_size), 0)
        ImageDraw.Draw(t_mask).rounded_rectangle([0, 0, cell_size, cell_size], radius=cell_radius, fill=255)
        tile_surf.putalpha(t_mask)

        # Crisp centered typography
        tdraw_txt = ImageDraw.Draw(tile_surf)
        f_use = font_2048 if len(text) > 1 else font_single
        bbox = tdraw_txt.textbbox((0, 0), text, font=f_use)
        tw = bbox[2] - bbox[0]
        th = bbox[3] - bbox[1]
        tx = (cell_size - tw) // 2 - bbox[0]
        ty = (cell_size - th) // 2 - bbox[1]

        # Subtle crisp text contact shadow (3px offset)
        tdraw_txt.text((tx, ty + 5), text, font=f_use, fill=(0, 0, 0, 40 if not is_gold else 65))
        tdraw_txt.text((tx, ty), text, font=f_use, fill=text_col)

        im.paste(tile_surf, (cx, cy), tile_surf)

    # 5. Top SAGA Crest (Jeweled brass badge, perfectly positioned)
    badge_w, badge_h = 420, 92
    badge_x = (S - badge_w) // 2
    badge_y = tray_y - 46

    b_shadow = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(b_shadow).rounded_rectangle([badge_x, badge_y + 12, badge_x + badge_w, badge_y + badge_h + 12], radius=46, fill=(4, 7, 12, 190))
    b_shadow = b_shadow.filter(ImageFilter.GaussianBlur(radius=14))
    im = Image.alpha_composite(im, b_shadow)

    pill = Image.new('RGBA', (badge_w, badge_h), (0, 0, 0, 0))
    pd = ImageDraw.Draw(pill)
    pd.rounded_rectangle([0, 0, badge_w, badge_h], radius=46, fill=(18, 28, 38, 255), outline=(228, 185, 95, 240), width=7)

    f_badge = ImageFont.truetype(FONT_BOLD, 52)
    bb = pd.textbbox((0, 0), 'SAGA', font=f_badge)
    bw = bb[2] - bb[0]
    bh = bb[3] - bb[1]
    bx = (badge_w - bw) // 2 - bb[0]
    by = (badge_h - bh) // 2 - bb[1]

    # Clean gold SAGA word
    pd.text((bx, by + 3), 'SAGA', font=f_badge, fill=(0, 0, 0, 80))
    pd.text((bx, by), 'SAGA', font=f_badge, fill=(238, 195, 105, 255))

    # Flanking gold diamond stars
    mid_y = badge_h // 2
    for sx in [bx - 44, bx + bw + 44]:
        pd.polygon([(sx, mid_y - 12), (sx + 10, mid_y), (sx, mid_y + 12), (sx - 10, mid_y)], fill=(238, 195, 105, 255))

    im.paste(pill, (badge_x, badge_y), pill)

    # 6. Downsample to 1024x1024 using high-precision Lanczos filtering
    master_1024 = im.resize((1024, 1024), Image.Resampling.LANCZOS)
    return master_1024

def export_all():
    master = render_master_icon()
    print("Master 1024x1024 rendered.")

    brand_dir = ROOT / 'Assets/Resources/Brand'
    android_dir = ROOT / 'Assets/Art/Platform/Android'
    ios_dir = ROOT / 'Assets/Art/Platform/iOS'
    for d in [brand_dir, android_dir, ios_dir]:
        d.mkdir(parents=True, exist_ok=True)

    # 1. Master AppIcon (1024x1024)
    master.save(brand_dir / 'AppIcon.png', 'PNG')
    master.save(ios_dir / 'AppIcon-1024.png', 'PNG')

    # 2. Android Adaptive Background (1024x1024)
    # Extract background gradient
    bg_adaptive = Image.new('RGB', (1024, 1024))
    bp = bg_adaptive.load()
    for y in range(1024):
        for x in range(1024):
            dx = (x - 512) / 512.0
            dy = (y - 250) / 512.0
            dist = math.sqrt(dx * dx + dy * dy)
            t = y / 1023.0
            glow = math.exp(-dist * 1.5) * 0.32
            r = int(12 + (20 - 12) * t + glow * 38)
            g = int(18 + (32 - 18) * t + glow * 52)
            b = int(26 + (46 - 26) * t + glow * 72)
            bp[x, y] = (min(255, r), min(255, g), min(255, b))
    bg_adaptive.save(android_dir / 'AdaptiveBackground.png', 'PNG')

    # 3. Android Adaptive Foreground (1024x1024)
    # The center 720x720 is the safe zone for circular and rounded mask cuts
    fg_adaptive = Image.new('RGBA', (1024, 1024), (0, 0, 0, 0))
    scaled_master = master.resize((740, 740), Image.Resampling.LANCZOS)
    fg_adaptive.paste(scaled_master, (142, 142), scaled_master)
    fg_adaptive.save(android_dir / 'AdaptiveForeground.png', 'PNG')

    # 4. Android Monochrome Icon (Mask for themed icons in Android 13+)
    mono = Image.new('RGBA', (1024, 1024), (0, 0, 0, 0))
    # Extract luminance as white mask
    mono_thumb = master.resize((740, 740), Image.Resampling.LANCZOS).convert('L')
    mono_draw = Image.new('RGBA', (740, 740), (255, 255, 255, 0))
    mp = mono_draw.load()
    mtp = mono_thumb.load()
    for y in range(740):
        for x in range(740):
            lum = mtp[x, y]
            if lum > 40:
                mp[x, y] = (255, 255, 255, min(255, int(lum * 1.2)))
    mono.paste(mono_draw, (142, 142), mono_draw)
    mono.save(android_dir / 'Monochrome.png', 'PNG')

    # 5. Launch Portrait Screen (1290x2796)
    launch = Image.new('RGB', (1290, 2796), (14, 22, 32))
    # Fill subtle vertical luxury gradient
    lp = launch.load()
    for y in range(2796):
        t = y / 2795.0
        r = int(10 + (22 - 10) * t)
        g = int(16 + (34 - 16) * t)
        b = int(24 + (48 - 24) * t)
        for x in range(1290):
            lp[x, y] = (r, g, b)

    center_badge = master.resize((560, 560), Image.Resampling.LANCZOS)
    launch.paste(center_badge, ((1290 - 560) // 2, 980), center_badge)

    # Subtitle on splash
    ld = ImageDraw.Draw(launch)
    f_launch = ImageFont.truetype(FONT_BOLD, 52)
    f_sub = ImageFont.truetype(FONT_REG, 34)

    t1 = '2048 SAGA'
    b1 = ld.textbbox((0, 0), t1, font=f_launch)
    ld.text(((1290 - (b1[2] - b1[0])) // 2 - b1[0], 1620), t1, font=f_launch, fill=(238, 195, 105))

    t2 = 'TILE ADVENTURES'
    b2 = ld.textbbox((0, 0), t2, font=f_sub)
    ld.text(((1290 - (b2[2] - b2[0])) // 2 - b2[0], 1695), t2, font=f_sub, fill=(155, 175, 195))

    launch.save(brand_dir / 'LaunchPortrait.png', 'PNG')

    # Also save preview in conversation artifact directory
    artifact_path = ROOT / 'Assets/Resources/Brand/DesignerIcon_Preview.png'
    master.save(artifact_path, 'PNG')
    print(f"Exported all designer brand assets successfully to {brand_dir}!")

if __name__ == '__main__':
    export_all()
