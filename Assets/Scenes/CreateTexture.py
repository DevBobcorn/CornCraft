from PIL import Image, ImageDraw, ImageFont
import math

def transPaste(backGround, foreGround, box=(0,0)):
    trans = Image.new("RGBA", backGround.size)
    trans.paste(foreGround, box, mask=foreGround)
    nim = Image.alpha_composite(backGround, trans)
    return nim

def reorganize(srcPath, gridWidth, gridHeight, cellSize):
    impath = r'Reorganized.png'

    srci = Image.open(srcPath)
    srcGridWidth = srci.width // cellSize
    srcGridHeight = srci.height // cellSize

    print(f'Source grid: {srcGridWidth}x{srcGridHeight}')

    im = Image.new('RGBA', (gridWidth * cellSize, gridHeight * cellSize), 'white')

    for j in range(gridHeight):
        for i in range(gridWidth):
            frameIndex = j * gridWidth + i

            yInSrc = frameIndex // srcGridWidth
            xInSrc = frameIndex % srcGridWidth

            cropped = srci.crop((xInSrc * cellSize, yInSrc * cellSize, xInSrc * cellSize + cellSize, yInSrc * cellSize + cellSize))

            im.paste(cropped, (i * cellSize, j * cellSize))

    im.save(impath)
    print(f'Image saved to {impath}')

def genSingle(gridWidth, gridHeight, cellSize):
    fntPath = ImageFont.truetype(r'arial.ttf', math.floor(cellSize / 2))
    impath = r'Numbers.png'

    im = Image.new('RGBA', (gridWidth * cellSize, gridHeight * cellSize), 'white')
    draw = ImageDraw.Draw(im)

    for j in range(gridHeight):
        for i in range(gridWidth):
            draw.text((i * cellSize + 2, j * cellSize + 2), str(j * gridWidth + i), fill='black', font=fntPath)

    im.save(impath)
    print(f'Image saved to {impath}')

def genSeparated(amount, size):
    fntPath = ImageFont.truetype(r'arial.ttf', math.floor(size / 2))

    for i in range(amount):
        im = Image.new('RGBA', (size, size), 'white')
        impath = f'Num{i}.png'
        draw = ImageDraw.Draw(im)

        draw.text((10, 10), str(i), fill='black', font=fntPath)

        im.save(impath)
        print(f'Image saved to {impath}')

if __name__ == '__main__':
    #genSeparated(27, 64)
    #genSingle(1, 32, 16)
    reorganize('D:/Unity/CornCraft/Assets/Scenes/Login/Textures/portal.png', 8, 8, 16)
    #reorganize('D:/Unity/CornCraft/Numbers.png', 8, 8, 16)