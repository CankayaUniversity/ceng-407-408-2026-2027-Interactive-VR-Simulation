# Reproduction requirements and bounded commands

## Distribution boundary
This branch contains the research presentation, aggregate measurements, record hashes and archive preview/audit tools.
Private videos, frames, poses, point clouds, Gaussian checkpoints, weights, databases, raw logs, Docker images and the complete original POC bundle are not distributed.
**A clean clone of this research branch cannot rerun the private experiments by itself.**
Commands below describe the preserved local POC, not scripts shipped in this archive. Use the pinned sources and method settings to prepare an isolated POC and your own input.

## Tested local POC entry points
Run from the existing isolated POC, not this repository. Existing output directories are refused; select fresh paths for a new experiment.

~~~powershell
# V1 bounded extraction.
.\Run-Video.ps1 -VideoPath "inputs/ev.mp4" -MaxFrames 128 -Fps 2

# Requires prepared V2 data, cameras and two successful smoke records.
powershell.exe -NoProfile -File benchmark/v2/scripts/Run-Stages.ps1 -Stage Train

# Registration only; do not invoke Gaussian training.
powershell.exe -NoProfile -File benchmark/v2-density/scripts/Run-Registration.ps1 -Stage Sequential
powershell.exe -NoProfile -File benchmark/v2-density/scripts/Run-Registration.ps1 -Stage Exhaustive -Dataset uniform
~~~

The V2 Train command is not a frame-preparation command.
PrepareSfm originally stopped after its16-frame SfM failure. Full128 matching and matched400-step A/B smoke runs were performed separately before full training.
The dense run used prepare_dense.py, register.py, analyze_overlap.py, summarize.py and validate.py. These identify original local adapters, not archive-distributed scripts.
The364-frame cameras have not been converted to a new trainer dataset or Gaussian checkpoint.

## Upstream references
- [Pinned LingBot source](https://github.com/Robbyant/lingbot-map/tree/b9dd1b1de535e6d36e777fb42b8433c781bfd783)
- [Pinned gsplat source](https://github.com/nerfstudio-project/gsplat/tree/937e29912570c372bed6747a5c9bf85fed877bae)
- [COLMAP camera/output conventions](https://colmap.github.io/format.html)

Official demos and local adapters have different export behavior. Preserve cameras, depth/confidence and ordering explicitly. Do not assume an available point head or editable mesh.

## Local archive preview
The preview server needs Python3 only. It serves pre-rendered HTML prepared locally or with GitHub's Markdown API. A local fallback renderer additionally uses markdown-it-py when available.
No npm install, external fonts, analytics or application frontend is required.

~~~powershell
python research/monocular-3d-reconstruction/tools/preview.py --port 8093
~~~

Open http://127.0.0.1:8093/ .
The optional private review route requires an ignored local manifest. It reads fixed local candidates only and has no upload or approval endpoint.
This is a localhost review aid, not a deployed website or GitHub Pages project.

## Checks for a future rerun
Preserve ordering and resize/calibration mapping. Match cameras to exact image names.
Report the largest connected model, missing views and runtime; do not merge component totals into a claimed room.
Match disk models by camera-name sets, not assumed dictionary IDs.
Separate PyTorch allocator peaks from whole-device usage, and CPU SfM time from GPU training.
Keep matched cohorts and holdouts explicit. Retain old directories, stderr and exit codes.
Run inference/training with network disabled and mount videos read-only.
