# Nursing Skills Room — sources and editable assets

The room layout, patient bed, furniture, call station, screen surfaces and
runtime displays were authored for this project. They represent a generic
introductory nursing practice room, not a particular hospital or device brand.

## Innerscene equipment geometry (CC0 1.0)

Downloaded 14 November 2026 from the author's original-model library:

- Patient monitor: https://www.innerscene.com/tools/library/3d-parts/patient-monitor-8ce1cd07
  Original file: `source/patient-monitor.glb`.
- Infusion pump: https://www.innerscene.com/tools/library/3d-parts/infusion-pump-d6f6e00d
  Original file: `source/infusion-pump.glb`.
- Author's stated licence: CC0 1.0 Universal,
  https://creativecommons.org/publicdomain/zero/1.0/ .

Both originals are preserved. In the assembled room, parts are renamed,
devices placed on stands, the pump door given a hinge, and printed displays
replaced by dedicated full-UV surfaces. The Unity application supplies live
demo displays and equipment behaviour. No remote service is used at runtime.

## Authoring

- `nursing-room.blend`: editable Blender source, packed source textures.
- `nursing-room.glb`: self-contained runtime model, metres / glTF Y up.
- `project-template.json`: catalogue entry, stable equipment IDs and viewpoints.
- `desktop-tools/create-nursing-room.py`: repeatable source generator. Requires
  the two original files above. The handset revision was generated with Blender 5.1.2.

Run from the repository root:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --python desktop-tools/create-nursing-room.py
.\desktop-tools\Prepare-DesktopContent.ps1
```

The generator updates the room's SHA-256 and its catalogue entry together.
Re-running it rebuilds the authored layout; manual Blender changes should be
kept in a separate source file or incorporated into the generator first.
