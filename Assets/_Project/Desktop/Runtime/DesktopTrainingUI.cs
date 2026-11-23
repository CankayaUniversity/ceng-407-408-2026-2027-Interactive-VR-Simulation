using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WorkplaceStudio.Core;

namespace WorkplaceStudio.Desktop
{
    public sealed partial class DesktopBootstrap
    {
        Button trainingButton, examButton, endTrainingButton;
        BedTraining activeExam;
        VisualElement examCard;
        VisualElement trainingCard;
        Label trainingHint, trainingAngle, trainingMarker;
        BedTraining activeTraining;
        Transform trainingTarget;
        bool foundTrainingBed;
        VisualElement lessonMenu;
        Button lessonMenuButton;

        List<BedTraining> Lessons(bool exam) => exam ? project.bedExams : project.bedTrainings;

        void ShowLessonMenu(bool exam)
        {
            if (project == null || busy || activeExam != null) return;
            var button = exam ? examButton : trainingButton;
            if (editing)
            {
                bool alreadyOpen = button == lessonMenuButton;
                CloseLessonMenu();
                if (!alreadyOpen) ShowAuthoringMenu(button, exam);
                return;
            }
            var menu = new GenericDropdownMenu();
            foreach (var lesson in Lessons(exam))
                menu.AddItem(lesson.title, !exam && lesson == activeTraining, () =>
                {
                    if (exam) BeginBedExam(lesson);
                    else BeginBedTraining(lesson);
                });
            menu.DropDown(button.worldBound, button, DropdownMenuSizeMode.Content);
        }

        void CloseLessonMenu()
        {
            if (lessonMenu == null) return;
            root.UnregisterCallback<PointerDownEvent>(DismissLessonMenu, TrickleDown.TrickleDown);
            lessonMenu.RemoveFromHierarchy();
            lessonMenu = null;
            lessonMenuButton = null;
        }

        void DismissLessonMenu(PointerDownEvent evt)
        {
            var target = evt.target as VisualElement;
            if (target != null && !lessonMenu.Contains(target) && target != lessonMenuButton && !lessonMenuButton.Contains(target))
                CloseLessonMenu();
        }

        void ShowAuthoringMenu(Button opener, bool exam)
        {
            lessonMenuButton = opener;
            lessonMenu = Box("lesson-menu-layer");
            lessonMenu.pickingMode = PickingMode.Ignore;
            root.Add(lessonMenu);
            root.RegisterCallback<PointerDownEvent>(DismissLessonMenu, TrickleDown.TrickleDown);

            var list = new ScrollView(); list.AddToClassList("lesson-menu"); lessonMenu.Add(list);
            var templates = new ScrollView(); templates.AddToClassList("lesson-menu"); lessonMenu.Add(templates);
            templates.style.display = DisplayStyle.None;
            var size = root.contentRect.size;
            float width = Mathf.Min(224f, size.x / 2);
            var anchor = root.WorldToLocal(new Vector2(opener.worldBound.xMin, opener.worldBound.yMax));
            float left = Mathf.Clamp(anchor.x, 0, size.x - width);
            if (left + 2 * width > size.x) left = Mathf.Max(width, left);
            list.style.left = left; list.style.top = anchor.y; list.style.width = width;
            list.style.maxHeight = Mathf.Max(32, size.y - anchor.y);

            Button first = null;
            foreach (var lesson in Lessons(exam))
            {
                var item = Button(lesson.title, () => { CloseLessonMenu(); EditBedLesson(lesson, exam); });
                item.RegisterCallback<PointerEnterEvent>(_ => templates.style.display = DisplayStyle.None);
                item.RegisterCallback<FocusInEvent>(_ => templates.style.display = DisplayStyle.None);
                list.Add(item); first ??= item;
            }
            Button add = null;
            void OpenTemplates()
            {
                float top = Mathf.Clamp(root.WorldToLocal(add.worldBound.min).y, 0, Mathf.Max(0, size.y - 44));
                bool openRight = left + 2 * width <= size.x;
                templates.style.width = width;
                templates.style.left = openRight ? left + width : Mathf.Max(0, left - width);
                templates.style.top = top; templates.style.maxHeight = size.y - top;
                templates.style.display = DisplayStyle.Flex;
            }
            void FocusTemplates() { OpenTemplates(); templates.contentContainer.Q<Button>()?.Focus(); }
            add = Button(exam ? "Add default exam" : "Add default training", FocusTemplates, "add-default");
            add.Add(Label("›", "submenu-arrow"));
            add.RegisterCallback<PointerEnterEvent>(_ => OpenTemplates());
            add.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                if (evt.direction == NavigationMoveEvent.Direction.Right) { FocusTemplates(); evt.StopPropagation(); }
            });
            list.Add(add);
            // The bundled definitions are the templates. The open project is an
            // editable copy, so deleting its last scenario keeps the template available.
            var defaults = catalogue.environments.SelectMany(room => exam ? room.bedExams : room.bedTrainings)
                .GroupBy(lesson => lesson.title, StringComparer.OrdinalIgnoreCase);
            foreach (var group in defaults)
            {
                var template = group.First();
                templates.Add(Button(template.title, () => { CloseLessonMenu(); AddDefaultBedLesson(template, exam); }));
            }
            templates.RegisterCallback<NavigationMoveEvent>(evt =>
            {
                if (evt.direction == NavigationMoveEvent.Direction.Left)
                { templates.style.display = DisplayStyle.None; add.Focus(); evt.StopPropagation(); }
            });
            lessonMenu.RegisterCallback<NavigationCancelEvent>(evt =>
            { CloseLessonMenu(); opener.Focus(); evt.StopPropagation(); });
            (first ?? add).Focus();
        }

        void AddDefaultBedLesson(BedTraining template, bool exam)
        {
            var lessons = Lessons(exam);
            var copy = template.Copy();
            for (int number = 2; lessons.Any(item => string.Equals(item.title, copy.title, StringComparison.OrdinalIgnoreCase)); number++)
            {
                string suffix = $" ({number})";
                copy.title = template.title.Substring(0, Math.Min(template.title.Length, 100 - suffix.Length)) + suffix;
            }
            var beds = project.equipment.Where(e => e.kind == "bed");
            copy.equipmentId = (beds.FirstOrDefault(b => b.id == template.equipmentId) ?? beds.First()).id;
            lessons.Add(copy);
            Changed();
            EditBedLesson(copy, exam);
        }

        void PrepareBedAttempt(BedTraining lesson)
        {
            activeArea = null;
            previousViews.Clear();
            SetMode(false);
            navigation.Apply(project.overview);
            environment.Equipment.Session.StartBedTraining(lesson.equipmentId);
            environment.Equipment.StepVisuals(1);
        }

        void BeginBedExam(BedTraining lesson)
        {
            activeExam = lesson;
            workspace.AddToClassList("exam-running");
            navigation.Spectator = project.navigationMode != "walking";
            PrepareBedAttempt(lesson);
            Message("");
            examCard = Box("exam-card");
            examCard.AddToClassList("training-card");
            examCard.Add(Label(lesson.title, "training-heading"));
            examCard.Add(Label("Patient · “" + lesson.patientRequest + "”", "training-request"));
            examCard.Add(Label($"Set the backrest to {lesson.targetDegrees}°.", "training-angle"));
            examCard.Add(Button("Finish exam", SubmitBedExam, "primary"));
            stage.Add(examCard);
        }

        void SubmitBedExam()
        {
            if (activeExam == null) return;
            var lesson = activeExam;
            int submitted = environment.Equipment.Session.Read(lesson.equipmentId).BackrestDegrees;
            bool passed = submitted == lesson.targetDegrees;
            CloseExam();
            SetMode(false);
            Message("Exam submitted.");
            ShowNotice(passed ? "Exam passed" : "Exam not passed",
                $"{lesson.title}\nTarget: {lesson.targetDegrees}°\nSubmitted backrest angle: {submitted}°");
        }

        void CloseExam()
        {
            if (activeExam == null) return;
            activeExam = null;
            examCard?.RemoveFromHierarchy(); examCard = null;
            workspace.RemoveFromClassList("exam-running");
        }

        void BeginBedTraining(BedTraining lesson)
        {
            PrepareBedAttempt(lesson);
            var bed = environment.Equipment.Definitions.First(d => d.id == lesson.equipmentId);
            var bedRoot = environment.Nodes[bed.nodeName];
            trainingTarget = bedRoot.Find("NR_Bed_Controls");
            activeTraining = lesson;
            trainingCard = Box("training-card");
            trainingCard.Add(Label(lesson.title, "training-heading"));
            trainingCard.Add(Label("Patient · “" + lesson.patientRequest + "”", "training-request"));
            trainingHint = Label("", "training-guidance"); trainingCard.Add(trainingHint);
            trainingAngle = Label("", "training-angle"); trainingCard.Add(trainingAngle);
            endTrainingButton = Button("Leave training", CloseTraining); trainingCard.Add(endTrainingButton);
            stage.Add(trainingCard);
            trainingMarker = Label(trainingTarget != null ? "Bed handset" : "Patient bed", "training-marker");
            trainingMarker.style.visibility = Visibility.Hidden;
            trainingMarker.pickingMode = PickingMode.Ignore; stage.Add(trainingMarker);
            bool hasHandset = bedRoot.GetComponentInChildren<BedHandsetKey>() != null;
            if (!hasHandset) trainingMarker.text = "Patient bed";
            if (trainingTarget == null) trainingTarget = bedRoot;
            trainingHint.text = hasHandset
                ? "Piko · Walk to the foot of the bed and find the wired handset."
                : "Piko · Approach the bed and open its controls.";
            UpdateTrainingAngle(0);
            trainingButton.EnableInClassList("selected", true);
        }

        void UpdateTrainingGuide()
        {
            if (trainingMarker == null || trainingTarget == null) return;
            var view = navigation.View;
            var point = view.WorldToViewportPoint(trainingTarget.position);
            bool visible = point.z > view.nearClipPlane && point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1;
            if (visible)
            {
                var size = stage.contentRect.size;
                var labelSize = trainingMarker.layout.size;
                var anchor = new Vector2(point.x * size.x, (1 - point.y) * size.y);
                var rect = new Rect(anchor - new Vector2(labelSize.x * .5f, labelSize.y + 8f), labelSize);
                bool hasLayout = labelSize.x > 0 && labelSize.y > 0;
                bool fits = hasLayout && (trainingTarget.name != "NR_Bed_Controls" || PlaceHandsetMarker(ref rect, size, preferSide: true));
                if (fits)
                {
                    trainingMarker.style.left = rect.x;
                    trainingMarker.style.top = rect.y;
                }
                trainingMarker.style.visibility = fits ? Visibility.Visible : Visibility.Hidden;
            }
            else trainingMarker.style.visibility = Visibility.Hidden;
            if (!foundTrainingBed && visible && Vector3.Distance(view.transform.position, trainingTarget.position) < 1.8f)
                ShowBedAdjustmentHint();
        }

        void ShowBedAdjustmentHint()
        {
            foundTrainingBed = true;
            trainingHint.text = trainingTarget.GetComponentInChildren<BedHandsetKey>() != null
                ? "Piko · Aim at ▲ and left-click. Each press raises the backrest by 15°. Use ▼ to lower it."
                : "Piko · Use Raise backrest in the bed controls. Each press raises it by 15°.";
        }

        void UpdateTrainingAngle(int angle)
        {
            trainingAngle.text = $"Backrest  {angle}°   /   Target  {activeTraining.targetDegrees}°";
        }

        void TrackTrainingAction(EquipmentEvent evt)
        {
            if (activeTraining == null || evt.EquipmentId != activeTraining.equipmentId) return;
            UpdateTrainingAngle(evt.After.BackrestDegrees);
            if (evt.Action != EquipmentAction.RaiseBackrest && evt.Action != EquipmentAction.LowerBackrest) return;
            if (evt.After.BackrestDegrees != activeTraining.targetDegrees) { ShowBedAdjustmentHint(); return; }
            trainingHint.text = $"Piko · Training complete. You adjusted the backrest to {activeTraining.targetDegrees}°.";
            trainingMarker.RemoveFromHierarchy(); trainingMarker = null;
            activeTraining = null;
            endTrainingButton.text = "Finish training";
            navigation.ReleasePointer();
        }

        void CloseTraining()
        {
            trainingCard?.RemoveFromHierarchy(); trainingCard = null;
            trainingMarker?.RemoveFromHierarchy(); trainingMarker = null;
            trainingHint = trainingAngle = null; endTrainingButton = null;
            activeTraining = null; trainingTarget = null; foundTrainingBed = false;
            trainingButton?.EnableInClassList("selected", false);
        }

        void EditBedLesson(BedTraining lesson, bool exam)
        {
            selectedEquipment = null; placing = false;
            navigation.ReleasePointer(); RevealInspector(); inspector.Clear();
            string kind = exam ? "exam" : "training";
            var lessons = Lessons(exam);
            inspector.Add(Label("Edit " + kind, "panel-title"));
            var title = new TextField(exam ? "Exam name" : "Training name") { value = lesson.title, maxLength = 100 };
            var request = new TextField("Patient request") { value = lesson.patientRequest, maxLength = 300, multiline = true };
            var beds = project.equipment.Where(e => e.kind == "bed").ToList();
            var bed = new DropdownField("Patient bed", beds.Select(e => e.title).ToList(), beds.FindIndex(e => e.id == lesson.equipmentId));
            var angle = new DropdownField("Target backrest angle", new List<string> { "15°", "30°", "45°", "60°" }, lesson.targetDegrees / 15 - 1);
            inspector.Add(title); inspector.Add(request); inspector.Add(bed); inspector.Add(angle);
            inspector.Add(Button("Apply " + kind, () =>
            {
                var name = title.value.Trim();
                if (name.Length == 0 || string.IsNullOrWhiteSpace(request.value)) { Message($"Enter a {kind} name and a patient request."); return; }
                if (lessons.Any(s => s != lesson && string.Equals(s.title, name, StringComparison.OrdinalIgnoreCase)))
                { Message($"Choose a distinct {kind} name."); return; }
                lesson.title = name; lesson.patientRequest = request.value.Trim(); lesson.targetDegrees = 15 * (angle.index + 1);
                lesson.equipmentId = beds[bed.index].id;
                Changed();
            }, "primary"));
            inspector.Add(Button("Cancel", RenderInspector));
            inspector.Add(Button($"Delete {kind}…", () => ShowModal($"Delete {kind}?", lesson.title, "Delete", () =>
            {
                lessons.Remove(lesson);
                Changed();
            }, true), "danger"));
        }
    }
}
