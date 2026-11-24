# Interactive VR Simulation — Workplace Studio

**Workplace Studio** is a Çankaya University CENG 407/408 graduation project for 2026–2027. In the Windows desktop prototype, an instructor can teach a nursing task as a guided exercise, then assess the same skill in an exam that the learner completes independently.

This release combines the nursing training and exam work with **Piko AI**. It includes a working bed handset and the first scenario, **Adjust the bed backrest**. A patient asks to sit up a little. The learner uses the handset at the foot of the bed to raise the backrest to **30°**.

| Environment | What you can do |
| --- | --- |
| **Nursing Skills Room** | Practise with the adjustable bed, patient monitor, infusion pump, and nurse-call button. Author, run, and grade bed-backrest scenarios. |
| **Charité Hospital · Operating Room** | Walk through the operating room and explore ten equipment and room hotspots. |
| **Boeing 737-800 Cockpit** | Explore the cockpit in spectator mode and use the editor to create your own views and hotspots. |

## Quick start on Windows

Install **Unity 6000.6.7f1** through Unity Hub and add Windows Build Support if you need the standalone application. Clone the project with Git. The three environment models and Piko are included in this branch, so Git LFS is unnecessary.

```powershell
git clone --single-branch --branch release/workplace-studio-2026-11-24 https://github.com/CankayaUniversity/ceng-407-408-2026-2027-Interactive-VR-Simulation.git
cd ceng-407-408-2026-2027-Interactive-VR-Simulation
```

Open the repository root in Unity Hub. In Unity, choose **Workplace Studio → Open Desktop Application Scene**, then press **Play**. For a standalone Windows application, choose **Workplace Studio → Build Windows Application** or run `.\desktop-tools\Build-Windows.ps1` from the repository root. After the build, run `Unity_Uygulama_Baslat.cmd` from that same folder. The generated Windows application lives under `Builds/WorkplaceStudio-Windows/` on your computer. [Desktop setup and build guide](docs/DESKTOP_APP.md)

## Try the nursing room

1. Open **Nursing Skills Room** from the Environments screen.
2. In **Preview**, select **Training ▾ → Adjust the bed backrest**. Piko helps you locate the handset and use its arrow buttons.
3. Right-click to enter walking mode and use **WASD** to approach the foot of the bed. Aim the centre dot at **▲** and left-click twice to move from 0° to 30°. Each press changes the angle by 15°. You can also press **Esc** and click the buttons with the free cursor. The hotspot and handset label leave the arrows visible.
4. Select **Finish training**, then **Exam ▾ → Adjust the bed backrest** to try the same task independently.

| | Training | Exam |
| --- | --- | --- |
| What the learner sees | The patient request, target and current angle, Piko’s instructions, and a handset marker. | The patient request and target angle. The guidance and location markers are hidden. |
| How the attempt ends | Reaching the target completes the exercise. | Press **Esc** to release the cursor and select **Finish exam**. The result shows the target, submitted angle, and pass or fail. |

Each attempt starts at 0°. In the exam you can correct the angle before submitting. For a 30° target, going up to 45° and back down to 30° earns a pass. Submitting at 45° earns a fail. The result is shown for that attempt.

## Create, edit, or delete a scenario

1. In the nursing room, select **Editor**. Open **Training ▾** and hover over **Add default training**, or open **Exam ▾** and hover over **Add default exam**.
2. Select **Adjust the bed backrest** from the side menu. This creates a copy in the selected list and opens its settings.
3. Edit the name, patient request, bed, and target angle (15°, 30°, 45°, or 60°). Select **Apply training** or **Apply exam**, then **Save** to keep the project on this computer.
4. To remove a scenario, select it in its editor menu, choose **Delete training…** or **Delete exam…**, confirm, and **Save**.

Training and exam copies can have different targets. For example, keep a 30° guided exercise and set the exam to 45°. To revisit your edits, open the project under **Saved projects** on the **Environments** screen. Older projects that shared one list get an independent exam copy for each existing training.

## Piko chat and Gemini setup

Select **Chat** beside Piko. The message field shows a blinking caret while it has focus. Try `Take me to the infusion pump` in the nursing room to navigate to that hotspot locally. Piko unlocks its conversational quiz when you visit five hotspots or all available hotspots in smaller rooms. The nursing room has four. The chat quiz is separate from the graded bed exam.

1. Create or copy your own Gemini API key. [Google AI Studio API keys](https://aistudio.google.com/apikey)
2. Open **Edit environment variables for your account** in Windows. Under **User variables**, create `GEMINI_API_KEY` and paste the key as its value. Keep the key on your computer.
3. Restart Workplace Studio. If you run the scene in Unity, restart the Unity Editor as well so the app inherits the new variable.
4. In the nursing room, open **Chat** and ask `How do the patient monitor and infusion pump differ?` to test an online reply.

The app also reads `%LOCALAPPDATA%\Cengaverler\WorkplaceStudio\piko-ai.json` if a local configuration already exists. Its configured key takes priority over the environment variable. The default model is `gemini-3.5-flash`, with `gemini-3.5-flash-lite` as a fallback for eligible server errors. When Piko uses Gemini, it sends the conversation and the current environment's hotspot titles and descriptions to Google. Local navigation commands, device practice, training, and the bed exam work without a Gemini key.

## Project scope and verification

The desktop app uses Unity, C#, glTFast, and JSON project files. You can import a local GLB, prepare areas and hotspots, and save training projects on the same computer. The earlier browser prototype uses React, TypeScript, Vite, and Babylon.js. The project roadmap includes AI-assisted 3D reconstruction from photographs and video, local or online reconstruction processing, and VR headset support. [Nursing room and equipment guide](docs/NURSING_ROOM.md) · [Room models and sources](desktop-content/nursing-room/SOURCES.md) · [Web editor](web-editor/README.md)

The 24 November 2026 release was checked with **28 desktop core checks and 18 Unity tests**. They cover handset interaction, scenario authoring, saving and reopening projects, and exam scoring. A separate live Gemini test confirmed that an online answer appeared in Piko's chat panel. The Windows build was also checked for caret visibility and movement while editing text. [Core checks](desktop-tests/Program.cs) · [Desktop test commands](desktop-tools/) · [Unity tests](Assets/_Project/Desktop/Tests/Editor/NursingRoomTests.cs)

## Project resources

- CENG 407 Literature and Technology Review, 15 November 2026. [PDF](docs/LITERATURE_AND_TECHNOLOGY_REVIEW.pdf) · [Markdown](docs/LITERATURE_AND_TECHNOLOGY_REVIEW.md)
- Unity desktop implementation and earlier web editor. [Desktop source](Assets/_Project/Desktop) · [Architecture](docs/DESKTOP_ARCHITECTURE.md) · [Web editor](web-editor)
- Sample model attributions and licences. [Nursing room sources](desktop-content/nursing-room/SOURCES.md) · [Charité asset notes](web-editor/public/environments/HOSPITAL_ASSET_NOTES.md) · [Cockpit asset notes](web-editor/public/environments/cockpit/ASSET_NOTES.md)
- Earlier desktop milestones. [Desktop migration](docs/CHANGES_UNITY_DESKTOP.md) · [Initial Windows checks](docs/DESKTOP_VALIDATION_2026_11_02.md) · [Hospital floor checks](docs/DESKTOP_VALIDATION_2026_11_14.md) · [Nursing room checks](docs/NURSING_ROOM_VALIDATION_2026_11_14.md)
- WISP mascot models, five animations, and Blender sources. [Preview and usage guide](Assets/_Project/Art/Models/Wisp/README.md)
- [Full documentation index](docs/README.md)

**Project team** · Hasan Durmuşoğlu, Alperen Kahveci, Erdinç Bartu Göker, Arif Kaan Yılmaz, Yusuf Çağlayan, and Mehmet Yiğit İnci. **Supervisor** · Gül Tokdemir.
