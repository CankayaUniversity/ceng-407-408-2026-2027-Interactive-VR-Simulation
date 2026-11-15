using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WorkplaceStudio.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

static class Program
{
    static int passed, failed;
    static string root, temporary, model;
    static void Check(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
    static void Reject(Action action) { try { action(); } catch { return; } throw new Exception("Invalid input was accepted"); }
    static void Test(string title, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS " + title); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + title + ": " + error.Message); }
    }
    static TrainingProject Sample() => new TrainingProject
    {
        id = "test", title = "Test project", environmentId = "test-environment", modelFile = "environment.glb", modelSha256 = GlbFiles.Sha256(model),
        editor = new EditorDocument { environmentId = "test-environment", modelUrl = "/test.glb", zones = new List<Area>
        {
            new Area { id = "root", title = "Root", environmentId = "test-environment", order = 0 },
            new Area { id = "child", title = "Child", environmentId = "test-environment", parentZoneId = "root", order = 0 },
            new Area { id = "grandchild", title = "Grandchild", environmentId = "test-environment", parentZoneId = "child", order = 0 },
        }, hotspots = new List<Hotspot> { new Hotspot { id = "point", title = "Point", description = "Original note", zoneId = "child", position = new[] { 1f, 2f, 3f } } } }
    };
    static void WriteGlb(string file, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json); var padded = new byte[(bytes.Length + 3) / 4 * 4];
        for (int i = 0; i < padded.Length; i++) padded[i] = i < bytes.Length ? bytes[i] : (byte)32;
        using (var writer = new BinaryWriter(File.Create(file)))
        {
            writer.Write(0x46546C67u); writer.Write(2u); writer.Write((uint)(12 + 8 + padded.Length + 8 + 4));
            writer.Write((uint)padded.Length); writer.Write(0x4E4F534Au); writer.Write(padded); writer.Write(4u); writer.Write(0x004E4942u); writer.Write(0u);
        }
    }
    static int Main(string[] args)
    {
        root = Path.GetFullPath(args.Length > 0 ? args[0] : Directory.GetCurrentDirectory());
        temporary = Path.Combine(root, "Builds", "DesktopCoreTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temporary);
        model = Path.Combine(temporary, "model.glb"); WriteGlb(model, "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":4}]}");
        Test("valid self-contained GLB header and digest", () => GlbFiles.Verify(model, GlbFiles.Sha256(model)));
        Test("LFS pointer and truncated GLB are rejected", () =>
        {
            var pointer = Path.Combine(temporary, "pointer.glb"); File.WriteAllText(pointer, "version https://git-lfs.github.com/spec/v1\noid sha256:invalid\nsize 999\n"); Reject(() => GlbFiles.Validate(pointer));
            var truncated = Path.Combine(temporary, "truncated.glb"); File.WriteAllBytes(truncated, File.ReadAllBytes(model).Take(25).ToArray()); Reject(() => GlbFiles.Validate(truncated));
        });
        Test("remote, relative and data URI dependencies are denied", () =>
        {
            foreach (var uri in new[] { "https://example.com/image.jpg", "../outside.bin", "file:///C:/secret", "data:application/octet-stream;base64,AAAA" })
            {
                var file = Path.Combine(temporary, "external.glb"); WriteGlb(file, "{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":4,\"uri\":" + Newtonsoft.Json.JsonConvert.SerializeObject(uri) + "}]}"); Reject(() => GlbFiles.Validate(file));
            }
        });
        Test("project JSON round-trips source-space coordinates and hierarchy", () =>
        {
            var project = Sample(); var reread = TrainingJson.Read<TrainingProject>(TrainingJson.Write(project)); TrainingValidation.Project(reread);
            Check(reread.editor.hotspots[0].position.SequenceEqual(new[] { 1f, 2f, 3f })); Check(reread.editor.zones[2].parentZoneId == "child");
        });
        Test("unknown versions, nonfinite vectors and duplicate IDs are rejected", () =>
        {
            var p = Sample(); p.formatVersion = 2; Reject(() => TrainingValidation.Project(p));
            p = Sample(); p.editor.hotspots[0].position[0] = float.NaN; Reject(() => TrainingValidation.Project(p));
            p = Sample(); p.editor.zones.Add(p.editor.zones[0]); Reject(() => TrainingValidation.Project(p));
            p = Sample(); p.overview.target = p.overview.position; Reject(() => TrainingValidation.Project(p));
        });
        Test("hierarchy cycles, missing parents and cross-environment areas are rejected", () =>
        {
            var p = Sample(); p.editor.zones[0].parentZoneId = "grandchild"; Reject(() => TrainingValidation.Project(p));
            p = Sample(); p.editor.zones[1].parentZoneId = "missing"; Reject(() => TrainingValidation.Project(p));
            p = Sample(); p.editor.zones[1].environmentId = "cockpit"; Reject(() => TrainingValidation.Project(p));
        });
        Test("moving a parent under its descendant leaves existing data intact", () =>
        {
            var p = Sample(); var before = TrainingJson.Write(p); Reject(() => AreaHierarchy.Move(p.editor, "root", "grandchild")); Check(TrainingJson.Write(p) == before);
        });
        Test("saving an existing area under the same parent preserves sibling order", () =>
        {
            var p = Sample(); p.editor.zones[0].order = null;
            var before = TrainingJson.Write(p); AreaHierarchy.Move(p.editor, "root", null);
            Check(TrainingJson.Write(p) == before);
            p.editor.zones[1].order = 4; before = TrainingJson.Write(p);
            AreaHierarchy.Move(p.editor, "child", "root"); Check(TrainingJson.Write(p) == before);
        });
        Test("reparenting appends after siblings with legacy implicit ordering", () =>
        {
            var p = Sample(); p.editor.zones[1].order = null;
            p.editor.zones.Add(new Area { id = "peer", title = "Peer", environmentId = "test-environment", parentZoneId = "root" });
            AreaHierarchy.Move(p.editor, "grandchild", "root"); TrainingValidation.Project(p);
            Check(AreaHierarchy.Children(p.editor, "root").Select(a => a.id).SequenceEqual(new[] { "child", "peer", "grandchild" }));
        });
        Test("removing an area reparents direct children and points without deleting deeper content", () =>
        {
            var p = Sample(); AreaHierarchy.Remove(p.editor, "child"); TrainingValidation.Project(p);
            Check(p.editor.zones.Count == 2); Check(p.editor.zones[1].parentZoneId == "root"); Check(p.editor.hotspots[0].zoneId == "root"); Check(p.editor.hotspots[0].id == "point");
        });
        Test("path resolution blocks traversal, absolute paths and prefix siblings", () =>
        {
            foreach (var path in new[] { "../outside.glb", "assets/../../outside.glb", "C:\\secret.glb", "https://example.com/model.glb", "/etc/model.glb", "assets//model.glb" }) Reject(() => LocalPaths.Resolve(temporary, path));
            Check(LocalPaths.Resolve(temporary, "assets/model.glb").StartsWith(temporary + Path.DirectorySeparatorChar));
        });
        Test("save and reopen portable project verifies copied GLB and preserves source", () =>
        {
            var folder = Path.Combine(temporary, "saved"); var p = Sample(); ProjectStore.Save(folder, p, model);
            var read = ProjectStore.Open(folder); Check(read.title == p.title); Check(read.editor.hotspots[0].description == "Original note");
            Check(read.modelFile.StartsWith("assets/")); Check(p.modelFile == "environment.glb"); Check(File.Exists(model));
        });
        Test("second save keeps a valid previous manifest", () =>
        {
            var folder = Path.Combine(temporary, "backup"); var p = Sample(); ProjectStore.Save(folder, p, model); p.title = "Updated"; ProjectStore.Save(folder, p, model);
            Check(ProjectStore.Open(folder).title == "Updated"); var previous = TrainingJson.Read<TrainingProject>(File.ReadAllText(Path.Combine(folder, "project.previous.json"))); Check(previous.title == "Test project");
        });
        Test("bad input never overwrites an existing saved project", () =>
        {
            var folder = Path.Combine(temporary, "preserved"); var p = Sample(); ProjectStore.Save(folder, p, model); var before = File.ReadAllText(Path.Combine(folder, "project.json"));
            p.editor.zones[0].parentZoneId = "grandchild"; Reject(() => ProjectStore.Save(folder, p, model)); Check(File.ReadAllText(Path.Combine(folder, "project.json")) == before);
        });
        Test("corrupted project assets are detected without rewriting metadata", () =>
        {
            var folder = Path.Combine(temporary, "corrupt"); ProjectStore.Save(folder, Sample(), model); var before = File.ReadAllText(Path.Combine(folder, "project.json"));
            var p = TrainingJson.Read<TrainingProject>(before); File.WriteAllText(LocalPaths.Resolve(folder, p.modelFile), "corrupted"); Reject(() => ProjectStore.Open(folder)); Check(File.ReadAllText(Path.Combine(folder, "project.json")) == before);
        });
        var catalogueFile = Path.Combine(root, "Assets", "_Project", "Desktop", "Resources", "DesktopCatalogue.json");
        Test("combined-final catalogue validates with ten hospital areas and source points", () =>
        {
            var catalogue = TrainingJson.Read<Catalogue>(File.ReadAllText(catalogueFile)); Check(catalogue.environments.Count == 3);
            foreach (var project in catalogue.environments) TrainingValidation.Project(project);
            var hospital = catalogue.environments.Find(p => p.environmentId == "charite-operating-room");
            Check(hospital.editor.zones.Count == 10 && hospital.editor.hotspots.Count == 10); Check(hospital.editor.hotspots[0].title == "Patient bed socket");
            Check(hospital.editor.hotspots[0].position[1] > 1); Check(hospital.editor.hotspots.All(s => hospital.editor.zones.Any(a => a.id == s.zoneId)));
        });
        var nursing = TrainingJson.Read<Catalogue>(File.ReadAllText(catalogueFile)).environments.Find(p => p.environmentId == "nursing-skills-room");
        Test("nursing room preserves equipment bindings and moving hotspot anchors on save/reopen", () =>
        {
            TrainingValidation.Project(nursing); Check(nursing.equipment.Count == 4); Check(nursing.editor.hotspots.Count == 4);
            var folder = Path.Combine(temporary, "nursing"); ProjectStore.Save(folder, nursing, Path.Combine(root, nursing.editor.modelUrl));
            var reopened = ProjectStore.Open(folder);
            Check(reopened.equipment.Select(e => e.id).SequenceEqual(nursing.equipment.Select(e => e.id)));
            Check(reopened.editor.hotspots.All(p => p.targetPosition != null && p.targetNode != null));
        });
        Test("equipment metadata rejects duplicate IDs and malformed moving anchors", () =>
        {
            var p = TrainingJson.Clone(nursing); p.equipment[1].id = p.equipment[0].id; Reject(() => TrainingValidation.Project(p));
            p = TrainingJson.Clone(nursing); p.editor.hotspots[0].targetPosition = new[] { float.NaN, 0, 0 }; Reject(() => TrainingValidation.Project(p));
        });
        Test("pump guards reject impossible actions without emitting lesson events", () =>
        {
            var session = new EquipmentSession(nursing.equipment); var events = new List<EquipmentEvent>(); session.Performed += events.Add;
            Check(!session.Execute("pump-01", EquipmentAction.ToggleRun, out _)); Check(events.Count == 0);
            Check(session.Execute("pump-01", EquipmentAction.TogglePower, out _));
            Check(session.Execute("pump-01", EquipmentAction.ToggleDoor, out _));
            Check(!session.Execute("pump-01", EquipmentAction.ToggleRun, out _));
            Check(session.Execute("pump-01", EquipmentAction.ToggleDoor, out _)); Check(session.Execute("pump-01", EquipmentAction.ToggleRun, out _));
            Check(!session.Execute("pump-01", EquipmentAction.ToggleDoor, out _));
            Check(session.Execute("pump-01", EquipmentAction.TogglePower, out _)); Check(!session.Read("pump-01").Running);
            Check(events.Select(e => e.Sequence).SequenceEqual(new long[] { 1,2,3,4,5 }));
            Check(!events[0].Before.Powered && events[0].After.Powered && events[3].After.Running);
        });
        Test("bed limits, call acknowledge and reset remain isolated between equipment", () =>
        {
            var session = new EquipmentSession(nursing.equipment);
            for (int i = 0; i < 8; i++) session.Execute("bed-01", EquipmentAction.RaiseBackrest, out _);
            Check(session.Read("bed-01").BackrestDegrees == 60);
            Check(!session.Execute("bed-01", EquipmentAction.ToggleRun, out _));
            Check(!session.Execute("unknown", EquipmentAction.Reset, out _));
            Check(session.Execute("call-01", EquipmentAction.Call, out _)); Check(session.Read("call-01").CallActive);
            Check(session.Execute("call-01", EquipmentAction.Acknowledge, out _)); Check(!session.Read("call-01").CallActive);
            Check(session.Execute("bed-01", EquipmentAction.ToggleRail, out _)); Check(session.Read("bed-01").RailLowered);
            Check(session.Execute("bed-01", EquipmentAction.Reset, out _)); Check(session.Read("bed-01").BackrestDegrees == 15 && !session.Read("bed-01").RailLowered);
            Check(session.Read("monitor-01").Powered && !session.Read("pump-01").Powered);
        });
        foreach (var relative in new[] { "web-editor/public/environments/charite_university_hospital_-_operating_room.glb", "web-editor/public/environments/cockpit/boeing_737-800_cockpit.glb", "web-editor/public/guides/piko/piko.glb", "desktop-content/nursing-room/nursing-room.glb" })
        {
            var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path)) Test("real bundled asset validates: " + Path.GetFileName(path), () => GlbFiles.Validate(path));
            else Console.WriteLine("SKIP local model not installed: " + relative);
        }
        Test("all desktop Unity sources have valid C# 9 syntax (engine type-check requires Unity)", () =>
        {
            var options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: new[] { "UNITY_EDITOR", "UNITY_EDITOR_WIN", "UNITY_STANDALONE_WIN", "UNITY_6000_0_OR_NEWER" });
            foreach (var file in Directory.GetFiles(Path.Combine(root, "Assets", "_Project", "Desktop"), "*.cs", SearchOption.AllDirectories))
            {
                var errors = CSharpSyntaxTree.ParseText(File.ReadAllText(file), options, file).GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                Check(errors.Count == 0, string.Join("; ", errors.Select(d => d.ToString())));
            }
        });
        Console.WriteLine($"Desktop core: {passed} passed, {failed} failed. Unity runtime is tested separately.");
        return failed == 0 ? 0 : 1;
    }
}
