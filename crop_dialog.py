from PIL import Image
src = r"C:\Users\Artem\.dsh\attachments\v1\objects\2e\2eeda238130eefddbe8a64bfdccf15be96704337330dfc4054aca76a268c204c"
im = Image.open(src).convert("RGB")
print(im.size)
im.crop((960, 290, 1610, 1060)).save(r"ios6clock\ref\dialog.png")
