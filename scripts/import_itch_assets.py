import os
import shutil
import sys

def main():
    temp = os.environ.get('TEMP', 'C:/Users/madly/AppData/Local/Temp')
    pro_src = os.path.join(temp, 'pro_kitchen', 'professional_kitchen')
    pixel_src = os.path.join(temp, 'pixel_mart')

    if not os.path.exists(pro_src) or not os.path.exists(pixel_src):
        import zipfile
        zip_pro = 'downloaded_assets/professional_kitchen.zip'
        zip_pixel = 'downloaded_assets/Pixel_Mart.zip'
        if os.path.exists(zip_pro):
            zipfile.ZipFile(zip_pro).extractall(os.path.join(temp, 'pro_kitchen'))
        if os.path.exists(zip_pixel):
            zipfile.ZipFile(zip_pixel).extractall(os.path.join(temp, 'pixel_mart'))

    unity_kitchen = 'unity/TwoLadles/Two Ladles/Assets/Sprites/Kitchen'
    unity_items = 'unity/TwoLadles/Two Ladles/Assets/Sprites/Items'

    for d in [unity_kitchen, unity_items]:
        os.makedirs(d, exist_ok=True)

    # 1. Kitchen Tilesets (0_mem0ry)
    tilesets = [
        'professional_kitchen.png',
        'professional_kitchen_room_door_tiles.png',
        'professional_kitchen_withshadows.png'
    ]
    for ts in tilesets:
        src = os.path.join(pro_src, ts)
        if os.path.exists(src):
            shutil.copy2(src, os.path.join(unity_kitchen, ts))
            print(f"Copied tileset: {ts}")

    # 2. Food Ingredients & Utensils (ghostpixxells Pixel Mart)
    key_items = {
        'meat1.png': 'pork_belly_raw.png',
        'meat2.png': 'beef_bulgogi_raw.png',
        'bacon.png': 'pork_portioned.png',
        'sausage_p.png': 'bulgogi_portioned.png',
        'sliced_bread_p.png': 'baguette_bread.png',
        'cabbage.png': 'cabbage_fresh.png',
        'cabbage_p.png': 'kimchi_slaw.png',
        'bell_pepper.png': 'pickled_veg.png',
        'fish.png': 'fish_cake_raw.png',
        'salmon.png': 'eomuk_prepared.png',
        'potato_p.png': 'tteok_portioned.png',
        'bowl.png': 'dish_bowl.png',
        'white_cheese_piece.png': 'dish_plate.png',
        'chopping board.png': 'prep_counter_icon.png',
        'frying pan.png': 'cooking_pan_icon.png',
        'kitchen_knife_set.png': 'knife_upgrade_icon.png',
        'kitchen_soap.png': 'dish_soap.png',
        'scrub_sponge.png': 'dish_sponge.png',
        'scrub_brush.png': 'dish_brush.png',
        'cleaning_gloves.png': 'cleaning_gloves.png',
        'spatula.png': 'spatula.png',
        'whisk.png': 'whisk.png',
        'cooking_oil.png': 'cooking_oil.png',
        'salt.png': 'salt.png',
        'sugar.png': 'sugar.png',
        'barbeque_sauce.png': 'korean_sauce.png',
        'teakettle.png': 'broth_pot.png'
    }

    item_count = 0
    for src_name, target_name in key_items.items():
        src_path = os.path.join(pixel_src, src_name)
        if os.path.exists(src_path):
            shutil.copy2(src_path, os.path.join(unity_items, target_name))
            item_count += 1
            print(f"Copied item: {src_name} -> {target_name}")

    print(f"Successfully imported {len(tilesets)} tilesets and {item_count} items into Unity!")

if __name__ == '__main__':
    main()
