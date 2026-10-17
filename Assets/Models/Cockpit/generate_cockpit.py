import bpy
import bmesh
import math
import os
import shutil
from mathutils import Vector, Euler, Matrix

TEXTURES_DIR = r"E:\Ders\Simulation\Assets\_Project\Art\Textures\Cockpit"

def get_or_create_mat(name, base_color, roughness=0.5, metallic=0.0, emission_color=None, emission_strength=0.0, alpha=1.0, tex_filename=None):
    mat = bpy.data.materials.get(name)
    if not mat:
        mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    
    output = nodes.new(type="ShaderNodeOutputMaterial")
    output.location = (400, 0)
    
    bsdf = nodes.new(type="ShaderNodeBsdfPrincipled")
    bsdf.location = (0, 0)
    links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])
    
    bsdf.inputs["Base Color"].default_value = base_color
    bsdf.inputs["Roughness"].default_value = roughness
    bsdf.inputs["Metallic"].default_value = metallic
    
    if tex_filename:
        tex_path = os.path.join(TEXTURES_DIR, tex_filename)
        if os.path.exists(tex_path):
            img = bpy.data.images.load(tex_path, check_existing=True)
            tex_node = nodes.new(type="ShaderNodeTexImage")
            tex_node.location = (-350, 0)
            tex_node.image = img
            links.new(tex_node.outputs["Color"], bsdf.inputs["Base Color"])
            if emission_strength > 0:
                if "Emission Color" in bsdf.inputs:
                    links.new(tex_node.outputs["Color"], bsdf.inputs["Emission Color"])
                    bsdf.inputs["Emission Strength"].default_value = emission_strength
                elif "Emission" in bsdf.inputs:
                    links.new(tex_node.outputs["Color"], bsdf.inputs["Emission"])
    else:
        if emission_color and emission_strength > 0:
            if "Emission Color" in bsdf.inputs:
                bsdf.inputs["Emission Color"].default_value = emission_color
                bsdf.inputs["Emission Strength"].default_value = emission_strength
            elif "Emission" in bsdf.inputs:
                bsdf.inputs["Emission"].default_value = emission_color
                
    if alpha < 1.0:
        if "Alpha" in bsdf.inputs:
            bsdf.inputs["Alpha"].default_value = alpha
        mat.blend_method = 'BLEND'
    else:
        mat.blend_method = 'OPAQUE'
        
    return mat

def add_box(bm, size, offset, rot=(0,0,0), mat_index=0):
    mat_rot = Euler(rot, 'XYZ').to_matrix().to_4x4()
    mat_trans = Matrix.Translation(Vector(offset))
    transform_matrix = mat_trans @ mat_rot
    
    faces_before = len(bm.faces)
    bmesh.ops.create_cube(bm, size=1.0, matrix=transform_matrix @ Matrix.Diagonal((*size, 1.0)))
    for face in bm.faces[faces_before:]:
        face.material_index = mat_index

def add_quad_display(bm, width, height, offset, rot=(0,0,0), mat_index=0):
    """Creates a planar display screen with perfectly mapped upright UV coordinates"""
    mat_rot = Euler(rot, 'XYZ').to_matrix().to_4x4()
    mat_trans = Matrix.Translation(Vector(offset))
    transform_matrix = mat_trans @ mat_rot
    
    hw = width * 0.5
    hh = height * 0.5
    
    # Vertices in local XY plane facing -Y (into cockpit)
    v1 = bm.verts.new(transform_matrix @ Vector((-hw, 0.0, -hh)))
    v2 = bm.verts.new(transform_matrix @ Vector(( hw, 0.0, -hh)))
    v3 = bm.verts.new(transform_matrix @ Vector(( hw, 0.0,  hh)))
    v4 = bm.verts.new(transform_matrix @ Vector((-hw, 0.0,  hh)))
    
    face = bm.faces.new((v1, v2, v3, v4))
    face.material_index = mat_index
    
    # Direct UV assignment
    uv_layer = bm.loops.layers.uv.verify()
    face.loops[0][uv_layer].uv = (0.0, 0.0)
    face.loops[1][uv_layer].uv = (1.0, 0.0)
    face.loops[2][uv_layer].uv = (1.0, 1.0)
    face.loops[3][uv_layer].uv = (0.0, 1.0)

def add_cylinder(bm, radius, depth, offset, rot=(0,0,0), segments=16, mat_index=0):
    mat_rot = Euler(rot, 'XYZ').to_matrix().to_4x4()
    mat_trans = Matrix.Translation(Vector(offset))
    transform_matrix = mat_trans @ mat_rot
    
    faces_before = len(bm.faces)
    bmesh.ops.create_cone(
        bm,
        cap_ends=True,
        cap_tris=False,
        segments=segments,
        radius1=radius,
        radius2=radius,
        depth=depth,
        matrix=transform_matrix
    )
    for face in bm.faces[faces_before:]:
        face.material_index = mat_index

def create_compound_mesh(name, pivot_world_pos, materials_list, build_fn, parent=None):
    mesh = bpy.data.meshes.new(name + "_mesh")
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    
    for mat in materials_list:
        obj.data.materials.append(mat)
        
    bm = bmesh.new()
    build_fn(bm)
    bm.to_mesh(mesh)
    bm.free()
    
    obj.location = Vector(pivot_world_pos)
    if parent:
        obj.parent = parent
        
    return obj

def unwrap_mesh(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    try:
        bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.02)
    except:
        pass
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)

def build_h145_cockpit():
    print("[CockpitBuilder] Starting high-fidelity Airbus H145 Cockpit generation...")
    
    # 1. Clear scene
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    
    # 2. Units
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    
    # 3. Materials Setup
    mat_dark = get_or_create_mat("Mat_Cockpit_DarkGrey", (0.12, 0.12, 0.14, 1.0), roughness=0.65, metallic=0.05)
    mat_cabin = get_or_create_mat("Mat_Cabin_LightBlueGrey", (0.42, 0.48, 0.54, 1.0), roughness=0.5, metallic=0.0)
    mat_fabric = get_or_create_mat("Mat_Seat_Fabric", (0.28, 0.32, 0.36, 1.0), roughness=0.8, metallic=0.0, tex_filename="Tex_Seat_Fabric.png")
    mat_leather_black = get_or_create_mat("Mat_Seat_Leather_Black", (0.05, 0.05, 0.06, 1.0), roughness=0.35, metallic=0.05)
    mat_harness = get_or_create_mat("Mat_Seatbelt_Blue", (0.18, 0.22, 0.28, 1.0), roughness=0.7)
    mat_chrome = get_or_create_mat("Mat_Chrome_Metal", (0.85, 0.85, 0.88, 1.0), roughness=0.18, metallic=0.95)
    mat_orange = get_or_create_mat("Mat_Safety_Orange", (1.0, 0.36, 0.02, 1.0), roughness=0.3)
    mat_glass = get_or_create_mat("Mat_Glass_Tint", (0.7, 0.82, 0.88, 0.28), roughness=0.1, alpha=0.28)
    
    # Emissive Screen Materials with authentic textures
    mat_vms = get_or_create_mat("Mat_Screen_VMS", (0.05, 0.05, 0.05, 1.0), emission_strength=1.3, tex_filename="Tex_VMS_Screen.png")
    mat_pfd_capt = get_or_create_mat("Mat_Screen_PFD_Capt", (0.05, 0.05, 0.05, 1.0), emission_strength=1.3, tex_filename="Tex_MFD_Captain_Screen.png")
    mat_pfd_copilot = get_or_create_mat("Mat_Screen_PFD_CoPilot", (0.05, 0.05, 0.05, 1.0), emission_strength=1.3, tex_filename="Tex_MFD_CoPilot_Screen.png")
    mat_ieasi = get_or_create_mat("Mat_Screen_IEASI", (0.05, 0.05, 0.05, 1.0), emission_strength=1.3, tex_filename="Tex_IEASI_Screen.png")
    mat_nav_dual = get_or_create_mat("Mat_Screen_DualNav", (0.02, 0.08, 0.02, 1.0), emission_strength=1.3, tex_filename="Tex_Dual_Nav_MFD.png")
    mat_overhead = get_or_create_mat("Mat_Overhead", (0.12, 0.12, 0.14, 1.0), tex_filename="Tex_Overhead_Panel.png")
    mat_engine_sw = get_or_create_mat("Mat_Engine_Switches", (0.12, 0.12, 0.14, 1.0), tex_filename="Tex_Engine_Switches.png")
    
    mat_warn_red = get_or_create_mat("Mat_Warning_Red", (0.9, 0.05, 0.05, 1.0), emission_color=(1.0, 0.05, 0.05, 1.0), emission_strength=2.2)
    mat_warn_amber = get_or_create_mat("Mat_Caution_Amber", (1.0, 0.55, 0.05, 1.0), emission_color=(1.0, 0.6, 0.05, 1.0), emission_strength=2.0)
    mat_lever_yellow = get_or_create_mat("Mat_Lever_Yellow", (0.95, 0.85, 0.1, 1.0), roughness=0.3)
    
    # 4. Roots
    root = bpy.data.objects.new("Helicopter_Cockpit_Root", None)
    bpy.context.collection.objects.link(root)
    
    static_root = bpy.data.objects.new("Structure_Static", None)
    static_root.parent = root
    bpy.context.collection.objects.link(static_root)
    
    interactive_root = bpy.data.objects.new("Interactive_Controls", None)
    interactive_root.parent = root
    bpy.context.collection.objects.link(interactive_root)
    
    all_created_objects = []
    
    # -------------------------------------------------------------
    # 5. STRUCTURE STATIC MESHES
    # -------------------------------------------------------------
    
    # 5.1 Cockpit Floor
    def build_floor(bm):
        add_box(bm, (1.68, 3.4, 0.08), (0.0, -0.1, -0.04), mat_index=0)
        add_box(bm, (0.44, 1.8, 0.01), (0.0, 0.2, 0.005), mat_index=1)
        add_box(bm, (0.04, 1.4, 0.02), (-0.22, -1.0, 0.01), mat_index=2)
        add_box(bm, (0.04, 1.4, 0.02), (0.22, -1.0, 0.01), mat_index=2)
    floor_obj = create_compound_mesh("Cockpit_Floor", (0,0,0), [mat_dark, mat_leather_black, mat_chrome], build_floor, static_root)
    all_created_objects.append(floor_obj)
    
    # 5.2 Windshield & Canopy Frame with A-Pillars & Door Struts
    def build_windshield(bm):
        add_box(bm, (1.68, 0.12, 0.08), (0.0, 0.72, 0.78), mat_index=0)
        # Curved A-Pillars
        add_box(bm, (0.08, 0.08, 0.75), (-0.78, 0.62, 1.15), rot=(0.28, 0, 0.12), mat_index=0)
        add_box(bm, (0.08, 0.08, 0.75), (0.78, 0.62, 1.15), rot=(0.28, 0, -0.12), mat_index=0)
        add_box(bm, (1.56, 0.10, 0.08), (0.0, 0.50, 1.48), mat_index=0)
        # Center post
        add_box(bm, (0.045, 0.05, 0.36), (0.0, 0.49, 1.28), mat_index=0)
        # Door shock absorbers / gas struts
        add_cylinder(bm, 0.014, 0.42, (-0.78, 0.35, 1.25), rot=(0.65, 0, 0.35), mat_index=1)
        add_cylinder(bm, 0.008, 0.22, (-0.78, 0.28, 1.35), rot=(0.65, 0, 0.35), mat_index=2)
        add_cylinder(bm, 0.014, 0.42, (0.78, 0.35, 1.25), rot=(0.65, 0, -0.35), mat_index=1)
        add_cylinder(bm, 0.008, 0.22, (0.78, 0.28, 1.35), rot=(0.65, 0, -0.35), mat_index=2)
        # Glass
        add_box(bm, (0.68, 0.02, 0.62), (-0.38, 0.62, 1.12), rot=(0.32, 0, 0.06), mat_index=3)
        add_box(bm, (0.68, 0.02, 0.62), (0.38, 0.62, 1.12), rot=(0.32, 0, -0.06), mat_index=3)
    windshield_obj = create_compound_mesh("Windshield_Frame", (0,0,0), [mat_dark, mat_leather_black, mat_chrome, mat_glass], build_windshield, static_root)
    all_created_objects.append(windshield_obj)
    
    # 5.3 Overhead Circuit Breaker Panel
    def build_overhead(bm):
        add_box(bm, (0.36, 1.10, 0.08), (0.0, 0.22, 1.46), rot=(-0.10, 0, 0), mat_index=0)
        add_box(bm, (0.32, 0.95, 0.01), (0.0, 0.22, 1.415), rot=(-0.10, 0, 0), mat_index=1)
        for row in range(5):
            for col in range(4):
                bx = -0.12 + col * 0.08
                by = -0.15 + row * 0.16
                add_cylinder(bm, 0.007, 0.016, (bx, by, 1.42), rot=(-0.10, 0, 0), segments=8, mat_index=2)
    overhead_obj = create_compound_mesh("Overhead_Panel_Frame", (0,0,0), [mat_dark, mat_overhead, mat_leather_black], build_overhead, static_root)
    all_created_objects.append(overhead_obj)
    
    # 5.4 Main Glare Shield / Dashboard Shroud
    def build_dashboard(bm):
        add_box(bm, (1.28, 0.38, 0.05), (0.0, 0.58, 1.08), rot=(0.14, 0, 0), mat_index=0)
        add_box(bm, (0.06, 0.36, 0.32), (-0.62, 0.58, 0.92), rot=(0.14, 0, 0), mat_index=0)
        add_box(bm, (0.06, 0.36, 0.32), (0.62, 0.58, 0.92), rot=(0.14, 0, 0), mat_index=0)
        add_box(bm, (1.26, 0.08, 0.36), (0.0, 0.68, 0.88), mat_index=0)
        add_box(bm, (0.58, 0.22, 0.06), (0.0, 0.54, 0.70), rot=(-0.12, 0, 0), mat_index=0)
        add_box(bm, (0.34, 0.44, 0.42), (0.0, 0.48, 0.46), rot=(-0.24, 0, 0), mat_index=0)
        add_box(bm, (0.34, 0.75, 0.36), (0.0, 0.05, 0.18), mat_index=0)
    dash_obj = create_compound_mesh("Main_Dashboard_Shroud", (0,0,0), [mat_dark], build_dashboard, static_root)
    all_created_objects.append(dash_obj)
    
    # 5.5 High-Detail Pilot Seats (Tapered Backrest & Open Hollow Trapezoidal Headrest)
    def build_seat(bm, is_captain=True):
        # Base pedestal
        add_box(bm, (0.42, 0.42, 0.18), (0.0, 0.0, 0.09), mat_index=0)
        # Bucket seat cushion with side bolsters
        add_box(bm, (0.40, 0.44, 0.10), (0.0, 0.04, 0.23), mat_index=1)
        add_box(bm, (0.06, 0.46, 0.14), (-0.19, 0.04, 0.27), mat_index=1)
        add_box(bm, (0.06, 0.46, 0.14), (0.19, 0.04, 0.27), mat_index=1)
        
        # Lower backrest (Z: 0.25 to 0.58)
        add_box(bm, (0.44, 0.10, 0.33), (0.0, -0.15, 0.43), rot=(-0.12, 0, 0), mat_index=1)
        # Upper backrest with tapered curved shoulders (Z: 0.58 to 0.86)
        add_box(bm, (0.34, 0.08, 0.28), (0.0, -0.18, 0.72), rot=(-0.12, 0, 0), mat_index=1)
        
        # Twin headrest mounting posts (Z: 0.86 to 0.96)
        hr_y = -0.21
        add_cylinder(bm, 0.010, 0.12, (-0.07, hr_y, 0.91), rot=(-0.12, 0, 0), mat_index=2)
        add_cylinder(bm, 0.010, 0.12, (0.07, hr_y, 0.91), rot=(-0.12, 0, 0), mat_index=2)
        
        # Open Hollow Trapezoidal Headrest (Z: 0.96 to 1.18)
        top_y = hr_y - 0.025
        mid_z = 1.07
        # Bottom bar
        add_box(bm, (0.22, 0.04, 0.035), (0.0, hr_y, 0.975), rot=(-0.12, 0, 0), mat_index=3)
        # Top bar (wider)
        add_box(bm, (0.28, 0.04, 0.04), (0.0, top_y, 1.17), rot=(-0.12, 0, 0), mat_index=3)
        # Left angled upright
        add_box(bm, (0.04, 0.04, 0.20), (-0.11, hr_y - 0.012, mid_z), rot=(-0.12, 0, -0.14), mat_index=3)
        # Right angled upright
        add_box(bm, (0.04, 0.04, 0.20), (0.11, hr_y - 0.012, mid_z), rot=(-0.12, 0, 0.14), mat_index=3)
        
        # 4-point seatbelt harness
        add_box(bm, (0.065, 0.015, 0.38), (-0.09, -0.13, 0.60), rot=(-0.12, 0, -0.10), mat_index=4)
        add_box(bm, (0.065, 0.015, 0.38), (0.09, -0.13, 0.60), rot=(-0.12, 0, 0.10), mat_index=4)
        add_cylinder(bm, 0.030, 0.018, (0.0, -0.06, 0.35), rot=(1.45, 0, 0), segments=16, mat_index=2)
        
        # Outer armrest
        arm_x = 0.23 if is_captain else -0.23
        add_box(bm, (0.045, 0.28, 0.04), (arm_x, 0.0, 0.44), mat_index=3)
    
    seat_capt = create_compound_mesh("Seat_Captain_Right", (0.46, -0.15, 0.0), [mat_dark, mat_fabric, mat_chrome, mat_leather_black, mat_harness], lambda bm: build_seat(bm, True), static_root)
    seat_copilot = create_compound_mesh("Seat_CoPilot_Left", (-0.46, -0.15, 0.0), [mat_dark, mat_fabric, mat_chrome, mat_leather_black, mat_harness], lambda bm: build_seat(bm, False), static_root)
    all_created_objects.extend([seat_capt, seat_copilot])
    
    # 5.6 Rear Medical Cabin (Moved further back into cabin so Dräger monitor doesn't block Co-pilot)
    def build_medical_cabin(bm):
        add_box(bm, (1.68, 0.08, 1.48), (0.0, -1.75, 0.74), mat_index=0)
        add_box(bm, (0.08, 2.2, 1.48), (-0.84, -0.65, 0.74), mat_index=0)
        add_box(bm, (0.08, 2.2, 1.48), (0.84, -0.65, 0.74), mat_index=0)
        # Orange rescue stretcher
        add_box(bm, (0.52, 1.65, 0.06), (0.0, -1.05, 0.08), mat_index=1)
        add_box(bm, (0.44, 0.28, 0.08), (0.0, -0.42, 0.12), mat_index=1)
        for sy in [-0.7, -1.1, -1.5]:
            add_box(bm, (0.54, 0.06, 0.02), (0.0, sy, 0.11), mat_index=2)
        # Dräger ceiling medical monitor moved back to Y = -0.95
        add_cylinder(bm, 0.02, 0.35, (-0.46, -0.95, 1.35), rot=(0, 0, 0), mat_index=3)
        add_box(bm, (0.28, 0.18, 0.22), (-0.46, -0.95, 1.15), rot=(0.2, 0.35, 0), mat_index=1)
        add_box(bm, (0.24, 0.01, 0.18), (-0.46, -0.85, 1.15), rot=(0.2, 0.35, 0), mat_index=4)
        # Medical Oxygen Cylinders
        add_cylinder(bm, 0.09, 0.85, (0.72, -0.75, 0.48), segments=16, mat_index=5)
        add_cylinder(bm, 0.09, 0.85, (0.72, -0.98, 0.48), segments=16, mat_index=5)
        add_cylinder(bm, 0.025, 0.08, (0.72, -0.75, 0.94), segments=12, mat_index=3)
        add_cylinder(bm, 0.025, 0.08, (0.72, -0.98, 0.94), segments=12, mat_index=3)
    med_obj = create_compound_mesh("Medical_Cabin_Interior", (0,0,0), [mat_cabin, mat_orange, mat_leather_black, mat_chrome, mat_vms, mat_dark], build_medical_cabin, static_root)
    all_created_objects.append(med_obj)
    
    # -------------------------------------------------------------
    # 6. EXACT 22 INTERACTIVE CONTROLS
    # -------------------------------------------------------------
    
    # 01_Cyclic_Captain: Flight stick in front of Captain
    def build_cyclic_capt(bm):
        add_cylinder(bm, 0.06, 0.08, (0.0, 0.0, 0.04), segments=16, mat_index=1)
        add_cylinder(bm, 0.018, 0.26, (0.0, 0.0, 0.18), segments=12, mat_index=0)
        add_cylinder(bm, 0.018, 0.24, (0.0, -0.05, 0.38), rot=(0.35, 0, 0), segments=12, mat_index=0)
        add_box(bm, (0.05, 0.08, 0.18), (0.0, -0.12, 0.54), rot=(0.22, 0, 0), mat_index=1)
        add_cylinder(bm, 0.012, 0.02, (0.0, -0.14, 0.62), rot=(0.4, 0, 0), segments=8, mat_index=2)
        add_box(bm, (0.018, 0.03, 0.03), (0.0, -0.07, 0.56), mat_index=3)
    obj_01 = create_compound_mesh("01_Cyclic_Captain", (0.46, 0.36, 0.0), [mat_dark, mat_leather_black, mat_chrome, mat_warn_red], build_cyclic_capt, interactive_root)
    all_created_objects.append(obj_01)
    
    # 02_Collective_Captain: Left side of Captain seat
    def build_collective_capt(bm):
        add_box(bm, (0.08, 0.12, 0.08), (0.0, 0.0, 0.04), mat_index=0)
        add_cylinder(bm, 0.04, 0.08, (0.0, 0.06, 0.06), rot=(1.1, 0, 0), segments=12, mat_index=1)
        add_cylinder(bm, 0.018, 0.42, (0.0, 0.18, 0.15), rot=(0.75, 0, 0), segments=12, mat_index=0)
        add_cylinder(bm, 0.026, 0.16, (0.0, 0.26, 0.23), rot=(0.75, 0, 0), segments=16, mat_index=1)
        add_box(bm, (0.06, 0.10, 0.08), (0.0, 0.36, 0.30), rot=(0.55, 0, 0), mat_index=1)
        add_cylinder(bm, 0.008, 0.025, (0.015, 0.38, 0.34), rot=(0.55, 0, 0), segments=8, mat_index=2)
        add_cylinder(bm, 0.008, 0.025, (-0.015, 0.38, 0.34), rot=(0.55, 0, 0), segments=8, mat_index=2)
    obj_02 = create_compound_mesh("02_Collective_Captain", (0.24, -0.02, 0.16), [mat_dark, mat_leather_black, mat_chrome], build_collective_capt, interactive_root)
    all_created_objects.append(obj_02)
    
    # 03_YawPedals_Captain: Dual pedals on front right floor
    def build_pedals(bm):
        add_box(bm, (0.34, 0.06, 0.04), (0.0, 0.0, 0.02), mat_index=0)
        add_box(bm, (0.10, 0.16, 0.03), (-0.11, 0.05, 0.08), rot=(0.45, 0, 0), mat_index=1)
        add_cylinder(bm, 0.012, 0.12, (-0.11, 0.02, 0.04), rot=(0.45, 0, 0), segments=8, mat_index=2)
        add_box(bm, (0.10, 0.16, 0.03), (0.11, 0.05, 0.08), rot=(0.45, 0, 0), mat_index=1)
        add_cylinder(bm, 0.012, 0.12, (0.11, 0.02, 0.04), rot=(0.45, 0, 0), segments=8, mat_index=2)
    obj_03 = create_compound_mesh("03_YawPedals_Captain", (0.46, 0.88, 0.0), [mat_dark, mat_leather_black, mat_chrome], build_pedals, interactive_root)
    all_created_objects.append(obj_03)
    
    # 04_MFD_Captain: Right PFD with emissive screen
    def build_mfd_capt(bm):
        add_box(bm, (0.28, 0.04, 0.32), (0.0, 0.0, 0.0), mat_index=0)
        # Emissive screen face
        add_box(bm, (0.24, 0.006, 0.28), (0.0, -0.022, 0.0), mat_index=1)
        for i in range(5):
            add_box(bm, (0.03, 0.012, 0.014), (-0.09 + i*0.045, -0.022, -0.145), mat_index=2)
        add_cylinder(bm, 0.014, 0.025, (-0.11, -0.025, -0.13), rot=(1.57, 0, 0), segments=12, mat_index=3)
        add_cylinder(bm, 0.014, 0.025, (0.11, -0.025, -0.13), rot=(1.57, 0, 0), segments=12, mat_index=3)
    obj_04 = create_compound_mesh("04_MFD_Captain", (0.34, 0.64, 0.90), [mat_dark, mat_pfd_capt, mat_leather_black, mat_chrome], build_mfd_capt, interactive_root)
    all_created_objects.append(obj_04)
    
    # 05_VMS_Display: Center Engine Instruments
    def build_vms(bm):
        add_box(bm, (0.24, 0.04, 0.34), (0.0, 0.0, 0.0), mat_index=0)
        add_box(bm, (0.21, 0.006, 0.30), (0.0, -0.022, 0.0), mat_index=1)
        for i in range(5):
            add_box(bm, (0.028, 0.012, 0.014), (-0.08 + i*0.04, -0.022, -0.155), mat_index=2)
    obj_05 = create_compound_mesh("05_VMS_Display", (0.0, 0.65, 0.90), [mat_dark, mat_vms, mat_leather_black], build_vms, interactive_root)
    all_created_objects.append(obj_05)
    
    # 06_IEASI: Backup Attitude Indicator
    def build_ieasi(bm):
        add_box(bm, (0.12, 0.035, 0.12), (0.0, 0.0, 0.0), mat_index=0)
        add_box(bm, (0.095, 0.006, 0.095), (0.0, -0.020, 0.0), mat_index=1)
        add_cylinder(bm, 0.010, 0.02, (-0.045, -0.022, -0.045), rot=(1.57, 0, 0), segments=12, mat_index=2)
    obj_06 = create_compound_mesh("06_IEASI", (-0.17, 0.65, 0.98), [mat_dark, mat_ieasi, mat_chrome], build_ieasi, interactive_root)
    all_created_objects.append(obj_06)
    
    # 07_MFD_CoPilot_Upper: Left PFD
    def build_mfd_copilot(bm):
        add_box(bm, (0.28, 0.04, 0.32), (0.0, 0.0, 0.0), mat_index=0)
        add_box(bm, (0.24, 0.006, 0.28), (0.0, -0.022, 0.0), mat_index=1)
        for i in range(5):
            add_box(bm, (0.03, 0.012, 0.014), (-0.09 + i*0.045, -0.022, -0.145), mat_index=2)
        add_cylinder(bm, 0.014, 0.025, (-0.11, -0.025, -0.13), rot=(1.57, 0, 0), segments=12, mat_index=3)
        add_cylinder(bm, 0.014, 0.025, (0.11, -0.025, -0.13), rot=(1.57, 0, 0), segments=12, mat_index=3)
    obj_07 = create_compound_mesh("07_MFD_CoPilot_Upper", (-0.38, 0.64, 0.90), [mat_dark, mat_pfd_copilot, mat_leather_black, mat_chrome], build_mfd_copilot, interactive_root)
    all_created_objects.append(obj_07)
    
    # 08_MFD_Center_Dual: Slanted radar screens
    def build_mfd_dual(bm):
        add_box(bm, (0.28, 0.18, 0.04), (0.0, 0.0, 0.0), rot=(-0.45, 0, 0), mat_index=0)
        add_box(bm, (0.24, 0.14, 0.005), (0.0, -0.005, 0.018), rot=(-0.45, 0, 0), mat_index=1)
        add_box(bm, (0.012, 0.14, 0.01), (0.0, -0.005, 0.022), rot=(-0.45, 0, 0), mat_index=2)
    obj_08 = create_compound_mesh("08_MFD_Center_Dual", (0.0, 0.52, 0.62), [mat_dark, mat_nav_dual, mat_leather_black], build_mfd_dual, interactive_root)
    all_created_objects.append(obj_08)
    
    # 09_Audio_Panels
    def build_audio(bm):
        add_box(bm, (0.26, 0.12, 0.035), (0.0, 0.0, 0.0), rot=(-0.35, 0, 0), mat_index=0)
        for ax in [-0.07, 0.07]:
            add_cylinder(bm, 0.012, 0.02, (ax, 0.01, 0.025), rot=(-0.35, 0, 0), segments=10, mat_index=1)
            add_cylinder(bm, 0.008, 0.016, (ax + 0.03, 0.01, 0.025), rot=(-0.35, 0, 0), segments=8, mat_index=1)
    obj_09 = create_compound_mesh("09_Audio_Panels", (0.0, 0.44, 0.54), [mat_dark, mat_chrome], build_audio, interactive_root)
    all_created_objects.append(obj_09)
    
    # 10_Remote_Compass_Slave
    def build_compass_slave(bm):
        add_box(bm, (0.11, 0.09, 0.03), (0.0, 0.0, 0.0), rot=(-0.25, 0, 0), mat_index=0)
        add_cylinder(bm, 0.012, 0.018, (0.0, 0.01, 0.02), rot=(-0.25, 0, 0), segments=10, mat_index=1)
    obj_10 = create_compound_mesh("10_Remote_Compass_Slave", (-0.07, 0.35, 0.47), [mat_dark, mat_chrome], build_compass_slave, interactive_root)
    all_created_objects.append(obj_10)
    
    # 11_Autopilot_Control_Panel
    def build_autopilot(bm):
        add_box(bm, (0.11, 0.09, 0.03), (0.0, 0.0, 0.0), rot=(-0.25, 0, 0), mat_index=0)
        for row in range(2):
            for col in range(3):
                add_box(bm, (0.02, 0.018, 0.01), (-0.03 + col*0.03, -0.02 + row*0.035, 0.02), rot=(-0.25, 0, 0), mat_index=1)
    obj_11 = create_compound_mesh("11_Autopilot_Control_Panel", (-0.07, 0.25, 0.42), [mat_dark, mat_warn_amber], build_autopilot, interactive_root)
    all_created_objects.append(obj_11)
    
    # 12_I_Drive_Control: Large rotary dial
    def build_idrive(bm):
        add_box(bm, (0.08, 0.08, 0.025), (0.0, 0.0, 0.01), mat_index=0)
        add_cylinder(bm, 0.032, 0.035, (0.0, 0.0, 0.03), segments=20, mat_index=1)
        add_cylinder(bm, 0.012, 0.008, (0.015, 0.015, 0.048), segments=8, mat_index=2)
    obj_12 = create_compound_mesh("12_I_Drive_Control", (-0.08, 0.12, 0.36), [mat_dark, mat_leather_black, mat_chrome], build_idrive, interactive_root)
    all_created_objects.append(obj_12)
    
    # 13_Weather_Radar_Panel
    def build_wx_radar(bm):
        add_box(bm, (0.11, 0.09, 0.03), (0.0, 0.0, 0.0), mat_index=0)
        add_box(bm, (0.08, 0.022, 0.005), (0.0, 0.025, 0.018), mat_index=1)
        add_cylinder(bm, 0.010, 0.018, (-0.025, -0.018, 0.02), segments=10, mat_index=2)
        add_cylinder(bm, 0.010, 0.018, (0.025, -0.018, 0.02), segments=10, mat_index=2)
    obj_13 = create_compound_mesh("13_Weather_Radar_Panel", (-0.07, 0.01, 0.36), [mat_dark, mat_nav_dual, mat_chrome], build_wx_radar, interactive_root)
    all_created_objects.append(obj_13)
    
    # 14_ELT_Switch
    def build_elt(bm):
        add_box(bm, (0.09, 0.07, 0.03), (0.0, 0.0, 0.0), rot=(-0.35, 0, 0), mat_index=0)
        add_box(bm, (0.035, 0.05, 0.03), (0.0, 0.0, 0.025), rot=(-0.35, 0, 0), mat_index=1)
        add_cylinder(bm, 0.006, 0.025, (0.0, 0.0, 0.025), rot=(-0.35, 0, 0), segments=8, mat_index=2)
    obj_14 = create_compound_mesh("14_ELT_Switch", (0.07, 0.44, 0.54), [mat_dark, mat_warn_red, mat_chrome], build_elt, interactive_root)
    all_created_objects.append(obj_14)
    
    # 15_Engine_Main_Switches: Dual engine mode switches with yellow levers and red flip guards
    def build_engine_switches(bm):
        add_box(bm, (0.24, 0.02, 0.09), (0.0, 0.0, 0.0), mat_index=0)
        add_quad_display(bm, 0.22, 0.08, (0.0, -0.012, 0.0), mat_index=1)
        for ex in [-0.065, 0.065]:
            add_box(bm, (0.036, 0.04, 0.06), (ex, -0.03, 0.0), mat_index=2)
            add_cylinder(bm, 0.006, 0.035, (ex, -0.025, 0.0), rot=(1.2, 0, 0), segments=8, mat_index=3)
            add_cylinder(bm, 0.010, 0.018, (ex, -0.04, 0.01), rot=(1.2, 0, 0), segments=8, mat_index=4)
    obj_15 = create_compound_mesh("15_Engine_Main_Switches", (0.09, 0.60, 0.72), [mat_dark, mat_engine_sw, mat_warn_red, mat_chrome, mat_lever_yellow], build_engine_switches, interactive_root)
    all_created_objects.append(obj_15)
    
    # 16_FMS_Selectors
    def build_fms(bm):
        add_box(bm, (0.16, 0.025, 0.08), (0.0, 0.0, 0.0), mat_index=0)
        for fx in [-0.05, 0.0, 0.05]:
            add_cylinder(bm, 0.012, 0.02, (fx, -0.018, 0.0), rot=(1.57, 0, 0), segments=10, mat_index=1)
    obj_16 = create_compound_mesh("16_FMS_Selectors", (-0.11, 0.60, 0.72), [mat_dark, mat_chrome], build_fms, interactive_root)
    all_created_objects.append(obj_16)
    
    # 17_Fire_Warning_Light
    def build_fire_light(bm):
        add_box(bm, (0.065, 0.035, 0.065), (0.0, 0.0, 0.0), mat_index=0)
        add_box(bm, (0.045, 0.012, 0.045), (0.0, -0.018, 0.0), mat_index=1)
    obj_17 = create_compound_mesh("17_Fire_Warning_Light", (0.13, 0.60, 1.05), [mat_dark, mat_warn_red], build_fire_light, interactive_root)
    all_created_objects.append(obj_17)
    
    # 18_Caution_Warnings_Panel
    def build_caution(bm):
        add_box(bm, (0.22, 0.03, 0.045), (0.0, 0.0, 0.0), mat_index=0)
        for i in range(6):
            add_box(bm, (0.028, 0.008, 0.028), (-0.08 + i*0.032, -0.015, 0.0), mat_index=1)
    obj_18 = create_compound_mesh("18_Caution_Warnings_Panel", (0.0, 0.60, 1.05), [mat_dark, mat_warn_amber], build_caution, interactive_root)
    all_created_objects.append(obj_18)
    
    # 19_Magnetic_Compass
    def build_compass(bm):
        add_box(bm, (0.05, 0.06, 0.04), (0.0, 0.0, 0.0), mat_index=0)
        add_cylinder(bm, 0.038, 0.06, (0.0, -0.04, 0.0), rot=(1.57, 0, 0), segments=16, mat_index=0)
        add_box(bm, (0.045, 0.005, 0.035), (0.0, -0.072, 0.0), mat_index=1)
    obj_19 = create_compound_mesh("19_Magnetic_Compass", (0.0, 0.48, 1.18), [mat_dark, mat_chrome], build_compass, interactive_root)
    all_created_objects.append(obj_19)
    
    # 20_Collective_CoPilot
    def build_collective_copilot(bm):
        add_box(bm, (0.08, 0.12, 0.08), (0.0, 0.0, 0.04), mat_index=0)
        add_cylinder(bm, 0.04, 0.08, (0.0, 0.06, 0.06), rot=(1.1, 0, 0), segments=12, mat_index=1)
        add_cylinder(bm, 0.018, 0.42, (0.0, 0.18, 0.15), rot=(0.75, 0, 0), segments=12, mat_index=0)
        add_cylinder(bm, 0.026, 0.16, (0.0, 0.26, 0.23), rot=(0.75, 0, 0), segments=16, mat_index=1)
        add_box(bm, (0.06, 0.10, 0.08), (0.0, 0.36, 0.30), rot=(0.55, 0, 0), mat_index=1)
    obj_20 = create_compound_mesh("20_Collective_CoPilot", (-0.68, -0.02, 0.16), [mat_dark, mat_leather_black], build_collective_copilot, interactive_root)
    all_created_objects.append(obj_20)
    
    # 21_YawPedals_CoPilot
    def build_pedals_copilot(bm):
        add_box(bm, (0.34, 0.06, 0.04), (0.0, 0.0, 0.02), mat_index=0)
        add_box(bm, (0.10, 0.16, 0.03), (-0.11, 0.05, 0.08), rot=(0.45, 0, 0), mat_index=1)
        add_cylinder(bm, 0.012, 0.12, (-0.11, 0.02, 0.04), rot=(0.45, 0, 0), segments=8, mat_index=2)
        add_box(bm, (0.10, 0.16, 0.03), (0.11, 0.05, 0.08), rot=(0.45, 0, 0), mat_index=1)
        add_cylinder(bm, 0.012, 0.12, (0.11, 0.02, 0.04), rot=(0.45, 0, 0), segments=8, mat_index=2)
    obj_21 = create_compound_mesh("21_YawPedals_CoPilot", (-0.46, 0.88, 0.0), [mat_dark, mat_leather_black, mat_chrome], build_pedals_copilot, interactive_root)
    all_created_objects.append(obj_21)
    
    # 22_Cyclic_CoPilot
    def build_cyclic_copilot(bm):
        add_cylinder(bm, 0.06, 0.08, (0.0, 0.0, 0.04), segments=16, mat_index=1)
        add_cylinder(bm, 0.018, 0.26, (0.0, 0.0, 0.18), segments=12, mat_index=0)
        add_cylinder(bm, 0.018, 0.24, (0.0, -0.05, 0.38), rot=(0.35, 0, 0), segments=12, mat_index=0)
        add_box(bm, (0.05, 0.08, 0.18), (0.0, -0.12, 0.54), rot=(0.22, 0, 0), mat_index=1)
        add_cylinder(bm, 0.012, 0.02, (0.0, -0.14, 0.62), rot=(0.4, 0, 0), segments=8, mat_index=2)
        add_box(bm, (0.018, 0.03, 0.03), (0.0, -0.07, 0.56), mat_index=3)
    obj_22 = create_compound_mesh("22_Cyclic_CoPilot", (-0.46, 0.36, 0.0), [mat_dark, mat_leather_black, mat_chrome, mat_warn_red], build_cyclic_copilot, interactive_root)
    all_created_objects.append(obj_22)
    
    # 7. Unwrapping non-quad meshes
    print(f"[CockpitBuilder] Unwrapping {len(all_created_objects)} meshes...")
    for o in all_created_objects:
        unwrap_mesh(o)
        
    # 8. Save Blend File
    blend_path = r"E:\Ders\Simulation\Assets\Models\Cockpit\Helicopter_Cockpit_Modular.blend"
    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    print(f"[CockpitBuilder] Saved blend file: {blend_path}")
    
    # 9. Export FBX for Unity (-Z Forward, Y Up)
    fbx_target_1 = r"E:\Ders\Simulation\Assets\Models\Cockpit\Helicopter_Cockpit_Modular.fbx"
    fbx_target_2 = r"E:\Ders\Simulation\Assets\_Project\Art\Models\Cockpit\Helicopter_Cockpit_Modular.fbx"
    
    bpy.ops.object.select_all(action='DESELECT')
    root.select_set(True)
    for c in root.children_recursive:
        c.select_set(True)
        
    bpy.ops.export_scene.fbx(
        filepath=fbx_target_1,
        use_selection=True,
        global_scale=1.0,
        apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=True,
        object_types={'EMPTY', 'MESH'},
        mesh_smooth_type='FACE',
        embed_textures=True
    )
    shutil.copyfile(fbx_target_1, fbx_target_2)
    print(f"[CockpitBuilder] Exported FBX to {fbx_target_1} and {fbx_target_2}")

if __name__ == "__main__":
    build_h145_cockpit()
