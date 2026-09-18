import bpy, json, hashlib, struct
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT=Path(__file__).parent
report=json.loads((OUT/'validation.json').read_text(encoding='utf-8'))
scene=bpy.context.scene
def signature(obj):
    h=hashlib.sha256()
    for v in obj.data.vertices:h.update(struct.pack('<3f',*v.co))
    for p in obj.data.polygons:h.update(struct.pack('<%di'%len(p.vertices),*p.vertices))
    for row in obj.matrix_world:h.update(struct.pack('<4f',*row))
    return h.hexdigest()
for name,sig in report['source_geometry_hashes'].items():assert signature(bpy.data.objects[name])==sig,name
rig=bpy.data.objects['Cape_Rig'];cape=bpy.data.objects['Cape_Rigged'];duck=bpy.data.objects['RubberDuck']
assert len(rig.data.bones)==22
for name in report['skinned_meshes']:
    ob=bpy.data.objects[name]
    assert any(m.type=='ARMATURE' and m.object==rig for m in ob.modifiers),name
    for v in ob.data.vertices:
        assert abs(sum(g.weight for g in v.groups)-1)<1e-5,(name,v.index)
        assert len([g for g in v.groups if g.weight>1e-7])<=4,(name,v.index,'too many influences')

def evaluated(ob):
    dep=bpy.context.evaluated_depsgraph_get();ev=ob.evaluated_get(dep);me=ev.to_mesh()
    pts=[ev.matrix_world@v.co for v in me.vertices];polys=[list(p.vertices) for p in me.polygons]
    ev.to_mesh_clear();return pts,polys

duckpts,duckfaces=evaluated(duck);duck_bvh=BVHTree.FromPolygons(duckpts,duckfaces)
scene.frame_set(1);rest,faces=evaluated(cape)
checks=[];max_motion=0
for frame in (1,16,31,46,61,76,91,106,121):
    scene.frame_set(frame);pts,faces=evaluated(cape)
    delta=max((a-b).length for a,b in zip(rest,pts));max_motion=max(max_motion,delta)
    overlap=len(duck_bvh.overlap(BVHTree.FromPolygons(pts,faces)))
    checks.append({'frame':frame,'max_displacement_m':delta,'duck_triangle_intersection_pairs':overlap,'min_z_m':min(p.z for p in pts)})
assert max_motion>.001,max_motion
assert checks[-1]['max_displacement_m']<1e-6,checks[-1]
assert all(c['min_z_m']>0 for c in checks),checks
assert all(c['duck_triangle_intersection_pairs']==0 for c in checks),checks
report['blend_reopen']={'geometry_preserved':True,'weights_normalized':True,'max_four_bone_influences':True,'loop_seam_error_m':checks[-1]['max_displacement_m'],'motion_checks':checks}
print('BLEND_VERIFIED',json.dumps(report['blend_reopen']))
source_anim={}
for f in (1,31,61,91,121):scene.frame_set(f);source_anim[f]=evaluated(cape)[0]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/'RubberDuck_RoyalCape.fbx'),anim_offset=0)
rigs=[o for o in bpy.data.objects if o.type=='ARMATURE'];assert len(rigs)==1
rig=rigs[0];assert len(rig.data.bones)==22
assert rig.animation_data and rig.animation_data.action
print('IMPORT_TIMING',bpy.context.scene.render.fps,list(rig.animation_data.action.frame_range))
meshes=[o for o in bpy.data.objects if o.type=='MESH'];assert len(meshes)==8,len(meshes)
for name in report['skinned_meshes']:
    assert any(m.type=='ARMATURE' for m in bpy.data.objects[name].modifiers),name
cape=bpy.data.objects['Cape_Rigged'];scene=bpy.context.scene
errors=[]
for f in (1,31,61,91,121):
    scene.frame_set(f);pts,_=evaluated(cape)
    assert len(pts)==len(source_anim[f])
    error=max((a-b).length for a,b in zip(source_anim[f],pts));errors.append(error)
assert max(errors)<1e-5,errors
imgs=[{'name':i.name,'size':list(i.size),'loaded':i.has_data} for i in bpy.data.images]
assert len([i for i in imgs if i['loaded'] and i['size']==[1024,1024]])==3,imgs
report['fbx_reimport']={'mesh_count':len(meshes),'bone_count':len(rig.data.bones),'action':rig.animation_data.action.name,'animation_max_position_error_m':max(errors),'textures':imgs}
(OUT/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('FBX_VERIFIED',json.dumps(report['fbx_reimport']))
