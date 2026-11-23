# interactive-vr-simulation

On 2 November 2026, work began on the transition from `codex/combined-final` to a Unity desktop application. 
The 14 November 2026 version is on the `codex/unity-desktop-2026-11-14` branch under `Assets/_Project/Desktop`. 
The initial focus is Windows, WASD and mouse controls, local mesh/GLB files, an editor for areas and information points, and local project saving.

**The first Windows application was built and tested on this computer.** With Unity 6000.6.7f1, 20 core/syntax checks and 16 Unity tests passed.
`Unity_Uygulama_Baslat.cmd` launches the local build. 
The build folder contains generated output and is excluded from Git. The hospital and cockpit environments, Piko, and the loading of locally saved projects were checked. 
AI and VR headset integration have yet to be added.

- [Desktop setup, usage, and build instructions](docs/DESKTOP_APP.md)
- [Desktop architecture](docs/DESKTOP_ARCHITECTURE.md)
- [Verification results and remaining work](docs/DESKTOP_VALIDATION_2026_11_02.md)
- [Overview of recent changes and first launch](docs/CHANGES_UNITY_DESKTOP.md)
- [Verification of the hospital floor fix](docs/DESKTOP_VALIDATION_2026_11_14.md)

The existing web version is kept under `web-editor`. Open the repository root as the Unity project. 
The native scene is `Assets/_Project/Desktop/Scenes/WorkplaceDesktop.unity`.

A domain-agnostic, cross-platform VR training platform built in Unity to streamline learning on complex machinery. 
By digitizing physical equipment into interactive 3D assets with embedded video guides, it delivers intuitive procedural onboarding across both immersive VR and standard screens.

## WISP mascot

The animated robot’s Unity FBX file, five animations, GLB copy, and Blender source files are in `Assets/_Project/Art/Models/Wisp`.
[Preview and usage guide](Assets/_Project/Art/Models/Wisp/README.md).
