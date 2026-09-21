"""
Procedural Vector Icon Generator for MachineIDLogger
Designed to match the clean, professional, high-contrast aesthetic of FaceSoter.
Pure procedural geometric vector rendering with antialiased supersampling.
"""

from pathlib import Path
from PIL import Image, ImageDraw

def generate_icons():
    # 1024x1024 supersampling canvas downscaled with Lanczos for flawless antialiasing
    CANVAS_SIZE = 1024
    s = CANVAS_SIZE / 256.0
    
    img = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # 1. Background: Fluent Blue squircle (matching FaceSoter's signature look)
    margin = int(12 * s)
    radius = int(50 * s)
    
    draw.rounded_rectangle(
        [margin, margin, CANVAS_SIZE - margin, CANVAS_SIZE - margin],
        radius=radius,
        fill=(0, 120, 212, 255),      # Microsoft Fluent Blue #0078D4
        outline=(60, 160, 245, 255),  # Fluent Highlight Blue #3CA0F5
        width=int(4 * s),
    )
    
    # 2. Central Hardware Microchip Body (representing System & Hardware Inspection)
    chip_margin = int(56 * s)
    chip_radius = int(22 * s)
    
    # Darker Navy/Slate chip base inside the blue squircle to create depth and contrast
    draw.rounded_rectangle(
        [chip_margin, chip_margin, CANVAS_SIZE - chip_margin, CANVAS_SIZE - chip_margin],
        radius=chip_radius,
        fill=(10, 25, 47, 255),       # Deep Navy #0A192F
        outline=(255, 255, 255, 255), # Crisp White Border
        width=int(4 * s),
    )
    
    # 3. Microchip Golden Contact Pins (3 on each of the 4 sides)
    pin_w = int(12 * s)
    pin_len = int(18 * s)
    pin_color = (255, 215, 0, 255) # Classic Gold Pin #FFD700
    
    offsets = [int(84 * s), int(128 * s), int(172 * s)]
    for off in offsets:
        # Top pins
        draw.rounded_rectangle([off - pin_w // 2, chip_margin - pin_len, off + pin_w // 2, chip_margin], radius=int(3 * s), fill=pin_color)
        # Bottom pins
        draw.rounded_rectangle([off - pin_w // 2, CANVAS_SIZE - chip_margin, off + pin_w // 2, CANVAS_SIZE - chip_margin + pin_len], radius=int(3 * s), fill=pin_color)
        # Left pins
        draw.rounded_rectangle([chip_margin - pin_len, off - pin_w // 2, chip_margin, off + pin_w // 2], radius=int(3 * s), fill=pin_color)
        # Right pins
        draw.rounded_rectangle([CANVAS_SIZE - chip_margin, off - pin_w // 2, CANVAS_SIZE - chip_margin + pin_len, off + pin_w // 2], radius=int(3 * s), fill=pin_color)
        
    # 4. Diagnostic Corner Brackets (like FaceSoter's corner brackets, representing scanning/detection)
    bracket_margin = int(32 * s)
    bw = int(20 * s)
    lw = int(4 * s)
    bracket_color = (130, 210, 255, 255) # Light Cyan/Sky
    
    # Top-Left
    draw.line([(bracket_margin, bracket_margin), (bracket_margin + bw, bracket_margin)], fill=bracket_color, width=lw)
    draw.line([(bracket_margin, bracket_margin), (bracket_margin, bracket_margin + bw)], fill=bracket_color, width=lw)
    # Top-Right
    draw.line([(CANVAS_SIZE - bracket_margin, bracket_margin), (CANVAS_SIZE - bracket_margin - bw, bracket_margin)], fill=bracket_color, width=lw)
    draw.line([(CANVAS_SIZE - bracket_margin, bracket_margin), (CANVAS_SIZE - bracket_margin, bracket_margin + bw)], fill=bracket_color, width=lw)
    # Bottom-Left
    draw.line([(bracket_margin, CANVAS_SIZE - bracket_margin), (bracket_margin + bw, CANVAS_SIZE - bracket_margin)], fill=bracket_color, width=lw)
    draw.line([(bracket_margin, CANVAS_SIZE - bracket_margin), (bracket_margin, CANVAS_SIZE - bracket_margin - bw)], fill=bracket_color, width=lw)
    # Bottom-Right
    draw.line([(CANVAS_SIZE - bracket_margin, CANVAS_SIZE - bracket_margin), (CANVAS_SIZE - bracket_margin - bw, CANVAS_SIZE - bracket_margin)], fill=bracket_color, width=lw)
    draw.line([(CANVAS_SIZE - bracket_margin, CANVAS_SIZE - bracket_margin), (CANVAS_SIZE - bracket_margin, CANVAS_SIZE - bracket_margin - bw)], fill=bracket_color, width=lw)
    
    # 5. Core Machine ID Emblem: Stylized Cryptographic ID Badge / Keyhole & Shield in pure white
    cx, cy = CANVAS_SIZE // 2, CANVAS_SIZE // 2
    
    # Identity Shield
    sw = int(44 * s)
    st = cy - int(44 * s)
    sb = cy + int(48 * s)
    sm = cy + int(10 * s)
    
    shield_pts = [
        (cx - sw, st),
        (cx + sw, st),
        (cx + sw, sm),
        (cx, sb),
        (cx - sw, sm),
    ]
    draw.polygon(shield_pts, fill=(255, 255, 255, 255))
    
    # Keyhole cutout in shield (Navy blue)
    key_r = int(14 * s)
    key_cy = cy - int(10 * s)
    draw.ellipse([cx - key_r, key_cy - key_r, cx + key_r, key_cy + key_r], fill=(10, 25, 47, 255))
    
    notch_w = int(8 * s)
    notch_t = key_cy + int(4 * s)
    notch_b = cy + int(24 * s)
    draw.polygon([
        (cx - notch_w, notch_b),
        (cx + notch_w, notch_b),
        (cx + int(4 * s), notch_t),
        (cx - int(4 * s), notch_t),
    ], fill=(10, 25, 47, 255))

    # Downsample using Lanczos
    icon_256 = img.resize((256, 256), Image.Resampling.LANCZOS)
    
    # Target directory
    root_dir = Path(__file__).resolve().parent.parent
    assets_dir = root_dir / "src" / "MachineIDLogger" / "Assets"
    assets_dir.mkdir(parents=True, exist_ok=True)
    
    # Standard PNG assets
    icon_256.save(assets_dir / "AppIcon.png", "PNG")
    icon_256.save(assets_dir / "Square150x150Logo.scale-200.png", "PNG")
    
    icon_88 = img.resize((88, 88), Image.Resampling.LANCZOS)
    icon_88.save(assets_dir / "Square44x44Logo.scale-200.png", "PNG")
    
    icon_48 = img.resize((48, 48), Image.Resampling.LANCZOS)
    icon_48.save(assets_dir / "Square44x44Logo.targetsize-48_altform-lightunplated.png", "PNG")
    
    icon_24 = img.resize((24, 24), Image.Resampling.LANCZOS)
    icon_24.save(assets_dir / "Square44x44Logo.targetsize-24_altform-unplated.png", "PNG")
    
    icon_50 = img.resize((50, 50), Image.Resampling.LANCZOS)
    icon_50.save(assets_dir / "StoreLogo.png", "PNG")
    
    # Multi-size Windows ICO with all standard desktop and Explorer resolutions
    ico_path = assets_dir / "AppIcon.ico"
    icon_sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (24, 24), (16, 16)]
    icon_256.save(ico_path, format="ICO", sizes=icon_sizes)
    
    print(f"Generated FaceSoter-style clean vector icons in {assets_dir}")

if __name__ == "__main__":
    generate_icons()
