import os
import zipfile

exclude_ext = [
    '.py', '.zip', '.md', '.csv', '.psd', '.xcf', '.svg'
]

def zipdir(datadir, zipf):
    for root, _, files in os.walk(datadir):
        for file in files:
            ext = file[file.rfind('.'):]

            if not ext in exclude_ext:
                zipf.write(os.path.join(root, file),
                        os.path.relpath(os.path.join(root, file), datadir))
            else:
                print(f'Excluding file {file}')

# Use CornCraft project root as current work directory
with zipfile.ZipFile('./Assets/Resources/CornCraftBuiltin.bytes', 'w', zipfile.ZIP_DEFLATED) as zipf:
    zipdir('./Extra Data/', zipf)

with zipfile.ZipFile('./Assets/Resources/VanillaFix.bytes', 'w', zipfile.ZIP_DEFLATED) as zipf:
    zipdir('./Vanilla Fix Resource/', zipf)

with zipfile.ZipFile('./Assets/Resources/BedrockRes.bytes', 'w', zipfile.ZIP_DEFLATED) as zipf:
    zipdir('./Bedrock Entity Resource/Base', zipf)

with zipfile.ZipFile('./Assets/Resources/BlockEntityModels.bytes', 'w', zipfile.ZIP_DEFLATED) as zipf:
    zipdir('./Bedrock Entity Resource/Block Entity Models', zipf)