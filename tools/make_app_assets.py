import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import make_assets as M
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.normpath(os.path.join(HERE, "..", "res"))

# soft shadow used behind the clock inside the popup
sh = Image.new("RGBA", (M.S, M.S), (0, 0, 0, 0))
d = ImageDraw.Draw(sh)
inset = 6 * M.SS
off = 10 * M.SS
d.ellipse([M.C - M.R + inset, M.C - M.R + inset + off, M.C + M.R - inset, M.C + M.R - inset + off], fill=(0, 0, 0, 120))
sh = sh.filter(ImageFilter.GaussianBlur(10 * M.SS)).resize((M.SIZE, M.SIZE), Image.LANCZOS)
sh.save(os.path.join(OUT, "shadow.png"))

# app icon
ang = M.clock_angles(10, 9, 36)
comp = Image.new("RGBA", (M.S, M.S), (0, 0, 0, 0))
L = M.PALETTES["light"]
comp.alpha_composite(M.face_layer(L, with_shadow=False))
for kind, a in zip(("hour", "minute", "second"), ang):
    comp.alpha_composite(M.rotated(M.hand_layer(kind, L), a))
box = (int(M.C - M.R), int(M.C - M.R), int(M.C + M.R), int(M.C + M.R))
icon = comp.crop(box).resize((256, 256), Image.LANCZOS)
icon.save(os.path.join(OUT, "app.ico"), sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
print("ok")
