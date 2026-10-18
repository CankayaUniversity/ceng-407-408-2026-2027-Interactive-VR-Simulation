import { readFileSync } from 'node:fs'
import { createHash } from 'node:crypto'
import { Matrix, Quaternion, Vector3 } from '@babylonjs/core/Maths/math.vector.js'

const path = process.argv[2] ?? 'public/environments/cockpit/boeing_737-800_cockpit.glb'
const bytes = readFileSync(path)
if (bytes.readUInt32LE(0) !== 0x46546c67 || bytes.readUInt32LE(4) !== 2) throw new Error('GLB 2.0 bekleniyor')
const gltf = JSON.parse(bytes.subarray(20, 20 + bytes.readUInt32LE(12)).toString())
const min = new Vector3(Infinity, Infinity, Infinity)
const max = new Vector3(-Infinity, -Infinity, -Infinity)
let sceneTriangles = 0
const triangleCount = (primitive) => {
  const count = gltf.accessors[primitive.indices ?? primitive.attributes.POSITION].count
  return primitive.mode === undefined || primitive.mode === 4 ? count / 3 : 0
}
function visit(index, parent) {
  const node = gltf.nodes[index]
  const local = node.matrix ? Matrix.FromArray(node.matrix) : Matrix.Compose(Vector3.FromArray(node.scale ?? [1, 1, 1]), Quaternion.FromArray(node.rotation ?? [0, 0, 0, 1]), Vector3.FromArray(node.translation ?? [0, 0, 0]))
  const world = local.multiply(parent)
  if (node.mesh !== undefined) {
    for (const primitive of gltf.meshes[node.mesh].primitives) {
      sceneTriangles += triangleCount(primitive)
      const accessor = gltf.accessors[primitive.attributes.POSITION]
      if (!accessor.min || !accessor.max) continue
      for (let corner = 0; corner < 8; corner++) {
        const point = Vector3.TransformCoordinates(new Vector3(...[0, 1, 2].map(axis => (corner & (1 << axis) ? accessor.max : accessor.min)[axis])), world)
        min.minimizeInPlace(point)
        max.maximizeInPlace(point)
      }
    }
  }
  node.children?.forEach(child => visit(child, world))
}
gltf.scenes[gltf.scene ?? 0].nodes.forEach(index => visit(index, Matrix.Identity()))
console.log(JSON.stringify({
  path, bytes: bytes.length, mib: bytes.length / 1024 ** 2,
  sha256: createHash('sha256').update(bytes).digest('hex'),
  asset: gltf.asset, meshes: gltf.meshes.length, nodes: gltf.nodes.length,
  materials: gltf.materials?.length ?? 0, textures: gltf.textures?.length ?? 0,
  images: gltf.images?.length ?? 0, extensions: gltf.extensionsUsed ?? [],
  uniqueMeshTriangles: gltf.meshes.reduce((sum, mesh) => sum + mesh.primitives.reduce((n, p) => n + triangleCount(p), 0), 0),
  sceneTriangles, bounds: { min: min.asArray(), max: max.asArray() },
}, null, 2))
