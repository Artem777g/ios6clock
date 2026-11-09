from PIL import Image, ImageDraw, ImageFont
im = Image.open(r"ios6clock\ref\dialog.png").convert("RGB")
d = ImageDraw.Draw(im)
red = (255, 75, 65)
for i in range(4):
    d.rounded_rectangle([214-i, 682-i, 406+i, 738+i], radius=16+i, outline=red, width=1)
f = ImageFont.truetype(r"C:\Windows\Fonts\segoeuib.ttf", 21)
d.text((26, 699), "Нажми сюда →", font=f, fill=red)
im.save(r"ios6clock\ref\dialog_annotated.png")
print("ok")
