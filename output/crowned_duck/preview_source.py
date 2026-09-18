import bpy
from pathlib import Path
from mathutils import Vector
out=Path(__file__).parent
for o in bpy.data.objects:
    print('VIS',o.name,'hide_render',o.hide_render,'hide_viewport',o.hide_viewport,'hide_get',o.hide_get(),'collections',[(c.name,c.hide_render,c.hide_viewport) for c in o.users_collection])
body=bpy.data.objects['Duck_Body']
top=[body.matrix_world@v.co for v in body.data.vertices if (body.matrix_world@v.co).z>0.197]
print('HEAD_TOP_CENTER',list(sum(top,Vector())/len(top)))
s=bpy.context.scene
s.render.engine='CYCLES'
s.cycles.device='CPU'
s.cycles.samples=24
s.render.resolution_x=800
s.render.resolution_y=800
s.render.resolution_percentage=100
s.render.filepath=str(out/'source_preview.png')
bpy.ops.render.render(write_still=True)
