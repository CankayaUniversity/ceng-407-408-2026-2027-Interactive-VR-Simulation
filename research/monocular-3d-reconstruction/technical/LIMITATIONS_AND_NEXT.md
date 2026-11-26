# Limitations and next experiment

## Supported findings
Local inference and exports worked. Additional frames improved recognizable coverage without demonstrating repaired surfaces.
COLMAP-pose Gaussian rendering was generally stronger within matched cohorts.
Dense sampling plus exhaustive matching improved connectivity, including38→82 of the same128 images.
Sharpness-aware slot selection alone did not improve sequential registration.

## Limits
No camera ground truth or measured physical scale. Zero-distortion PINHOLE is provisional.
A/B agreement is not independent accuracy. Each recording has a different normalization.
Camera estimation may use held-out RGB. V1/V2 cohorts differ.
The4,000-step random-initialization trainer is not a reference-quality ceiling.
The235-view model misses the first15.25s and has a10.61s registered-view gap.
The blur-aware variant has no exhaustive counterpart.
Point clouds and Gaussian PLY are not meshes, colliders, navmeshes or editable devices.
No GLB conversion or Unity/VR integration was tested.

## Next experiment
Inspect the235-camera sparse model for continuity, duplicate structures and missing regions.
Only after validation, prepare a fresh camera dataset and a small bounded Gaussian pilot on the connected component.
Preserve older experiments; do not silently mix coordinate systems or initialize from a previous checkpoint.
If coverage remains weak, improve capture sharpness through slower movement and lighting while retaining lateral parallax and overlapping views.

## Future possibilities
Gaussian rendering may supply a visual layer. Separately measured geometry must support collision/navigation.
Equipment should be separately curated, licensed assets with IDs, transforms, colliders and explicit behaviors.
Scale/axes, occlusion, stereo and real headset timing need dedicated tests. Headless rasterizer calls cannot establish90Hz VR.
This is a proposal, not implementation.
