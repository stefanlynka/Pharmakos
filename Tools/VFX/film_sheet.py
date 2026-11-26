"""Contact sheet of CueTestBench.Film frames (Claude/Cues/Shots/film_NN.png), cropped to a screen region, numbered.
    python Tools/VFX/film_sheet.py <name> x0 y0 x1 y1 [cols]   -> Claude/Cues/Shots/<name>.png
"""
import glob, sys
from PIL import Image, ImageDraw
name = sys.argv[1]; box = tuple(int(v) for v in sys.argv[2:6]); cols = int(sys.argv[6]) if len(sys.argv) > 6 else 6
files = sorted(glob.glob('Claude/Cues/Shots/film_*.png'))
ims = [Image.open(f).convert('RGB').crop(box) for f in files]
w, h = ims[0].size
scale = min(1.0, 1800 / (w * cols))
tw, th = int(w * scale), int(h * scale)
rows = (len(ims) + cols - 1) // cols
sheet = Image.new('RGB', (tw * cols, (th + 14) * rows), (0, 0, 0))
d = ImageDraw.Draw(sheet)
for i, im in enumerate(ims):
    x, y = (i % cols) * tw, (i // cols) * (th + 14)
    sheet.paste(im.resize((tw, th)), (x, y + 14)); d.text((x + 2, y + 1), files[i][-6:-4], fill=(255, 220, 150))
sheet.save('Claude/Cues/Shots/%s.png' % name); print(sheet.size)
