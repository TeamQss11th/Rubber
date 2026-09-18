import bpy, json
from mathutils import Vector

print('DUCK_INSPECT_BEGIN')
for o in bpy.data.objects:
    print(json.dumps({'name':o.name,'type':o.type,'location':list(o.location),'rotation':list(o.rotation_euler),'scale':list(o.scale),'dimensions':list(o.dimensions),'bounds':[list(o.matrix_world @ Vector(v)) for v in o.bound_box] if o.type=='MESH' else [],'vertices':len(o.data.vertices) if o.type=='MESH' else 0,'materials':[m.name if m else None for m in o.data.materials] if o.type=='MESH' else [],'modifiers':[(m.name,m.type) for m in o.modifiers]},ensure_ascii=False))
for m in bpy.data.materials:
    print('MATERIAL',m.name, list(m.diffuse_color))
    if m.use_nodes:
        for n in m.node_tree.nodes:
            print('NODE',n.name,n.type,[(i.name,str(i.default_value)) for i in n.inputs if not i.is_linked and hasattr(i,'default_value')])
for i in bpy.data.images:
    print('IMAGE',i.name,i.filepath,list(i.size),bool(i.packed_file))
print('DUCK_INSPECT_END')
