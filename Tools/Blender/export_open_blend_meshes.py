import bpy
import os
import sys


def get_args():
    if "--" not in sys.argv:
        raise RuntimeError("Expected output path after --")

    args = sys.argv[sys.argv.index("--") + 1:]
    if not args:
        raise RuntimeError("Output path is required")

    return args[0]


def main():
    output_path = get_args()
    mesh_objects = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    if not mesh_objects:
        raise RuntimeError("No mesh objects found in opened blend file")

    os.makedirs(os.path.dirname(output_path), exist_ok=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in mesh_objects:
        obj.select_set(True)

    bpy.context.view_layer.objects.active = mesh_objects[0]
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    bpy.ops.export_scene.fbx(
        filepath=output_path,
        use_selection=True,
        object_types={"MESH"},
        global_scale=1.0,
        apply_unit_scale=True,
        add_leaf_bones=False,
    )

    print(f"Exported {len(mesh_objects)} mesh object(s) to {output_path}")


if __name__ == "__main__":
    main()
