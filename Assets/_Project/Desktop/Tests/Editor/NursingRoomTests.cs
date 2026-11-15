#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WorkplaceStudio.Core;
using WorkplaceStudio.Desktop;

namespace WorkplaceStudio.Tests
{
    public sealed class NursingRoomTests
    {
        LoadedScene scene;
        GameObject player;
        [UnityTearDown] public IEnumerator CleanUp()
        {
            scene?.Dispose(); if (player != null) Object.Destroy(player);
            if (Application.isPlaying) yield return new ExitPlayMode();
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
