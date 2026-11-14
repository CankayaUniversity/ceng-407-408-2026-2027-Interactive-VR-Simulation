import fs from 'node:fs'
import path from 'node:path'
import crypto from 'node:crypto'
import Module, { createRequire } from 'node:module'
import { fileURLToPath } from 'node:url'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const require = createRequire(path.join(root, 'web-editor/package.json'))
const ts = require('typescript')
const cache = new Map()
function sourceModule(filename) {
  filename = path.resolve(filename)
  if (cache.has(filename)) return cache.get(filename).exports
  const module = new Module(filename)
  cache.set(filename, module)
  module.filename = filename
  module.paths = Module._nodeModulePaths(path.dirname(filename))
  const originalRequire = module.require.bind(module)
  module.require = specifier => specifier.startsWith('.')
    ? sourceModule(path.resolve(path.dirname(filename), specifier + (specifier.endsWith('.ts') ? '' : '.ts')))
    : originalRequire(specifier)
  module._compile(ts.transpileModule(fs.readFileSync(filename, 'utf8'), {
    compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS },
  }).outputText, filename)
  return module.exports
}
const { environments } = sourceModule(path.join(root, 'web-editor/src/features/environments/registry.ts'))
const catalogue = {
  formatVersion: 1,
  sourceCommit: '8f1c2d68193d802edfafd7663a6f105f1ea33731',
  environments: Object.values(environments).map(definition => {
    const source = path.join(root, 'web-editor/public', definition.modelUrl)
    const bytes = fs.readFileSync(source)
    if (bytes.readUInt32LE(0) !== 0x46546c67) throw new Error('Download the real Git LFS model first: ' + source)
    return {
      formatVersion: 1, id: definition.id, title: definition.displayName,
      environmentId: definition.id, modelFile: 'environments/' + definition.id + '/environment.glb',
      modelSha256: crypto.createHash('sha256').update(bytes).digest('hex'),
      navigationMode: definition.navigationMode, overview: definition.camera,
      attribution: definition.attribution,
      editor: {
        schemaVersion: 1, environmentId: definition.id, modelUrl: definition.modelUrl,
        modelTransform: definition.modelTransform,
        zones: definition.starterContent?.zones ?? [], hotspots: definition.starterContent?.hotspots ?? [],
      },
    }
  }),
}
const output = path.join(root, 'Assets/_Project/Desktop/Resources/DesktopCatalogue.json')
fs.mkdirSync(path.dirname(output), { recursive: true })
fs.writeFileSync(output, JSON.stringify(catalogue, null, 2) + '\n')
console.log('Exported combined-final data: ' + catalogue.environments.map(p => `${p.title}: ${p.editor.zones.length} areas / ${p.editor.hotspots.length} points`).join('; '))
