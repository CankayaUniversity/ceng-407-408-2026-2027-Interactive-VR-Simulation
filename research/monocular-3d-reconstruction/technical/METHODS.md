# Methods and comparison boundaries

This document supports the root research presentation. It describes experiments that actually ran, not a completed production pipeline.

## Environment and isolation
Windows11; RTX4060Ti16GB; driver621.40; approximately16GB host RAM; Docker Linux memory limit approximately7.56GiB.
Python3.11 / PyTorch2.8.0+cu128 / CUDA12.8 were tested in isolated containers. Host Python, drivers, WSL settings and Workplace Studio containers were not changed.
Later experiments reused tested images.

## Pinned sources
- LingBot-Map: b9dd1b1de535e6d36e777fb42b8433c781bfd783.
- Official model revision: e8984048a940fbdfa19909f8cc0c0ad834c1ee5a.
- gsplat1.5.3: 937e29912570c372bed6747a5c9bf85fed877bae.
- GLM submodule: 33b4a621a697a305bc3a7610d290677b96beb181.
- pycolmap3.13.0; CPU SfM.
- Tested LingBot image: sha256:8c0fcac012e52f158d04f5f2be5e5e34f7799e92206403b30d18710b8c5419a0.
- Tested Gaussian image: sha256:1947913d9603fd2c910f1d5c8c483c22fa81f9987b5e34f73f6fa3f9017e7b06.

Image IDs identify local artifacts, not public registry addresses. Package/apt records do not guarantee bit-identical future builds.

## LingBot inference and exports
Streaming, SDPA, bf16 aggregator, memory-mapped checkpoint load, CPU prediction offload, sliding window16, two scale frames, keyframe interval2 and one camera iteration.
Official landscape preprocessing produced518×294 RGB; no extra crop was required for these inputs.
The separate adapter retained RGB, depth, raw depth confidence, predicted K, W2C, C2W, ordering and nominal timestamps.
Confidence is not a calibrated probability. VFR timestamps are not guaranteed exact PTS.
A direct world-point head was unavailable on this inference path. Clouds were depth-unprojected, confidence-filtered at1.5 and sampled1/10.
Counts and structural checks do not establish surface accuracy.

## Camera conventions
OpenCV/COLMAP axes: right, down, forward. Homogeneous W2C inversion gives C2W.
K_training = diag(768/518,432/294,1) K_inference.
Both trainers use768×432 RGB resized from the original1920×1080 decoded images.
A uses per-frame predicted K; B refines a shared PINHOLE camera. Distortion, rolling shutter and stabilization are uncalibrated.

A is normalized by training-camera mean center and95th-percentile radius.
B is placed into A coordinates with one training-center Sim3. No per-frame correction or nonrigid alignment.
Each recording has its own normalization; units are not meters or a shared cross-recording world.
The dense364 run retains raw COLMAP coordinates and does not align to LingBot.

## Gaussian comparison
The trainer consumes fixed external cameras, does not invoke COLMAP and does not optimize poses.
Shared settings:768×432,4,000 steps, seed42,20,000 random initial Gaussians in a normalized cube, same gsplat rasterizer and DefaultStrategy.
SH degree0→1→2; batch1; loss0.8L1+0.2local box SSIM. Evaluation SSIM uses skimage Gaussian-window configuration.
The120,000 growth threshold is soft; a growth event may overshoot it.
This local common trainer is not the official simple_trainer reference-quality recipe.

V1 matched38:33train/5held-out. V2 matched38:32train/6held-out.
Every eighth image at order%8==4 is reserved when present in the matched camera cohort.
Camera estimation may use reserved RGB views: this is pose-assisted image holdout.
Different recordings have different views and cohorts; cross-video scores are not geometry-accuracy measurements.

## Sampling and matching
V2:2721 frames,29.9741127655FPS,90.778333s.
The364-frame set retains the exact128 previous images by subdividing original intervals into2 or3; approximately4FPS, maximum0.367s gap.
The alternative picks maximum Laplacian variance within±3 source frames per slot, retains every slot and has maximum0.534s gap.
Sharpness is content-dependent, not a calibrated blur measure.
Dense sequential overlap30 replaces10 to preserve roughly7s temporal neighborhood; quadratic overlap stays enabled.
SIFT4096/image maximum768/shared PINHOLE/2threads/min model6; principal point and extra-parameter refinement disabled.
Only the stronger ordinary dense sequence received an exhaustive extension. No exhaustive blur-aware result exists.

Overlap proxy: adjacent pairs with at least15 COLMAP geometric inliers. It is not actual common image area or sufficient parallax.
Registration means the largest single connected model, never a sum of disconnected components.

## Actual failures and validation
- Official checkpoint loading initially failed with host-memory OSError12, before inference. mmap resolved it without model-internal edits.
- PowerShell5.1 misclassified native stderr. A separate process helper captures both streams and the actual exit code; no global shell setting changed.
- The first16 V2 frames could not initialize SfM; full-sequence matching was a separate bounded attempt.
- Closed SQLite diagnostics on read-only mounts used immutable read mode to avoid journal side-file creation.
- pycolmap returned component IDs did not always equal disk-directory order. Exact camera-name sets identified the correct model, exported as validated-largest-model.
- The235-camera disk model, finite K/poses, inverse matrices and rotations were checked. The original128 control images were pixel-identical at768×432.
- Validation does not prove physical accuracy.

## License provenance
LingBot code states Apache2.0. The inspected model card refers to the project license; separate model-license metadata was absent.
gsplat states Apache2.0; COLMAP uses BSD terms with separately licensed dependencies; preserve upstream notices.
The original Graphdeco implementation has research/commercial restrictions and was not selected.
These are provenance notes, not complete legal clearance. No weights, private captures or upstream code are redistributed.
