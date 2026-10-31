#!/usr/bin/env python3
"""Writes the 7 Unity scenes (+ .meta) and EditorBuildSettings.asset with stable GUIDs.
Each scene holds one GameObject with a SceneEntry marker (the composition root builds the UI at runtime).
Usage: python3 -I Tools/write_scenes.py"""
import hashlib, os
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
def guid(key): return hashlib.md5(('veliqora:' + key).encode()).hexdigest()
SCENES = ['Bootstrap', 'Home', 'LevelMap', 'Gameplay', 'LevelComplete', 'Shop', 'Customization']  # == ScreenId order
ENTRY_GUID = guid('SceneEntry.cs')

script_meta = os.path.join(ROOT, 'Assets/_Veliqora/Scripts/Infrastructure/SceneEntry.cs.meta')
open(script_meta, 'w').write(f"""fileFormatVersion: 2
guid: {ENTRY_GUID}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")

scene_dir = os.path.join(ROOT, 'Assets/_Veliqora/Scenes')
os.makedirs(scene_dir, exist_ok=True)
build = []
for index, name in enumerate(SCENES):
    yaml = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 100001}}
  - component: {{fileID: 100002}}
  m_Layer: 0
  m_Name: SceneEntry ({name})
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &100002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ENTRY_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  Screen: {index}
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {{fileID: 100001}}
"""
    path = os.path.join(scene_dir, name + '.unity')
    open(path, 'w').write(yaml)
    g = guid('scene:' + name)
    open(path + '.meta', 'w').write(f"""fileFormatVersion: 2
guid: {g}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")
    build.append((name, g))

lines = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!1045 &1", "EditorBuildSettings:", "  m_ObjectHideFlags: 0",
         "  serializedVersion: 2", "  m_Scenes:"]
for name, g in build:
    lines += ["  - enabled: 1", f"    path: Assets/_Veliqora/Scenes/{name}.unity", f"    guid: {g}"]
lines += ["  m_configObjectReferences: {}", ""]
open(os.path.join(ROOT, 'ProjectSettings/EditorBuildSettings.asset'), 'w').write("\n".join(lines))
print("wrote", len(build), "scenes")
