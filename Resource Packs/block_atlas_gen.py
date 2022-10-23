from PIL import Image, ImageOps
import os, glob, json

version = '1.16.5'

proc_types = [
    'block',
    'item'
]

recolor_dict = {
    'minecraft:block/water_flow': (63, 118, 228),
    'minecraft:block/water_overlay': (63, 118, 228),
    'minecraft:block/water_still': (63, 118, 228),
}

skip_list = []

def recolor(srci, col):
    #Preserve the alpha value before converting it..
    r,g,b,alpha = srci.split()
    gray = srci.convert('L')
    rec = ImageOps.colorize(gray, (0,0,0), (255,255,255), col, 0 ,255 ,157).convert('RGBA')
    #Recover its transparency..
    rec.putalpha(alpha)
    #rec.show()
    return rec

size = 1024
rct = 16
lncnt = int(size / rct) # How many textures in a line
print('Textures in a line: ' + str(lncnt))

atlas = Image.new('RGBA', (size, size), (0, 0, 0, 0))

offset = 0
i = 0
j = lncnt - 1

atlas_dict = { }

packpath = 'vanilla-' + version + '/assets/'

namespaces = os.listdir(packpath)
for nspath in namespaces:
    print('NameSpace: ' + nspath)
    
    for proc_type in proc_types:
        paths = glob.iglob(packpath + nspath + '/textures/' + proc_type + '/**/*?.png', recursive=True) # Also search sub-folders...

        pathLen = len(packpath + nspath + '/textures/')

        for path in paths:
            texname = nspath + ':' + path[pathLen:-4]
            texname = texname.replace('//', '/').replace('\\', '/')

            if texname in skip_list:
                print('Skipping ' + texname)
                continue
            
            print('Processing ' + texname)
            #print('\t\t\t[\"' + texname + '\"] = '+ str(offset) + ',')
            atlas_dict[texname] = offset
            tex = Image.open(path).convert('RGBA')

            # Rescale if necessary
            if tex.width != rct:
                print('Rescaling ' + texname + ' to make its width match ' + str(rct))
                tex = tex.resize((rct, round(tex.height / tex.width) * rct))

            # Crop if necessary
            if tex.width != tex.height:
                print('Cropping ' + texname)
                tex = tex.crop((0, 0, tex.width, tex.width))
            
            # Recolor if necessary
            if recolor_dict.__contains__(texname):
                print('Recoloring ' + texname)
                tex = recolor(tex, recolor_dict[texname])
                
            atlas.paste(tex, (i * rct, j * rct))
            
            i += 1
            offset += 1
            if i == lncnt:
                i = 0
                j -= 1

with open("./block_atlas_" + version + "_dict.json", "w+") as f:
    data = json.dumps(atlas_dict, sort_keys=True, indent=4, separators=(',', ': '))
    f.write(data) 

atlas.save('./block_atlas_' + version + '.png')
print('Done.')
