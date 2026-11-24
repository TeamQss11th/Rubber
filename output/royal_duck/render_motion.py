import bpy
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).parent
frames=OUT/'motion_frames';frames.mkdir(exist_ok=True)
s=bpy.context.scene
s.render.engine='CYCLES';s.cycles.device='CPU';s.cycles.samples=16;s.cycles.use_denoising=True;s.cycles.seed=0
s.render.resolution_x=512;s.render.resolution_y=512;s.render.resolution_percentage=100
cam=s.camera
cam.location=(.49,.54,.30);cam.rotation_euler=(Vector((0,.014,.112))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=.315
for index,frame in enumerate(range(1,121,5)):
    s.frame_set(frame);s.render.filepath=str(frames/f'cape_{index:02d}.png');bpy.ops.render.render(write_still=True)
print('MOTION_FRAMES_COMPLETE')
