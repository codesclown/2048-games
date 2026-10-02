"""Rebuild original geometric Unity brand assets with bundled OFL Noto Sans. Requires Pillow."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, hashlib
ROOT=Path(__file__).resolve().parents[1]
FONT=ROOT/'Assets/Resources/Fonts/NotoSans-Bold.ttf'
NAVY='#0B1627';GOLD='#EAC780'

def mark(size,monochrome=False):
    image=Image.new('RGBA',(size,size))
    d=ImageDraw.Draw(image)
    gap=round(size*.055);cell=(size-gap)//2
    colors=['#7189B2','#8C91CB','#BBA0CD',GOLD]
    for i,value in enumerate('2048'):
        x=(i%2)*(cell+gap);y=(i//2)*(cell+gap)
        d.rounded_rectangle((x,y,x+cell-1,y+cell-1),radius=round(cell*.19),fill='white' if monochrome else colors[i])
        if not monochrome:
            f=ImageFont.truetype(str(FONT),round(cell*.62))
            box=d.textbbox((0,0),value,font=f)
            d.text((x+(cell-(box[2]-box[0]))/2-box[0],y+(cell-(box[3]-box[1]))/2-box[1]),value,font=f,fill=NAVY)
    return image

def background(size):
    image=Image.new('RGB',(size,size));p=image.load()
    for y in range(size):
        for x in range(size):
            t=(x+size-y)/(2*size)
            p[x,y]=tuple(round(a+(b-a)*t) for a,b in zip((9,19,33),(29,47,72)))
    return image

brand=ROOT/'Assets/Resources/Brand';android=ROOT/'Assets/Art/Platform/Android';ios=ROOT/'Assets/Art/Platform/iOS';source=ROOT/'Assets/Art/Source'
for folder in (brand,android,ios,source):folder.mkdir(parents=True,exist_ok=True)
mark(512).save(brand/'Mark.png')
app=background(1024);app.paste(mark(620),(202,202),mark(620));app.save(brand/'AppIcon.png')
app.save(ios/'AppIcon-1024.png')
fg=Image.new('RGBA',(1024,1024));m=mark(466);fg.paste(m,(279,279),m);fg.save(android/'AdaptiveForeground.png')
background(1024).save(android/'AdaptiveBackground.png')
mono=Image.new('RGBA',(1024,1024));m=mark(466,True);mono.paste(m,(279,279),m);mono.save(android/'Monochrome.png')
# Neutral launch artwork complements the native safe-area loading UI; no pre-rounded mask.
launch=Image.new('RGB',(1290,2796),NAVY);launch.paste(mark(420),(435,1020),mark(420));d=ImageDraw.Draw(launch);f=ImageFont.truetype(str(FONT),104)
d.text((645,1538),'2048',font=f,fill=GOLD,anchor='mm');launch.save(brand/'LaunchPortrait.png')
# Inspectable vector source; raster exports above use the bundled font, not system fonts.
source.joinpath('mark.svg').write_text('''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><title>2048 Premium original tile monogram</title><rect width="242" height="242" rx="46" fill="#7189B2"/><rect x="270" width="242" height="242" rx="46" fill="#8C91CB"/><rect y="270" width="242" height="242" rx="46" fill="#BBA0CD"/><rect x="270" y="270" width="242" height="242" rx="46" fill="#EAC780"/><g fill="#0B1627" font-family="Noto Sans" font-weight="700" font-size="150" text-anchor="middle"><text x="121" y="174">2</text><text x="391" y="174">0</text><text x="121" y="444">4</text><text x="391" y="444">8</text></g></svg>''')
# iOS asset catalog retained for exported Xcode projects; Unity platform icons also configured by editor.
cat=ios/'AppIcon.appiconset';cat.mkdir(exist_ok=True)
entries=[]
for idiom,size,scales in [('iphone',20,[2,3]),('iphone',29,[2,3]),('iphone',40,[2,3]),('iphone',60,[2,3]),('ipad',20,[1,2]),('ipad',29,[1,2]),('ipad',40,[1,2]),('ipad',76,[1,2]),('ipad',83.5,[2]),('ios-marketing',1024,[1])]:
    for scale in scales:
        px=int(size*scale);name=f'icon-{px}.png';app.resize((px,px),Image.Resampling.LANCZOS).save(cat/name)
        entries.append({'idiom':idiom,'size':f'{size:g}x{size:g}','scale':f'{scale}x','filename':name})
(cat/'Contents.json').write_text(json.dumps({'images':entries,'info':{'version':1,'author':'xcode'}},indent=2)+'\n')
manifest={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((ROOT/'Assets/Resources/Fonts').glob('*')) if p.suffix in ('.ttf','.txt')}
(ROOT/'Docs/font-checksums.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('Generated brand, adaptive/monochrome layers, opaque iOS catalog and launch artwork.')
