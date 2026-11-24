from pathlib import Path
from PIL import Image
OUT=Path(__file__).parent
frames=[Image.open(p).convert('RGB') for p in sorted((OUT/'motion_frames').glob('cape_*.png'))]
assert len(frames)==24,len(frames)
frames[0].save(OUT/'Cape_Breeze_preview.webp',save_all=True,append_images=frames[1:],duration=[167,167,166]*8,loop=0,quality=87,method=6)
print('ANIMATED_PREVIEW_SAVED',OUT/'Cape_Breeze_preview.webp')
