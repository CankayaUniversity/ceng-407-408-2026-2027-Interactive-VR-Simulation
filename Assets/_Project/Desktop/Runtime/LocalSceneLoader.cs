using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Loading;
using GLTFast.Logging;
using UnityEngine;
using WorkplaceStudio.Core;

namespace WorkplaceStudio.Desktop
{
    public static class Coordinates
    {
        // glTFast mirrors X when importing right-handed glTF into Unity.
        public static Vector3 ToUnity(float[] value) => new Vector3(-value[0], value[1], value[2]);
        public static float[] ToSource(Vector3 value) => new[] { -value.x, value.y, value.z };
        public static Quaternion Rotation(float[] r) => Quaternion.AngleAxis(-r[1] * Mathf.Rad2Deg, Vector3.up) *
            Quaternion.AngleAxis(r[0] * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(-r[2] * Mathf.Rad2Deg, Vector3.forward);
    }

    sealed class NoExternalDownloads : IDownloadProvider
    {
        public Task<IDownload> Request(Uri url) => Task.FromException<IDownload>(new InvalidDataException("External model resources are blocked."));
        public Task<ITextureDownload> RequestTexture(Uri url, bool nonReadable) => Task.FromException<ITextureDownload>(new InvalidDataException("External texture resources are blocked."));
    }

    public sealed class LoadedScene : IDisposable
    {
        public GameObject Root { get; }
        public Bounds Bounds { get; }
        readonly GltfImport importer;
        public LoadedScene(GameObject root, GltfImport importer, Bounds bounds) { Root = root; this.importer = importer; Bounds = bounds; }
        public void Dispose() { if (Root != null) UnityEngine.Object.Destroy(Root); importer.Dispose(); }
    }

    public static class LocalSceneLoader
    {
        public static async Task<LoadedScene> Load(string path, TrainingProject project, CancellationToken cancellation)
        {
            GlbFiles.Verify(path, project.modelSha256);
            cancellation.ThrowIfCancellationRequested();
            var root = new GameObject("Environment - " + project.environmentId);
            root.SetActive(false);
            var logger = new CollectingLogger();
            var importer = new GltfImport(downloadProvider: new NoExternalDownloads(), logger: logger);
            try
            {
                if (!await importer.LoadFile(path, importSettings: new ImportSettings { AnimationMethod = AnimationMethod.None }, cancellationToken: cancellation))
                    throw new InvalidDataException("Could not load the GLB model. Check format and texture support.");
                var instantiator = new GameObjectInstantiator(importer, root.transform, logger, new InstantiationSettings { Mask = ComponentType.Mesh });
                if (!await importer.InstantiateMainSceneAsync(instantiator, cancellation)) throw new InvalidDataException("Could not instantiate the GLB scene.");
                if (logger.Items != null && logger.Items.Any(item => item.Type == LogType.Error)) throw new InvalidDataException("The model could not be opened because some geometry or materials failed to load.");
                cancellation.ThrowIfCancellationRequested();
                var transform = project.editor.modelTransform;
                root.transform.position = Coordinates.ToUnity(transform.position);
                root.transform.rotation = Coordinates.Rotation(transform.rotation);
                root.transform.localScale = new Vector3(transform.scale[0], transform.scale[1], transform.scale[2]);
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidDataException("The model has no renderable geometry.");
                root.SetActive(true);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                int count = 0;
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (filter.sharedMesh == null) continue;
                    if (!filter.sharedMesh.isReadable) throw new InvalidDataException("Mesh data is not readable for collision. Use the desktop build settings.");
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                    if (++count % 32 == 0) await Task.Yield();
                }
                cancellation.ThrowIfCancellationRequested();
                return new LoadedScene(root, importer, bounds);
            }
            catch { UnityEngine.Object.Destroy(root); importer.Dispose(); throw; }
        }
    }
}
