import fs from 'node:fs'
import path from 'node:path'
import crypto from 'node:crypto'
import { fileURLToPath } from 'node:url'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const desktop = path.join(root, 'Assets/_Project/Desktop')
const guid = relative => crypto.createHash('sha256').update('WorkplaceStudioDesktop/' + relative.replaceAll('\\', '/')).digest('hex').slice(0, 32)
function meta(full, folder = false) {
  const relative = path.relative(root, full).replaceAll('\\', '/')
  if (!fs.existsSync(full + '.meta')) fs.writeFileSync(full + '.meta', `fileFormatVersion: 2\nguid: ${guid(relative)}\n${folder ? 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' : ''}`)
}
function walk(directory) {
  meta(directory, true)
  for (const item of fs.readdirSync(directory, { withFileTypes: true })) {
    if (item.name.endsWith('.meta')) continue
    const full = path.join(directory, item.name)
    if (item.isDirectory()) walk(full)
    else meta(full)
  }
}
const resources = path.join(desktop, 'Resources')
const rendererGuid = guid('Assets/_Project/Desktop/Resources/DesktopRenderer.asset')
for (const [input, output] of [['URP_Renderer.asset', 'DesktopRenderer.asset'], ['URP_Settings.asset', 'DesktopPipeline.asset']]) {
  const source = path.join(root, 'Assets/_Project/Settings', input)
  const destination = path.join(resources, output)
  if (!fs.existsSync(destination)) {
    if (!fs.existsSync(source)) throw new Error('Original URP settings unavailable; use the committed Desktop pipeline assets.')
    const contents = fs.readFileSync(source, 'utf8').replace('m_Name: URP_Renderer', 'm_Name: DesktopRenderer')
      .replace('m_Name: URP_Settings', 'm_Name: DesktopPipeline').replaceAll('a28a900eb4e6ec347820df4014ed9264', rendererGuid)
    fs.writeFileSync(destination, contents)
  }
}
const scriptGuid = guid('Assets/_Project/Desktop/Runtime/DesktopBootstrap.cs')
const scenes = path.join(desktop, 'Scenes')
fs.mkdirSync(scenes, { recursive: true })
const scenePath = path.join(scenes, 'WorkplaceDesktop.unity')
if (!fs.existsSync(scenePath)) fs.writeFileSync(scenePath, `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 1001}
  - component: {fileID: 1002}
  m_Layer: 0
  m_Name: Workplace Studio Desktop
  m_TagString: Untagged
  m_IsActive: 1
--- !u!4 &1001
Transform:
  m_ObjectHideFlags: 0
  m_GameObject: {fileID: 1000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_Children: []
  m_Father: {fileID: 0}
--- !u!114 &1002
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_GameObject: {fileID: 1000}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${scriptGuid}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: WorkplaceStudio.Desktop::WorkplaceStudio.Desktop.DesktopBootstrap
`)
walk(desktop)
const streaming = path.join(root, 'Assets/StreamingAssets')
if (fs.existsSync(streaming)) meta(streaming, true)
console.log('Desktop scene, pipeline assets and deterministic Unity metadata prepared.')
