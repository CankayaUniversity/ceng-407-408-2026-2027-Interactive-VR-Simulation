# Interactive VR Simulation

**Workplace Studio** is a Çankaya University CENG 407/408 graduation project for 2026–2027. In the Windows desktop prototype, an instructor can teach a nursing task as a guided exercise, then assess the same skill in an exam that the learner completes independently.

The `nursing-room-training-exam-2026-11-23` branch adds a working bed handset and the first scenario, **Adjust the bed backrest**. A patient asks to sit up a little. The learner uses the handset at the foot of the bed to raise the backrest to **30°**.

## Try the nursing room

1. Check out `nursing-room-training-exam-2026-11-23` and open the repository root in **Unity 6000.6.7f1**.
2. Select **Workplace Studio → Open Desktop Application Scene**, press **Play**, and open **Nursing Skills Room**.
3. In **Preview**, select **Training ▾ → Adjust the bed backrest**. Piko helps you locate the handset and use its arrow buttons.
4. Right-click to enter walking mode and use **WASD** to approach the foot of the bed. Aim the centre dot at **▲** and left-click twice to move from 0° to 30°. Each press changes the angle by 15°. You can also press **Esc** and click the buttons with the free cursor. The hotspot and handset label leave the arrows visible.
5. Select **Finish training**, then **Exam ▾ → Adjust the bed backrest** to try the same task independently.

| | Training | Exam |
| --- | --- | --- |
| What the learner sees | The patient request, target and current angle, Piko’s instructions, and a handset marker. | The patient request and target angle. The guidance and location markers are hidden. |
| How the attempt ends | Reaching the target completes the exercise. | Press **Esc** to release the cursor and select **Finish exam**. The result shows the target, submitted angle, and pass or fail. |

Each attempt starts at 0°. In the exam you can correct the angle before submitting. For a 30° target, going up to 45° and back down to 30° earns a pass. Submitting at 45° earns a fail. The result is shown for that attempt.

To run the standalone Windows application, build it once with **Workplace Studio → Build Windows Application**, then launch `Unity_Uygulama_Baslat.cmd` from the repository root. [Desktop setup and build guide](docs/DESKTOP_APP.md)

## Create, edit, or delete a scenario

1. In the nursing room, select **Editor**. Open **Training ▾** and hover over **Add default training**, or open **Exam ▾** and hover over **Add default exam**.
2. Select **Adjust the bed backrest** from the side menu. This creates a copy in the selected list and opens its settings.
3. Edit the name, patient request, bed, and target angle (15°, 30°, 45°, or 60°). Select **Apply training** or **Apply exam**, then **Save** to keep the project on this computer.
4. To remove a scenario, select it in its editor menu, choose **Delete training…** or **Delete exam…**, confirm, and **Save**.

Training and exam copies can have different targets. For example, keep a 30° guided exercise and set the exam to 45°. To revisit your edits, open the project under **Saved projects** on the **Environments** screen. Older projects that shared one list get an independent exam copy for each existing training.

## Project scope and verification

The room also contains an interactive patient monitor, infusion pump, and nurse-call button. The adjustable bed is the first device with a guided exercise and a graded exam. The wider project plans to reconstruct environments from photos or video using local or opt-in online AI processing and support VR headsets. [Nursing room and equipment guide](docs/NURSING_ROOM.md) · [Room models and sources](desktop-content/nursing-room/SOURCES.md)

On 23 November 2026, **27 desktop core checks and 18 Unity tests** passed before the Windows application was built. They covered handset interaction, template selection, independent editing and deletion, saving and reopening scenarios, and grading the submitted angle. [Core checks](desktop-tests/Program.cs) · [Nursing room tests](Assets/_Project/Desktop/Tests/Editor/NursingRoomTests.cs)

## Project resources

- CENG 407 Literature and Technology Review, 15 November 2026. [PDF](docs/LITERATURE_AND_TECHNOLOGY_REVIEW.pdf) · [Markdown](docs/LITERATURE_AND_TECHNOLOGY_REVIEW.md)
- Unity desktop implementation and earlier web editor. [Desktop source](Assets/_Project/Desktop) · [Architecture](docs/DESKTOP_ARCHITECTURE.md) · [Web editor](web-editor)
- Earlier desktop milestones. [Desktop migration](docs/CHANGES_UNITY_DESKTOP.md) · [Initial Windows checks](docs/DESKTOP_VALIDATION_2026_11_02.md) · [Hospital floor checks](docs/DESKTOP_VALIDATION_2026_11_14.md) · [Nursing room checks](docs/NURSING_ROOM_VALIDATION_2026_11_14.md)
- WISP mascot models, five animations, and Blender sources. [Preview and usage guide](Assets/_Project/Art/Models/Wisp/README.md)
- [Full documentation index](docs/README.md)
