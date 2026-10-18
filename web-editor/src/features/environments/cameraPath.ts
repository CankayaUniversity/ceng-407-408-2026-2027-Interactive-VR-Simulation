import { Vector3 } from '@babylonjs/core/Maths/math.vector.js'
import { Ray } from '@babylonjs/core/Culling/ray.js'
import type { Scene } from '@babylonjs/core/scene.js'
import type { AbstractMesh } from '@babylonjs/core/Meshes/abstractMesh.js'

/** Düz kamera yolunda beş ışınla tutucu yüzey kontrolü; tam swept-volume çarpışması değildir. */
export function isCameraPathClear(scene: Scene, modelMeshes: ReadonlySet<AbstractMesh>, from: Vector3, to: Vector3, configuredClearance: number) {
  const direction = to.subtract(from)
  const distance = direction.length()
  if (distance < 0.0001) return true
  direction.normalize()
  const clearance = Math.max(0, Math.min(0.1, configuredClearance))
  const offsets = [Vector3.Zero(), new Vector3(clearance, 0, 0), new Vector3(-clearance, 0, 0), new Vector3(0, clearance, 0), new Vector3(0, -clearance, 0)]
  return offsets.every(offset => !scene.pickWithRay(new Ray(from.add(offset), direction, distance), mesh => modelMeshes.has(mesh), true)?.hit)
}
