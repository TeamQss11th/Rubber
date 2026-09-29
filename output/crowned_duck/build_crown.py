import bpy, math, json, hashlib, struct
from pathlib import Path
from mathutils import Vector, Euler

OUT=Path(__file__).parent
DUCK=bpy.data.objects['RubberDuck']

def signature(o):
    h=hashlib.sha256()
    for v in o.data.vertices: h.update(struct.pack('<3f',*v.co))
    for p in o.data.polygons:
        h.update(struct.pack('<%di'%len(p.vertices),*p.vertices))
        h.update(struct.pack('<i',p.material_index))
    for row in o.matrix_world: h.update(struct.pack('<4f',*row))
    return {'vertices':len(o.data.vertices),'polygons':len(o.data.polygons),'sha256':h.hexdigest()}

before={o.name:signature(o) for o in bpy.data.objects if o.type=='MESH' and (o.name.startswith('Duck_') or o.name=='RubberDuck')}
coll=bpy.data.collections.new('Crown_Accessory')
bpy.context.scene.collection.children.link(coll)
parts=[]

def part(o,name):
    o.name=name
    for c in list(o.users_collection): c.objects.unlink(o)
    coll.objects.link(o)
    for p in o.data.polygons: p.use_smooth=True
    parts.append(o)
    return o

# A hollow five-point crown with curved scallops, a rolled base and ball finials.
n=240
count=5
phase=-math.pi/2
radius=.0215
thickness=.0014
verts=[]
for i in range(n):
    angle=phase+2*math.pi*i/n
    peak=(1-abs(math.sin(count*(angle-phase)/2)))**1.45
    height=.0115+.017*peak
    top_radius=radius+.0045*(height/.0285)
    for r,z in [(radius,0),(top_radius,height),(top_radius-thickness,height),(radius-thickness,0)]:
        verts.append((r*math.cos(angle),r*math.sin(angle),z))
faces=[]
for i in range(n):
    a=i*4;b=((i+1)%n)*4
    faces.extend([(a,b,b+1,a+1),(a+1,b+1,b+2,a+2),(a+2,b+2,b+3,a+3),(a+3,b+3,b,a)])
mesh=bpy.data.meshes.new('Crown_ScallopedShell')
mesh.from_pydata(verts,[],faces);mesh.update()
shell=bpy.data.objects.new('Crown_Shell',mesh);coll.objects.link(shell)
part(shell,'Crown_Shell')
bpy.context.view_layer.objects.active=shell
shell.select_set(True)
bevel=shell.modifiers.new('Soft gold edges','BEVEL');bevel.width=.00035;bevel.segments=3
bpy.ops.object.modifier_apply(modifier=bevel.name)
for z,r,minor in [(0.001,.02155,.00135),(.005,.0220,.00115)]:
    bpy.ops.mesh.primitive_torus_add(major_segments=96,minor_segments=12,major_radius=r,minor_radius=minor,location=(0,0,z))
    part(bpy.context.object,'Crown_RolledBand')
for i in range(count):
    angle=phase+2*math.pi*i/count
    r=radius+.0045
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=16,radius=.0037,location=(r*math.cos(angle),r*math.sin(angle),.0285))
    part(bpy.context.object,'Crown_RoundTip')
bpy.ops.object.select_all(action='DESELECT')
for o in parts: o.select_set(True)
bpy.context.view_layer.objects.active=shell
bpy.ops.object.join()
crown=bpy.context.object;crown.name='Crown_Gold'
# Keep accessory independent, allowing removal without altering the original duck.
crown.rotation_euler=Euler((math.radians(-5),math.radians(16),math.radians(-10)),'XYZ')
crown.location=(.012,-.0345,.1942)
crown['description']='Small gold crown, offset to the duck right and tilted 16 degrees, based on supplied crown image.'
crown['duck_mesh_unchanged']=True

mat=bpy.data.materials.new('Crown_Warm_Gold');mat.use_nodes=True
mat.diffuse_color=(.83,.47,.065,1)
nodes=mat.node_tree.nodes;links=mat.node_tree.links
bsdf=nodes.get('Principled BSDF')
bsdf.inputs['Metallic'].default_value=.48
bsdf.inputs['Roughness'].default_value=.29
bsdf.inputs['Coat Weight'].default_value=.18
coords=nodes.new('ShaderNodeTexCoord')
noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=9;noise.inputs['Detail'].default_value=3.5
links.new(coords.outputs['Generated'],noise.inputs['Vector'])
ramp=nodes.new('ShaderNodeValToRGB')
ramp.color_ramp.elements[0].position=.18;ramp.color_ramp.elements[0].color=(.75,.37,.035,1)
ramp.color_ramp.elements[1].position=.83;ramp.color_ramp.elements[1].color=(1,.72,.19,1)
links.new(noise.outputs['Fac'],ramp.inputs['Fac'])
links.new(ramp.outputs['Color'],bsdf.inputs['Base Color'])
crown.data.materials.clear();crown.data.materials.append(mat)
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.015)
bpy.ops.object.mode_set(mode='OBJECT')
image=bpy.data.images.new('Crown_Gold_BaseColor',width=1024,height=1024,alpha=False)
image.colorspace_settings.name='sRGB'
tex=nodes.new('ShaderNodeTexImage');tex.image=image;nodes.active=tex
s=bpy.context.scene;s.render.engine='CYCLES';s.cycles.device='CPU';s.cycles.samples=32
s.render.bake.use_pass_direct=False;s.render.bake.use_pass_indirect=False;s.render.bake.use_pass_color=True
s.render.bake.margin=12
bpy.ops.object.bake(type='DIFFUSE')
image.filepath_raw=str(OUT/'Crown_Gold_BaseColor.png');image.file_format='PNG';image.save();image.pack()
links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
for node in (coords,noise,ramp):nodes.remove(node)
tex.location=(-300,100)

after={name:signature(bpy.data.objects[name]) for name in before}
assert before==after,'Original duck geometry changed'
(OUT/'geometry_validation.json').write_text(json.dumps({'original_duck_meshes_unchanged':before==after,'original':before,'edited':after,'crown':signature(crown)},indent=2),encoding='utf-8')

# Export only the visible game duck and accessory; omit source editable backups and studio.
bpy.ops.object.select_all(action='DESELECT')
DUCK.select_set(True);crown.select_set(True);bpy.context.view_layer.objects.active=DUCK
bpy.ops.export_scene.fbx(filepath=str(OUT/'RubberDuck_Crown.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True)

s.render.resolution_x=1000;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.cycles.samples=64;s.cycles.use_denoising=True
cam=bpy.data.objects['Preview_Camera']
cam.location=(.40,-.60,.32)
cam.rotation_euler=(Vector((0,-.007,.119))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO';cam.data.ortho_scale=.292
s.camera=cam
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RubberDuck_Crown.blend'))
s.render.filepath=str(OUT/'RubberDuck_Crown_preview.png')
bpy.ops.render.render(write_still=True)
cam.location=(0,-.70,.27)
cam.rotation_euler=(Vector((0,-.012,.12))-cam.location).to_track_quat('-Z','Y').to_euler()
s.render.filepath=str(OUT/'RubberDuck_Crown_front.png')
bpy.ops.render.render(write_still=True)
print('CROWN_BUILD_COMPLETE',json.dumps({'original_preserved':before==after,'crown_vertices':len(crown.data.vertices)}))
