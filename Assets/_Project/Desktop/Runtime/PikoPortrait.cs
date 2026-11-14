using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.UIElements;
using WorkplaceStudio.Core;

namespace WorkplaceStudio.Desktop
{
    public sealed class PikoPortrait : MonoBehaviour
    {
        const string ExpectedHash = "fd2338c12af77d767f769fdf0c06beba1b3279df78ceb6c36dbda2c5f82383ac";
        VisualElement container, portrait;
        Camera portraitCamera;
        GameObject modelRoot, cameraRoot;
        GltfImport importer;
        RenderTexture texture;
        Animation animationPlayer;
        CancellationTokenSource cancellation;
        bool waving, minimized;

        public void Initialize(VisualElement anchor, Action<string> message)
        {
            container = new VisualElement(); container.AddToClassList("piko");
            var button = new Button(Wave) { tooltip = "Say hello to Piko" }; button.AddToClassList("piko-button");
            portrait = button; container.Add(button);
            var controls = new VisualElement(); controls.AddToClassList("piko-controls"); controls.Add(new Label("Piko"));
            var minimize = new Button(() => { minimized = !minimized; portrait.style.display = minimized ? DisplayStyle.None : DisplayStyle.Flex; if (portraitCamera != null) portraitCamera.enabled = !minimized; if (animationPlayer != null) { if (minimized) animationPlayer.Stop(); else Idle(); } }) { text = "− / +", tooltip = "Minimize / show Piko" };
            controls.Add(minimize); container.Add(controls); anchor.Add(container);
            cancellation = new CancellationTokenSource(); _ = Load(button, message, cancellation.Token);
        }
        public void Attach(VisualElement anchor) { if (container != null && container.parent != anchor) { container.RemoveFromHierarchy(); anchor.Add(container); } }
        async Task Load(Button button, Action<string> message, CancellationToken token)
        {
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, "WorkplaceStudio", "guides", "piko.glb");
                if (!File.Exists(path)) { container.style.display = DisplayStyle.None; return; }
                GlbFiles.Verify(path, ExpectedHash);
                modelRoot = new GameObject("Piko portrait"); modelRoot.transform.SetParent(transform); modelRoot.transform.position = new Vector3(1000, 0, 0);
                importer = new GltfImport(downloadProvider: new NoExternalDownloads());
                if (!await importer.LoadFile(path, importSettings: new ImportSettings { AnimationMethod = AnimationMethod.Legacy }, cancellationToken: token) ||
                    !await importer.InstantiateMainSceneAsync(new GameObjectInstantiator(importer, modelRoot.transform, settings: new InstantiationSettings { Mask = ComponentType.Mesh | ComponentType.Animation, Layer = 30 }), token)) throw new InvalidDataException("Piko could not be loaded.");
                token.ThrowIfCancellationRequested();
                foreach (var child in modelRoot.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
                animationPlayer = modelRoot.GetComponentInChildren<Animation>(true);
                if (animationPlayer == null || animationPlayer["Idle"] == null || animationPlayer["Wave"] == null) throw new InvalidDataException("Piko Idle/Wave animation clips were not found.");
                animationPlayer.Play("Idle"); animationPlayer["Idle"].time = 0; animationPlayer.Sample();
                Bounds bounds = BoundsOfModel();
                animationPlayer.Play("Wave");
                for (int i = 0; i <= 10; i++) { animationPlayer["Wave"].time = animationPlayer["Wave"].length * i / 10; animationPlayer.Sample(); bounds.Encapsulate(BoundsOfModel()); }
                Idle();
                texture = new RenderTexture(256, 320, 24, RenderTextureFormat.ARGB32); texture.Create();
                var image = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore }; image.AddToClassList("piko-image"); button.Add(image);
                cameraRoot = new GameObject("Piko portrait camera"); cameraRoot.transform.SetParent(transform);
                portraitCamera = cameraRoot.AddComponent<Camera>(); portraitCamera.cullingMask = 1 << 30; portraitCamera.clearFlags = CameraClearFlags.SolidColor;
                portraitCamera.backgroundColor = Color.clear; portraitCamera.targetTexture = texture; portraitCamera.nearClipPlane = .01f; portraitCamera.farClipPlane = 100;
                portraitCamera.fieldOfView = 38;
                float distance = Mathf.Max(bounds.size.y, bounds.size.x / .8f) / (2 * Mathf.Tan(portraitCamera.fieldOfView * Mathf.Deg2Rad / 2)) * 1.12f + bounds.size.z / 2;
                cameraRoot.transform.position = bounds.center + new Vector3(.08f, .03f, 1).normalized * distance;
                cameraRoot.transform.LookAt(bounds.center);
                var lightRoot = new GameObject("Piko key light"); lightRoot.transform.SetParent(modelRoot.transform);
                var light = lightRoot.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 2; light.cullingMask = 1 << 30; lightRoot.transform.rotation = Quaternion.Euler(30, 150, 0);
                portraitCamera.enabled = !minimized;
            }
            catch (OperationCanceledException) { }
            catch (Exception error) { if (!token.IsCancellationRequested) { container.style.display = DisplayStyle.None; message("Could not open Piko: " + error.Message); } }
        }
        Bounds BoundsOfModel()
        {
            var renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidDataException("Piko geometry is missing.");
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        void Idle()
        {
            waving = false; if (animationPlayer == null) return;
            animationPlayer["Idle"].wrapMode = WrapMode.Loop; animationPlayer.Play("Idle");
        }
        void Wave()
        {
            if (animationPlayer == null || waving || minimized) return;
            waving = true; animationPlayer["Wave"].wrapMode = WrapMode.Once; animationPlayer.Play("Wave");
        }
        void Update() { if (waving && animationPlayer != null && !animationPlayer.IsPlaying("Wave")) Idle(); }
        void OnDestroy()
        {
            cancellation?.Cancel(); cancellation?.Dispose(); importer?.Dispose();
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (modelRoot != null) Destroy(modelRoot); if (cameraRoot != null) Destroy(cameraRoot);
        }
    }
}
