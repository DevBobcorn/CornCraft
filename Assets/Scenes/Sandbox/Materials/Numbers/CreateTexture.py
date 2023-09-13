from PIL import Image, ImageDraw, ImageFont
import math

def genSingle(gridSize, cellSize):
    fntPath = ImageFont.truetype(r'arial.ttf', math.floor(cellSize / 2))
    impath = r'Numbers.png'

    im = Image.new('RGBA', (gridSize * cellSize, gridSize * cellSize), 'white')
    draw = ImageDraw.Draw(im)

    for j in range(gridSize):
        for i in range(gridSize):
            draw.text((i * cellSize + 10, j * cellSize + 10), str(j * gridSize + i), fill='black', font=fntPath)

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
    genSeparated(27, 64)