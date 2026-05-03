import bpy
import math
import os
import sys


OUTPUT_DIR = os.environ.get("BLENDER_EXPORT_DIR")
if not OUTPUT_DIR and "--" in sys.argv:
    args = sys.argv[sys.argv.index("--") + 1:]
    if args:
        OUTPUT_DIR = args[0]

if not OUTPUT_DIR:
    raise RuntimeError("Output directory argument is required")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block in list(bpy.data.meshes):
        if block.users == 0:
            bpy.data.meshes.remove(block)


def export_selected(name):
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    filepath = os.path.join(OUTPUT_DIR, f"{name}.fbx")
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        object_types={"MESH"},
        global_scale=1.0,
        apply_unit_scale=True,
        add_leaf_bones=False,
    )


def finalize(name):
    obj = bpy.context.active_object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    export_selected(name)


def create_cube():
    clear_scene()
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 0.0, 0.0))
    finalize("blend_cube")


def create_round_cube():
    clear_scene()
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(0.0, 0.0, 0.0))
    obj = bpy.context.active_object
    bevel = obj.modifiers.new(name="Bevel", type="BEVEL")
    bevel.width = 0.12
    bevel.segments = 4
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    finalize("blend_roundcube")


def create_pyramid():
    clear_scene()
    bpy.ops.mesh.primitive_cone_add(vertices=4, radius1=0.8, depth=1.2, location=(0.0, 0.0, 0.0))
    bpy.context.active_object.rotation_euler[2] = math.radians(45.0)
    finalize("blend_pyramid")


def create_torus():
    clear_scene()
    bpy.ops.mesh.primitive_torus_add(major_radius=0.55, minor_radius=0.18, location=(0.0, 0.0, 0.0))
    bpy.context.active_object.rotation_euler[0] = math.radians(90.0)
    finalize("blend_torus")


def create_cylinder():
    clear_scene()
    bpy.ops.mesh.primitive_cylinder_add(radius=0.45, depth=1.1, location=(0.0, 0.0, 0.0))
    finalize("blend_cylinder")


for builder in (
    create_cube,
    create_round_cube,
    create_pyramid,
    create_torus,
    create_cylinder,
):
    builder()
