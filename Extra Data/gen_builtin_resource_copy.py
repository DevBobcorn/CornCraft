import os
import shutil

exclude_ext = [
    '.py', '.zip', '.md', '.csv', '.psd', '.xcf', '.svg'
]

user_folder = os.path.expanduser("~")
print(user_folder)

def cpdir(datadir, target_folder):
    rel_path = ''

    for root, _, files in os.walk(datadir):
        for file in files:
            ext = file[file.rfind('.'):]

            if not ext in exclude_ext:
                
                path_in_folder = os.path.join(root, file)
                rel_path = os.path.relpath(path_in_folder, datadir)

                print(f'{path_in_folder} -> {target_folder}/{rel_path}')

                shutil.copy(path_in_folder, f'{target_folder}/{rel_path}')
            else:
                print(f'Excluding file {file}')

# Use CornCraft project root as current work directory
cpdir('./Extra Data/', f'{user_folder}/AppData/LocalLow/DevBobcorn/CornCraft/Extra Data')

cpdir('./Vanilla Fix Resource/', f'{user_folder}/AppData/LocalLow/DevBobcorn/CornCraft/Resource Packs/vanilla_fix')