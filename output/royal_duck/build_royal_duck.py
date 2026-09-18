import bpy, math, json, hashlib, struct
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT=Path(__file__).parent
SCENE=bpy.context.scene
DUCK=bpy.data.objects['RubberDuck']
CROWN=bpy.data.objects['Crown_Gold']

def signature(obj):
    h=hashlib.sha256()
    for v in obj.data.vertices: h.update(struct.pack('<3f',*v.co))
    for p in obj.data.polygons: h.update(struct.pack('<%di'%len(p.vertices),*p.vertices))
    for row in obj.matrix_world: h.update(struct.pack('<4f',*row))
    return h.hexdigest()

original={o.name:signature(o) for o in bpy.data.objects if o.type=='MESH' and (o.name.startswith('Duck_') or o.name in ('RubberDuck','Crown_Gold'))}
bvh=BVHTree.FromPolygons([DUCK.matrix_world@v.co for v in DUCK.data.vertices],[list(p.vertices) for p in DUCK.data.polygons])
coll=bpy.data.collections.new('Royal_Cape_Accessory');SCENE.collection.children.link(coll)
new_meshes=[]

def register(obj,name):
    obj.name=name
    for c in list(obj.users_collection): c.objects.unlink(obj)
    coll.objects.link(obj)
    if obj.type=='MESH':
        for p in obj.data.polygons: p.use_smooth=True
        new_meshes.append(obj)
    return obj

def make_mesh(name,verts,faces,mat=None):
    me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(verts,[],faces);me.update()
    ob=bpy.data.objects.new(name,me);coll.objects.link(ob)
    register(ob,name)
    if mat:me.materials.append(mat)
    return ob

def material(name,col,rough=.55,metal=0):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*col,1)
    p=m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    return m

red=material('Cape_Cranberry_Velvet',(.38,.019,.029),.68)
red.node_tree.nodes.get('Principled BSDF').inputs['Sheen Weight'].default_value=.27
lining=material('Cape_Warm_Cream_Lining',(.72,.47,.20),.68)
fur=material('Collar_Soft_Ivory',(.88,.75,.49),.82)
enamel=material('Clasp_Ivory_Enamel',(.88,.75,.49),.31)
gold=material('Cape_Antique_Gold_Trim',(.84,.47,.075),.34,.4)

# The cloth is sampled against the unchanged duck to drape above the tail and wings.
CY=-.035
PHI=2.30
NX=81;NY=37
def neck(theta):
    z=.106+.010*math.cos(theta)
    dr=Vector((math.sin(theta),math.cos(theta),0))
    hit=bvh.ray_cast(Vector((0,CY,z))+dr*.28,-dr,.5)[0]
    r=(hit-Vector((0,CY,z))).dot(dr) if hit else .047
    return max(.038,r)+.005,z

def cape_raw(theta,t,fold=True):
    nr,nz=neck(theta)
    end=.111+.043*max(0,math.cos(theta))**2
    ease=math.sin(t*math.pi/2)**.85
    r=nr+(end-nr)*ease
    wav=(.0026*math.cos(11*theta+.35)+.0011*math.cos(19*theta))*(math.sin(t*math.pi/2)**1.2) if fold else 0
    r+=wav
    x=r*math.sin(theta);y=CY+r*math.cos(theta)
    z=nz*(1-t)+(.017+.003*math.cos(5*theta))*t+.009*math.sin(math.pi*t)
    hit=bvh.ray_cast(Vector((x,y,.30)),Vector((0,0,-1)),.4)[0]
    floor=hit.z+.007 if hit else .006
    z=max(z,floor)
    return Vector((x,y,z))

grid=[[cape_raw(-PHI+2*PHI*i/(NX-1),j/(NY-1)) for i in range(NX)] for j in range(NY)]
# Smooth the support envelope while respecting collision clearance and keeping folds.
limits=[[p.z for p in row] for row in grid]
for _ in range(18):
    previous=[[p.z for p in row] for row in grid]
    for j in range(1,NY-1):
        for i in range(1,NX-1):
            avg=(previous[j-1][i]+previous[j+1][i]+previous[j][i-1]+previous[j][i+1])/4
            grid[j][i].z=max(limits[j][i],previous[j][i]*.45+avg*.55)

# A vertical drape offset can be too small at steep side surfaces; enforce actual clearance.
for row in grid:
    for pt in row:
        nearest,normal,index,distance=bvh.find_nearest(pt)
        if nearest is not None and distance<.0055:
            away=(pt-nearest).normalized()
            pt[:]=nearest+away*.0055

verts=[tuple(p) for row in grid for p in row]
faces=[]
for j in range(NY-1):
    for i in range(NX-1):
        a=j*NX+i;faces.append((a,a+1,a+NX+1,a+NX))
cape=make_mesh('Cape_Rigged',verts,faces,red);cape.data.materials.append(lining)
uv=cape.data.uv_layers.new(name='Cape_UV')
for poly in cape.data.polygons:
    for li in poly.loop_indices:
        vi=cape.data.loops[li].vertex_index;uv.data[li].uv=((vi%NX)/(NX-1),(vi//NX)/(NY-1))
params={cape.name:[(i/(NX-1),j/(NY-1)) for j in range(NY) for i in range(NX)]}

# Continuous gold piping follows the open front edges and hem.
def tube(name,points,radius,mat,uvparams=None,sides=8):
    vs=[];fs=[];pp=[]
    prior_axis=None
    for k,pt in enumerate(points):
        tangent=(points[min(k+1,len(points)-1)]-points[max(0,k-1)]).normalized()
        ref=Vector((0,0,1))
        if abs(tangent.dot(ref))>.95:ref=Vector((0,1,0))
        a=(prior_axis-tangent*prior_axis.dot(tangent)).normalized() if prior_axis is not None else tangent.cross(ref).normalized()
        b=tangent.cross(a).normalized();prior_axis=a
        for q in range(sides):
            off=(a*math.cos(q*2*math.pi/sides)+b*math.sin(q*2*math.pi/sides))*radius
            vs.append(tuple(pt+off));pp.append(uvparams[k] if uvparams else (.5,0))
    for k in range(len(points)-1):
        for q in range(sides):
            fs.append((k*sides+q,k*sides+(q+1)%sides,(k+1)*sides+(q+1)%sides,(k+1)*sides+q))
    fs.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+q for q in range(sides))])
    ob=make_mesh(name,vs,fs,mat);params[ob.name]=pp
    return ob

edge_points=[grid[j][0] for j in range(NY)]+[grid[-1][i] for i in range(1,NX)]+[grid[j][-1] for j in reversed(range(NY-1))]
edge_params=[(0,j/(NY-1)) for j in range(NY)]+[(i/(NX-1),1) for i in range(1,NX)]+[(1,j/(NY-1)) for j in reversed(range(NY-1))]
trim=tube('Cape_Gold_Edging',edge_points,.0014,gold,edge_params)

# Soft ivory collar: a rounded, gently scalloped band rather than human-sized fur.
vs=[];fs=[];NC=160;NS=12
for i in range(NC):
    th=2*math.pi*i/NC;nr,nz=neck(th)
    dr=Vector((math.sin(th),math.cos(th),0))
    center=Vector((dr.x*nr,CY+dr.y*nr,nz+.0025))
    puff=1+.08*math.cos(18*th)+.035*math.sin(31*th)
    for q in range(NS):
        a=q*2*math.pi/NS
        pos=center+dr*(.0074*math.cos(a)*puff)+Vector((0,0,.0058*math.sin(a)*puff))
        vs.append(tuple(pos))
for i in range(NC):
    for q in range(NS):fs.append((i*NS+q,((i+1)%NC)*NS+q,((i+1)%NC)*NS+(q+1)%NS,i*NS+(q+1)%NS))
collar=make_mesh('Cape_Ivory_Collar',vs,fs,fur)

nr,nz=neck(math.pi)
clasp_pos=Vector((0,CY-nr-.007,nz+.001))
bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=16,radius=1,location=clasp_pos)
clasp=register(bpy.context.object,'Cape_Gold_Clasp');clasp.scale=(.007,.0026,.007)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
clasp.data.materials.append(gold)
bpy.ops.mesh.primitive_torus_add(major_segments=40,minor_segments=10,major_radius=.0055,minor_radius=.00065,location=clasp_pos+Vector((0,-.0021,0)),rotation=(math.pi/2,0,0))
rim=register(bpy.context.object,'Cape_Clasp_Rim');rim.data.materials.append(gold)

# Tiny ivory drop emblem ties the clasp to the duck without adding a costume theme.
bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,radius=1,location=clasp_pos+Vector((0,-.0027,0)))
emblem=register(bpy.context.object,'Cape_Clasp_Ivory_Inlay');emblem.scale=(.0025,.0007,.0035)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);emblem.data.materials.append(enamel)

def select_only(ob):
    bpy.ops.object.select_all(action='DESELECT');ob.hide_set(False);ob.select_set(True);bpy.context.view_layer.objects.active=ob

def bake_material(ob,mat,name,dark,light,scale):
    select_only(ob)
    if not ob.data.uv_layers:
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.01);bpy.ops.object.mode_set(mode='OBJECT')
    nodes=mat.node_tree.nodes;links=mat.node_tree.links;p=nodes.get('Principled BSDF')
    texcoord=nodes.new('ShaderNodeTexCoord');noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=scale;noise.inputs['Detail'].default_value=2.5
    ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(*dark,1);ramp.color_ramp.elements[1].color=(*light,1)
    links.new(texcoord.outputs['Generated'],noise.inputs['Vector']);links.new(noise.outputs['Fac'],ramp.inputs['Fac']);links.new(ramp.outputs['Color'],p.inputs['Base Color'])
    img=bpy.data.images.new(name,width=1024,height=1024,alpha=False)
    tex=nodes.new('ShaderNodeTexImage');tex.image=img;nodes.active=tex
    SCENE.render.engine='CYCLES';SCENE.cycles.device='CPU';SCENE.cycles.samples=8
    SCENE.render.bake.use_pass_direct=False;SCENE.render.bake.use_pass_indirect=False;SCENE.render.bake.use_pass_color=True;SCENE.render.bake.margin=12
    # Temporarily remove unused material slots to avoid unrelated bake targets.
    slots=list(ob.data.materials);ob.data.materials.clear();ob.data.materials.append(mat)
    bpy.ops.object.bake(type='DIFFUSE')
    ob.data.materials.clear()
    for m in slots:ob.data.materials.append(m)
    img.filepath_raw=str(OUT/(name+'.png'));img.file_format='PNG';img.save();img.pack();img.filepath='//'+name+'.png'
    links.new(tex.outputs['Color'],p.inputs['Base Color'])
    for node in (texcoord,noise,ramp):nodes.remove(node)
    tex.location=(-320,70)

bake_material(cape,red,'Cape_Red_BaseColor',(.25,.008,.016),(.46,.024,.036),42)
bake_material(collar,fur,'Collar_Ivory_BaseColor',(.68,.51,.28),(.96,.86,.64),65)

# 7 independently adjustable radial chains, each with three deform bones.
arm=bpy.data.armatures.new('Cape_Rig_Data');rig=bpy.data.objects.new('Cape_Rig',arm);coll.objects.link(rig)
rig.show_in_front=True;arm.display_type='STICK'
select_only(rig);bpy.ops.object.mode_set(mode='EDIT')
root=arm.edit_bones.new('Cape_Root');root.head=(0,CY,.101);root.tail=(0,CY,.125)
CHAIN=7;cuts=[.06,.37,.70,1.0]
def grid_at(u,t):
    fi=max(0,min(NX-1,u*(NX-1)));fj=max(0,min(NY-1,t*(NY-1)))
    i=min(NX-2,int(fi));j=min(NY-2,int(fj));a=fi-i;b=fj-j
    return grid[j][i].lerp(grid[j][i+1],a).lerp(grid[j+1][i].lerp(grid[j+1][i+1],a),b)
for k in range(CHAIN):
    u=k/(CHAIN-1);theta=-PHI+2*PHI*u;parent=root
    for seg in range(3):
        b=arm.edit_bones.new(f'Cape_{k+1:02d}_{seg+1:02d}')
        b.head=grid_at(u,cuts[seg]);b.tail=grid_at(u,cuts[seg+1]);b.parent=parent;b.use_connect=seg>0
        tangent=Vector((math.cos(theta),-math.sin(theta),0));b.align_roll(tangent.cross(b.tail-b.head).normalized());parent=b
bpy.ops.object.mode_set(mode='OBJECT')

def skin(ob,coordinates=None):
    ob.parent=rig
    vg={b.name:ob.vertex_groups.new(name=b.name) for b in arm.bones}
    centers=[0,.22,.535,.85]
    for v in ob.data.vertices:
        if coordinates is None: vg['Cape_Root'].add([v.index],1,'REPLACE');continue
        u,t=coordinates[v.index]
        kf=u*(CHAIN-1);k=min(CHAIN-2,int(kf));f=kf-k
        if t>=centers[-1]: sw=[(3,1)]
        else:
            j=next(q for q in range(3) if centers[q]<=t<centers[q+1]);a=(t-centers[j])/(centers[j+1]-centers[j]);sw=[(j,1-a),(j+1,a)]
        combined={}
        for seg,w in sw:
            if seg==0:combined['Cape_Root']=combined.get('Cape_Root',0)+w
            else:
                for chain,cw in [(k,1-f),(k+1,f)]:
                    name=f'Cape_{chain+1:02d}_{seg:02d}';combined[name]=combined.get(name,0)+w*cw
        for name,w in combined.items():
            if w>1e-7:vg[name].add([v.index],w,'REPLACE')
    mod=ob.modifiers.new('Cape skinning','ARMATURE');mod.object=rig

for ob in new_meshes:skin(ob,params.get(ob.name))
# Real thickness is applied after skin weights are created so the lining inherits them.
select_only(cape)
sol=cape.modifiers.new('Cloth thickness','SOLIDIFY');sol.thickness=.0012;sol.offset=0;sol.material_offset=1;sol.material_offset_rim=1
bpy.ops.object.modifier_apply(modifier=sol.name)

SCENE.render.fps=30;SCENE.frame_start=1;SCENE.frame_end=121
for frame in range(1,122,5):
    cycle=2*math.pi*(frame-1)/120
    for k in range(CHAIN):
        for seg in range(3):
            pb=rig.pose.bones[f'Cape_{k+1:02d}_{seg+1:02d}'];pb.rotation_mode='XYZ'
            phase=k*.38-seg*.7
            flutter=(math.sin(cycle+phase)-math.sin(phase))*.65+(math.sin(2*cycle+phase)-math.sin(phase))*.18
            amplitude=math.radians([.4,1.3,2.5][seg])
            pb.rotation_euler=(amplitude*flutter,0,math.radians([.1,.25,.5][seg])*math.sin(cycle)*math.sin(k*.8))
            pb.keyframe_insert(data_path='rotation_euler',frame=frame,group=pb.name)
action=rig.animation_data.action;action.name='Cape_Idle_Breeze_4s';action.use_fake_user=True
rig['usage']='Pose Cape_01 through Cape_07 chains. Frame 1 is rest; frames 1-121 are a seamless 4-second breeze loop at 30 fps. This is bone animation, not automatic runtime cloth physics.'
SCENE.frame_set(1)

assert original=={name:signature(bpy.data.objects[name]) for name in original}
report={'original_duck_and_crown_unchanged':True,'source_geometry_hashes':original,'rig_bones':len(arm.bones),'animation':action.name,'frames':[1,121],'fps':30,'cape_vertices':len(cape.data.vertices),'skinned_meshes':[o.name for o in new_meshes]}
(OUT/'validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')

# Resolve portable copies of the previously packed crown image.
for img in bpy.data.images:
    if img.name=='Crown_Gold_BaseColor':
        img.filepath_raw=str(OUT/'Crown_Gold_BaseColor.png');img.save();img.pack();img.filepath='//Crown_Gold_BaseColor.png'

bpy.ops.object.select_all(action='DESELECT')
for o in [DUCK,CROWN,rig]+new_meshes:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'RubberDuck_RoyalCape.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True)

cam=bpy.data.objects['Preview_Camera'];cam.location=(.43,-.62,.31);cam.rotation_euler=(Vector((0,.006,.116))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.315
SCENE.camera=cam;SCENE.render.engine='CYCLES';SCENE.cycles.device='CPU';SCENE.cycles.samples=40;SCENE.cycles.use_denoising=True
SCENE.render.resolution_x=1000;SCENE.render.resolution_y=1000;SCENE.render.resolution_percentage=100
select_only(cape)
bpy.context.preferences.filepaths.save_version=0
SCENE.render.filepath=str(OUT/'RubberDuck_RoyalCape_preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'RubberDuck_RoyalCape.blend'))
bpy.ops.render.render(write_still=True)
cam.location=(.43,.58,.32);cam.rotation_euler=(Vector((0,.01,.11))-cam.location).to_track_quat('-Z','Y').to_euler()
SCENE.render.filepath=str(OUT/'RubberDuck_RoyalCape_back.png');bpy.ops.render.render(write_still=True)
print('ROYAL_CAPE_BUILD_COMPLETE',json.dumps(report))
