#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using WorkplaceStudio.Core;
using WorkplaceStudio.Desktop;

namespace WorkplaceStudio.Tests
{
    public sealed class NursingRoomTests
    {
        LoadedScene scene;
        GameObject player;
        InputGateTestWindow testWindow;
        [UnityTearDown] public IEnumerator CleanUp()
        {
            scene?.Dispose(); if (player != null) Object.Destroy(player);
            if (testWindow != null) testWindow.Close();
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator BedLessonCanBeEditedPractisedAndSubmittedAsAnExam()
        {
            yield return new EnterPlayMode();
            yield return ExerciseBedTraining();
        }
        IEnumerator ExerciseBedTraining()
        {
            const string scenePath = "Assets/_Project/Desktop/Scenes/WorkplaceDesktop.unity";
            var opening = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            while (!opening.isDone) yield return null;
            player = SceneManager.GetSceneByPath(scenePath).GetRootGameObjects().First(o => o.GetComponent<DesktopBootstrap>() != null);
            Assert.That(player.GetComponent<UIDocument>(), Is.Not.Null, "The desktop scene must initialize its dashboard.");
            var ui = player.GetComponent<UIDocument>().rootVisualElement;
            // Batch tests have no interactive Game view. Host the real dashboard
            // on an attached panel so UI Toolkit dispatches navigation events.
            testWindow = ScriptableObject.CreateInstance<InputGateTestWindow>();
            testWindow.ShowUtility(); testWindow.position = new Rect(50, 50, 1200, 700); testWindow.rootVisualElement.Add(ui);
            yield return null;
            void Submit(VisualElement element)
            {
                element.Focus();
                using (var evt = NavigationSubmitEvent.GetPooled()) element.SendEvent(evt);
            }
            Button FindButton(string text) => ui.Query<Button>().ToList().First(b => b.text == text);
            Submit(ui.Q(className: "environment-card").Q<Button>());
            var nav = player.GetComponentInChildren<DesktopNavigation>();
            Assert.That(ui.Q(className: "dashboard").style.display.value, Is.EqualTo(DisplayStyle.None), "Open environment button");
            float deadline = Time.realtimeSinceStartup + 90f;
            while (!nav.Ready && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(nav.Ready, Is.True, ui.Q<Label>(className: "status").text);
            yield return null;
            const string backrest = "Adjust the bed backrest";
            IEnumerator SelectLesson(string title = backrest, string menuButton = "Training ▾")
            {
                Submit(FindButton(menuButton));
                yield return null;
                var editorMenu = ui.Q(className: "lesson-menu-layer");
                if (editorMenu != null)
                {
                    var rows = editorMenu.Q<ScrollView>().contentContainer.Query<Button>().ToList();
                    Submit(rows.Single(b => b.text == title));
                    yield return null;
                    yield break;
                }
                var menu = ui.panel.visualTree.Query<Label>().ToList()
                    .Where(l => l.text == title).Select(l => l.GetFirstAncestorOfType<ScrollView>())
                    .First(scroll => scroll != null).contentContainer;
                menu.Focus();
                int moves = menu.Query<Label>(className: GenericDropdownMenu.labelUssClassName).ToList()
                    .FindIndex(l => l.text == title) + 1;
                Assert.That(moves, Is.Positive);
                for (int i = 0; i < moves; i++)
                    using (var evt = NavigationMoveEvent.GetPooled(Vector2.down)) menu.SendEvent(evt);
                Submit(menu);
                yield return null;
            }
            IEnumerator AddFromTemplate(bool exam)
            {
                var opener = FindButton(exam ? "Exam ▾" : "Training ▾");
                testWindow.SendEvent(new Event { type = EventType.MouseMove, mousePosition = opener.worldBound.center });
                Submit(opener);
                yield return null;
                var menus = ui.Q(className: "lesson-menu-layer").Query<ScrollView>().ToList();
                var add = menus[0].contentContainer.Q<Button>(className: "add-default");
                Assert.That(add.text, Is.EqualTo(exam ? "Add default exam" : "Add default training"));
                Assert.That(menus[1].style.display.value, Is.EqualTo(DisplayStyle.None));
                testWindow.Repaint();
                float layoutTime = Time.realtimeSinceStartup + .2f;
                while (Time.realtimeSinceStartup < layoutTime) yield return null;
                Assert.That(add.worldBound.height, Is.GreaterThan(0));
                testWindow.SendEvent(new Event { type = EventType.MouseMove, mousePosition = add.worldBound.center });
                testWindow.Repaint();
                float until = Time.realtimeSinceStartup + .25f;
                while (Time.realtimeSinceStartup < until) yield return null;
                Assert.That(menus[1].resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(menus[1].worldBound.width, Is.GreaterThan(0));
                Assert.That(menus[1].worldBound.Overlaps(menus[0].worldBound), Is.False);
                var choice = menus[1].contentContainer.Query<Button>().ToList().Single(b => b.text == backrest);
                Assert.That(choice.worldBound.height, Is.GreaterThan(0));
                Assert.That(testWindow.rootVisualElement.panel.Pick(choice.worldBound.center), Is.EqualTo(choice));
                foreach (var type in new[] { EventType.MouseMove, EventType.MouseDown, EventType.MouseUp })
                    testWindow.SendEvent(new Event { type = type, mousePosition = choice.worldBound.center, button = 0 });
                yield return null;
            }
            yield return SelectLesson();
            Assert.That(ui.Q(className: "training-card"), Is.Not.Null);
            var session = Object.FindAnyObjectByType<EquipmentRig>().Session;
            Assert.That(session.Read("bed-01").BackrestDegrees, Is.Zero);
            session.Execute("monitor-01", EquipmentAction.TogglePower, out _);
            session.Execute("bed-01", EquipmentAction.RaiseBackrest, out _);
            Assert.That(ui.Q(className: "training-marker"), Is.Not.Null);
            session.Execute("bed-01", EquipmentAction.RaiseBackrest, out _);
            Assert.That(ui.Q<Label>(className: "training-guidance").text, Does.Contain("Training complete"));
            Assert.That(ui.Q(className: "training-marker"), Is.Null);
            Submit(FindButton("Finish training"));
            Assert.That(ui.Q(className: "training-card"), Is.Null);
            yield return SelectLesson();
            Assert.That(session.Read("bed-01").BackrestDegrees, Is.Zero);
            Submit(FindButton("Editor"));
            Assert.That(ui.Q(className: "training-card"), Is.Null);
            yield return SelectLesson();
            var target = ui.Query<DropdownField>().ToList().First(field => field.label == "Target backrest angle");
            target.index = 2;
            Submit(FindButton("Apply training"));
            Submit(FindButton("Preview"));
            yield return SelectLesson();
            Assert.That(ui.Q<Label>(className: "training-angle").text, Does.Contain("Target  45°"));
            Submit(FindButton("Editor"));
            yield return AddFromTemplate(false);
            Assert.That(ui.Query<TextField>().ToList().First(f => f.label == "Training name").value, Is.EqualTo(backrest + " (2)"));
            ui.Query<TextField>().ToList().First(f => f.label == "Training name").value = "Practice attempt";
            ui.Query<TextField>().ToList().First(f => f.label == "Patient request").value = "Help me sit up.";
            Submit(FindButton("Apply training"));
            yield return SelectLesson("Practice attempt");
            Submit(FindButton("Delete training…"));
            var dialog = ui.Q(className: "modal-card");
            Submit(dialog.Query<Button>().ToList().First(b => b.text == "Cancel"));
            Submit(FindButton("Delete training…"));
            Submit(FindButton("Delete"));
            Submit(FindButton("Preview"));
            yield return SelectLesson();
            Assert.That(ui.Q<Label>(className: "training-angle").text, Does.Contain("Target  45°"));
            Submit(FindButton("Editor"));
            Submit(FindButton("Training ▾"));
            yield return null;
            var editorMenu = ui.Q(className: "lesson-menu-layer");
            Assert.That(editorMenu.Query<Button>().ToList().Any(b => b.text == "Practice attempt"), Is.False);
            using (var cancel = NavigationCancelEvent.GetPooled()) editorMenu.SendEvent(cancel);

            yield return SelectLesson(menuButton: "Exam ▾");
            Assert.That(ui.Query<DropdownField>().ToList().Single(f => f.label == "Target backrest angle").value, Is.EqualTo("30°"));
            yield return AddFromTemplate(true);
            ui.Query<TextField>().ToList().Single(f => f.label == "Exam name").value = "Independent exam";
            ui.Query<DropdownField>().ToList().Single(f => f.label == "Target backrest angle").index = 2;
            Submit(FindButton("Apply exam"));

            var catalogue = TrainingJson.Read<Catalogue>(Resources.Load<TextAsset>("DesktopCatalogue").text);
            var room = catalogue.environments.First(p => p.environmentId == "nursing-skills-room");
            var stage = ui.Q(className: "stage");
            var keys = Object.FindObjectsByType<BedHandsetKey>();
            Assert.That(keys.Length, Is.EqualTo(2));
            IEnumerator VerifyMarkerClear(VisualElement marker, WorkplaceStudio.Core.Pose pose)
            {
                nav.Apply(pose);
                // The UI layout pass is driven by the editor panel's repaint,
                // independently of the Play Mode camera update.
                testWindow.Repaint();
                var layoutTime = Time.realtimeSinceStartup + .35f;
                while (Time.realtimeSinceStartup < layoutTime) yield return null;
                Assert.That(marker.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(marker.resolvedStyle.visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(marker.worldBound.width, Is.GreaterThan(0));
                foreach (var key in keys)
                {
                    var bounds = key.GetComponent<Renderer>().bounds;
                    var min = Vector2.positiveInfinity; var max = Vector2.negativeInfinity;
                    for (var i = 0; i < 8; i++)
                    {
                        var v = nav.View.WorldToViewportPoint(new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x,
                            (i & 2) == 0 ? bounds.min.y : bounds.max.y, (i & 4) == 0 ? bounds.min.z : bounds.max.z));
                        var pixel = stage.LocalToWorld(new Vector2(v.x * stage.contentRect.width, (1 - v.y) * stage.contentRect.height));
                        min = Vector2.Min(min, pixel); max = Vector2.Max(max, pixel);
                    }
                    var keyRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    Assert.That(marker.worldBound.Overlaps(keyRect), Is.False,
                        $"{key.name}: marker {marker.worldBound}, key {keyRect}, stage {stage.worldBound}");
                }
            }
            var views = new[] { room.overview, room.editor.zones.First(z => z.id == "nursing-bed").camera };
            var pin = ui.Query<Button>(className: "pin").ToList().Single(b => b.text == "1");
            foreach (var view in views) yield return VerifyMarkerClear(pin, view);
            Submit(FindButton("Preview"));
            yield return SelectLesson();
            var label = ui.Q<Label>(className: "training-marker");
            Assert.That(label.text, Is.EqualTo("Bed handset"));
            Assert.That(label.pickingMode, Is.EqualTo(PickingMode.Ignore));
            foreach (var view in views) yield return VerifyMarkerClear(label, view);

            // Use the independently authored exam and grade only the submitted angle.
            nav.Spectator = true;
            yield return SelectLesson("Independent exam", "Exam ▾");
            var exam = ui.Q(className: "exam-card");
            Assert.That(exam, Is.Not.Null);
            Assert.That(nav.Spectator, Is.False);
            Assert.That(exam.Q<Label>(className: "training-angle").text, Does.Contain("45°"));
            Assert.That(session.Read("bed-01").BackrestDegrees, Is.Zero);
            Assert.That(ui.Q(className: "training-marker"), Is.Null);
            Assert.That(ui.Q(className: "training-guidance"), Is.Null);
            Assert.That(ui.Q(className: "equipment-item"), Is.Null);
            Assert.That(ui.Q(className: "pin"), Is.Null);
            Assert.That(ui.Q(className: "sidebar").style.display.value, Is.EqualTo(DisplayStyle.None));
            for (int press = 0; press < 3; press++) session.Execute("bed-01", EquipmentAction.RaiseBackrest, out _);
            Assert.That(ui.Q(className: "modal-card"), Is.Null);
            session.Execute("bed-01", EquipmentAction.LowerBackrest, out _);
            Submit(FindButton("Finish exam"));
            Assert.That(ui.Q(className: "modal-card").Q<Label>(className: "panel-title").text, Is.EqualTo("Exam not passed"));
            Assert.That(ui.Q(className: "modal-card").Q<Label>(className: "description").text, Does.Contain("Submitted backrest angle: 30°"));
            Submit(FindButton("OK"));
            Assert.That(ui.Q(className: "equipment-item"), Is.Not.Null);
            yield return SelectLesson("Independent exam", "Exam ▾");
            Assert.That(session.Read("bed-01").BackrestDegrees, Is.Zero);
            for (int press = 0; press < 4; press++) session.Execute("bed-01", EquipmentAction.RaiseBackrest, out _);
            session.Execute("bed-01", EquipmentAction.LowerBackrest, out _);
            Submit(FindButton("Finish exam"));
            Assert.That(ui.Q(className: "modal-card").Q<Label>(className: "panel-title").text, Is.EqualTo("Exam passed"));
            Submit(FindButton("OK"));
            Submit(FindButton("Editor"));
            yield return SelectLesson();
            Submit(FindButton("Delete training…"));
            Submit(FindButton("Delete"));
            yield return SelectLesson("Independent exam", "Exam ▾");
            Submit(FindButton("Delete exam…"));
            Submit(ui.Q(className: "modal-card").Query<Button>().ToList().Single(b => b.text == "Cancel"));
            Submit(FindButton("Delete exam…"));
            Submit(FindButton("Delete"));
            Submit(FindButton("Preview"));
            Assert.That(FindButton("Training ▾").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(FindButton("Exam ▾").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Submit(FindButton("Editor"));
            yield return SelectLesson(menuButton: "Exam ▾");
            Submit(FindButton("Delete exam…"));
            Submit(FindButton("Delete"));
            yield return AddFromTemplate(true);
            Assert.That(ui.Query<TextField>().ToList().Single(f => f.label == "Exam name").value, Is.EqualTo(backrest));
            yield return AddFromTemplate(false);
            Assert.That(ui.Query<TextField>().ToList().Single(f => f.label == "Training name").value, Is.EqualTo(backrest));
        }
        [UnityTest] public IEnumerator AuthoredRoomLoadsWithWorkingEquipmentAndWalkableFloor()
        {
            yield return new EnterPlayMode();
            var catalogue = TrainingJson.Read<Catalogue>(Resources.Load<TextAsset>("DesktopCatalogue").text);
            var project = catalogue.environments.Find(p => p.environmentId == "nursing-skills-room");
            TrainingValidation.Project(project);
            using (var cancellation = new CancellationTokenSource(60000))
            {
                var loading = LocalSceneLoader.Load(Path.Combine(Application.streamingAssetsPath, "WorkplaceStudio", project.modelFile), project, cancellation.Token);
                while (!loading.IsCompleted) yield return null;
                scene = loading.GetAwaiter().GetResult();
            }
            Assert.That(scene.Equipment, Is.Not.Null); Assert.That(scene.Equipment.Definitions.Count, Is.EqualTo(4));
            foreach (var point in project.editor.hotspots) Assert.That(scene.Nodes.ContainsKey(point.targetNode), Is.True, point.targetNode);
            var session = scene.Equipment.Session;
            var bedView = project.editor.zones.Find(z => z.id == "nursing-bed").camera;
            var eye = Coordinates.ToUnity(bedView.position);
            Physics.SyncTransforms();
            void PressHandsetKey(string node, EquipmentAction action)
            {
                var centre = scene.Nodes[node].GetComponent<Renderer>().bounds.center;
                Assert.That(Physics.Raycast(eye, centre - eye, out var hit, 8f), Is.True, node);
                var key = hit.transform.GetComponentInParent<BedHandsetKey>();
                Assert.That(key, Is.Not.Null, "The saved bed view must have a clear line of sight to " + node);
                Assert.That(key.Action, Is.EqualTo(action));
                Assert.That(session.Execute(scene.Equipment.FindEquipment(hit.transform), key.Action, out _), Is.True);
            }
            PressHandsetKey("NR_Bed_Raise", EquipmentAction.RaiseBackrest);
            Assert.That(session.Read("bed-01").BackrestDegrees, Is.EqualTo(30));
            PressHandsetKey("NR_Bed_Lower", EquipmentAction.LowerBackrest);
            Assert.That(session.Read("bed-01").BackrestDegrees, Is.EqualTo(15));
            var pillow = scene.Nodes["Pillow"]; float initialPillowHeight = pillow.position.y;
            session.Execute("bed-01", EquipmentAction.RaiseBackrest, out _); scene.Equipment.StepVisuals(1);
            Assert.That(pillow.position.y, Is.GreaterThan(initialPillowHeight + .1f), "Raising the backrest must lift the head end in world space.");
            session.Execute("bed-01", EquipmentAction.Reset, out _); scene.Equipment.StepVisuals(1);
            Assert.That(pillow.position.y, Is.EqualTo(initialPillowHeight).Within(.001));
            var door = scene.Nodes["NR_Pump_Door"]; var closed = door.localRotation;
            Assert.That(session.Execute("pump-01", EquipmentAction.ToggleDoor, out _), Is.True);
            scene.Equipment.StepVisuals(1); Assert.That(Quaternion.Angle(closed, door.localRotation), Is.EqualTo(95).Within(.1));
            Assert.That(session.Execute("pump-01", EquipmentAction.TogglePower, out _), Is.True);
            Assert.That(session.Execute("pump-01", EquipmentAction.ToggleRun, out _), Is.False);
            session.Execute("pump-01", EquipmentAction.ToggleDoor, out _); session.Execute("pump-01", EquipmentAction.ToggleRun, out _);
            Assert.That(session.Read("pump-01").Running, Is.True);
            var rail = scene.Nodes["NR_Bed_Rail"]; var raised = rail.position;
            session.Execute("bed-01", EquipmentAction.ToggleRail, out _); scene.Equipment.StepVisuals(1);
            Assert.That(raised.y - rail.position.y, Is.EqualTo(.31f).Within(.001));
            var callLight = scene.Nodes["NR_Call_Light"].GetComponent<Renderer>();
            session.Execute("call-01", EquipmentAction.Call, out _); scene.Equipment.StepVisuals(1);
            Assert.That(session.Read("call-01").CallActive, Is.True);
            Assert.That(callLight.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Unlit"));
            Assert.That(callLight.sharedMaterial.GetColor("_BaseColor").r, Is.GreaterThan(.9f), "Active call must visibly illuminate amber.");
            session.Execute("call-01", EquipmentAction.Acknowledge, out _); scene.Equipment.StepVisuals(1);
            Assert.That(session.Read("call-01").CallActive, Is.False);
            Assert.That(callLight.sharedMaterial.GetColor("_BaseColor").r, Is.LessThan(.1f));
            Assert.That(scene.Equipment.FindEquipment(scene.Nodes["NR_Monitor_Display"]), Is.EqualTo("monitor-01"));
            player = new GameObject("Nursing walking test"); var navigation = player.AddComponent<DesktopNavigation>(); navigation.Initialize();
            navigation.ConfigureForEnvironment(project.environmentId); navigation.Apply(project.overview); navigation.Ready = true;
            for (int i = 0; i < 300; i++) navigation.MoveWalking(Vector3.zero, false, .02f);
            Assert.That(navigation.View.transform.position.y, Is.EqualTo(1.65).Within(.08));
            for (int i = 0; i < 100; i++) navigation.MoveWalking(Vector3.back, false, .01f);
            Assert.That(navigation.View.transform.position.z, Is.LessThan(project.overview.position[2] - 1));
            Assert.That(navigation.View.transform.position.y, Is.EqualTo(1.65).Within(.08));
            yield return null;
            var renderer = scene.Nodes["NR_Monitor_Display"].GetComponent<Renderer>();
            Assert.That(renderer.sharedMaterial.GetTexture("_BaseMap"), Is.TypeOf<Texture2D>());
        }
    }
}
#endif
