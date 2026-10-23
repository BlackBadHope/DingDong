"""Original DingDong public artwork. No image or game resource is an input.

The shape catalogue contains only resource names and canvas/frame dimensions.
All pixels are drawn here from geometric primitives and a deterministic palette.
Python 3 standard library only. Output is confined to the chosen mod directory.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import struct
import zlib


class Canvas:
    def __init__(self, width, height):
        self.w, self.h = width, height
        self.p = bytearray(width * height * 4)

    def pixel(self, x, y, color):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            self.p[(y * self.w + x) * 4:(y * self.w + x + 1) * 4] = bytes(color)

    def rect(self, x0, y0, x1, y1, color):
        for y in range(max(0, int(y0)), min(self.h, int(y1))):
            for x in range(max(0, int(x0)), min(self.w, int(x1))):
                self.pixel(x, y, color)

    def line(self, x0, y0, x1, y1, color, width=1):
        steps = max(abs(int(x1 - x0)), abs(int(y1 - y0)), 1)
        for i in range(steps + 1):
            x, y = x0 + (x1-x0)*i/steps, y0 + (y1-y0)*i/steps
            self.rect(round(x)-width//2, round(y)-width//2,
                      round(x)+width//2+1, round(y)+width//2+1, color)

    def disc(self, cx, cy, radius, color):
        for y in range(int(cy-radius), int(cy+radius)+1):
            for x in range(int(cx-radius), int(cx+radius)+1):
                if (x-cx)**2+(y-cy)**2 <= radius**2:
                    self.pixel(x, y, color)

    def diamond(self, cx, cy, rx, ry, color):
        for y in range(int(cy-ry), int(cy+ry)+1):
            dx = int(rx * (1-abs(y-cy)/max(1, ry)))
            self.rect(cx-dx, y, cx+dx+1, y+1, color)

    def paste(self, other, x0, y0):
        for y in range(other.h):
            for x in range(other.w):
                c = other.p[(y*other.w+x)*4:(y*other.w+x+1)*4]
                if c[3]:
                    self.pixel(x+x0, y+y0, c)

    def png(self):
        def chunk(kind, data):
            return struct.pack('>I', len(data))+kind+data+struct.pack('>I', zlib.crc32(kind+data))
        rows = b''.join(b'\x00'+self.p[y*self.w*4:(y+1)*self.w*4] for y in range(self.h))
        return (b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR', struct.pack('>IIBBBBB', self.w, self.h, 8, 6, 0, 0, 0))
                +chunk(b'IDAT', zlib.compress(rows, 9))+chunk(b'IEND', b''))


INK = (16, 24, 42, 255)
LIGHT = (240, 239, 204, 255)
GOLD = (225, 169, 69, 255)


def palette(name):
    h = hashlib.sha256(name.encode()).digest()
    c = tuple(80 + b % 145 for b in h[:3]) + (255,)
    dark = tuple(int(v*.48) for v in c[:3]) + (255,)
    pale = tuple(min(255, int(v*.45+140)) for v in c[:3]) + (255,)
    return c, dark, pale


def rune(c, x, y, r, color, kind):
    # Twelve original angular symbols, built without a font or external glyph.
    c.diamond(x, y, r, r, INK)
    c.diamond(x, y, max(1, r-2), max(1, r-2), color)
    rays = 3 + kind % 4
    for n in range(rays):
        a = n * 2*math.pi/rays + (kind % 3)*.24
        c.line(x, y, x+math.cos(a)*r*.7, y+math.sin(a)*r*.7, LIGHT)
    c.disc(x, y, 1, GOLD)


def item(name, width, height):
    c = Canvas(width, height)
    color, dark, pale = palette(name)
    n = int.from_bytes(hashlib.sha256(name.encode()).digest()[:2], 'little')
    s = min(width, height)/64
    def line(x0,y0,x1,y1,col,w=2): c.line(x0*s,y0*s,x1*s,y1*s,col,max(1,round(w*s)))
    if any(t in name.lower() for t in ('bow', 'repeater', 'carbine', 'vortex')):
        if 'bow' in name.lower():
            pts = [(19,8),(34,16),(40,31),(34,48),(19,56)]
            for a,b in zip(pts,pts[1:]): line(*a,*b,INK,6); line(*a,*b,color,3)
            line(19,8,19,56,LIGHT,1); line(10,32,50,32,GOLD,2)
        else:
            line(12,46,26,34,dark,8); line(22,34,49,18,INK,11)
            line(23,33,50,17,color,6); line(29,28,41,21,GOLD,2)
            line(26,36,35,43,INK,5); line(13,45,20,42,GOLD,3)
    elif any(t in name for t in ('Blade','Cleaver','Glaive','Hearthbrand')):
        line(14,52,48,13,INK,8); line(18,46,48,13,color,5)
        line(21,41,48,13,LIGHT,1); line(17,48,28,50,GOLD,3)
        line(14,52,9,58,dark,4)
    elif any(t in name for t in ('Whip','Lash','Chain','Cord')):
        line(13,51,23,41,INK,6); line(13,51,23,41,GOLD,3)
        pts=[(22,41),(35,46),(47,36),(38,25),(48,14),(56,20)]
        for a,b in zip(pts,pts[1:]): line(*a,*b,INK,5); line(*a,*b,color,2)
    elif any(t in name for t in ('Tome','Grimoire')):
        c.rect(12*s,13*s,49*s,52*s,INK); c.rect(16*s,16*s,46*s,49*s,color)
        line(18,18,18,47,GOLD,2); rune(c,34*s,32*s,10*s,pale,n)
    elif any(t in name for t in ('Wand','Staff','Scepter','Lantern')):
        line(14,53,42,20,INK,6); line(14,53,42,20,GOLD,2)
        rune(c,43*s,17*s,10*s,color,n)
    elif name.endswith('_Head') or name.endswith('_Body') or name.endswith('_Legs'):
        if name.endswith('_Head'):
            c.rect(width*.20,height*.22,width*.80,height*.70,INK)
            c.rect(width*.25,height*.18,width*.75,height*.49,color)
            c.rect(width*.29,height*.46,width*.71,height*.54,GOLD)
        elif name.endswith('_Body'):
            c.rect(width*.22,height*.24,width*.78,height*.80,INK)
            c.rect(width*.28,height*.27,width*.72,height*.73,color)
            c.diamond(width/2,height*.48,width*.17,height*.19,GOLD)
        else:
            c.rect(width*.24,height*.21,width*.46,height*.80,INK)
            c.rect(width*.54,height*.21,width*.76,height*.80,INK)
            c.rect(width*.28,height*.25,width*.43,height*.72,color)
            c.rect(width*.57,height*.25,width*.72,height*.72,color)
    else:
        rune(c, width/2, height/2, min(width,height)*.35, color, n)
        c.rect(width*.22,height*.77,width*.78,height*.82,GOLD)
    return c


def armor(name, width, height):
    c = Canvas(width,height)
    color,dark,pale = palette(name.split('_Body')[0].split('_Head')[0].split('_Legs')[0])
    for row in range((height+55)//56):
        for col in range((width+39)//40):
            x,y=col*40,row*56
            if name.endswith('_Head_Head'):
                c.rect(x+12,y+4,x+28,y+14,INK); c.rect(x+13,y+3,x+27,y+10,color)
                c.rect(x+12,y+11,x+28,y+13,GOLD); c.rect(x+15,y+4,x+18,y+7,pale)
            elif name.endswith('_Body_Body'):
                c.rect(x+13,y+17,x+27,y+33,INK); c.rect(x+14,y+18,x+26,y+31,color)
                c.rect(x+11,y+17,x+16,y+22,GOLD); c.rect(x+24,y+17,x+29,y+22,GOLD)
                c.diamond(x+20,y+23,3,5,pale)
                # Composite arm frames: each column has a different pointing angle.
                angle=col*math.pi/4
                c.line(x+14,y+21,x+14+math.sin(angle)*9,y+21+math.cos(angle)*10,dark,3)
            else:
                stride=round(math.sin(row*.9)*3)
                c.rect(x+14+stride,y+33,x+19+stride,y+49,INK)
                c.rect(x+21-stride,y+33,x+26-stride,y+49,INK)
                c.rect(x+15+stride,y+34,x+18+stride,y+47,color)
                c.rect(x+22-stride,y+34,x+25-stride,y+47,color)
                c.rect(x+13+stride,y+47,x+20+stride,y+50,GOLD)
                c.rect(x+20-stride,y+47,x+27-stride,y+50,GOLD)
    return c


def panel(name,width,height):
    c=Canvas(width,height)
    border=(104,183,177,245) if ('Ready' in name or 'Selected' in name) else (100,119,154,245)
    c.rect(2,2,width-2,height-2,INK); c.rect(4,4,width-4,height-4,border)
    c.rect(6,6,width-6,height-6,(29,43,64,235))
    for x,y in [(10,10),(width-11,10),(10,height-11),(width-11,height-11)]:
        c.diamond(x,y,3,3,GOLD)
    if 'Close' in name:
        c.line(width*.36,height*.30,width*.64,height*.70,border,4)
        c.line(width*.64,height*.30,width*.36,height*.70,border,4)
    return c


def main():
    ap=argparse.ArgumentParser(); ap.add_argument('--output',type=Path,required=True)
    args=ap.parse_args(); root=args.output.resolve()
    catalog=json.loads(Path(__file__).with_name('asset-shapes.json').read_text(encoding='utf-8'))
    records=[]; sheet=Canvas(640,((len(catalog['assets'])+9)//10)*72)
    for index,a in enumerate(catalog['assets']):
        rel=Path(a['path']); dest=(root/rel).resolve()
        if not dest.is_relative_to(root) or a['format'] != 1: raise ValueError('Invalid asset shape')
        w,h=a['width'],a['height']; name=rel.stem
        if h>1000 or w==360: image=armor(name,w,h)
        elif 'Ui' in rel.parts or 'Panel' in name: image=panel(name,w,h)
        else: image=item(name,w,h)
        payload=struct.pack('<iii',1,w,h)+image.p
        dest.parent.mkdir(parents=True,exist_ok=True); dest.write_bytes(payload)
        records.append({'path':rel.as_posix(),'sha256':hashlib.sha256(payload).hexdigest(),
                        'width':w,'height':h,'origin':'original-geometric-generator-v1'})
        if w<=64 and h<=64: sheet.paste(image,(index%10)*64,(index//10)*72)
    icon=Canvas(80,80); icon.rect(3,3,77,77,INK); icon.diamond(40,39,33,33,(35,116,139,255))
    icon.rect(28,27,52,52,GOLD); icon.disc(40,27,12,GOLD)
    icon.line(24,54,56,54,LIGHT,3); icon.disc(40,60,4,LIGHT)
    payload=icon.png(); (root/'icon.png').write_bytes(payload)
    records.append({'path':'icon.png','sha256':hashlib.sha256(payload).hexdigest(),
                    'width':80,'height':80,'origin':'original-geometric-generator-v1'})
    manifest={'format':'dingdong-original-art-v1','sourcePixelsRead':False,
              'generatorSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'assets':records}
    (root/'ORIGINAL-ASSETS.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
    (Path(__file__).parent/'preview.png').write_bytes(sheet.png())
    print(json.dumps({'generated':len(records),'sourcePixelsRead':False,'output':str(root)}))


if __name__=='__main__': main()
