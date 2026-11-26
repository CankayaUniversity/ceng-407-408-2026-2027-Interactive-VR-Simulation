# Publication authorization and image boundary

The initial permission covered narrow computer/equipment crops. The user subsequently provided two specific reconstructed-overview screenshots and expressly authorized their use, with the first real-photo thumbnail blurred.
This permission does not cover the complete source frames, full-room contact sheets, video, raw geometry, poses, checkpoints or future unrestricted imagery.

## Published derivatives
C02–C06 equipment crops and individually supplied S01/S02 screenshots in assets/approved/APPROVAL.json are in scope.
The record identifies each exact derivative hash, source-record hash, dimensions and crop box.
The derivatives were visually checked against the user's equipment-only constraint; this records scoped authorization, not blanket approval of the original images.
Equipment crops preserve the original pixels. S01 changes only the requested real-photo thumbnail with a strong deterministic blur; all other RGB pixels are identical. S02 preserves all RGB pixels. No generative editing or reconstruction retouching was used; source metadata was omitted.

- C02/C03: the same pixel rectangle from the32/128 LingBot first-camera screenshots. Colored lines are camera axes.
- C04/C05/C06: keyboard/desk crops from V1 training view125, RGB/A/B. These are training views, not held-out evidence.

Other full held-out and novel-view panels remain withheld. The new permission applies only to the two specifically supplied overview screenshots, not to arbitrary further room images.
Any reserved panel is explicitly labelled; it must not be mistaken for an experimental image.

## Local-only material
Full source candidates remain in the original local POC and the ignored .local review manifest.
No original frame/video or full contact sheet is committed. There is no upload or approval endpoint in the local review server.

## Future changes
Any additional image outside the equipment-crop and two individually supplied screenshot authorizations needs new explicit user permission.
Validate the actual derivative, not just a source filename. Keep crop hashes/provenance and truthful captions.
Do not use private imagery to illustrate quality claims it cannot support; the tiny training-view crops are not the basis of the full-view PSNR/SSIM.

## Publication scope
Only research/monocular-3d-reconstruction is authorized for push.
No main change, automatic merge, GitHub Pages website, repository-setting edit or application/Unity source change is authorized.
Branch URL:
https://github.com/CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation/tree/research/monocular-3d-reconstruction

## Individually authorized viewer captures

- S01: the first screenshot supplied by the user; raw reference-photo rectangle [1084,150,1327,289] strongly blurred. The reconstructed scene and UI outside it are unchanged.
- S02: the second screenshot supplied by the user, unchanged RGB pixels.

These are separate viewer states, not a controlled same-camera before/after. The second screenshot displays Train Step30; it must not be described as independently proving all32 frames are simultaneously visible. Neither screenshot proves a mesh/GLB export merely because the viewer exposes that control.
