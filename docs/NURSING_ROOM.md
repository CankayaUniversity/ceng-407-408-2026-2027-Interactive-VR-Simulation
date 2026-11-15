# Nursing Skills Room

An additional local environment for introductory nursing equipment practice.
The operating room and cockpit are separate catalogue entries.

## Use

Open **Nursing Skills Room** on the Environments screen. Right-click for
WASD walking, Shift for faster walking, Esc to release the cursor. While
walking, aim at equipment and press **E** to open its practice controls.
With the cursor released, click the equipment or use **Equipment practice**
in Scene Explorer. The equipment list frames the corresponding saved view and
opens its controls. Four saved views and equipment hotspots are supplied.
The selected device's pin is hidden while using controls so it does not cover
its screen. Editor mode continues to show every pin for placement/editing.

- Patient monitor: power toggle, locally drawn illustrative waveforms.
- Infusion pump: power, start/pause demo, smoothly opening/closing door.
  Starting requires power and a closed door; opening requires a stopped demo.
- Bed: backrest 0–60° in 15° increments, smoothly moving side rail.
- Nurse call: place and acknowledge a call; indicator changes colour.
- Each device can be reset. These are practice controls, not medication
  programming or a clinical procedure. No dose/fluid delivery is simulated.

The room has a physical floor/walls and walking uses a 1.65 m eye height,
1.5 m/s movement and a ×2 Shift boost. The geometry remains at metre scale.

## Base for future interactive lessons

`TrainingProject.equipment` is an optional list in the portable project JSON.
Old projects without it open with an empty list. Each definition contains:

- Stable ID (`monitor-01`, `pump-01`, `bed-01`, `call-01`).
- Kind, title, root node and named display/moving/indicator parts.

`EquipmentSession` in Core owns device state and validates commands. Its
`Performed` event provides equipment ID, action, before/after snapshots and
an ordered sequence number. Failed commands emit no completion event. A
future lesson runner can evaluate these events, supply Piko hints, enforce
step order and record attempts without reading UI button text or changing
the models. A future XR adapter can submit the same commands.

`EquipmentRig` binds model parts to that session and animates presentation.
`DesktopEquipmentUI` handles desktop selection and controls. Displays draw
locally into textures. No network, AI service or browser runtime is added.

Hotspots may include `targetNode` + `targetPosition`, a node-local source-space
anchor. Those points follow moving parts. Existing points without a local
anchor continue to use their original model-root position. New editor points
capture both positions; missing/ambiguous node names fall back to root space.
Required equipment nodes fail loading clearly if renamed or missing.

**Saving** preserves room geometry, views, hotspots and equipment bindings.
Practice state/history is session-only and starts fresh when a project opens.
There is no claim that a lesson sequence, assessment or learner progress
storage has already been implemented.

## Extend

1. Keep `NR_...` node names unique and stable in Blender.
2. For another device of a supported kind, add an equipment definition with
   a new stable ID and its node names. Update the source template/catalogue.
3. For a new kind/action, add its validation and state rules in Core, then
   add its presentation binding and controls in Runtime.
4. Attach lesson sequencing to `EquipmentSession.Performed`, keeping scoring
   and lesson content separate from physical device state.

Source geometry and licences: [SOURCES.md](../desktop-content/nursing-room/SOURCES.md).

Checks and remaining verification: [14 November validation](NURSING_ROOM_VALIDATION_2026_11_14.md).
