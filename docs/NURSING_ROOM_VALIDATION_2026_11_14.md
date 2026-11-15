# Nursing Skills Room — 14 November 2026 validation

## Automated checks and build

- Core/content checks: 25 passed, 0 failed.
- Unity tests: 17 passed, 0 failed (`Builds/DesktopTests/editmode-results.xml`).
- Windows build completed successfully (`Logs/desktop-build.log`).
- The Unity nursing-room integration test loads the actual GLB and checks
  equipment bindings, moving-part anchors, pump guards/door rotation, bed
  backrest height and rail movement, call-indicator material, display textures,
  and grounded horizontal movement over six seconds.

The packaged nursing GLB SHA-256 is
`21027884293933c245fd8ca1d0ba8554f6059772747c206753f4f847aff2e83f`.

## Observed in the Windows application

- Opened the new room; all four equipment entries and saved views appeared.
- Selected equipment through the scene and equipment list. Device controls
  opened and the selected pin stopped covering the device display.
- Switched the monitor off/on; the standby screen and live demo waveforms
  were visible. Adjusted close views to make device screens easier to read.
- Opened/closed the pump door, switched power and started its demo. Disabled
  controls matched the power/door/running interlocks.
- Corrected a reversed backrest rotation. Raising from 15 to 30 degrees now
  visibly lifts the head/pillow rather than lowering them.
- Corrected the call indicator's shader/material binding. Calling changed
  the indicator to amber; acknowledging returned it to blue and READY.
- Removed thin floor seams that produced aliasing, enabled 4x MSAA, and
  reduced the nursing room's light intensity to avoid washed-out equipment.
- Saved the room, returned to the dashboard, and reopened the saved project.
  Also closed/restarted the application and reopened that same save.
  The save contains four areas, four hotspots and four equipment definitions;
  its copied GLB hash matches the packaged model.
- Practice state resets on reopening, as designed; project geometry, views,
  hotspot anchors and equipment definitions persist.
- The latest Player.log contained no managed exceptions. It reported a
  Direct3D debug info-queue query failure and ICU text-data fallback notices;
  rendering and the English interface remained usable.

## Limits and follow-up

- Collision checks can block a direct camera transition from the monitor
  close view to the nurse-call view because the bed lies between them.
  Return to Overview before choosing the other device. No teleport through
  equipment was introduced.
- Right-click capture was visibly confirmed. Short synthetic W/E/Escape
  key presses through desktop automation did not establish movement,
  selection or release. Physical-keyboard WASD/E/Escape verification remains
  outstanding; the automated navigation/grounding tests passed. The app was
  restarted and left open with its cursor released.
- This is generic introductory equipment practice. Clinical workflows,
  medication/dose delivery, lesson sequencing/scoring, VR hardware and AI
  reconstruction are not validated or implemented by this change.
- No fresh-machine installation or physical network-disconnection test was
  performed. New room changes remain local and have not been committed or
  pushed as part of these checks.
