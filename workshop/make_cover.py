from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
SIZE = 1024
image = Image.new("RGB", (SIZE, SIZE), "white")
icon = Image.open(ROOT / "assets" / "guide1.png").convert("RGBA")
icon = icon.crop(icon.getbbox())
# Integer nearest-neighbor scaling preserves the original pixel art.
icon = icon.resize((icon.width * 18, icon.height * 18), Image.Resampling.NEAREST)
image.paste(icon, ((SIZE - icon.width) // 2, 130), icon)
draw = ImageDraw.Draw(image)
title = "Xice的体验优化"
font = ImageFont.truetype("C:/Windows/Fonts/msyhbd.ttc", 78)
box = draw.textbbox((0, 0), title, font=font)
draw.text(((SIZE - (box[2] - box[0])) // 2 - box[0], 790 - box[1]), title, font=font, fill="#17151e")
image.save(ROOT / "preview.png")
