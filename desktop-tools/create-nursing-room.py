"""Build the editable Nursing Skills Room with Blender 5.1 or 5.2; no online resources.
Usage: blender --background --python desktop-tools/create-nursing-room.py
Coordinates below are Blender metres (Z up); glTF export converts to Y up.
"""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'desktop-content/nursing-room'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, color, rough=.6, metal=0, emission=0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=rough; p.inputs['Metallic'].default_value=metal
    if emission: p.inputs['Emission Color'].default_value=(*color,1); p.inputs['Emission Strength'].default_value=emission
    return m
WHITE=mat('Warm white enamel',(.81,.84,.83),.4)
WALL=mat('Ivory plaster',(.82,.85,.82),.92)
TEAL=mat('Clinical teal',(.065,.28,.31),.7)
BLUE=mat('Blue upholstery',(.13,.32,.39),.85)
METAL=mat('Brushed aluminium',(.48,.55,.58),.32,.6)
DARK=mat('Graphite rubber',(.035,.055,.07),.8)
WOOD=mat('Oak furniture',(.57,.40,.23),.6)
SHEET=mat('Fresh linen',(.91,.92,.87),.95)
FLOOR=mat('Pale mineral floor',(.48,.56,.57),.8)
GROUT=mat('Floor joints',(.39,.46,.47),.9)
GLOW=mat('Ceiling diffuser',(.91,.96,1),.4,0,.8)
SKY=mat('Window daylight',(.54,.74,.83),.65,0,.25)
LEAF=mat('Plant leaves',(.13,.32,.18),.9)

def group(name, loc=(0,0,0), parent=None):
    o=bpy.data.objects.new(name,None); bpy.context.collection.objects.link(o); o.parent=parent; o.location=loc; return o
def box(name,loc,size,material,parent=None,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=(0,0,0)); o=bpy.context.object; o.name=name
    o.dimensions=size; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.parent=parent; o.location=loc; o.data.materials.append(material)
    if bevel:
        mod=o.modifiers.new('Soft edges','BEVEL'); mod.width=min(bevel,min(size)*.3); mod.segments=3
        o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
    return o
def cylinder(name,loc,radius,depth,material,parent=None,axis='Z'):
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=radius,depth=depth)
    o=bpy.context.object; o.name=name; o.parent=parent; o.location=loc
    if axis=='Y': o.rotation_euler[0]=math.pi/2
    if axis=='X': o.rotation_euler[1]=math.pi/2
    o.data.materials.append(material)
    mod=o.modifiers.new('Rounded edge','BEVEL'); mod.width=min(.008,radius*.2); mod.segments=2
    o.modifiers.new('Weighted normals','WEIGHTED_NORMAL'); return o
def text(name,value,loc,size,material=TEAL,parent=None):
    curve=bpy.data.curves.new(name,'FONT'); curve.body=value; curve.size=size; curve.extrude=.0004; curve.align_x='CENTER'
    o=bpy.data.objects.new(name,curve); bpy.context.collection.objects.link(o); o.parent=parent; o.location=loc
    o.rotation_euler=(math.pi/2,0,0); curve.materials.append(material)
    bpy.context.view_layer.objects.active=o; o.select_set(True); bpy.ops.object.convert(target='MESH'); o.select_set(False); return o
def screen(name,loc,width,height,parent):
    # Dedicated full-UV front plane; runtime replaces this material with a live display.
    vertices=[(-width/2,0,-height/2),(width/2,0,-height/2),(width/2,0,height/2),(-width/2,0,height/2)]
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(vertices,[],[(0,1,2,3)]); mesh.update()
    uv=mesh.uv_layers.new(name='UVMap')
    for loop,xy in zip(uv.data,[(0,0),(1,0),(1,1),(0,1)]): loop.uv=xy
    o=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(o); o.parent=parent; o.location=loc; o.data.materials.append(DARK); return o

room=group('NR_Room')
box('NR_Floor',(0,0,-.1),(7,7.6,.2),FLOOR,room)
# Seamless clinical flooring avoids thin coplanar seam geometry and aliasing.
box('Back wall',(0,3.8,1.65),(7,.18,3.3),WALL,room)
box('Right wall',(3.5,0,1.65),(.18,7.6,3.3),WALL,room)
# A genuine window opening, rather than a painted wall.
box('Window wall base',(-3.5,0,.58),(.18,7.6,1.16),WALL,room)
box('Window wall upper',(-3.5,0,3.02),(.18,7.6,.56),WALL,room)
for y in [-2.9,2.9]: box('Window wall side',(-3.5,y,1.95),(.18,1.8,1.6),WALL,room)
box('Window daylight',(-3.58,0,1.95),(.02,4,1.6),SKY,room)
for z in [1.16,2.75]: box('Window frame',(-3.38,0,z),(.18,4.1,.06),WHITE,room)
for y in [-2,0,2]: box('Window mullion',(-3.38,y,1.95),(.18,.055,1.6),WHITE,room)
box('Window sill',(-3.26,0,1.14),(.4,4.18,.07),WHITE,room)
box('Entrance left wall',(-1.4,-3.8,1.65),(4.2,.18,3.3),WALL,room)
box('Entrance right wall',(3.2,-3.8,1.65),(.6,.18,3.3),WALL,room)
box('Entrance lintel',(1.85,-3.8,2.95),(2.3,.18,.7),WALL,room)
box('Entrance door',(2,-3.83,1.25),(1.9,.1,2.5),TEAL,room)
box('Door glass',(2,-3.90,1.8),(1.25,.02,.75),SKY,room)
box('Door handle',(1.23,-3.70,1.05),(.26,.06,.025),METAL,room)
box('Ceiling',(0,0,3.35),(7,7.6,.12),WALL,room)
for x in [-1.8,1.8]:
    for y in [-1.6,1.6]:
        box('Ceiling light trim',(x,y,3.26),(1.25,.66,.05),METAL,room)
        box('Ceiling light diffuser',(x,y,3.225),(1.16,.57,.022),GLOW,room)
for x in [-3.38,3.38]: box('Protective wall rail',(x,0,.82),(.05,7.4,.18),WOOD,room)
box('Headwall teal panel',(0,3.68,1.65),(3.6,.06,2.0),TEAL,room)
box('Bedhead services',(0,3.60,1.42),(3.35,.13,.24),WHITE,room)
for x in [-.95,-.65,.65,.95]:
    box('Service socket',(x,3.518,1.42),(.13,.025,.13),WHITE,room)
    cylinder('Service port',(x,3.496,1.42),.025,.012,DARK,room,'Y')
text('Room label','NURSING SKILLS LAB',(0,3.59,2.33),.17,SHEET,room)
text('Room subtitle','01  /  PATIENT CARE',(0,3.59,2.08),.075,SHEET,room)

bed=group('NR_Bed',(0,1.65,0))
box('Bed chassis',(0,0,.30),(.86,2.03,.14),METAL,bed)
box('Bed lifting column',(0,0,.48),(.35,.7,.33),WHITE,bed)
box('Bed platform',(0,0,.70),(1.08,2.28,.14),WHITE,bed)
for x in [-.44,.44]:
    for y in [-.9,.9]:
        cylinder('Bed caster',(x,y,.12),.11,.065,DARK,bed,'X')
        box('Caster support',(x,y,.21),(.07,.07,.15),METAL,bed)
box('Seat mattress',(0,-.43,.83),(.98,1.18,.17),BLUE,bed,.055)
box('Foot linen',(0,-.52,.931),(.94,.95,.025),SHEET,bed,.025)
back=group('NR_Bed_Backrest',(0,.13,.79),bed)
box('Backrest frame',(0,.52,0),(1.02,1.06,.055),WHITE,back)
box('Backrest mattress',(0,.52,.09),(.98,1.04,.16),BLUE,back,.055)
box('Pillow',(0,.86,.22),(.72,.32,.13),SHEET,back,.055)
for y in [-1.18,1.18]:
    box('Bed end board',(0,y,.88),(1.14,.08,.60),WHITE,bed,.035)
    box('Bed end inset',(0,y-.047,1.0),(.85,.024,.22),TEAL,bed)
rail=group('NR_Bed_Rail',(.59,-.16,.85),bed)
for y in [-.68,.68]: cylinder('Rail post',(0,y,.1),.022,.44,METAL,rail)
box('Side safety rail',(0,0,.33),(.09,1.54,.21),WHITE,rail,.035)
for y in [-.48,0,.48]: box('Rail hand opening',(0,-.0+y,.34),(.098,.29,.07),TEAL,rail,.025)
other=group('Bed fixed side rail',(-.59,-.16,.85),bed)
box('Fixed rail',(0,0,.33),(.09,1.54,.21),WHITE,other,.035)
for y in [-.68,.68]: cylinder('Fixed rail post',(0,y,.1),.022,.44,METAL,other)
# Dock the handset on the aisle-facing footboard, clear of the moving side rail.
box('Handset cradle',(.36,-1.25,1.04),(.19,.09,.16),TEAL,bed,.018)
remote=group('NR_Bed_Controls',(.36,-1.335,1.10),bed)
remote.rotation_euler[0]=math.radians(-14)
box('Handset shell',(0,0,0),(.17,.05,.31),WHITE,remote,.02)
box('Handset face',(0,-.028,0),(.148,.015,.284),DARK,remote,.012)
text('Handset legend','BED',(0,-.037,.096),.032,SHEET,remote)
for node,z,direction in [('NR_Bed_Raise',.042,1),('NR_Bed_Lower',-.060,-1)]:
    key=box(node,(0,-.042,z),(.112,.024,.08),TEAL,remote,.012)
    for side in [-1,1]:
        mark=box('Handset arrow',(side*.014,-.014,0),(.046,.006,.012),SHEET,key,.003)
        mark.rotation_euler[1]=side*direction*math.radians(38)
cord=bpy.data.curves.new('Handset cord','CURVE')
cord.dimensions='3D'; cord.bevel_depth=.007; cord.bevel_resolution=3
path=cord.splines.new('BEZIER')
points=[(.36,-1.372,.955),(.35,-1.38,.77),(.27,-1.32,.70),(.20,-1.23,.81)]
path.bezier_points.add(len(points)-1)
for point,position in zip(path.bezier_points,points):
    point.co=position
    point.handle_left_type=point.handle_right_type='AUTO'
cable=bpy.data.objects.new('Handset cord',cord)
bpy.context.collection.objects.link(cable); cable.parent=bed; cord.materials.append(DARK)
bpy.ops.object.select_all(action='DESELECT')
cable.select_set(True); bpy.context.view_layer.objects.active=cable
bpy.ops.object.convert(target='MESH')

def import_device(filename,root_name,loc,rename):
    before=set(bpy.context.scene.objects)
    bpy.ops.import_scene.gltf(filepath=str(OUT/'source'/filename))
    imported=set(bpy.context.scene.objects)-before
    parent=group(root_name,loc)
    for o in imported:
        if o.parent not in imported: o.parent=parent
        if o.type=='MESH': o.name=rename.get(o.name,root_name+'_'+o.name.replace(' ','_'))
    return parent,imported

monitor,objects=import_device('patient-monitor.glb','NR_Monitor',(-1.25,1.70,1.31),{'Display':'NR_Monitor_Display','Rotary knob':'NR_Monitor_Knob'})
display=bpy.data.objects['NR_Monitor_Display']; bpy.data.objects.remove(display,do_unlink=True)
screen('NR_Monitor_Display',(-.018,-.079,.161),.258,.169,monitor)
cylinder('Monitor stand',(-1.25,1.70,.72),.025,1.2,METAL,room)
box('Monitor tray',(-1.25,1.70,1.30),(.48,.32,.035),WHITE,room)
for a in range(5):
    angle=a*math.tau/5; x=-1.25+math.cos(angle)*.24; y=1.70+math.sin(angle)*.24
    cylinder('Monitor caster',(x,y,.055),.045,.035,DARK,room,'X')
    arm=box('Monitor base arm',(-1.25+math.cos(angle)*.12,1.70+math.sin(angle)*.12,.10),(.28,.045,.04),METAL,room); arm.rotation_euler[2]=angle

pump,objects=import_device('infusion-pump.glb','NR_Pump',(1.23,1.65,1.19),{})
door=group('NR_Pump_Door',(-.096,-.090,.09),pump)
bpy.context.view_layer.update()
for o in objects:
    if o.type=='MESH' and any(part in o.name for part in ['Door_frame','Door_latch','Door_window']):
        world=o.matrix_world.copy(); o.parent=door; o.matrix_world=world
panel=bpy.data.objects.get('NR_Pump_Display_and_keypad')
if panel: bpy.data.objects.remove(panel,do_unlink=True)
screen('NR_Pump_Display',(.0495,-.089,.119),.088,.056,pump)
for row in range(3):
    for col in range(3): box('Pump keypad',(.022+col*.027,-.09,.070-row*.017),(.02,.008,.012),WHITE,pump,.003)
box('Pump start key',(.025,-.091,.014),(.035,.009,.014),TEAL,pump,.003)
box('Pump pause key',(.075,-.091,.014),(.035,.009,.014),BLUE,pump,.003)
cylinder('IV stand pole',(1.23,1.78,1.05),.016,1.95,METAL,room)
box('IV top bar',(1.23,1.78,2.04),(.47,.028,.025),METAL,room)
box('IV fluid bag',(1.05,1.78,1.86),(.14,.05,.24),WHITE,room,.035)
box('IV fluid bag label',(1.05,1.748,1.86),(.1,.01,.13),BLUE,room,.005)
for a in range(5):
    angle=a*math.tau/5; arm=box('IV stand base',(1.23+math.cos(angle)*.11,1.78+math.sin(angle)*.11,.07),(.27,.035,.03),METAL,room); arm.rotation_euler[2]=angle
    cylinder('IV caster',(1.23+math.cos(angle)*.23,1.78+math.sin(angle)*.23,.045),.035,.03,DARK,room,'X')

call=group('NR_NurseCall',(1.36,3.51,1.42))
box('Call plate',(0,0,0),(.17,.025,.19),WHITE,call,.015)
cylinder('NR_Call_Button',(0,-.022,0),.052,.02,BLUE,call,'Y')
box('NR_Call_Light',(0,-.024,.072),(.08,.012,.015),TEAL,call,.003)
text('Call label','CALL', (0,-.035,-.071),.022,TEAL,call)

# Bedside cabinet and visitor seating.
cab=group('Bedside cabinet',(-1.85,2.95,0))
box('Cabinet body',(0,0,.44),(.62,.57,.80),WOOD,cab,.025)
box('Cabinet top',(0,0,.87),(.67,.62,.06),WHITE,cab)
for z in [.26,.51,.73]:
    box('Drawer face',(0,-.30,z),(.55,.035,.18),WHITE,cab,.015)
    box('Drawer handle',(0,-.326,z),(.20,.03,.018),METAL,cab,.008)
chair=group('Visitor chair',(-2.55,.3,0))
box('Chair seat',(0,0,.46),(.62,.58,.15),BLUE,chair,.06)
box('Chair back',(0,.25,.83),(.62,.12,.67),BLUE,chair,.055)
for x in [-.24,.24]:
    for y in [-.22,.22]: cylinder('Chair leg',(x,y,.23),.018,.46,WOOD,chair)
# Wash station near the entrance, free of the main walking path.
sink=group('Hand hygiene station',(2.9,-1.7,0))
box('Sink cabinet',(0,0,.46),(.8,.55,.85),WOOD,sink)
box('Sink worktop',(0,0,.92),(.88,.63,.08),WHITE,sink)
box('Sink bowl',(0,-.01,.96),(.52,.38,.025),METAL,sink,.07)
cylinder('Tap',(0,.2,1.08),.018,.24,METAL,sink)
box('Mirror',(0,.28,1.52),(.73,.03,.68),SKY,sink)
box('Sanitizer dispenser',(-.42,.25,1.32),(.12,.12,.24),WHITE,sink)
box('Waste bin',(2.90,-2.57,.23),(.42,.42,.46),WHITE,room,.05)
box('Waste bin lid',(2.90,-2.57,.48),(.44,.44,.06),TEAL,room,.025)

# Deduplicate identical embedded source textures before packaging.
image_hashes={}
for image in list(bpy.data.images):
    if not image.packed_file: continue
    digest=hashlib.sha256(image.packed_file.data).hexdigest()
    if digest in image_hashes:
        for m in bpy.data.materials:
            if not m.use_nodes: continue
            for n in m.node_tree.nodes:
                if n.type=='TEX_IMAGE' and n.image==image: n.image=image_hashes[digest]
    else: image_hashes[digest]=image

# The .blend is an editable source, the self-contained GLB is the runtime asset.
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'nursing-room.blend'))
bpy.ops.export_scene.gltf(filepath=str(OUT/'nursing-room.glb'),export_format='GLB',export_apply=True,export_animations=False,export_cameras=False,export_lights=False,export_extras=False)
sha=hashlib.sha256((OUT/'nursing-room.glb').read_bytes()).hexdigest()
def pose(pos,target,fov=65): return {'position':pos,'target':target,'fovDegrees':fov}
views=[('bed','Bed controls',[.72,1.45,.38],[.36,1.10,-.315],'NR_Bed_Controls',[0,.19,0]),
       ('monitor','Patient monitor',[-1.23,1.55,-.85],[-1.25,1.47,-1.7],'NR_Monitor_Display',[0,0,.004]),
       ('pump','Infusion pump',[1.25,1.42,-.90],[1.26,1.30,-1.65],'NR_Pump_Display',[0,0,.004]),
       ('call','Nurse call',[2.10,1.6,-2.15],[1.36,1.42,-3.51],'NR_Call_Button',[0,0,0])]
zones=[]; hotspots=[]
# Positions for node-local pins come from actual authored transforms, not guesses.
bpy.context.view_layer.update()
for i,(key,title,eye,target,node,offset) in enumerate(views):
    o=bpy.data.objects[node]; point=o.matrix_world.translation
    zones.append({'id':'nursing-'+key,'title':title,'environmentId':'nursing-skills-room','order':i,'calibration':'reviewed','camera':pose(eye,target,40 if key in ['bed','monitor','pump'] else 65)})
    if key=='bed':
        point=o.matrix_world @ Vector((offset[0],-offset[2],offset[1]))
    hotspots.append({'id':'nursing-'+key+'-point','title':title,'description':'Explore the equipment, then use the practice controls. This room uses generic equipment for introductory skills practice.','zoneId':'nursing-'+key,'targetNode':node,'targetPosition':offset,'position':[point.x,point.z,-point.y]})
equipment=[{'id':'bed-01','title':'Adjustable patient bed','kind':'bed','nodeName':'NR_Bed','motionNode':'NR_Bed_Backrest','auxiliaryNode':'NR_Bed_Rail'},
           {'id':'monitor-01','title':'Patient monitor','kind':'monitor','nodeName':'NR_Monitor','screenNode':'NR_Monitor_Display'},
           {'id':'pump-01','title':'Infusion pump','kind':'pump','nodeName':'NR_Pump','screenNode':'NR_Pump_Display','motionNode':'NR_Pump_Door'},
           {'id':'call-01','title':'Nurse call','kind':'call','nodeName':'NR_NurseCall','auxiliaryNode':'NR_Call_Light'}]
backrest={'title':'Adjust the bed backrest','patientRequest':"I'd like to sit up a little. Could you raise the back of my bed?",'equipmentId':'bed-01','targetDegrees':30}
project={'formatVersion':1,'id':'nursing-skills-room','title':'Nursing Skills Room','environmentId':'nursing-skills-room',
 'modelFile':'environments/nursing-skills-room/environment.glb','modelSha256':sha,'navigationMode':'walking',
 'overview':pose([2.45,1.65,3.1],[-.1,1.25,-1.6],68),
 'attribution':{'author':'Cengaverler · Innerscene equipment','license':'Original room; equipment CC0 1.0','sourceUrl':'See desktop-content/nursing-room/SOURCES.md'},
 'equipment':equipment,
 'bedExams':[backrest],
 'bedTrainings':[backrest],
 'editor':{'schemaVersion':1,'environmentId':'nursing-skills-room','modelUrl':'desktop-content/nursing-room/nursing-room.glb','modelTransform':{'position':[0,0,0],'rotation':[0,0,0],'scale':[1,1,1]},'zones':zones,'hotspots':hotspots}}
(OUT/'project-template.json').write_text(json.dumps(project,indent=2)+'\n',encoding='utf-8')
catalogue_path=ROOT/'Assets/_Project/Desktop/Resources/DesktopCatalogue.json'
catalogue=json.loads(catalogue_path.read_text(encoding='utf-8')); catalogue['environments']=[p for p in catalogue['environments'] if p['environmentId']!='nursing-skills-room']; catalogue['environments'].insert(0,project)
catalogue_path.write_text(json.dumps(catalogue,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('Nursing room created:',sha)
