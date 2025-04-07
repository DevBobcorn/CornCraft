import shutil, os

script_dir = os.path.dirname(os.path.abspath(__file__))

os.chdir(script_dir)

# inventory_types
# 1.16.5 -> 1.17.1
#        -> 1.18.2
#        -> 1.19.2
#        -> 1.19.3
# 1.19.4
# 1.20.1 -> 1.20.2
# 1.20.4 -> 1.20.6
versions = {
    '1.16.5': ['1.17.1', '1.18.2', '1.19.2', '1.19.3'],
    '1.20.1': ['1.20.2'],
    '1.20.4': ['1.20.6']
}

for (src, dsts) in versions.items():
    for dst in dsts:
        s = f'./inventories/inventory_types-{src}.json'
        d = f'./inventories/inventory_types-{dst}.json'

        print(f'{s} -> {d}')

        shutil.copy(s, d)

