import math
import os

import bpy
from mathutils import Vector


PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
OUTPUT_DIR = os.path.join(PROJECT_ROOT, "Assets/Art/Characters/PrototypeHumanoids")
SOURCE_FILE = os.path.join(PROJECT_ROOT, "ArtSource/Characters/PrototypeHumanoids.blend")
PREVIEW_FILE = "/tmp/prototype-humanoids-preview.png"


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (
        bpy.data.meshes,
        bpy.data.armatures,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        for block in list(collection):
            if block.users == 0:
                collection.remove(block)


def make_material(name, color, roughness=0.78):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Roughness"].default_value = roughness
    return material


def assign_material_and_bone(obj, material, bone_name):
    obj.data.materials.append(material)
    group = obj.vertex_groups.new(name=bone_name)
    group.add(range(len(obj.data.vertices)), 1.0, "REPLACE")
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    return obj


def apply_transform(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.select_set(False)


def add_ellipsoid(name, location, scale, material, bone_name, parts, subdivisions=2):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_transform(obj)
    assign_material_and_bone(obj, material, bone_name)
    parts.append(obj)
    return obj


def add_cylinder(name, start, end, radius, material, bone_name, parts, vertices=10):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=direction.length,
        location=(start + end) * 0.5,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0.0, 0.0, 1.0)).rotation_difference(direction.normalized())
    apply_transform(obj)
    assign_material_and_bone(obj, material, bone_name)
    parts.append(obj)
    return obj


def add_box(name, location, size, material, bone_name, parts, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = size
    apply_transform(obj)
    if bevel > 0.0:
        modifier = obj.modifiers.new("Soft Edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 1
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)
    assign_material_and_bone(obj, material, bone_name)
    parts.append(obj)
    return obj


def add_nose(name, location, material, parts):
    bpy.ops.mesh.primitive_cone_add(
        vertices=6,
        radius1=0.022,
        radius2=0.006,
        depth=0.055,
        location=location,
        rotation=(math.radians(90.0), 0.0, 0.0),
    )
    obj = bpy.context.object
    obj.name = name
    apply_transform(obj)
    assign_material_and_bone(obj, material, "Head")
    parts.append(obj)


def add_tapered_torso(name, rings, material, parts):
    segments = 10
    vertices = []
    faces = []
    ring_indices = []

    for z, half_width, half_depth, _weights in rings:
        current = []
        for segment in range(segments):
            angle = math.tau * segment / segments
            current.append(len(vertices))
            vertices.append((math.cos(angle) * half_width, math.sin(angle) * half_depth, z))
        ring_indices.append(current)

    for ring in range(len(ring_indices) - 1):
        current = ring_indices[ring]
        following = ring_indices[ring + 1]
        for segment in range(segments):
            next_segment = (segment + 1) % segments
            faces.append((
                current[segment],
                current[next_segment],
                following[next_segment],
                following[segment],
            ))

    faces.append(tuple(reversed(ring_indices[0])))
    faces.append(tuple(ring_indices[-1]))

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.validate()
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)

    groups = {}
    for ring_index, (_z, _width, _depth, weights) in enumerate(rings):
        for bone_name, weight in weights.items():
            group = groups.get(bone_name)
            if group is None:
                group = obj.vertex_groups.new(name=bone_name)
                groups[bone_name] = group
            group.add(ring_indices[ring_index], weight, "REPLACE")

    for polygon in mesh.polygons:
        polygon.use_smooth = True
    parts.append(obj)
    return obj


def add_bone(edit_bones, name, head, tail, parent=None):
    bone = edit_bones.new(name)
    bone.head = head
    bone.tail = tail
    bone.use_deform = name != "Root"
    if parent is not None:
        bone.parent = parent
    return bone


def build_armature(prefix, points):
    data = bpy.data.armatures.new(f"{prefix}_ArmatureData")
    armature = bpy.data.objects.new(f"{prefix}_Armature", data)
    bpy.context.collection.objects.link(armature)
    armature.show_in_front = True
    armature.data.display_type = "OCTAHEDRAL"
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bones = data.edit_bones

    root = add_bone(bones, "Root", (0.0, 0.0, 0.0), (0.0, 0.0, points["root_tail"]))
    hips = add_bone(bones, "Hips", (0.0, 0.0, points["hips"]), (0.0, 0.0, points["spine"]), root)
    spine = add_bone(bones, "Spine", (0.0, 0.0, points["spine"]), (0.0, 0.0, points["chest"]), hips)
    chest = add_bone(bones, "Chest", (0.0, 0.0, points["chest"]), (0.0, 0.0, points["neck"]), spine)
    neck = add_bone(bones, "Neck", (0.0, 0.0, points["neck"]), (0.0, 0.0, points["head"]), chest)
    add_bone(bones, "Head", (0.0, 0.0, points["head"]), (0.0, 0.0, points["head_top"]), neck)

    for side_name, sign in (("Left", 1.0), ("Right", -1.0)):
        shoulder = (sign * points["shoulder_x"], 0.0, points["shoulder_z"])
        elbow = (sign * points["elbow_x"], 0.0, points["shoulder_z"])
        wrist = (sign * points["wrist_x"], 0.0, points["shoulder_z"])
        hand_end = (sign * points["hand_x"], 0.0, points["shoulder_z"])
        upper_arm = add_bone(bones, f"{side_name}UpperArm", shoulder, elbow, chest)
        lower_arm = add_bone(bones, f"{side_name}LowerArm", elbow, wrist, upper_arm)
        add_bone(bones, f"{side_name}Hand", wrist, hand_end, lower_arm)

        hip = (sign * points["leg_x"], 0.0, points["hips"])
        knee = (sign * points["leg_x"], 0.0, points["knee"])
        ankle = (sign * points["leg_x"], 0.0, points["ankle"])
        foot = (sign * points["leg_x"], -points["foot_mid"], points["foot_z"])
        toe = (sign * points["leg_x"], -points["foot_length"], points["foot_z"])
        upper_leg = add_bone(bones, f"{side_name}UpperLeg", hip, knee, hips)
        lower_leg = add_bone(bones, f"{side_name}LowerLeg", knee, ankle, upper_leg)
        foot_bone = add_bone(bones, f"{side_name}Foot", ankle, foot, lower_leg)
        add_bone(bones, f"{side_name}Toes", foot, toe, foot_bone)

    bpy.ops.object.mode_set(mode="OBJECT")
    armature.select_set(False)
    if len(data.bones) != 20:
        raise RuntimeError(f"Expected 20 bones, created {len(data.bones)}")
    return armature


def join_parts(prefix, parts, armature):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    body = bpy.context.object
    body.name = f"{prefix}_Body"
    try:
        bpy.ops.object.material_slot_remove_unused()
    except RuntimeError:
        pass
    for polygon in body.data.polygons:
        polygon.use_smooth = True
    modifier = body.modifiers.new("Humanoid Armature", "ARMATURE")
    modifier.object = armature
    modifier.use_deform_preserve_volume = True
    body.parent = armature
    body.matrix_parent_inverse = armature.matrix_world.inverted()
    body.select_set(False)
    return body


def build_character(prefix, gender):
    female = gender == "female"
    scale = 0.944 if female else 1.0
    shoulder_half = 0.205 if female else 0.235
    hip_half = 0.19 if female else 0.17
    waist_half = 0.145 if female else 0.155
    depth = 0.115 if female else 0.125
    arm_radius = 0.052 if female else 0.064
    forearm_radius = 0.044 if female else 0.054
    thigh_radius = 0.078 if female else 0.09
    calf_radius = 0.06 if female else 0.072

    points = {
        "root_tail": 0.1 * scale,
        "ankle": 0.105 * scale,
        "knee": 0.555 * scale,
        "hips": 0.985 * scale,
        "spine": 1.12 * scale,
        "chest": 1.315 * scale,
        "neck": 1.505 * scale,
        "head": 1.585 * scale,
        "head_top": 1.79 * scale,
        "shoulder_z": 1.435 * scale,
        "shoulder_x": shoulder_half * 0.84,
        "elbow_x": shoulder_half * 0.84 + 0.305 * scale,
        "wrist_x": shoulder_half * 0.84 + 0.57 * scale,
        "hand_x": shoulder_half * 0.84 + 0.695 * scale,
        "leg_x": hip_half * 0.56,
        "foot_z": 0.055 * scale,
        "foot_mid": 0.14 * scale,
        "foot_length": 0.265 * scale,
    }

    skin = make_material(f"{prefix}_Skin", (0.56, 0.34, 0.22) if not female else (0.62, 0.39, 0.27))
    shirt = make_material(f"{prefix}_Shirt", (0.17, 0.28, 0.22) if not female else (0.39, 0.18, 0.18))
    pants = make_material(f"{prefix}_Pants", (0.11, 0.15, 0.18) if not female else (0.16, 0.13, 0.12))
    shoes = make_material(f"{prefix}_Shoes", (0.055, 0.045, 0.038), 0.9)
    hair = make_material(f"{prefix}_Hair", (0.045, 0.028, 0.018), 0.92)
    eyes = make_material(f"{prefix}_Eyes", (0.025, 0.021, 0.017), 0.45)
    belt = make_material(f"{prefix}_Belt", (0.13, 0.075, 0.035), 0.86)

    armature = build_armature(prefix, points)
    parts = []
    rings = [
        (0.98 * scale, hip_half, depth, {"Hips": 1.0}),
        (1.075 * scale, waist_half * 1.04, depth * 0.94, {"Hips": 0.55, "Spine": 0.45}),
        (1.20 * scale, waist_half, depth, {"Spine": 1.0}),
        (1.345 * scale, shoulder_half * 0.92, depth * (1.12 if female else 1.05), {"Spine": 0.28, "Chest": 0.72}),
        (1.455 * scale, shoulder_half, depth * 0.92, {"Chest": 1.0}),
    ]
    add_tapered_torso(f"{prefix}_Torso", rings, shirt, parts)
    add_ellipsoid(f"{prefix}_Pelvis", (0.0, 0.0, 0.99 * scale), (hip_half, depth, 0.105 * scale), pants, "Hips", parts)
    add_box(f"{prefix}_Belt", (0.0, 0.0, 1.045 * scale), (hip_half * 0.96, depth * 1.02, 0.018 * scale), belt, "Hips", parts, 0.006)

    neck_start = (0.0, 0.0, points["neck"])
    neck_end = (0.0, 0.0, points["head"] + 0.015 * scale)
    add_cylinder(f"{prefix}_Neck", neck_start, neck_end, 0.055 * scale, skin, "Neck", parts, 10)
    head_center = (0.0, -0.004, 1.69 * scale)
    add_ellipsoid(f"{prefix}_Head", head_center, (0.105 * scale, 0.09 * scale, 0.135 * scale), skin, "Head", parts, 2)
    add_ellipsoid(f"{prefix}_EarL", (0.103 * scale, 0.0, 1.69 * scale), (0.018 * scale, 0.012 * scale, 0.028 * scale), skin, "Head", parts, 1)
    add_ellipsoid(f"{prefix}_EarR", (-0.103 * scale, 0.0, 1.69 * scale), (0.018 * scale, 0.012 * scale, 0.028 * scale), skin, "Head", parts, 1)
    add_nose(f"{prefix}_Nose", (0.0, -0.104 * scale, 1.685 * scale), skin, parts)
    add_ellipsoid(f"{prefix}_EyeL", (0.038 * scale, -0.086 * scale, 1.718 * scale), (0.013 * scale, 0.009 * scale, 0.011 * scale), eyes, "Head", parts, 1)
    add_ellipsoid(f"{prefix}_EyeR", (-0.038 * scale, -0.086 * scale, 1.718 * scale), (0.013 * scale, 0.009 * scale, 0.011 * scale), eyes, "Head", parts, 1)
    add_ellipsoid(f"{prefix}_HairCap", (0.0, 0.005, 1.755 * scale), (0.11 * scale, 0.092 * scale, 0.085 * scale), hair, "Head", parts, 2)
    if female:
        add_ellipsoid(f"{prefix}_HairBack", (0.0, 0.078 * scale, 1.64 * scale), (0.09 * scale, 0.055 * scale, 0.16 * scale), hair, "Head", parts, 2)
        add_ellipsoid(f"{prefix}_Ponytail", (0.0, 0.115 * scale, 1.52 * scale), (0.047 * scale, 0.045 * scale, 0.12 * scale), hair, "Head", parts, 2)

    for side_name, sign in (("Left", 1.0), ("Right", -1.0)):
        shoulder = Vector((sign * points["shoulder_x"], 0.0, points["shoulder_z"]))
        elbow = Vector((sign * points["elbow_x"], 0.0, points["shoulder_z"]))
        wrist = Vector((sign * points["wrist_x"], 0.0, points["shoulder_z"]))
        hand_end = Vector((sign * points["hand_x"], 0.0, points["shoulder_z"]))
        add_cylinder(f"{prefix}_{side_name}UpperArm", shoulder, elbow, arm_radius, shirt, f"{side_name}UpperArm", parts)
        add_ellipsoid(f"{prefix}_{side_name}Elbow", elbow, (forearm_radius, forearm_radius, forearm_radius), shirt, f"{side_name}LowerArm", parts, 1)
        add_cylinder(f"{prefix}_{side_name}LowerArm", elbow, wrist, forearm_radius, shirt, f"{side_name}LowerArm", parts)
        add_ellipsoid(
            f"{prefix}_{side_name}Hand",
            (wrist + hand_end) * 0.5,
            ((hand_end - wrist).length * 0.52, 0.046 * scale, 0.057 * scale),
            skin,
            f"{side_name}Hand",
            parts,
            2,
        )

        hip = Vector((sign * points["leg_x"], 0.0, points["hips"]))
        knee = Vector((sign * points["leg_x"], 0.0, points["knee"]))
        ankle = Vector((sign * points["leg_x"], 0.0, points["ankle"]))
        add_cylinder(f"{prefix}_{side_name}UpperLeg", hip, knee, thigh_radius, pants, f"{side_name}UpperLeg", parts)
        add_ellipsoid(f"{prefix}_{side_name}Knee", knee, (calf_radius * 1.04, calf_radius * 1.02, calf_radius * 1.04), pants, f"{side_name}LowerLeg", parts, 1)
        add_cylinder(f"{prefix}_{side_name}LowerLeg", knee, ankle, calf_radius, pants, f"{side_name}LowerLeg", parts)

        rear_y = -points["foot_length"] * 0.28
        front_y = -points["foot_length"] * 0.73
        foot_width = 0.105 * scale if female else 0.12 * scale
        add_box(
            f"{prefix}_{side_name}Foot",
            (sign * points["leg_x"], rear_y, points["foot_z"]),
            (foot_width, points["foot_length"] * 0.44, 0.07 * scale),
            shoes,
            f"{side_name}Foot",
            parts,
            0.018 * scale,
        )
        add_box(
            f"{prefix}_{side_name}Toes",
            (sign * points["leg_x"], front_y, points["foot_z"]),
            (foot_width * 1.04, points["foot_length"] * 0.48, 0.066 * scale),
            shoes,
            f"{side_name}Toes",
            parts,
            0.018 * scale,
        )

    body = join_parts(prefix, parts, armature)
    return armature, body


def export_character(armature, body, filepath):
    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        use_armature_deform_only=False,
        bake_anim=False,
        mesh_smooth_type="FACE",
        path_mode="AUTO",
    )
    bpy.ops.object.select_all(action="DESELECT")


def point_camera(camera, target):
    direction = Vector(target) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def create_preview(male_armature, female_armature):
    male_armature.location.x = -1.05
    female_armature.location.x = 1.05

    ground_material = make_material("Preview_Ground", (0.085, 0.09, 0.095), 0.95)
    bpy.ops.mesh.primitive_plane_add(size=10.0, location=(0.0, 0.0, 0.0))
    ground = bpy.context.object
    ground.name = "Preview_Ground"
    ground.data.materials.append(ground_material)

    bpy.ops.object.camera_add(location=(3.8, -6.5, 2.55))
    camera = bpy.context.object
    camera.name = "Preview_Camera"
    camera.data.lens = 56
    point_camera(camera, (0.0, 0.0, 0.92))
    bpy.context.scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-2.8, -3.6, 4.2))
    key = bpy.context.object
    key.name = "Preview_Key"
    key.data.energy = 950
    key.data.shape = "DISK"
    key.data.size = 4.0
    point_camera(key, (0.0, 0.0, 1.0))

    bpy.ops.object.light_add(type="AREA", location=(3.2, -1.2, 2.8))
    fill = bpy.context.object
    fill.name = "Preview_Fill"
    fill.data.energy = 650
    fill.data.size = 3.0
    point_camera(fill, (0.0, 0.0, 1.0))

    bpy.ops.object.light_add(type="AREA", location=(0.0, 2.8, 3.4))
    rim = bpy.context.object
    rim.name = "Preview_Rim"
    rim.data.energy = 900
    rim.data.size = 3.0
    point_camera(rim, (0.0, 0.0, 1.0))

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PREVIEW_FILE
    scene.render.film_transparent = False
    scene.world.color = (0.025, 0.03, 0.035)
    bpy.ops.wm.save_as_mainfile(filepath=SOURCE_FILE)
    bpy.ops.render.render(write_still=True)


def main():
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    os.makedirs(os.path.dirname(SOURCE_FILE), exist_ok=True)
    clear_scene()
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    male_armature, male_body = build_character("PrototypeMale", "male")
    export_character(male_armature, male_body, os.path.join(OUTPUT_DIR, "PrototypeMale.fbx"))
    female_armature, female_body = build_character("PrototypeFemale", "female")
    export_character(female_armature, female_body, os.path.join(OUTPUT_DIR, "PrototypeFemale.fbx"))
    create_preview(male_armature, female_armature)

    print("PROTOTYPE_HUMANOIDS_OK")
    print(f"Male bones: {len(male_armature.data.bones)}")
    print(f"Female bones: {len(female_armature.data.bones)}")
    print(f"Male vertices: {len(male_body.data.vertices)}")
    print(f"Female vertices: {len(female_body.data.vertices)}")
    print(f"Output: {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
