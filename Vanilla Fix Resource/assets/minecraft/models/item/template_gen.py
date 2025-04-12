import os

script_dir = os.path.dirname(os.path.abspath(__file__))

os.chdir(script_dir)

colors = [
    'white', 'orange', 'magenta', 'light_blue', 'yellow',
    'lime', 'pink', 'gray', 'light_gray', 'cyan', 'purple',
    'blue', 'brown', 'green', 'red', 'black'
]

with open('template_gen.json') as f:
    txt = f.read()

    for color in colors:
        with open(f'{color}_shulker_box.json', 'w+') as fo:
            fo.write(txt.replace('COLOR', color))