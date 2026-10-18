import type { SceneDefinition } from './types'

export const environments = {
  cockpit: {
    id: 'b737-cockpit',
    displayName: 'Boeing 737-800 Cockpit',
    modelUrl: '/environments/cockpit/boeing_737-800_cockpit.glb',
    modelTransform: { position: [0, 0, 0], rotation: [0, 0, 0], scale: [1, 1, 1] },
    navigationMode: 'seated',
    interaction: {
      look: { maxPitchDegrees: 85 },
      zoom: { minDegrees: 25, maxDegrees: 85, sensitivity: 0.035, durationMs: 180 },
      transition: { durationMs: 650, clearance: 0.02 },
      inspector: { speed: 0.12, boostMultiplier: 2, maxDeltaMs: 50 },
    },
    camera: {
      // Tarayıcıda panel/cam görüşü kontrol edildi; kesin pilot göz noktası ekip onayını bekliyor.
      position: [0.4, 2.15, 1.1], target: [0.4, 1.95, 2],
      fovDegrees: 70, near: 0.01, far: 100,
      calibration: 'provisional',
    },
  },
  hospital: {
    id: 'charite-operating-room',
    displayName: 'Charité Hospital · Operating Room',
    modelUrl: '/environments/charite_university_hospital_-_operating_room.glb',
    modelTransform: { position: [0, 0, 0], rotation: [0, 0, 0], scale: [1, 1, 1] },
    navigationMode: 'walking',
    camera: { position: [0, 1.65, 4.5], target: [0, 1.65, 0], fovDegrees: 70, near: 0.03, far: 100, calibration: 'provisional' },
    interaction: {
      look: { maxPitchDegrees: 85 },
      zoom: { minDegrees: 25, maxDegrees: 85, sensitivity: 0.035, durationMs: 180 },
      transition: { durationMs: 650, clearance: 0.12 },
      inspector: { speed: 0.8, boostMultiplier: 2, maxDeltaMs: 50 },
      // Referans zemin model analizinde y≈0. Ölçek ve serbest alan ekip incelemesine açıktır.
      walking: { speed: 0.8, boostMultiplier: 2, maxDeltaMs: 50, groundHeight: 0, eyeHeight: 1.65,
        bounds: { min: [-5, 0, -5], max: [5, 6, 5] } },
    },
    attribution: {
      author: 'Digital Surgery',
      sourceUrl: 'https://sketchfab.com/3d-models/charite-university-hospital-operating-room-fa63aa330e1048a7ab7e63a54845c6c3',
      license: 'CC-BY-NC-4.0', licenseUrl: 'https://creativecommons.org/licenses/by-nc/4.0/',
    },
  },
} satisfies Record<string, SceneDefinition>

export type EnvironmentKey = keyof typeof environments
