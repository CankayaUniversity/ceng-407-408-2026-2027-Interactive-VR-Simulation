using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using WorkplaceStudio.Core;
using Pose = WorkplaceStudio.Core.Pose;

namespace WorkplaceStudio.Desktop
{
    public sealed partial class DesktopBootstrap : MonoBehaviour, ITrainingActions
    {
        VisualElement root, stage, tree, inspector, markerLayer, modal, workspace, dashboard, sidebar;
        Label status, contextLabel, titleLabel, modeLabel, movementHelp;
        Button previewButton, editorButton, explorerToggle, inspectorToggle, focusToggle;
        UIDocument document;
        PanelSettings panel;
        Catalogue catalogue;
        TrainingProject project;
        LoadedScene environment;
        DesktopNavigation navigation;
        DesktopInputGate inputGate;
        CancellationTokenSource loading;
        readonly Dictionary<Hotspot, Button> pins = new Dictionary<Hotspot, Button>();
        readonly List<Pose> previousViews = new List<Pose>();
        string sourceModel, projectDirectory, activeArea, selectedArea, selectedHotspot;
        bool editing, dirty, placing, destroyed, allowQuit, busy;
        bool explorerOpen = true, inspectorOpen, focusView, explorerBeforeFocus, inspectorBeforeFocus;
        int navigationRevision;
        PikoPortrait piko;
        Light environmentLight;
        RenderPipelineAsset previousPipeline, previousQualityPipeline;
        AmbientMode previousAmbientMode;
        Color previousAmbientLight;
        string ProjectsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cengaverler", "WorkplaceStudio", "Projects");

        void Awake()
        {
            previousPipeline = GraphicsSettings.defaultRenderPipeline; previousQualityPipeline = QualitySettings.renderPipeline;
            var desktopPipeline = Resources.Load<RenderPipelineAsset>("DesktopPipeline");
            if (desktopPipeline != null) { GraphicsSettings.defaultRenderPipeline = desktopPipeline; QualitySettings.renderPipeline = desktopPipeline; }
            Application.targetFrameRate = 60;
            var rig = new GameObject("Desktop input adapter"); rig.transform.SetParent(transform);
            navigation = rig.AddComponent<DesktopNavigation>(); navigation.Initialize();
            previousAmbientMode = RenderSettings.ambientMode; previousAmbientLight = RenderSettings.ambientLight;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f, .53f, .6f);
            var lightRoot = new GameObject("Environment key light"); lightRoot.transform.SetParent(transform);
            environmentLight = lightRoot.AddComponent<Light>(); environmentLight.type = LightType.Directional; environmentLight.intensity = 1.6f;
            environmentLight.cullingMask = ~(1 << 30); lightRoot.transform.rotation = Quaternion.Euler(45, -35, 0);
            document = gameObject.AddComponent<UIDocument>();
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ConstantPixelSize; panel.scale = 1;
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DesktopRuntimeTheme"); document.panelSettings = panel;
            root = document.rootVisualElement; root.name = "studio";
            var style = Resources.Load<StyleSheet>("DesktopControls"); if (style != null) root.styleSheets.Add(style);
            try
            {
                var content = Resources.Load<TextAsset>("DesktopCatalogue");
                if (content == null) throw new InvalidDataException("Desktop content catalogue was not found.");
                catalogue = TrainingJson.Read<Catalogue>(content.text);
                if (catalogue.formatVersion != 1) throw new InvalidDataException("Unsupported content catalogue version.");
                foreach (var item in catalogue.environments) TrainingValidation.Project(item);
                BuildShell(); ShowDashboard();
                piko = gameObject.AddComponent<PikoPortrait>();
                piko.Initialize(root, Message, () => navigation?.ReleasePointer(), FocusHotspot);
            }
            catch (Exception error) { root.Add(new Label("Could not start the application: " + error.Message)); }
            Application.wantsToQuit += WantsToQuit;
        }

        static VisualElement Box(string className)
        {
            var element = new VisualElement(); element.AddToClassList(className); return element;
        }
        static Label Label(string text, string className = null)
        {
            var label = new Label(text); if (className != null) label.AddToClassList(className); return label;
        }
        static Button Button(string text, Action action, string className = null)
        {
            var button = new Button(action) { text = text }; if (className != null) button.AddToClassList(className); return button;
        }
        void BuildShell()
        {
            var header = Box("header"); header.Add(Label("w.", "brand-mark")); header.Add(Label("Workplace Studio", "brand"));
            header.Add(Button("Environments", () => GuardChanges(ShowDashboard), "quiet"));
            header.Add(Label("Local workspace", "header-note")); header.Add(Label("OFFLINE", "offline"));
            header.Add(Button("Exit", () => Application.Quit(), "quiet")); root.Add(header);
            dashboard = Box("dashboard"); root.Add(dashboard);
            workspace = Box("workspace"); root.Add(workspace);
            var toolbar = Box("toolbar"); titleLabel = Label("", "workspace-title"); toolbar.Add(titleLabel);
            previewButton = Button("Preview", () => SetMode(false)); editorButton = Button("Editor", () => SetMode(true)); toolbar.Add(previewButton); toolbar.Add(editorButton);
            toolbar.Add(Button("Save", SaveProject, "primary")); toolbar.Add(Button("Overview", ReturnToOverview));
            toolbar.Add(Button("Previous view", () => { if (previousViews.Count > 0) { var pose = previousViews[previousViews.Count - 1]; previousViews.RemoveAt(previousViews.Count - 1); GoTo(pose, null); } }));
            toolbar.Add(Button("Spectator", () => { navigation.ReleasePointer(); navigation.Spectator = !navigation.Spectator; UpdateContext(); }));
            explorerToggle = Button("Explorer", () => { explorerOpen = !explorerOpen; focusView = false; UpdatePanels(); });
            explorerToggle.tooltip = "Show or hide Scene Explorer"; toolbar.Add(explorerToggle);
            inspectorToggle = Button("Inspector", () => { inspectorOpen = !inspectorOpen; focusView = false; UpdatePanels(); });
            inspectorToggle.tooltip = "Show or hide Inspector"; toolbar.Add(inspectorToggle);
            focusToggle = Button("Focus view", ToggleFocusView); focusToggle.tooltip = "Use the full workspace for the 3D environment"; toolbar.Add(focusToggle);
            trainingButton = Button("Training ▾", () => ShowLessonMenu(false)); toolbar.Add(trainingButton);
            examButton = Button("Exam ▾", () => ShowLessonMenu(true)); toolbar.Add(examButton);
            workspace.Add(toolbar);
            var body = Box("workspace-body"); workspace.Add(body);
            sidebar = Box("sidebar"); sidebar.Add(Label("Scene Explorer", "panel-title"));
            var search = new TextField { label = "Find an area or point" }; search.RegisterValueChangedCallback(_ => RenderTree(search.value)); sidebar.Add(search);
            tree = new ScrollView(); tree.AddToClassList("tree"); sidebar.Add(tree); body.Add(sidebar);
            stage = Box("stage"); stage.focusable = true; body.Add(stage);
            stage.RegisterCallback<GeometryChangedEvent>(_ => UpdateViewport());
            stage.RegisterCallback<PointerDownEvent>(OnStagePointer);
            markerLayer = Box("marker-layer"); markerLayer.pickingMode = PickingMode.Ignore; stage.Add(markerLayer);
            movementHelp = Label("Right-click: capture mouse · WASD: move · Shift: faster · Esc: release cursor", "stage-help"); movementHelp.pickingMode = PickingMode.Ignore; stage.Add(movementHelp);
            inspector = new ScrollView(); inspector.AddToClassList("inspector"); body.Add(inspector);
            var bottom = Box("statusbar"); status = Label("", "status"); contextLabel = Label("", "context"); modeLabel = Label("", "mode-note");
            bottom.Add(status); bottom.Add(contextLabel); bottom.Add(modeLabel); workspace.Add(bottom);
            root.RegisterCallback<GeometryChangedEvent>(_ => UpdateViewport());
            inputGate = new DesktopInputGate(root, () => navigation.Captured, stage);
            UpdatePanels();
        }
        void UpdatePanels()
        {
            sidebar.style.display = explorerOpen && activeExam == null ? DisplayStyle.Flex : DisplayStyle.None;
            inspector.style.display = inspectorOpen ? DisplayStyle.Flex : DisplayStyle.None;
            explorerToggle.EnableInClassList("selected", explorerOpen);
            inspectorToggle.EnableInClassList("selected", inspectorOpen);
            focusToggle.EnableInClassList("selected", focusView);
            focusToggle.text = focusView ? "Exit focus" : "Focus view";
        }
        void ToggleFocusView()
        {
            if (!focusView)
            { explorerBeforeFocus = explorerOpen; inspectorBeforeFocus = inspectorOpen; explorerOpen = inspectorOpen = false; focusView = true; }
            else
            { explorerOpen = explorerBeforeFocus; inspectorOpen = inspectorBeforeFocus; focusView = false; }
            UpdatePanels();
        }
        void RevealInspector() { inspectorOpen = true; focusView = false; UpdatePanels(); }
        void ShowDashboard()
        {
            loading?.Cancel(); navigationRevision++; navigation.CancelMove(); navigation.Ready = false; navigation.ReleasePointer();
            environment?.Dispose(); environment = null; project = null; dirty = false; pins.Clear(); previousViews.Clear();
            ClearEquipmentUI();
            workspace.style.display = DisplayStyle.None; dashboard.style.display = DisplayStyle.Flex; navigation.View.enabled = false;
            root.style.backgroundColor = new Color(.063f, .071f, .09f);
            piko?.SetProject(null);
            piko?.Attach(root);
            dashboard.Clear(); dashboard.Add(Label("Training environments", "hero-title"));
            dashboard.Add(Label("Open a workplace to prepare views and equipment hotspots.", "hero-description"));
            var cards = Box("cards"); dashboard.Add(cards);
            foreach (var item in catalogue.environments)
            {
                var card = Box("environment-card"); card.Add(Label(item.environmentId == "nursing-skills-room" ? "NURSING SKILLS" : item.environmentId == "charite-operating-room" ? "OPERATING ROOM" : "COCKPIT", "eyebrow"));
                card.Add(Label(item.title, "card-title"));
                card.Add(Label(item.editor.hotspots.Count + " points · " + item.editor.zones.Count + " areas", "muted"));
                card.Add(Button("Open environment  ↗", () => _ = Open(TrainingJson.Clone(item), LocalPaths.Resolve(Application.streamingAssetsPath, "WorkplaceStudio/" + item.modelFile), null), "primary"));
                cards.Add(card);
            }
            var actions = Box("actions"); actions.Add(Button("Open local GLB model", ImportModel)); actions.Add(Button("Open training project", OpenProject)); dashboard.Add(actions);
            dashboard.Add(Label("Saved projects", "section-title"));
            var saved = new ScrollView(); dashboard.Add(saved);
            if (Directory.Exists(ProjectsDirectory))
                foreach (var folder in Directory.GetDirectories(ProjectsDirectory).OrderByDescending(Directory.GetLastWriteTimeUtc))
                {
                    var manifest = Path.Combine(folder, "project.json"); if (!File.Exists(manifest)) continue;
                    string name;
                    try { if (new FileInfo(manifest).Length > 2 * 1024 * 1024) throw new InvalidDataException(); name = TrainingJson.Read<TrainingProject>(File.ReadAllText(manifest)).title; } catch { name = Path.GetFileName(folder) + " · record needs review"; }
                    saved.Add(Button(name, () => OpenSaved(folder), "project-row"));
                }
            dashboard.Add(Label("Projects are saved on this computer. Review world scale and training content before use.", "muted"));
        }
        async Task Open(TrainingProject next, string model, string directory, bool fit = false)
        {
            loading?.Cancel(); loading?.Dispose(); loading = new CancellationTokenSource(); var token = loading.Token;
            navigationRevision++; navigation.CancelMove(); navigation.ReleasePointer(); navigation.Ready = false; busy = true;
            dashboard.style.display = DisplayStyle.None; workspace.style.display = DisplayStyle.Flex;
            root.style.backgroundColor = new Color(.063f, .071f, .09f);
            titleLabel.text = next.title; Message("Loading environment…");
            if (environment != null) environment.Root.SetActive(false);
            LoadedScene loaded = null;
            try
            {
                TrainingValidation.Project(next);
                await Task.Yield(); loaded = await LocalSceneLoader.Load(model, next, token);
                token.ThrowIfCancellationRequested(); if (destroyed) throw new OperationCanceledException();
                environment?.Dispose(); environment = loaded; loaded = null;
                project = next; sourceModel = model; projectDirectory = directory;
                ConfigureEnvironmentLighting(next.environmentId);
                activeArea = selectedArea = selectedHotspot = null; previousViews.Clear(); editing = false; dirty = fit;
                if (fit)
                {
                    var bounds = environment.Bounds; var eye = bounds.center + new Vector3(0, bounds.extents.y * .2f, Mathf.Max(2, bounds.extents.magnitude * 1.8f));
                    project.overview = new Pose { position = Coordinates.ToSource(eye), target = Coordinates.ToSource(bounds.center), fovDegrees = 60 };
                }
                navigation.View.enabled = true; navigation.Spectator = next.navigationMode != "walking";
                navigation.ConfigureForEnvironment(next.environmentId);
                navigation.Apply(project.overview); navigation.Ready = true; busy = false;
                BindEquipmentUI();
                root.style.backgroundColor = Color.clear;
                piko?.SetProject(project);
                piko?.Attach(stage);
                SetMode(false); Message("Environment loaded · Original model materials"); UpdateViewport();
            }
            catch (OperationCanceledException) { loaded?.Dispose(); }
            catch (Exception error)
            {
                loaded?.Dispose(); if (destroyed || token.IsCancellationRequested) return;
                busy = false; if (environment != null) { environment.Root.SetActive(true); navigation.Ready = true; root.style.backgroundColor = Color.clear; titleLabel.text = project.title; UpdateContext(); }
                Message("Could not open: " + error.Message); ShowNotice("Could not open environment", error.Message, () => { if (project == null) ShowDashboard(); });
            }
        }
        void SetMode(bool edit)
        {
            if (project == null || busy) return;
            CloseLessonMenu();
            CloseTraining();
            navigationRevision++; navigation.CancelMove(); navigation.ReleasePointer(); editing = edit; placing = false;
            selectedArea = selectedHotspot = null; previewButton.EnableInClassList("selected", !edit); editorButton.EnableInClassList("selected", edit);
            selectedEquipment = null;
            inspectorOpen = edit; focusView = false; UpdatePanels();
            RenderTree(); RenderInspector(); RenderMarkers(); UpdateContext();
        }
        void RenderTree(string query = "")
        {
            tree.Clear(); if (project == null || activeExam != null) return;
            AddEquipmentTree();
            if (editing)
            {
                tree.Add(Button("Save an area", () => AreaForm(null, null), "tree-action"));
                tree.Add(Button("Add information point", StartPlacing, "tree-action"));
            }
            tree.Add(Button("Environment overview", ReturnToOverview, "tree-item"));
            var visible = new HashSet<string>(); string term = query.Trim();
            if (term.Length > 0)
            {
                foreach (var area in project.editor.zones.Where(a => a.title.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                    foreach (var ancestor in AreaHierarchy.Path(project.editor, area.id)) visible.Add(ancestor.id);
                foreach (var spot in project.editor.hotspots.Where(s => s.title.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                    foreach (var ancestor in AreaHierarchy.Path(project.editor, spot.zoneId)) visible.Add(ancestor.id);
            }
            Action<VisualElement, string> add = null;
            add = (parent, parentId) =>
            {
                foreach (var area in AreaHierarchy.Children(project.editor, parentId))
                {
                    if (term.Length > 0 && !visible.Contains(area.id)) continue;
                    var branch = new Foldout { text = "", value = true }; branch.AddToClassList("area-branch");
                    branch.Add(Button(area.title, () => { if (editing) { selectedArea = area.id; selectedHotspot = selectedEquipment = null; RenderInspector(); } else OpenArea(area.id); }, "tree-item"));
                    foreach (var spot in project.editor.hotspots.Where(s => s.zoneId == area.id))
                        if (term.Length == 0 || spot.title.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 || area.title.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                            branch.Add(Button("• " + spot.title, () => ActivateHotspot(spot.id), "point-item"));
                    add(branch, area.id); parent.Add(branch);
                }
            };
            add(tree, null);
            var unassigned = project.editor.hotspots.Where(s => s.zoneId == null || !project.editor.zones.Any(a => a.id == s.zoneId)).ToList();
            if (unassigned.Count > 0) tree.Add(Label("Unassigned points", "section-title"));
            foreach (var spot in unassigned) if (term.Length == 0 || spot.title.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                tree.Add(Button("• " + spot.title, () => ActivateHotspot(spot.id), "point-item"));
            if (project.attribution != null)
                tree.Add(Label("Source: " + project.attribution.author + " · " + project.attribution.license + "\n" + project.attribution.sourceUrl, "attribution"));
        }
        public void ReturnToOverview()
        {
            if (project == null || busy) return;
            GoTo(project.overview, null);
        }
        public void OpenArea(string id)
        {
            if (project == null || busy) return;
            var area = project.editor.zones.Find(a => a.id == id); if (area != null) GoTo(area.camera, id);
        }
        void GoTo(Pose pose, string area, Action after = null)
        {
            if (activeExam != null) return;
            int request = ++navigationRevision; var origin = navigation.Capture();
            navigation.MoveTo(pose, result =>
            {
                if (destroyed || request != navigationRevision || result == DesktopMoveResult.Cancelled) return;
                if (result == DesktopMoveResult.Completed)
                {
                    previousViews.Add(origin); if (previousViews.Count > 16) previousViews.RemoveAt(0);
                    activeArea = area; selectedArea = selectedHotspot = selectedEquipment = null;
                    piko?.SetActiveArea(activeArea);
                    RenderInspector(); RenderMarkers(); UpdateContext();
                    Message("Saved view opened.");
                }
                else Message("Equipment blocks the camera path. Information can be shown in the current view.");
                after?.Invoke();
            });
        }
        public void ActivateHotspot(string id)
        {
            if (project == null || busy || activeExam != null) return;
            var spot = project.editor.hotspots.Find(s => s.id == id); if (spot == null) return;
            navigation.ReleasePointer();
            Action show = () =>
            {
                selectedHotspot = id;
                selectedArea = selectedEquipment = null;
                piko?.NotifyHotspotVisited(id);
                RenderInspector();
            };
            if (editing) { navigationRevision++; navigation.CancelMove(); show(); return; }
            var area = project.editor.zones.Find(a => a.id == spot.zoneId);
            if (area != null && activeArea != area.id) GoTo(area.camera, area.id, show);
            else { navigationRevision++; navigation.CancelMove(); show(); }
        }
        void FocusHotspot(string id)
        {
            if (project == null || busy || activeExam != null) return;
            var spot = project.editor.hotspots.Find(s => s.id == id); if (spot == null) return;
            navigation.ReleasePointer();
            piko?.NotifyHotspotVisited(id);

            var area = project.editor.zones.Find(a => a.id == spot.zoneId);
            Vector3 spotUnity = environment != null
                ? HotspotWorldPosition(spot)
                : Coordinates.ToUnity(spot.position);
            Vector3 eyeUnity = area?.camera != null
                ? Coordinates.ToUnity(area.camera.position)
                : (navigation.View != null ? navigation.View.transform.position : spotUnity + new Vector3(0, 0.5f, 2f));
            if ((spotUnity - eyeUnity).sqrMagnitude < 0.04f)
            {
                spotUnity = area?.camera != null
                    ? Coordinates.ToUnity(area.camera.target)
                    : eyeUnity + Vector3.forward;
            }
            var focusPose = new Pose
            {
                position = Coordinates.ToSource(eyeUnity),
                target = Coordinates.ToSource(spotUnity),
                fovDegrees = area?.camera != null ? area.camera.fovDegrees : 45f
            };

            activeArea = area?.id; selectedArea = selectedEquipment = null; selectedHotspot = id;
            piko?.SetActiveArea(activeArea);
            RenderInspector(); RenderMarkers(); UpdateContext();

            int request = ++navigationRevision; var origin = navigation.Capture();
            navigation.MoveTo(focusPose, result =>
            {
                if (destroyed || request != navigationRevision || result == DesktopMoveResult.Cancelled) return;
                if (result != DesktopMoveResult.Completed) navigation.Apply(focusPose);
                previousViews.Add(origin); if (previousViews.Count > 16) previousViews.RemoveAt(0);
                activeArea = area?.id; selectedArea = selectedEquipment = null; selectedHotspot = id;
                piko?.SetActiveArea(activeArea);
                RenderInspector(); RenderMarkers(); UpdateContext();
                Message("Focused hotspot: " + spot.title);
            }, ignoreObstacles: true);
        }
        void UpdateContext()
        {
            if (project == null) return;
            bool canAddBedLesson = editing && project.equipment.Any(d => d.kind == "bed");
            trainingButton.style.display = canAddBedLesson || project.bedTrainings.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            examButton.style.display = canAddBedLesson || project.bedExams.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            titleLabel.text = project.title + (dirty ? "  •" : "");
            contextLabel.text = string.Join(" / ", AreaHierarchy.Path(project.editor, activeArea).Select(a => a.title));
            modeLabel.text = activeExam != null ? "Exam" : (editing ? "Editor" : "Preview") + " · " + (navigation.Spectator ? "Spectator · WASD · Shift/Ctrl: up/down" : "Walking · WASD") + " · " + project.editor.zones.Count + " areas / " + project.editor.hotspots.Count + " points";
        }
        void RenderInspector()
        {
            inspector.Clear(); if (project == null) return;
            if (RenderSelectedEquipment()) return;
            var spot = project.editor.hotspots.Find(s => s.id == selectedHotspot);
            var area = project.editor.zones.Find(a => a.id == selectedArea);
            if (editing || spot != null) RevealInspector();
            else { inspectorOpen = false; UpdatePanels(); }
            if (spot != null)
            {
                inspector.Add(Label(editing ? "Edit hotspot" : "Training information", "eyebrow"));
                if (editing) { HotspotForm(spot, false); return; }
                inspector.Add(Label(spot.title, "panel-title")); inspector.Add(Label(spot.description, "description"));
                AddHotspotEquipment(spot);
                inspector.Add(Button("Close", () => { selectedHotspot = null; RenderInspector(); }));
            }
            else if (area != null && editing)
            {
                inspector.Add(Label(area.title, "panel-title"));
                inspector.Add(Button("Open saved view", () => OpenArea(area.id)));
                inspector.Add(Button("Edit area", () => AreaForm(area, area.parentZoneId)));
                inspector.Add(Button("Add sub-area", () => AreaForm(null, area.id)));
                inspector.Add(Button("Add information point to this area", () => { activeArea = area.id; StartPlacing(); }));
                inspector.Add(Button("Update saved view", () => { area.camera = navigation.Capture(); Changed(); }));
                inspector.Add(Button("Move earlier", () => { AreaHierarchy.Reorder(project.editor, area.id, -1); Changed(); }));
                inspector.Add(Button("Move later", () => { AreaHierarchy.Reorder(project.editor, area.id, 1); Changed(); }));
                inspector.Add(Button("Remove area…", () => Confirm("Remove area?", "Direct sub-areas and information points move to the parent. All deeper content is kept. Only this saved view is removed.", () => { AreaHierarchy.Remove(project.editor, area.id); if (activeArea == area.id) activeArea = area.parentZoneId; selectedArea = null; Changed(); }), "danger"));
            }
            else
            {
                inspector.Add(Label(editing ? "Create training content" : "Explore the training", "panel-title"));
                inspector.Add(Label(editing ? "Frame the equipment, save a view, then add a hotspot to its surface." : "Choose a saved view or hotspot in Scene Explorer.", "description"));
                if (editing) { inspector.Add(Button("Save a viewpoint", () => AreaForm(null, activeArea))); inspector.Add(Button("Add a hotspot", StartPlacing)); }
                var siblings = AreaHierarchy.Children(project.editor, project.editor.zones.Find(a => a.id == activeArea)?.parentZoneId);
                int index = siblings.FindIndex(a => a.id == activeArea);
                if (!editing && activeArea != null)
                {
                    var current = project.editor.zones.Find(a => a.id == activeArea);
                    inspector.Add(Button("Back to parent", () => { if (current?.parentZoneId == null) ReturnToOverview(); else OpenArea(current.parentZoneId); }));
                    if (index > 0) inspector.Add(Button("Previous area", () => OpenArea(siblings[index - 1].id)));
                    if (index >= 0 && index < siblings.Count - 1) inspector.Add(Button("Next area", () => OpenArea(siblings[index + 1].id)));
                    foreach (var child in AreaHierarchy.Children(project.editor, activeArea)) inspector.Add(Button(child.title, () => OpenArea(child.id)));
                    foreach (var point in project.editor.hotspots.Where(s => s.zoneId == activeArea)) inspector.Add(Button(point.title, () => ActivateHotspot(point.id)));
                }
            }
        }
        DropdownField AreaPicker(string label, string initial, string exclude = null)
        {
            var candidates = project.editor.zones.Where(a => exclude == null || !AreaHierarchy.Path(project.editor, a.id).Any(p => p.id == exclude)).ToList();
            var ids = new List<string> { null }; ids.AddRange(candidates.Select(a => a.id));
            var labels = new List<string> { "Environment / unassigned" }; labels.AddRange(candidates.Select(a => string.Join(" / ", AreaHierarchy.Path(project.editor, a.id).Select(p => p.title))));
            var picker = new DropdownField(label, labels, Math.Max(0, ids.IndexOf(initial))); picker.userData = ids; return picker;
        }
        static string PickedArea(DropdownField picker) => ((List<string>)picker.userData)[picker.index];
        void AreaForm(Area existing, string parent)
        {
            selectedEquipment = null;
            if (!editing || busy) return; navigation.ReleasePointer();
            RevealInspector();
            inspector.Clear(); inspector.Add(Label(existing == null ? "Save an area" : "Edit area", "panel-title"));
            var name = new TextField("Area name") { value = existing?.title ?? "", maxLength = 100 }; inspector.Add(name);
            var picker = AreaPicker("Parent area", parent, existing?.id); inspector.Add(picker);
            inspector.Add(Label(existing == null ? "Save captures your current camera angle." : "Saving area details keeps the existing camera view.", "muted"));
            inspector.Add(Button("Save area details", () =>
            {
                if (string.IsNullOrWhiteSpace(name.value)) { Message("Enter an area name."); return; }
                if (existing == null)
                {
                    if (project.editor.zones.Count >= 32) { Message("You can save up to 32 areas."); return; }
                    var item = new Area { id = Guid.NewGuid().ToString("N"), title = name.value.Trim(), environmentId = project.environmentId, camera = navigation.Capture() };
                    project.editor.zones.Add(item); AreaHierarchy.Move(project.editor, item.id, PickedArea(picker)); selectedArea = item.id;
                }
                else { AreaHierarchy.Move(project.editor, existing.id, PickedArea(picker)); existing.title = name.value.Trim(); }
                Changed();
            }, "primary"));
            inspector.Add(Button("Cancel", RenderInspector));
        }
        void StartPlacing()
        {
            selectedEquipment = null;
            if (!editing || busy || project.editor.hotspots.Count >= 100) return;
            navigationRevision++; navigation.CancelMove(); navigation.ReleasePointer(); placing = true;
            Message("Click a model surface to place a hotspot. Press Esc to cancel.");
        }
        void OnStagePointer(PointerDownEvent evt)
        {
            if (evt.target != stage || !navigation.Ready || busy) return;
            if (evt.button == 1) { placing = false; stage.Focus(); navigation.CapturePointer(); inputGate.SyncCapture(); evt.StopPropagation(); return; }
            if (evt.button != 0 || Mouse.current == null || environment == null) return;
            var ray = navigation.View.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!placing) { if (!editing) InspectEquipment(ray, pressHandsetKey: true); return; }
            if (!Physics.Raycast(ray, out var hit, 200, ~((1 << 30) | (1 << 31)), QueryTriggerInteraction.Ignore)) { Message("No model surface was selected."); return; }
            placing = false; inspector.Clear();
            HotspotForm(new Hotspot { id = Guid.NewGuid().ToString("N"), title = "", zoneId = selectedArea ?? activeArea, targetNode = hit.collider.name,
                targetPosition = Coordinates.ToSource(hit.collider.transform.InverseTransformPoint(hit.point)),
                position = Coordinates.ToSource(environment.Root.transform.InverseTransformPoint(hit.point)) }, true);
        }
        void HotspotForm(Hotspot spot, bool fresh)
        {
            RevealInspector();
            var title = new TextField("Hotspot title") { value = spot.title, maxLength = 100 }; inspector.Add(title);
            var description = new TextField("Training information") { value = spot.description, multiline = true, maxLength = 2000 }; description.AddToClassList("description-input"); inspector.Add(description);
            var picker = AreaPicker("Move to area", spot.zoneId); inspector.Add(picker);
            inspector.Add(Button("Save hotspot", () =>
            {
                if (string.IsNullOrWhiteSpace(title.value)) { Message("Enter a hotspot title."); return; }
                spot.title = title.value.Trim(); spot.description = description.value.Trim(); spot.zoneId = PickedArea(picker);
                if (fresh) project.editor.hotspots.Add(spot);
                selectedHotspot = null; Changed();
            }, "primary"));
            inspector.Add(Button("Cancel", RenderInspector));
            if (!fresh) inspector.Add(Button("Delete hotspot", () => Confirm("Delete this hotspot?", spot.title, () => { project.editor.hotspots.Remove(spot); selectedHotspot = null; Changed(); }), "danger"));
        }
        void Changed()
        {
            try { TrainingValidation.Project(project); dirty = true; RenderTree(); RenderInspector(); RenderMarkers(); UpdateContext(); Message("Changes are ready. Use Save to write the project to disk."); }
            catch (Exception error) { Message(error.Message); }
        }
        void RenderMarkers()
        {
            markerLayer.Clear(); pins.Clear(); if (project == null || activeExam != null) return;
            foreach (var spot in project.editor.hotspots)
            {
                if (!editing && spot.zoneId != activeArea) continue;
                var pin = Button((project.editor.hotspots.IndexOf(spot) + 1).ToString(), () => ActivateHotspot(spot.id), "pin");
                pin.tooltip = spot.title; pin.focusable = true; pin.pickingMode = PickingMode.Position; markerLayer.Add(pin); pins.Add(spot, pin);
            }
        }
        void LateUpdate()
        {
            inputGate?.SyncCapture();
            if (project == null || environment == null || navigation.View == null) return;
            string hint = navigation.Spectator
                ? (navigation.Captured
                    ? "Mouse captured · WASD: move · Shift: up · Ctrl: down · Esc: release cursor"
                    : "Right-click: capture mouse · WASD: move · Shift: up · Ctrl: down · Esc: release cursor")
                : (navigation.Captured
                    ? "Mouse captured · WASD: move · Shift: faster · Esc: release cursor"
                    : "Right-click: capture mouse · WASD: move · Shift: faster · Esc: release cursor");
            if (movementHelp.text != hint) movementHelp.text = hint;
            UpdateEquipmentInput();
            UpdateTrainingGuide();
            foreach (var pair in pins)
            {
                var viewport = navigation.View.WorldToViewportPoint(HotspotWorldPosition(pair.Key));
                bool selectedDevice = selectedEquipment != null && EquipmentForHotspot(pair.Key) == selectedEquipment;
                bool visible = (editing || (pair.Key.id != selectedHotspot && !selectedDevice)) && viewport.z > navigation.View.nearClipPlane && viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1;
                if (visible)
                {
                    var size = stage.contentRect.size;
                    const float diameter = 34f;
                    var rect = new Rect(viewport.x * size.x - diameter / 2, (1 - viewport.y) * size.y - diameter / 2, diameter, diameter);
                    if (pair.Key.targetNode == "NR_Bed_Controls") visible = PlaceHandsetMarker(ref rect, size);
                    pair.Value.style.left = rect.x; pair.Value.style.top = rect.y;
                }
                pair.Value.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (placing && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { placing = false; Message("Hotspot placement cancelled."); }
        }
        void UpdateViewport()
        {
            if (stage == null || navigation?.View == null || root.resolvedStyle.width <= 0 || stage.resolvedStyle.width <= 0) return;
            var bounds = stage.worldBound;
            navigation.View.rect = new Rect(bounds.x / root.resolvedStyle.width, 1 - bounds.yMax / root.resolvedStyle.height,
                bounds.width / root.resolvedStyle.width, bounds.height / root.resolvedStyle.height);
        }
        void SaveProject()
        {
            if (project == null || busy) return; navigation.ReleasePointer();
            try
            {
                var directory = projectDirectory ?? Path.Combine(ProjectsDirectory, Guid.NewGuid().ToString("N"));
                ProjectStore.Save(directory, project, sourceModel); projectDirectory = directory;
                project.modelFile = "assets/" + project.modelSha256.ToLowerInvariant() + ".glb";
                sourceModel = LocalPaths.Resolve(directory, project.modelFile); dirty = false; UpdateContext();
                Message("Saved: " + directory);
            }
            catch (Exception error) { Message("Could not save: " + error.Message); }
        }
        void OpenSaved(string directory)
        {
            try { var saved = ProjectStore.Open(directory); _ = Open(saved, LocalPaths.Resolve(directory, saved.modelFile), directory); }
            catch (Exception error) { ShowNotice("Could not open saved project", error.Message + "\nThe original files are preserved."); }
        }
        void OpenProject()
        {
            navigation.ReleasePointer();
            try { var file = WindowsFileDialog.Open(true); if (file != null) OpenSaved(Path.GetDirectoryName(file)); }
            catch (Exception error) { ShowNotice("Could not open project", error.Message); }
        }
        void ImportModel()
        {
            navigation.ReleasePointer();
            try
            {
                var file = WindowsFileDialog.Open(); if (file == null) return; GlbFiles.Validate(file);
                string id = Guid.NewGuid().ToString("N");
                var imported = new TrainingProject { id = id, title = Path.GetFileNameWithoutExtension(file), environmentId = id,
                    modelFile = "environment.glb", modelSha256 = GlbFiles.Sha256(file), navigationMode = "spectator",
                    editor = new EditorDocument { environmentId = id, modelUrl = "environment.glb" } };
                _ = Open(imported, file, null, true);
            }
            catch (Exception error) { ShowNotice("Could not open model", error.Message); }
        }
        void GuardChanges(Action action)
        {
            if (dirty) Confirm("Unsaved changes", "Continuing discards unsaved changes in this session.", action);
            else action();
        }
        void Confirm(string title, string text, Action accepted) => ShowModal(title, text, "Continue", accepted, true);
        void ShowNotice(string title, string text, Action closed = null) => ShowModal(title, text, "OK", closed, false);
        void ShowModal(string title, string text, string accept, Action accepted, bool cancel)
        {
            navigation.ReleasePointer(); if (modal != null) root.Remove(modal);
            modal = Box("modal-overlay"); var card = Box("modal-card"); card.Add(Label(title, "panel-title")); card.Add(Label(text, "description"));
            var first = Button(accept, () => { root.Remove(modal); modal = null; accepted?.Invoke(); }, "primary"); card.Add(first);
            if (cancel) card.Add(Button("Cancel", () => { root.Remove(modal); modal = null; })); modal.Add(card); root.Add(modal); first.Focus();
        }
        void Message(string message) { if (status != null) status.text = message; }
        bool WantsToQuit()
        {
            if (!dirty || allowQuit) return true;
            Confirm("Exit Workplace Studio?", "You have unsaved changes. Choose Cancel to return and save them.", () => { allowQuit = true; Application.Quit(); }); return false;
        }
        void OnDestroy()
        {
            destroyed = true; navigationRevision++; loading?.Cancel(); loading?.Dispose();
            inputGate?.Dispose();
            ClearEquipmentUI();
            Application.wantsToQuit -= WantsToQuit; environment?.Dispose();
            GraphicsSettings.defaultRenderPipeline = previousPipeline; QualitySettings.renderPipeline = previousQualityPipeline;
            RenderSettings.ambientMode = previousAmbientMode; RenderSettings.ambientLight = previousAmbientLight;
            if (panel != null) Destroy(panel);
        }
    }
}
