"""Draws SnakeWood's icon: a log hovering over a glowing pad.

Usage: python3 tools/make_icon.py

Writes thunderstore/SnakeWood/icon.png (the package icon) and SnakeWood/icon.png (embedded in
the DLL as the build-menu icon). 256x256, drawn at 4x and downsampled for smooth edges.
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

S = 4
SIZE = 256
ROOT = Path(__file__).resolve().parent.parent

OUTLINE = (28, 22, 16, 255)
BARK = (110, 74, 42, 255)
BARK_DARK = (78, 52, 30, 255)
WOOD = (214, 170, 110, 255)
RING = (170, 124, 72, 255)
GLOW = (90, 210, 255, 255)


def p(*xy):
    return [v * S for v in xy]


def main():
    img = Image.new("RGBA", (SIZE * S, SIZE * S), (0, 0, 0, 0))

    # Hover glow under the log.
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    g = ImageDraw.Draw(glow)
    g.ellipse(p(40, 186, 216, 226), fill=(90, 210, 255, 170))
    glow = glow.filter(ImageFilter.GaussianBlur(10 * S))
    img.alpha_composite(glow)

    d = ImageDraw.Draw(img)
    # Speed lines behind.
    for y, x0 in ((92, 6), (124, 0), (156, 10)):
        d.rounded_rectangle(p(x0, y, x0 + 44, y + 8), radius=4 * S, fill=GLOW)

    # Log body (side view), end cap on the right.
    d.rounded_rectangle(p(46, 84, 206, 172), radius=10 * S, fill=BARK, outline=OUTLINE, width=6 * S)
    for y in (106, 128, 150):
        d.line(p(62, y, 188, y), fill=BARK_DARK, width=4 * S)
    d.ellipse(p(170, 80, 234, 176), fill=WOOD, outline=OUTLINE, width=6 * S)
    d.ellipse(p(186, 102, 218, 154), outline=RING, width=4 * S)
    d.ellipse(p(197, 120, 207, 136), fill=RING)

    # Seat strap.
    d.rectangle(p(96, 80, 118, 176), fill=(60, 40, 26, 255), outline=OUTLINE, width=4 * S)

    img = img.resize((SIZE, SIZE), Image.LANCZOS)
    for out in (ROOT / "thunderstore" / "SnakeWood" / "icon.png", ROOT / "SnakeWood" / "icon.png"):
        img.save(out)
        print(f"Wrote {out}")


if __name__ == "__main__":
    main()
