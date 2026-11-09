from PIL import Image, ImageDraw, ImageFont
src = r"C:\Users\Artem\.dsh\attachments\v1\objects\68\6824d248c78be907aca6e20a690fd22652cfd80f2bddc4b995fc56d243817f74"
im = Image.open(src).convert("RGB")
crop = im.crop((540, 860, 945, 1200))
d = ImageDraw.Draw(crop)
red = (255, 80, 70)
d.rounded_rectangle([41, 24, 382, 77], radius=8, outline=red, width=3)
d.rounded_rectangle([37, 265, 381, 305], radius=10, outline=red, width=3)
f = ImageFont.truetype(r"C:\Windows\Fonts\segoeuib.ttf", 26)
d.text((8, 30), "1", font=f, fill=red)
d.text((8, 268), "2", font=f, fill=red)
crop.save(r"ios6clock\ref\commit.png")
print("ok")
