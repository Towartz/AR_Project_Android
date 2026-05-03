import os
import sys

import bpy


def get_args():
    if "--" not in sys.argv:
        raise SystemExit("Usage: blender -b file.blend --python export_blend_to_fbx.py -- output.fbx")

    args = sys.argv[sys.argv.index("--") + 1 :]
    if len(args) != 1:
        raise SystemExit("Expected exactly one output FBX path")

    return args[0]


def select_exportable_objects():
    bpy.ops.object.select_all(action="DESELECT")
    exportable_types = {"MESH", "ARMATURE", "EMPTY"}
    count = 0
    for obj in bpy.context.scene.objects:
        if obj.type in exportable_types:
            obj.select_set(True)
            count += 1

    if count == 0:
        raise SystemExit("No mesh/armature/empty objects found to export")


def main():
    output_path = os.path.abspath(get_args())
    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    try:
        bpy.ops.preferences.addon_enable(module="io_scene_fbx")
    except Exception:
        pass

    select_exportable_objects()
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        object_types={"EMPTY", "ARMATURE", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        bake_space_transform=False,
        add_leaf_bones=False,
        path_mode="COPY",
        embed_textures=False,
    )

    print("Exported FBX:", output_path)


if __name__ == "__main__":
    main()
