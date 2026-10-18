"""Run with Blender --background --python export_unity.py -- DEMO.blend OUT.fbx."""
import bpy, sys, json
from pathlib import Path

source, target = map(Path, sys.argv[sys.argv.index('--') + 1:])
bpy.ops.wm.open_mainfile(filepath=str(source))
scene = bpy.context.scene
scene.name = 'Wisp_AllAnimations'
scene.render.fps = 30
scene.frame_start, scene.frame_end = 0, 538
owners = list(scene.objects) + list(bpy.data.shape_keys)
for owner in owners:
    ad = owner.animation_data
    if not ad:
        continue
    ad.action = None
    for track in ad.nla_tracks:
        track.mute = False
        for strip in track.strips:
            strip.frame_start -= 1
scene.frame_set(0)
collection = bpy.data.collections.get('WISP | export')
assert collection, list(bpy.data.collections.keys())
bpy.ops.object.select_all(action='DESELECT')
for obj in collection.all_objects:
    obj.select_set(True)
assert len(bpy.context.selected_objects) > 50
target.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=str(target), use_selection=True, object_types={'EMPTY', 'MESH'},
    bake_anim=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1, bake_anim_simplify_factor=0,
    add_leaf_bones=False, apply_unit_scale=True, axis_forward='-Z', axis_up='Y',
    use_mesh_modifiers=False, global_scale=1, path_mode='AUTO')
print('WISP_FBX_EXPORTED', target, target.stat().st_size, flush=True)
