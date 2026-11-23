using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using WorkplaceStudio.Core;

namespace WorkplaceStudio.Desktop
{
    public sealed partial class DesktopBootstrap
    {
        string selectedEquipment;
        Label equipmentBadge;
        VisualElement crosshair;
        readonly List<string> practiceHistory = new List<string>();
        EquipmentRig boundEquipment;
        Bounds? handsetBounds;
        void ConfigureEnvironmentLighting(string environmentId)
        {
            bool nursing = environmentId == "nursing-skills-room";
            environmentLight.intensity = nursing ? .7f : 1.6f;
            RenderSettings.ambientLight = nursing ? new Color(.45f, .48f, .52f) : new Color(.5f, .53f, .6f);
        }
        void ClearEquipmentUI()
        {
            CloseLessonMenu();
            CloseExam();
            CloseTraining();
            if (boundEquipment != null) boundEquipment.Session.Performed -= OnEquipmentPerformed;
            boundEquipment = null; handsetBounds = null; selectedEquipment = null; practiceHistory.Clear();
            equipmentBadge?.RemoveFromHierarchy(); crosshair?.RemoveFromHierarchy(); equipmentBadge = null; crosshair = null;
        }
        void BindEquipmentUI()
        {
            ClearEquipmentUI(); boundEquipment = environment.Equipment;
            if (boundEquipment == null) return;
            if (environment.Nodes.TryGetValue("NR_Bed_Controls", out var handset))
            {
                // The handset is docked on the fixed footboard. Cache its full silhouette,
                // including the raised arrow faces, after the room transform is applied.
                foreach (var part in handset.GetComponentsInChildren<Renderer>())
                {
                    var bounds = handsetBounds ?? part.bounds;
                    bounds.Encapsulate(part.bounds);
                    handsetBounds = bounds;
                }
            }
            boundEquipment.Session.Performed += OnEquipmentPerformed;
            equipmentBadge = Label("EQUIPMENT PRACTICE  ·  Left-click to use  ·  E to inspect", "equipment-badge"); equipmentBadge.pickingMode = PickingMode.Ignore; stage.Add(equipmentBadge);
            crosshair = Box("equipment-crosshair"); crosshair.pickingMode = PickingMode.Ignore; stage.Add(crosshair);
        }
        void AddEquipmentTree()
        {
            if (environment?.Equipment == null) return;
            tree.Add(Label("Equipment practice", "section-title"));
            foreach (var d in environment.Equipment.Definitions)
                tree.Add(Button(d.title + "  ↗", () => OpenEquipment(d.id, true), "equipment-item"));
            tree.Add(Label("Saved views & hotspots", "section-title"));
        }
        void OpenEquipment(string id, bool frameEquipment = false)
        {
            if (environment?.Equipment?.Session.Read(id) == null || busy) return;
            void Show() { selectedEquipment = id; selectedHotspot = selectedArea = null; RenderInspector(); }
            if (frameEquipment)
            {
                var point = project.editor.hotspots.FirstOrDefault(p => EquipmentForHotspot(p) == id && project.editor.zones.Any(a => a.id == p.zoneId));
                var area = project.editor.zones.Find(a => a.id == point?.zoneId);
                if (area != null) { GoTo(area.camera, area.id, Show); return; }
            }
            navigationRevision++; navigation.CancelMove(); navigation.ReleasePointer();
            Show();
        }
        string EquipmentForHotspot(Hotspot spot) => environment?.Equipment != null && spot.targetNode != null && environment.Nodes.TryGetValue(spot.targetNode, out var node)
            ? environment.Equipment.FindEquipment(node) : null;
        void InspectEquipment(Ray ray, bool pressHandsetKey = false)
        {
            if (environment?.Equipment == null) return;
            if (Physics.Raycast(ray, out var hit, 8, ~((1 << 30) | (1 << 31)), QueryTriggerInteraction.Ignore))
            {
                var id = environment.Equipment.FindEquipment(hit.transform);
                var key = hit.transform.GetComponentInParent<BedHandsetKey>();
                if (pressHandsetKey && key != null && id != null)
                {
                    bool accepted = environment.Equipment.Session.Execute(id, key.Action, out var feedback);
                    if (accepted && selectedEquipment == id) RenderInspector();
                    Message(accepted ? "Backrest " + environment.Equipment.Session.Read(id).BackrestDegrees + "°" : feedback);
                    return;
                }
                if (id != null)
                {
                    // Equipped beds reveal their panel at the physical handset during an exam.
                    if (activeExam != null && id == activeExam.equipmentId)
                    {
                        var bed = hit.transform.GetComponentInParent<EquipmentTarget>().transform;
                        var handset = bed.Find("NR_Bed_Controls");
                        if (handset != null && !hit.transform.IsChildOf(handset)) return;
                    }
                    OpenEquipment(id); return;
                }
            }
            if (activeExam == null) Message("Aim at a monitor, pump, bed or nurse-call button to inspect it.");
        }
        void UpdateEquipmentInput()
        {
            if (crosshair != null) crosshair.style.display = navigation.Captured ? DisplayStyle.Flex : DisplayStyle.None;
            if (environment.Equipment == null || editing || busy || modal != null || !navigation.Captured) return;

            bool clicked = Mouse.current?.leftButton.wasPressedThisFrame == true;
            bool inspect = Keyboard.current?.eKey.wasPressedThisFrame == true;
            if (clicked || inspect)
                InspectEquipment(navigation.View.ViewportPointToRay(new Vector3(.5f, .5f)), pressHandsetKey: clicked);
        }
        Vector3 HotspotWorldPosition(Hotspot spot)
        {
            if (spot.targetPosition != null && spot.targetNode != null && environment.Nodes.TryGetValue(spot.targetNode, out var node))
                return node.TransformPoint(Coordinates.ToUnity(spot.targetPosition));
            return environment.Root.transform.TransformPoint(Coordinates.ToUnity(spot.position));
        }
        bool PlaceHandsetMarker(ref Rect pin, Vector2 stageSize, bool preferSide = false)
        {
            if (!handsetBounds.HasValue) return true;
            var bounds = handsetBounds.Value;
            var min = bounds.min;
            var max = bounds.max;
            Vector2 topLeft = Vector2.positiveInfinity, bottomRight = Vector2.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
                var point = navigation.View.WorldToViewportPoint(corner);
                if (point.z <= navigation.View.nearClipPlane) return false;
                var pixel = new Vector2(point.x * stageSize.x, (1 - point.y) * stageSize.y);
                topLeft = Vector2.Min(topLeft, pixel); bottomRight = Vector2.Max(bottomRight, pixel);
            }
            if (stageSize.x < pin.width || stageSize.y < pin.height) return false;
            pin.x = Mathf.Clamp(pin.x, 0, stageSize.x - pin.width);
            pin.y = Mathf.Clamp(pin.y, 0, stageSize.y - pin.height);
            const float gap = 8f;
            var keepClear = Rect.MinMaxRect(topLeft.x - gap, topLeft.y - gap, bottomRight.x + gap, bottomRight.y + gap);
            if (!pin.Overlaps(keepClear)) return true;
            if (!preferSide && keepClear.yMin >= pin.height) pin.y = keepClear.yMin - pin.height;
            else if (keepClear.xMax + pin.width <= stageSize.x) pin.x = keepClear.xMax;
            else if (keepClear.xMin >= pin.width) pin.x = keepClear.xMin - pin.width;
            else if (keepClear.yMin >= pin.height) pin.y = keepClear.yMin - pin.height;
            else if (keepClear.yMax + pin.height <= stageSize.y) pin.y = keepClear.yMax;
            else return false;
            return true;
        }
        bool RenderSelectedEquipment()
        {
            if (selectedEquipment == null || environment?.Equipment == null) return false;
            var d = environment.Equipment.Definitions.FirstOrDefault(e => e.id == selectedEquipment);
            if (d == null) { selectedEquipment = null; return false; }
            RevealInspector();
            if (activeExam == null) inspector.Add(Label("EQUIPMENT PRACTICE", "eyebrow"));
            inspector.Add(Label(d.title, "panel-title"));
            AddEquipmentControls(d);
            inspector.Add(Button("Close controls", () => { selectedEquipment = null; RenderInspector(); }));
            return true;
        }
        void AddHotspotEquipment(Hotspot spot)
        {
            if (environment.Equipment == null || spot.targetNode == null || !environment.Nodes.TryGetValue(spot.targetNode, out var node)) return;
            var id = environment.Equipment.FindEquipment(node);
            var d = environment.Equipment.Definitions.FirstOrDefault(e => e.id == id);
            if (d != null) AddEquipmentControls(d);
        }
        void AddEquipmentControls(EquipmentDefinition d)
        {
            var s = environment.Equipment.Session.Read(d.id);
            if (activeExam == null) inspector.Add(Label("Generic equipment · introductory practice", "muted"));
            void Control(string label, EquipmentAction action, bool enabled = true)
            {
                var button = Button(label, () =>
                {
                    bool accepted = environment.Equipment.Session.Execute(d.id, action, out var feedback);
                    Message(feedback); if (accepted) RenderInspector();
                });
                button.SetEnabled(enabled); inspector.Add(button);
            }
            if (d.kind == "monitor" || d.kind == "pump")
            {
                inspector.Add(Label(s.Powered ? "POWER  /  ON" : "POWER  /  STANDBY", "equipment-state"));
                Control(s.Powered ? "Switch off" : "Switch on", EquipmentAction.TogglePower);
            }
            if (d.kind == "monitor" && activeExam == null) inspector.Add(Label("The live display shows illustrative waveforms and values. Switch the monitor off and on to explore its basic controls.", "description"));
            if (d.kind == "pump")
            {
                inspector.Add(Label(s.DoorOpen ? "Door open" : s.Running ? "Demo running" : "Demo stopped", "equipment-state"));
                Control(s.Running ? "Pause demo" : "Start demo", EquipmentAction.ToggleRun, s.Powered && !s.DoorOpen);
                Control(s.DoorOpen ? "Close door" : "Open door", EquipmentAction.ToggleDoor, !s.Running);
                inspector.Add(Label("Practice controls only. No medication, dose or fluid delivery is simulated.", "description"));
            }
            if (d.kind == "bed")
            {
                inspector.Add(Label("Backrest  /  " + s.BackrestDegrees + "°", "equipment-state"));
                Control("Raise backrest +15°", EquipmentAction.RaiseBackrest, s.BackrestDegrees < 60);
                Control("Lower backrest −15°", EquipmentAction.LowerBackrest, s.BackrestDegrees > 0);
                Control(s.RailLowered ? "Raise side rail" : "Lower side rail", EquipmentAction.ToggleRail);
            }
            if (d.kind == "call")
            {
                inspector.Add(Label(s.CallActive ? "CALL ACTIVE" : "READY", "equipment-state"));
                Control("Call nurse", EquipmentAction.Call, !s.CallActive); Control("Acknowledge call", EquipmentAction.Acknowledge, s.CallActive);
            }
            if (activeExam != null) return;
            Control("Reset equipment", EquipmentAction.Reset);
            if (practiceHistory.Count > 0)
            {
                inspector.Add(Label("Recent actions", "section-title"));
                foreach (var line in practiceHistory.Take(4)) inspector.Add(Label(line, "practice-history"));
            }
        }
        void OnEquipmentPerformed(EquipmentEvent evt)
        {
            TrackTrainingAction(evt);
            var d = environment.Equipment.Definitions.First(e => e.id == evt.EquipmentId);
            string action = evt.Action == EquipmentAction.TogglePower ? (evt.After.Powered ? "Power on" : "Power off")
                : evt.Action == EquipmentAction.ToggleRun ? (evt.After.Running ? "Demo started" : "Demo paused")
                : evt.Action == EquipmentAction.ToggleDoor ? (evt.After.DoorOpen ? "Door opened" : "Door closed")
                : evt.Action == EquipmentAction.ToggleRail ? (evt.After.RailLowered ? "Rail lowered" : "Rail raised")
                : evt.Action == EquipmentAction.RaiseBackrest || evt.Action == EquipmentAction.LowerBackrest ? "Backrest " + evt.After.BackrestDegrees + "°"
                : evt.Action == EquipmentAction.Call ? "Call placed" : evt.Action == EquipmentAction.Acknowledge ? "Call acknowledged" : "Reset";
            practiceHistory.Insert(0, d.title + " · " + action);
            if (practiceHistory.Count > 12) practiceHistory.RemoveAt(practiceHistory.Count - 1);
        }
    }
}
