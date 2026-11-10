import os
import io
import uuid
import zipfile
from PIL import Image

def generate_meta_content(guid, base_meta_path):
    with open(base_meta_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()
    lines[1] = f"guid: {guid}\n"
    return "".join(lines)

def create_beef_raw():
    beef_art = [
        '................................',
        '................................',
        '................................',
        '................................',
        '................................',
        '.............######.............',
        '..........###WWWWWW###..........',
        '........##WWWWWWWWWWww##........',
        '.......#WWWWWWwwWWwwwwww#.......',
        '......#WWWWwwwwRrrrwwrwws#......',
        '.....#WWwwrrRRRRRRrrrdrwss#.....',
        '....#WWwrrRRRRRRRrrrrddrwss#....',
        '...#WWwrrRRRRRRrrrrrrdddswss#...',
        '...#WwrrRRRRrrrrrdddddddsdss#...',
        '..#WwrrRRrrBBBBrrrdddddddsdss#..',
        '..#wrrRrrrBBmmBBddddDDDDdddsd#..',
        '..#wrrrrrBBmmmmBBddDDDDDDdds##..',
        '..#wrrrrrBBmmmmBBdDDDDDDDdd#....',
        '..#wdrrrrdBBmmBBdDDDDDDDdd#.....',
        '...#dddddddBBBBdDDDDDDDdd#......',
        '...#ddddddddddDDDDDDDddd#.......',
        '....#sddddddDDDDDDDDddd#........',
        '.....#ssddddDDDDDDDddd#.........',
        '......##sssddDDDDDddd#..........',
        '........##sssdddDdd##...........',
        '..........###sdd###.............',
        '.............###................',
        '................................',
        '................................',
        '................................',
        '................................',
        '................................',
    ]
    palette = {
        '.': (0, 0, 0, 0),
        '#': (42, 10, 12, 255),    # Outline dark
        'R': (228, 54, 62, 255),   # Red highlight
        'r': (184, 30, 36, 255),   # Red midtone
        'd': (136, 18, 24, 255),   # Red dark shade
        'D': (92, 10, 14, 255),    # Red deep shadow
        'W': (255, 250, 240, 255), # Fat highlight
        'w': (238, 222, 196, 255), # Fat midtone
        's': (196, 174, 144, 255), # Fat shade
        'B': (242, 238, 226, 255), # Bone outer
        'b': (205, 196, 180, 255), # Bone shade
        'm': (168, 52, 60, 255),   # Bone marrow center
    }
    img = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    for y in range(32):
        for x in range(32):
            col = palette[beef_art[y][x]]
            if col[3] > 0:
                img.putpixel((x, y), col)
    return img

def create_noodles_raw():
    noodle_art = [
        '................................',
        '................................',
        '................................',
        '................................',
        '................................',
        '................................',
        '................................',
        '................................',
        '.....##.######....######.##.....',
        '....#hh#HHHHHH####HHHHHH#hh#....',
        '...#hhhhHHhHHH####HHHhHHhhhh#...',
        '..#hhhhhHHhHHHRRRRHHHhHHhhhhh#..',
        '..#hwhhhHhHHHHROGRHhHHHHhhwhh#..',
        '.#hhwhhhHHHHHHROGRHHHHHHhhhwhh#.',
        '#hwhwhhhHHHHHHROGRHHHHHHhhwhwhh#',
        '#whwhwhwHHHHHHROGRHHHHHHwwhwhwh#',
        '#whswswsSSssssROGRssssSSsswswsw#',
        '#swswswsSSssssROGRssssSSssswsws#',
        '.#sswsswssssssROGRsssssssswssw#.',
        '..#sssswSssSssRRRRssSssSsssss#..',
        '..#dssssSssSss####ssSssSssss#...',
        '...#ddssSssSSS####SSSssSssdd#...',
        '....#dd#dddddd####dddddd#dd#....',
        '.....##.######....######.##.....',
        '................................',
        '................................',
        '................................',
        '................................',
        '................................',
        '................................',
        '................................',
        '................................',
    ]
    palette = {
        '.': (0, 0, 0, 0),
        '#': (50, 40, 28, 255),    # Outline dark warm brown
        'w': (255, 255, 250, 255), # Pure noodle highlight white
        'h': (255, 250, 238, 255), # Light cream
        'H': (246, 238, 220, 255), # Noodle midtone body
        'S': (228, 216, 192, 255), # Shaded cream
        's': (206, 190, 162, 255), # Darker shade
        'd': (174, 156, 128, 255), # Under-shadow
        'R': (215, 38, 48, 255),   # Red paper band
        'r': (168, 22, 32, 255),   # Red paper band shade
        'G': (250, 210, 60, 255),  # Gold label
        'O': (210, 160, 30, 255),  # Gold label shade
    }
    img = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    for y in range(32):
        for x in range(32):
            col = palette[noodle_art[y][x]]
            if col[3] > 0:
                img.putpixel((x, y), col)
    return img

def main():
    repo_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    zip_path = os.path.join(repo_root, 'downloaded_assets', 'Pixel_Mart.zip')
    base_meta_path = os.path.join(repo_root, 'unity', 'TwoLadles', 'Two Ladles', 'Assets', 'Resources', 'Sprites', 'Items', 'beef_bulgogi_raw.png.meta')

    dest_dirs = [
        os.path.join(repo_root, 'unity', 'TwoLadles', 'Two Ladles', 'Assets', 'Resources', 'Sprites', 'Items'),
        os.path.join(repo_root, 'unity', 'TwoLadles', 'Two Ladles', 'Assets', 'Sprites', 'Items')
    ]

    for d in dest_dirs:
        os.makedirs(d, exist_ok=True)

    # 1. Flour from Pixel_Mart
    with zipfile.ZipFile(zip_path) as z:
        flour_bytes = z.read('flour.png')

    flour_img = Image.open(io.BytesIO(flour_bytes))
    beef_img = create_beef_raw()
    noodles_img = create_noodles_raw()

    items = [
        ('flour.png', flour_img),
        ('flour_sack.png', flour_img),
        ('beef_raw.png', beef_img),
        ('noodles_raw.png', noodles_img),
    ]

    for filename, img in items:
        # Generate persistent GUID
        guid = uuid.uuid5(uuid.NAMESPACE_DNS, f"twoladles.item.{filename}").hex
        for d in dest_dirs:
            img_path = os.path.join(d, filename)
            img.save(img_path, format='PNG')
            if 'unity' in d.lower():
                meta_path = img_path + '.meta'
                if not os.path.exists(meta_path):
                    with open(meta_path, 'w', encoding='utf-8') as f:
                        f.write(generate_meta_content(guid, base_meta_path))
                    print(f"Wrote {meta_path} (guid: {guid})")
                else:
                    print(f"Meta already exists: {meta_path}")
            print(f"Saved {img_path}")

    print("Successfully generated all new food assets!")

if __name__ == '__main__':
    main()
