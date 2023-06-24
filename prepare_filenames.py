import sys
import os
import shutil

DIFFUSE_PROXIES = [
    "diffuse",
    "color",
    "diff",
    "col"
]

DIFFUSE_NAME = "_color"

METAL_PROXIES = [
    "metal",
    "metallic"
]

METAL_NAME = "_metal"

ROUGHNESS_PROXIES = [
    "roughness",
    "rough"
]

ROUGHNESS_NAME = "_rough"

NORMAL_PROXIES = [
    "normal",
    "nor"
]

NORMAL_NAME = "_normal"

AO_PROXIES = [
    "ao",
    "ambientocclusion",
    "ambient_occlusion",
    "occlusion"
]

AO_NAME = "_ao"

EXTENSIONS = ['.png', '.jpg', '.jpeg', '.exr', '.tga']

def process_file(filepath: str, prefix: str, outdir: str, force_extension = False):
    (root, ext) = os.path.splitext(os.path.basename(filepath))
    
    if ext not in EXTENSIONS and not force_extension:
        print(f'Skipped file: {root + ext}')
        return False
    
    newfile: str

    if string_matches(root, DIFFUSE_PROXIES):
        newfile = prefix + DIFFUSE_NAME
    elif string_matches(root, METAL_PROXIES):
        newfile = prefix + METAL_NAME
    elif string_matches(root, ROUGHNESS_PROXIES):
        newfile = prefix + ROUGHNESS_NAME
    elif string_matches(root, NORMAL_PROXIES):
        newfile = prefix + NORMAL_NAME
    elif string_matches(root, AO_PROXIES):
        newfile = prefix + AO_NAME
    else:
        print(f'Could not determine the map type for {root + ext}')
        return False

    destfile = os.path.join(outdir, newfile + ext)
    shutil.copyfile(filepath, destfile)

    print(os.path.basename(filepath) + ' --> ' + os.path.basename(destfile))
    return True

def string_matches(string: str, proxies: list[str]):
    string = string.lower()
    for val in proxies:
        if val in string: return True

    return False

if __name__ == '__main__':
    if (len(sys.argv) < 3):
        print("Usage: python prepare_filenames.py [directory] [prefix]")
        exit(1)

    directory = sys.argv[1]
    prefix = sys.argv[2]

    files = filter(lambda file : os.path.isfile(directory+'/'+file), os.listdir(directory))

    i = 0
    for file in files:
        if process_file(os.path.join(directory, file), prefix, directory): i+= i

    print(f'Renamed {i} files.')
    