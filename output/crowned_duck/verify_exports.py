import bpy, json, hashlib, struct
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).parent
report=json.loads((OUT/'geometry_validation.json').read_text(encoding='utf-8'))
duck=bpy.data.objects['RubberDuck']
source_vertices=[tuple(duck.matrix_world@v.co) for v in duck.data.vertices]
def signature(o):
    h=hashlib.sha256()
    for v in o.data.vertices:h.update(struct.pack('<3f',*v.co))
    for p in o.data.polygons:
        h.update(struct.pack('<%di'%len(p.vertices),*p.vertices));h.update(struct.pack('<i',p.material_index))
    for row in o.matrix_world:h.update(struct.pack('<4f',*row))
    return h.hexdigest()
for name,data in report['original'].items():
    assert signature(bpy.data.objects[name])==data['sha256'],name
packed=[i.name for i in bpy.data.images if i.packed_file]
assert 'Crown_Gold_BaseColor' in packed
print('BLEND_REOPEN_VERIFIED',len(source_vertices),packed)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/'RubberDuck_Crown.fbx'))
meshes=[o for o in bpy.data.objects if o.type=='MESH']
assert len(meshes)==2,[(o.name,o.type) for o in bpy.data.objects]
duck=bpy.data.objects['RubberDuck'];crown=bpy.data.objects['Crown_Gold']
assert len(duck.data.vertices)==len(source_vertices)
error=max((duck.matrix_world@v.co-Vector(p)).length for v,p in zip(duck.data.vertices,source_vertices))
assert error<1e-6,error
imgs=[{'name':i.name,'size':list(i.size),'path':i.filepath,'loaded':i.has_data} for i in bpy.data.images]
assert any(i['loaded'] and i['size']==[1024,1024] for i in imgs),imgs
report['blend_reopen_verified']=True
report['fbx_reimport']={'meshes':[o.name for o in meshes],'duck_vertex_count':len(duck.data.vertices),'max_duck_position_error_m':error,'images':imgs}
(OUT/'geometry_validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('FBX_REIMPORT_VERIFIED',json.dumps(report['fbx_reimport']))
