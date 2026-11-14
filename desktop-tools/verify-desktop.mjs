import fs from 'node:fs'
import path from 'node:path'
import crypto from 'node:crypto'
import assert from 'node:assert/strict'
import { fileURLToPath } from 'node:url'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const base = path.join(root, 'Assets/_Project/Desktop')
const guids = new Map()
const read = relative => fs.readFileSync(path.join(base, relative), 'utf8')
const guid = relative => /guid: ([a-f0-9]{32})/.exec(read(relative + '.meta'))?.[1]
function visit(directory) {
  for (const item of fs.readdirSync(directory, { withFileTypes: true })) {
    if (item.name.endsWith('.meta')) continue
    const full = path.join(directory, item.name)
    assert.ok(fs.existsSync(full + '.meta'), 'Unity metadata missing: ' + full)
    const value = /guid: ([a-f0-9]{32})/.exec(fs.readFileSync(full + '.meta', 'utf8'))?.[1]
    assert.ok(value && !guids.has(value), 'Missing/duplicate Unity GUID: ' + full)
    guids.set(value, full)
    if (item.isDirectory()) visit(full)
  }
}
visit(base)
assert.ok(read('Scenes/WorkplaceDesktop.unity').includes('guid: ' + guid('Runtime/DesktopBootstrap.cs')))
assert.ok(read('Resources/DesktopPipeline.asset').includes('guid: ' + guid('Resources/DesktopRenderer.asset')))
const catalogue = JSON.parse(read('Resources/DesktopCatalogue.json'))
assert.equal(catalogue.sourceCommit, '8f1c2d68193d802edfafd7663a6f105f1ea33731')
for (const scene of catalogue.environments) {
  const source = path.join(root, 'web-editor/public', scene.editor.modelUrl)
  const generated = path.join(root, 'Assets/StreamingAssets/WorkplaceStudio', scene.modelFile)
  for (const file of [source, generated]) {
    const bytes = fs.readFileSync(file)
    assert.equal(bytes.readUInt32LE(0), 0x46546c67)
    assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'), scene.modelSha256)
  }
}
const manifest = JSON.parse(fs.readFileSync(path.join(root, 'Packages/manifest.json'), 'utf8'))
const lock = JSON.parse(fs.readFileSync(path.join(root, 'Packages/packages-lock.json'), 'utf8'))
for (const name of ['com.unity.cloud.gltfast', 'com.unity.nuget.newtonsoft-json']) {
  assert.equal(manifest.dependencies[name], lock.dependencies[name].version)
  assert.equal(lock.dependencies[name].depth, 0)
}
assert.ok(fs.readFileSync(path.join(root, 'ProjectSettings/ProjectSettings.asset'), 'utf8').includes('GLTFAST_KEEP_MESH_DATA;GLTFAST_SAFE'))
console.log(`PASS desktop assets: ${guids.size} unique metadata GUIDs; scene/pipeline references; canonical model hashes; pinned package entries.`)
console.log('Asset checks only. Run Test-Unity.ps1 and Build-Windows.ps1 for engine tests and Windows compilation; inspect the player separately.')
