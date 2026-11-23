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
  The wired handset sits on the front of the footboard. In Preview, aim the
  walking crosshair at its up or down key and left-click to move the backrest
  one increment. You can also click the keys with the cursor released.
  Click its casing or select **Adjustable patient bed** to open the bed panel.
- Nurse call: place and acknowledge a call; indicator changes colour.
- Each device can be reset. These are practice controls, not medication
  programming or a clinical procedure. No dose/fluid delivery is simulated.

The room has a physical floor/walls and walking uses a 1.65 m eye height,
1.5 m/s movement and a ×2 Shift boost. The geometry remains at metre scale.

## Guided bed training

In Preview, select **Training ▾ → Adjust the bed backrest**. The patient
asks to sit up. Follow Piko's handset marker and use the arrows to reach
the displayed target. Each attempt starts at 0°. A matching backrest
action completes the lesson. Use **Finish training** to return to practice.
To leave early while walking, press Esc and choose **Leave training**.

In **Editor → Training**, hover over **Add default training** and choose
**Adjust the bed backrest** from the side menu. This creates a copy and
opens its settings. You can change the name, patient request, bed and
target angle. Choose **Apply training** to apply edits and **Save** to
keep the project. Select an existing training to edit it or choose
**Delete training…** and confirm.

## Bed exam

In **Editor → Exam**, hover over **Add default exam** and choose a template.
Choose **Apply exam** after editing the copy. To remove it, select the exam,
choose **Delete exam…** and confirm. **Save** keeps your changes.

In **Preview**, select **Exam ▾** next to **Training** and choose an exam.
Each attempt starts at the overview with the selected bed at 0°. Read the
patient's request and target, locate the handset, and adjust the bed.
Press **Esc** to release the cursor and choose **Finish exam**. The result
compares the submitted angle with the target. You can correct the adjustment
as often as needed before submitting. Results are shown for the current attempt.

## Equipment and lesson data

`TrainingProject.equipment` is an optional list in the portable project JSON.
Old projects without it open with an empty list. Each definition contains:

- Stable ID (`monitor-01`, `pump-01`, `bed-01`, `call-01`).
- Kind, title, root node and named display/moving/indicator parts.

`EquipmentSession` in Core owns device state and validates commands. Its
`Performed` event provides equipment ID, action, before/after snapshots and
an ordered sequence number. The guided lesson evaluates these events against
the selected bed and target angle from the project's `bedTrainings` list.
Exams use the separate `bedExams` list. Opening an older project that used
one list for both menus copies its trainings into independently editable exams.

`EquipmentRig` binds model parts to that session and animates presentation.
`DesktopEquipmentUI` handles desktop selection and controls. Displays draw
locally into textures. No network, AI service or browser runtime is added.

Hotspots may include `targetNode` + `targetPosition`, a node-local source-space
anchor. Those points follow moving parts. Existing points without a local
anchor continue to use their original model-root position. New editor points
capture both positions; missing/ambiguous node names fall back to root space.
Required equipment nodes fail loading clearly if renamed or missing.

**Saving** preserves room geometry, views, hotspots, equipment bindings and
lesson definitions. Practice state and learner progress last for the session.

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
