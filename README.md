# Interactive VR Simulation — Workplace Studio

Çankaya University CENG 407/408 graduation project, 2026–2027.

**Workplace Studio** is an extensible, interactive 3D training platform for
healthcare, aviation, and other high-attention domains. It enables instructors
to prepare training environments and lets learners explore and practise in
those environments on a desktop computer today, with a path to VR support.

> The first training focus is foundational equipment and room-awareness
> training for newly qualified nurses.

**Current integration branch:** `nursing-room-literature-2026-11-15`. It
contains the full current implementation: the Nursing Skills Room, its
equipment-interaction foundation, the hospital and cockpit environments, Piko,
and the Literature & Technology Review sources.

## Project vision

The planned workflow is:

```text
Photographs / video
        ↓
AI-assisted 3D environment reconstruction
        ↓
GLB / mesh-based training environment
        ↓
Instructor authoring, hotspots, and scenarios
        ↓
Interactive desktop or VR training
```

The platform is intended to make reusable training scenarios possible for
clinical rooms, aircraft cockpits, industrial facilities, and other
institutional settings.

## Current prototype

The working prototype currently includes:

- A Unity-based Windows desktop application.
- Local GLB/mesh-based 3D environment loading.
- Mouse and keyboard exploration.
- Training areas, hotspots, and information-point authoring.
- Local project storage and portable training-project folders.
- Piko, an animated guide mascot.
- A preserved web-editor prototype.
- Three sample environments:
  - **Nursing Skills Room**
  - **Charité Hospital · Operating Room**
  - **Boeing 737-800 Cockpit**

Planned project deliverables that are not yet implemented include:

- VR headset and OpenXR integration.
- AI-assisted 3D reconstruction from photographs and video.
- Institution-selectable local or online AI processing.
- Guided scenarios, assessment, and learner-progress tracking.
- Institutional authentication, secure deployment, and an installer.

> This repository is an educational and research prototype. It is not intended
> for clinical use or for processing real patient data.

## Environments

| Environment | Purpose | Included content |
|---|---|---|
| Nursing Skills Room | Foundational nursing training | Patient bed, patient monitor, infusion pump, nurse-call system, and basic equipment interactions |
| Charité Hospital · Operating Room | Reference healthcare environment | Source hotspots, equipment descriptions, and walking exploration |
| Boeing 737-800 Cockpit | Aviation training example | Cockpit exploration and a foundation for future procedural training |

### Nursing Skills Room

The project-specific Nursing Skills Room is designed for introductory training
scenarios. It includes:

- An adjustable patient bed.
- A live-display patient monitor.
- An infusion pump with an animated door.
- An illuminated nurse-call system.
- Independent, device-level interaction events.
- A foundation for future step-by-step learning flows.

The Blender source, local GLB, and equipment sources are kept in:

```text
desktop-content/nursing-room/
```

See [Nursing Skills Room](docs/NURSING_ROOM.md) for implementation and
validation details.

## Technology stack

| Area | Technology |
|---|---|
| Desktop application | Unity 6000.6.7f1 |
| Application code | C# |
| 3D environment format | GLB / glTF 2.0 |
| Runtime GLB loading | glTFast |
| Web prototype | React, TypeScript, Vite |
| Web 3D viewer | Babylon.js |
| 3D modelling and editing | Blender |
| Local project storage | JSON-based project files |

## Quick start — Unity desktop application

### Requirements

- Unity Hub.
- **Unity Editor 6000.6.7f1**.
- Windows Build Support for Unity.
- Git.

Clone this full integration branch:

```powershell
git clone --branch nursing-room-literature-2026-11-15 --single-branch `
  https://github.com/CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation.git
```

Then:

1. Open the repository root from Unity Hub.
2. On the first launch, wait for packages to resolve and C# compilation to finish.
3. In Unity, select **Workplace Studio → Open Desktop Application Scene**.
4. Press Play in `Assets/_Project/Desktop/Scenes/WorkplaceDesktop.unity`.

To generate a Windows application:

```powershell
.\desktop-tools\Build-Windows.ps1
```

Alternatively, use **Workplace Studio → Build Windows Application** in Unity.

> `Builds/` contains generated output and is intentionally excluded from Git.
> When sharing a built application, copy the complete build folder rather than
> only the `.exe` file.

## Using the application

- Select a training environment from the dashboard.
- Right-click inside the 3D view to capture mouse and keyboard input.
- Use `WASD` to move; use `Esc` to release the cursor and return to the UI.
- Use the Editor view to create and organise areas, hotspots, and descriptions.
- **Save** writes the complete work to a local project folder.
- Local training projects are stored by default under:

```text
%LOCALAPPDATA%\Cengaverler\WorkplaceStudio\Projects
```

For installation, use, storage, and build details, see the
[Desktop Application Guide](docs/DESKTOP_APP.md).

## Web-editor prototype

The earlier browser-based authoring prototype is retained in:

```text
web-editor/
```

For web development:

```powershell
cd web-editor
npm ci
npm run dev
```

The web editor established the first approaches to hotspot authoring, scene
exploration, and training-content preparation. The primary development path is
now the Unity desktop application, designed to remain ready for later VR work.

See [web-editor/README.md](web-editor/README.md) for its dedicated setup and
technical notes.

## Verification

Run the following checks from the repository root:

```powershell
.\desktop-tools\Test-Core.ps1
.\desktop-tools\Prepare-DesktopContent.ps1
node desktop-tools\verify-desktop.mjs
.\desktop-tools\Test-Unity.ps1
```

The current desktop implementation was validated with Unity 6000.6.7f1,
25 core/syntax checks, and 17 Unity tests. Detailed evidence and remaining
limitations are recorded in the project documentation.

## Privacy and deployment approach

The platform is designed around a hybrid model:

- Privacy-sensitive institutions can keep environment data and training content
  on local systems.
- Future AI-assisted reconstruction can use local models or explicitly approved
  online services, according to the institution's requirements.
- Consent, institutional policy, and security controls must be evaluated before
  using real clinical, personal, or sensitive organisational data.

## Third-party assets and licences

Some sample environments use third-party assets:

- Charité operating-room model: **CC-BY-NC-4.0**.
- Boeing 737-800 cockpit model: subject to its source licence.
- Nursing Skills Room layout: original project work.
- Nursing equipment source and attribution notes:
  [desktop-content/nursing-room/SOURCES.md](desktop-content/nursing-room/SOURCES.md).

Licences for all example assets must be reviewed separately before commercial
or institutional redistribution.

## Documentation

- [Literature & Technology Review — PDF](docs/LITERATURE_AND_TECHNOLOGY_REVIEW.pdf)
- [Literature & Technology Review — Markdown](docs/LITERATURE_AND_TECHNOLOGY_REVIEW.md)
- [Documentation index](docs/README.md)
- [Desktop Application Guide](docs/DESKTOP_APP.md)
- [Desktop Architecture](docs/DESKTOP_ARCHITECTURE.md)
- [Nursing Skills Room](docs/NURSING_ROOM.md)
- [Web Editor documentation](web-editor/docs/)
- [Piko / WISP asset guide](Assets/_Project/Art/Models/Wisp/README.md)

## Team

| Student ID | Name |
|---|---|
| 202111052 | Hasan DURMUŞOĞLU |
| 201911032 | Alperen KAHVECİ |
| 202111054 | Erdinç Bartu GÖKER |
| 202011027 | Arif Kaan YILMAZ |
| 202211016 | Yusuf ÇAĞLAYAN |
| 202111067 | Mehmet Yiğit İNCİ |

**Supervisor:** Gül Tokdemir

---

Çankaya University · Department of Computer Engineering · CENG 407/408 · 2026–2027
