"""iOS 6 style clock (Mondaine / SBB) — face + hand layers.

Drawn at 4x and downsampled for clean anti-aliasing.  All geometry is
expressed as a fraction of the face radius R (like the real iOS 6 clock).
Two palettes are produced: light and dark.
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "res"))
os.makedirs(OUT, exist_ok=True)

SS = 4
SIZE = 1024
S = SIZE * SS
C = S / 2
R = 470 * SS

PALETTES = {
    "light": dict(
        face=(255, 255, 255),
        mark=(17, 17, 17),
        rim=(228, 228, 228),
        vignette=30,
        hand=(17, 17, 17),
        red=(208, 24, 24),
        shine=(255, 235, 235),
        shadow=85,
    ),
    "dark": dict(
        face=(13, 13, 15),
        mark=(245, 245, 247),
        rim=(52, 52, 56),
        vignette=0,
        hand=(245, 245, 247),
        red=(255, 59, 48),
        shine=(255, 255, 255),
        shadow=170,
    ),
}


def bar(draw, angle_deg, r_in, r_out, width, fill):
    th = math.radians(angle_deg)
    ux, uy = math.sin(th), -math.cos(th)
    px, py = -uy, ux
    hw = width / 2
    pts = [
        (C + ux * r_in + px * hw, C + uy * r_in + py * hw),
        (C + ux * r_in - px * hw, C + uy * r_in - py * hw),
        (C + ux * r_out - px * hw, C + uy * r_out - py * hw),
        (C + ux * r_out + px * hw, C + uy * r_out + py * hw),
    ]
    draw.polygon(pts, fill=fill)


def hand_shape(draw, tip, tail, width, fill, radius_frac=0.16):
    hw = width / 2
    r = width * radius_frac
    draw.rounded_rectangle([C - hw, C - tip, C + hw, C + tail], radius=r, fill=fill)


def face_layer(pal, with_shadow=True):
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    if with_shadow:
        sh = Image.new("RGBA", (S, S), (0, 0, 0, 0))
        sd = ImageDraw.Draw(sh)
        off = 5 * SS
        sd.ellipse([C - R, C - R + off, C + R, C + R + off],
                   fill=(0, 0, 0, pal["shadow"]))
        img.alpha_composite(sh.filter(ImageFilter.GaussianBlur(12 * SS)))

    d.ellipse([C - R, C - R, C + R, C + R], fill=pal["face"] + (255,))

    if pal["vignette"]:
        yy, xx = np.mgrid[0:S, 0:S]
        rr = np.sqrt((xx - C) ** 2 + (yy - C) ** 2) / R
        a = np.clip((rr - 0.84) / 0.16, 0, 1) ** 2 * pal["vignette"]
        a[rr > 0.999] = 0
        vig = np.zeros((S, S, 4), dtype=np.uint8)
        vig[..., 3] = a.astype(np.uint8)
        img.alpha_composite(Image.fromarray(vig, "RGBA").filter(ImageFilter.GaussianBlur(2.5 * SS)))

    d.ellipse([C - R, C - R, C + R, C + R], outline=pal["rim"] + (255,), width=3 * SS)

    tick = pal["mark"] + (255,)
    for i in range(60):
        if i % 5 == 0:
            continue
        bar(d, i * 6, 0.878 * R, 0.966 * R, 0.0135 * R, tick)
    for i in range(12):
        bar(d, i * 30, 0.755 * R, 0.966 * R, 0.074 * R, tick)

    return img


def hand_layer(kind, pal):
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    hand = pal["hand"] + (255,)
    red = pal["red"] + (255,)

    if kind == "hour":
        hand_shape(d, 0.600 * R, 0.280 * R, 0.075 * R, hand)
    elif kind == "minute":
        hand_shape(d, 0.872 * R, 0.280 * R, 0.071 * R, hand)
    elif kind == "second":
        lw = 0.014 * R
        d.rounded_rectangle([C - lw / 2, C - 0.610 * R, C + lw / 2, C + 0.250 * R],
                            radius=lw / 2, fill=red)
        dot = 0.095 * R
        cy = C - 0.610 * R
        d.ellipse([C - dot, cy - dot, C + dot, cy + dot], fill=red)
        hub = 0.026 * R
        d.ellipse([C - hub, C - hub, C + hub, C + hub], fill=red)
        hl = 0.008 * R
        d.ellipse([C - hl - 0.004 * R, C - hl - 0.004 * R,
                   C + hl - 0.004 * R, C + hl - 0.004 * R], fill=pal["shine"] + (235,))
    else:
        raise ValueError(kind)

    return img


def rotated(layer, angle_deg):
    return layer.rotate(-angle_deg, resample=Image.BICUBIC, center=(C, C))


def shadow_of(layer, dx, dy, blur, alpha):
    a = layer.split()[3].point(lambda v: int(v * alpha))
    sh = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    sh.putalpha(a)
    sh = sh.filter(ImageFilter.GaussianBlur(blur))
    out = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    out.paste(sh, (int(dx), int(dy)), sh)
    return out


def clock_angles(h, m, s):
    return ((h % 12) * 30 + m * 0.5 + s * 0.5 / 60, m * 6 + s * 0.1, s * 6)


def compose(pal, angle_h, angle_m, angle_s, size=SIZE, shadow=True, bg=(236, 236, 236)):
    canvas = Image.new("RGBA", (S, S), bg + (255,))
    canvas.alpha_composite(face_layer(pal, with_shadow=shadow))
    for kind, ang in (("hour", angle_h), ("minute", angle_m), ("second", angle_s)):
        layer = rotated(hand_layer(kind, pal), ang)
        if shadow:
            canvas.alpha_composite(shadow_of(layer, 1.2 * SS, 2.4 * SS, 2.2 * SS, 0.22))
        canvas.alpha_composite(layer)
    return canvas.resize((size, size), Image.LANCZOS).convert("RGB")


if __name__ == "__main__":
    angles = clock_angles(10, 9, 36)

    for name, pal in PALETTES.items():
        suffix = "" if name == "light" else "_" + name
        face_layer(pal, with_shadow=False).resize((SIZE, SIZE), Image.LANCZOS).save(
            os.path.join(OUT, "face%s.png" % suffix))
        for kind in ("hour", "minute", "second"):
            hand_layer(kind, pal).resize((SIZE, SIZE), Image.LANCZOS).save(
                os.path.join(OUT, "%s%s.png" % (kind, suffix)))

    compose(PALETTES["light"], *angles).save(os.path.join(OUT, "preview.png"))
    compose(PALETTES["dark"], *angles, bg=(26, 26, 28)).save(os.path.join(OUT, "preview_dark.png"))
    print("done")
