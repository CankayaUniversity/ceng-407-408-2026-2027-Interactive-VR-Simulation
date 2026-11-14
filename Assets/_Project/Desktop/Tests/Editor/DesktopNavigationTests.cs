#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using WorkplaceStudio.Core;
using WorkplaceStudio.Desktop;
using Pose = WorkplaceStudio.Core.Pose;

namespace WorkplaceStudio.Tests
{
    public sealed class DesktopNavigationTests
    {
        GameObject rig, obstacle;
        DesktopNavigation navigation;
        [SetUp] public void SetUp()
        {
            rig = new GameObject("Desktop navigation test");
            navigation = rig.AddComponent<DesktopNavigation>(); navigation.Initialize();
            navigation.Apply(new Pose { position = new[] { -3000f, 2f, 3000f }, target = new[] { -3000f, 2f, 2990f } });
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(rig); if (obstacle != null) Object.DestroyImmediate(obstacle); }
        [Test] public void SourceCoordinatesRoundTripWithoutHandednessDrift()
        {
            var source = new[] { -5.1407764f, 2.7760885f, -4.3897204f };
            CollectionAssert.AreEqual(source, Coordinates.ToSource(Coordinates.ToUnity(source)));
            Assert.That(Coordinates.ToUnity(source).x, Is.GreaterThan(0));
        }
        [Test] public void ApplyAndCaptureKeepEyePositionAndWorldUp()
        {
            var capture = navigation.Capture();
            Assert.That(capture.position[0], Is.EqualTo(-3000).Within(.001));
            Assert.That(capture.position[1], Is.EqualTo(2).Within(.001));
            Assert.That(capture.position[2], Is.EqualTo(3000).Within(.001));
            Assert.That(Vector3.Dot(navigation.View.transform.up, Vector3.up), Is.EqualTo(1).Within(.0001));
        }
        [Test] public void CancellationSettlesAsCancelledRatherThanBlocked()
        {
            DesktopMoveResult? result = null;
            navigation.MoveTo(new Pose { position = new[] { -3000f, 2f, 2997f }, target = new[] { -3000f, 2f, 2990f } }, value => result = value);
            Assert.That(navigation.Moving, Is.True); navigation.CancelMove();
            Assert.That(result, Is.EqualTo(DesktopMoveResult.Cancelled)); Assert.That(navigation.Moving, Is.False);
        }
        [Test] public void EquipmentBlocksCameraPathWithoutMovingTheEye()
        {
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube); obstacle.transform.position = new Vector3(3000, 2, 2998);
            Physics.SyncTransforms(); var before = navigation.View.transform.position; DesktopMoveResult? result = null;
            navigation.MoveTo(new Pose { position = new[] { -3000f, 2f, 2996f }, target = new[] { -3000f, 2f, 2990f } }, value => result = value);
            Assert.That(result, Is.EqualTo(DesktopMoveResult.Blocked)); Assert.That(navigation.View.transform.position, Is.EqualTo(before));
        }
        void CreateFloor()
        {
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = new Vector3(3000, -.25f, 3000);
            obstacle.transform.localScale = new Vector3(20, .5f, 20);
            navigation.ConfigureForEnvironment("charite-operating-room");
            navigation.Ready = true;
        }
        [Test] public void TallerHospitalProfileGroundsOldEyePoseBeforeWalking()
        {
            CreateFloor();
            navigation.Apply(new Pose { position = new[] { -3000f, 1.65f, 3000f }, target = new[] { -3000f, 1.65f, 2990f } });
            Assert.That(rig.transform.position.y, Is.LessThan(0), "Reproduce feet below the floor after the height change.");
            Assert.That(navigation.Capture().position[1], Is.EqualTo(1.65f).Within(.001), "Inspection pose must stay unchanged until walking.");
            for (int i = 0; i < 600; i++) navigation.MoveWalking(Vector3.forward, false, .005f);
            Assert.That(navigation.View.transform.position.y, Is.EqualTo(2.4f).Within(.06));
            Assert.That(rig.transform.position.z, Is.GreaterThan(3006), "Grounding must still allow horizontal movement.");
        }
        [Test] public void ReturningFromSpectatorReestablishesStandingHeight()
        {
            CreateFloor();
            navigation.MoveWalking(Vector3.zero, false, .02f);
            navigation.Spectator = true;
            navigation.Apply(new Pose { position = new[] { -3000f, 1f, 3000f }, target = new[] { -3000f, 1f, 2990f } });
            navigation.MoveWalking(Vector3.zero, false, .02f);
            Assert.That(navigation.View.transform.position.y, Is.EqualTo(1).Within(.001));
            navigation.Spectator = false;
            for (int i = 0; i < 120; i++) navigation.MoveWalking(Vector3.zero, false, .02f);
            Assert.That(navigation.View.transform.position.y, Is.EqualTo(2.4f).Within(.06));
        }
    }
    public sealed class HospitalFloorTests
    {
        GameObject rig;
        LoadedScene scene;
        [UnityTearDown] public IEnumerator TearDown()
        {
            scene?.Dispose();
            if (rig != null) Object.Destroy(rig);
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
        [UnityTest] public IEnumerator ActualHospitalFloorSupportsWalkingFromOriginalOverview()
        {
            // glTFast's runtime defer agent requires Play Mode (DontDestroyOnLoad).
            yield return new EnterPlayMode();
            rig = new GameObject("Hospital walking integration test");
            var navigation = rig.AddComponent<DesktopNavigation>(); navigation.Initialize();
            var catalogue = TrainingJson.Read<Catalogue>(Resources.Load<TextAsset>("DesktopCatalogue").text);
            var hospital = catalogue.environments.Find(item => item.environmentId == "charite-operating-room");
            var path = Path.Combine(Application.streamingAssetsPath, "WorkplaceStudio", hospital.modelFile);
            using (var cancellation = new CancellationTokenSource(60000))
            {
                var loading = LocalSceneLoader.Load(path, hospital, cancellation.Token);
                while (!loading.IsCompleted) yield return null;
                scene = loading.GetAwaiter().GetResult();
                navigation.ConfigureForEnvironment(hospital.environmentId);
                navigation.Apply(hospital.overview); navigation.Ready = true;
                Physics.SyncTransforms();
                Assert.That(Physics.Raycast(navigation.View.transform.position, Vector3.down, out var floor, 5,
                    ~((1 << 30) | (1 << 31)), QueryTriggerInteraction.Ignore), Is.True, "Actual scanned floor must be available.");
                Assert.That(rig.transform.position.y, Is.LessThan(floor.point.y), "Original overview reproduces the embedded capsule.");
                for (int i = 0; i < 600; i++) navigation.MoveWalking(Vector3.zero, false, .02f);
                Assert.That(navigation.View.transform.position.y, Is.EqualTo(floor.point.y + navigation.EyeHeight).Within(.08));
                for (int i = 0; i < 60; i++) navigation.MoveWalking(Vector3.back, false, .01f);
                Assert.That(navigation.View.transform.position.z, Is.LessThan(3.5f), "WASD movement must traverse the scanned floor.");
                Assert.That(navigation.View.transform.position.y, Is.EqualTo(floor.point.y + navigation.EyeHeight).Within(.12));
            }
        }
    }
}
#endif
