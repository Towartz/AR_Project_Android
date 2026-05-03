import os
import sys

import bpy


def main():
    if "--" not in sys.argv:
        raise SystemExit("Usage: blender -b source.blend --python export_fbx_from_blend.py -- output.fbx")

    output_path = sys.argv[sys.argv.index("--") + 1]
    output_path = os.path.abspath(output_path)
    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    bpy.ops.object.select_all(action="DESELECT")
    exportable_types = {"MESH", "EMPTY", "ARMATURE"}
    selected_count = 0
    active_object = None

    for obj in bpy.context.scene.objects:
        if obj.type not in exportable_types:
            continue
        if obj.hide_get() or obj.hide_viewport:
            continue

        obj.select_set(True)
        selected_count += 1
        if active_object is None and obj.type == "MESH":
            active_object = obj

    if selected_count == 0:
        raise SystemExit("No visible mesh/empty/armature objects found to export.")

    bpy.context.view_layer.objects.active = active_object
    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        add_leaf_bones=False,
        path_mode="COPY",
        embed_textures=True,
        axis_forward="-Z",
        axis_up="Y",
    )

    print(f"Exported {selected_count} object(s) to {output_path}")


if __name__ == "__main__":
    main()
