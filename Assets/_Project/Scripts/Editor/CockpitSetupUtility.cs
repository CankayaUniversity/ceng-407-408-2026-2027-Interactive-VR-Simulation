#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Simulation.Interaction;

namespace Simulation.Editor
{
    [InitializeOnLoad]
    public static class CockpitSetupUtility
    {
        private const string FbxPath = "Assets/_Project/Art/Models/Cockpit/Helicopter_Cockpit_Modular.fbx";
        private const string AltFbxPath = "Assets/Models/Cockpit/Helicopter_Cockpit_Modular.fbx";
        private const string ScenePath = "Assets/_Project/Scenes/02_TrainingLab.unity";
        private const string PinsTextureFolder = "Assets/_Project/Art/Textures/Cockpit/Pins";

        [InitializeOnLoadMethod]
        private static void AutoRunOnEditorLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (GameObject.Find("Helicopter_Cockpit_Modular") == null || GameObject.Find("CockpitInspectionManager") == null)
                {
                    Debug.Log("[CockpitSetup] Auto-executing ReplacePlaceholder()...");
                    ReplacePlaceholder();
                }
            };
        }

        [MenuItem("Simulation/Replace Placeholder With Modular Cockpit")]
        public static void ReplacePlaceholder()
        {
            // 1. Ensure scene is open
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Debug.Log("[CockpitSetup] Beginning comprehensive Airbus H145 cockpit integration...");

            // 2. Remove existing placeholders or prior instances
            GameObject placeholder = GameObject.Find("EmergencyButton");
            if (placeholder == null) placeholder = GameObject.Find("Cube");
            if (placeholder != null)
            {
                Debug.Log($"[CockpitSetup] Removing old placeholder '{placeholder.name}'");
                Undo.DestroyObjectImmediate(placeholder);
            }

            GameObject existingCockpit = GameObject.Find("Helicopter_Cockpit_Modular");
            if (existingCockpit != null)
            {
                Undo.DestroyObjectImmediate(existingCockpit);
            }

            GameObject existingPins = GameObject.Find("Cockpit_Hotspot_Pins");
            if (existingPins != null)
            {
                Undo.DestroyObjectImmediate(existingPins);
            }

            GameObject existingManager = GameObject.Find("CockpitInspectionManager");
            if (existingManager != null)
            {
                Undo.DestroyObjectImmediate(existingManager);
            }

            GameObject existingCanvas = GameObject.Find("Cockpit_UI_Canvas");
            if (existingCanvas != null)
            {
                Undo.DestroyObjectImmediate(existingCanvas);
            }

            // 3. Load FBX asset
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbxPrefab == null)
            {
                fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AltFbxPath);
            }

            if (fbxPrefab == null)
            {
                Debug.LogError($"[CockpitSetup] Could not load FBX at {FbxPath} or {AltFbxPath}");
                return;
            }

            // 4. Instantiate in scene
            Vector3 spawnPosition = new Vector3(0f, 0f, 0.5f);
            GameObject cockpitInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
            cockpitInstance.name = "Helicopter_Cockpit_Modular";
            cockpitInstance.transform.position = spawnPosition;
            cockpitInstance.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(cockpitInstance, "Instantiate Helicopter Cockpit");

            // 5. Find Interactive_Controls and configure BoxColliders
            Transform interactiveControls = cockpitInstance.transform.Find("Interactive_Controls");
            if (interactiveControls == null)
            {
                var allTransforms = cockpitInstance.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t.name == "Interactive_Controls")
                    {
                        interactiveControls = t;
                        break;
                    }
                }
            }

            int collidersAdded = 0;
            if (interactiveControls != null)
            {
                foreach (Transform child in interactiveControls)
                {
                    BoxCollider boxCol = child.GetComponent<BoxCollider>();
                    if (boxCol == null)
                    {
                        boxCol = child.gameObject.AddComponent<BoxCollider>();
                    }

                    MeshRenderer mr = child.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        boxCol.center = child.InverseTransformPoint(mr.bounds.center);
                        boxCol.size = mr.bounds.size;
                    }
                    collidersAdded++;
                }
                Debug.Log($"[CockpitSetup] Configured BoxColliders on {collidersAdded} interactive control components.");
            }

            // 6. Setup Manager
            GameObject managerObj = new GameObject("CockpitInspectionManager");
            CockpitInspectionManager manager = managerObj.AddComponent<CockpitInspectionManager>();
            manager.targetCamera = Camera.main;
            Undo.RegisterCreatedObjectUndo(managerObj, "Create Cockpit Inspection Manager");

            // 7. Create 3D Hotspot Pins
            GameObject pinsRoot = new GameObject("Cockpit_Hotspot_Pins");
            pinsRoot.transform.position = spawnPosition;
            Undo.RegisterCreatedObjectUndo(pinsRoot, "Create Hotspot Pins");

            // Create default unlit pin material
            Material pinBaseMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            pinBaseMat.name = "Mat_Pin_Unlit";
            pinBaseMat.SetFloat("_Surface", 1.0f); // Transparent
            pinBaseMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            pinBaseMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            pinBaseMat.SetInt("_ZWrite", 0);
            pinBaseMat.renderQueue = 3000;

            for (int i = 1; i <= 22; i++)
            {
                string componentName = manager.hotspots[i - 1].objectName;
                Transform compTransform = null;
                if (interactiveControls != null)
                {
                    compTransform = interactiveControls.Find(componentName);
                }

                Vector3 pinPos = spawnPosition;
                if (compTransform != null)
                {
                    MeshRenderer mr = compTransform.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        pinPos = mr.bounds.center;
                    }
                    else
                    {
                        pinPos = compTransform.position;
                    }
                    // Offset slightly towards viewer
                    pinPos += new Vector3(0f, 0.02f, -0.04f);
                }
                else
                {
                    pinPos += manager.hotspots[i - 1].cameraLookAt;
                }

                GameObject pinObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                pinObj.name = $"Pin_{i:02d}_{componentName}";
                pinObj.transform.parent = pinsRoot.transform;
                pinObj.transform.position = pinPos;
                pinObj.transform.localScale = Vector3.one * 0.055f;

                // Load pin texture
                string texPath = $"{PinsTextureFolder}/Pin_{i:02d}.png";
                Texture2D pinTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                Material pinMat = new Material(pinBaseMat);
                if (pinTex != null)
                {
                    pinMat.mainTexture = pinTex;
                }
                pinObj.GetComponent<MeshRenderer>().material = pinMat;

                // Add HotspotPin script
                CockpitHotspotPin pinScript = pinObj.AddComponent<CockpitHotspotPin>();
                pinScript.hotspotId = i;
                pinScript.hotspotName = manager.hotspots[i - 1].title;
                pinScript.visualTransform = pinObj.transform;
            }

            Debug.Log("[CockpitSetup] Created 22 3D Billboard Pin Badges.");

            // 8. Build Screen Space UI Overlay
            BuildUIOverlay(manager);

            // 9. Configure Camera Initial View
            if (Camera.main != null)
            {
                Camera.main.transform.position = manager.defaultOverviewPos + spawnPosition;
                Vector3 lookTarget = manager.defaultOverviewLookAt + spawnPosition;
                Camera.main.transform.rotation = Quaternion.LookRotation(lookTarget - Camera.main.transform.position);
                Camera.main.fieldOfView = manager.defaultOverviewFOV;
                Debug.Log("[CockpitSetup] Positioned Camera.main at Cockpit Overview.");
            }

            // 10. Mark scene dirty & save
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[CockpitSetup] SUCCESS! Complete Airbus H145 cockpit simulation and interactive inspection system ready!");
        }

        private static void BuildUIOverlay(CockpitInspectionManager manager)
        {
            GameObject canvasObj = new GameObject("Cockpit_UI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Cockpit UI Canvas");

            CockpitUIOverlay uiOverlay = canvasObj.AddComponent<CockpitUIOverlay>();

            // Default font
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // --- 1. Bottom Navigation Bar ---
            GameObject bottomPanel = new GameObject("BottomBarPanel");
            bottomPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform bottomRect = bottomPanel.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0.5f, 0f);
            bottomRect.anchorMax = new Vector2(0.5f, 0f);
            bottomRect.pivot = new Vector2(0.5f, 0f);
            bottomRect.anchoredPosition = new Vector2(0f, 25f);
            bottomRect.sizeDelta = new Vector2(580f, 55f);

            Image bottomBg = bottomPanel.AddComponent<Image>();
            bottomBg.color = new Color(0.08f, 0.10f, 0.14f, 0.88f);

            // Left Button "<"
            GameObject prevBtnObj = CreateButton(bottomPanel.transform, "PrevButton", "<", new Vector2(-235f, 0f), new Vector2(45f, 40f), defaultFont);
            uiOverlay.prevButton = prevBtnObj.GetComponent<Button>();

            // Right Button ">"
            GameObject nextBtnObj = CreateButton(bottomPanel.transform, "NextButton", ">", new Vector2(235f, 0f), new Vector2(45f, 40f), defaultFont);
            uiOverlay.nextButton = nextBtnObj.GetComponent<Button>();

            // Title Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(bottomPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0f, 0f);
            titleRect.sizeDelta = new Vector2(380f, 40f);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = defaultFont;
            titleText.fontSize = 20;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.text = "Airbus H145 Cockpit (Overview)";
            uiOverlay.bottomTitleText = titleText;
            uiOverlay.bottomBarPanel = bottomPanel;

            // Overview Button
            GameObject ovBtnObj = CreateButton(bottomPanel.transform, "OverviewButton", "Overview", new Vector2(0f, 60f), new Vector2(130f, 32f), defaultFont);
            uiOverlay.overviewButton = ovBtnObj.GetComponent<Button>();

            // --- 2. Hotspot Detail Card (Tooltip) ---
            GameObject cardPanel = new GameObject("DetailCardPanel");
            cardPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform cardRect = cardPanel.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0f, 0.5f);
            cardRect.anchoredPosition = new Vector2(120f, 60f);
            cardRect.sizeDelta = new Vector2(380f, 220f);

            Image cardBg = cardPanel.AddComponent<Image>();
            cardBg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

            // Card Title
            GameObject cTitleObj = new GameObject("CardTitle");
            cTitleObj.transform.SetParent(cardPanel.transform, false);
            RectTransform cTitleRect = cTitleObj.AddComponent<RectTransform>();
            cTitleRect.anchoredPosition = new Vector2(0f, 75f);
            cTitleRect.sizeDelta = new Vector2(340f, 35f);
            Text cTitleText = cTitleObj.AddComponent<Text>();
            cTitleText.font = defaultFont;
            cTitleText.fontSize = 22;
            cTitleText.fontStyle = FontStyle.Bold;
            cTitleText.alignment = TextAnchor.MiddleLeft;
            cTitleText.color = new Color(0.95f, 0.95f, 0.95f, 1.0f);
            uiOverlay.detailTitleText = cTitleText;

            // Card Subtitle
            GameObject cSubObj = new GameObject("CardSubtitle");
            cSubObj.transform.SetParent(cardPanel.transform, false);
            RectTransform cSubRect = cSubObj.AddComponent<RectTransform>();
            cSubRect.anchoredPosition = new Vector2(0f, 45f);
            cSubRect.sizeDelta = new Vector2(340f, 25f);
            Text cSubText = cSubObj.AddComponent<Text>();
            cSubText.font = defaultFont;
            cSubText.fontSize = 15;
            cSubText.fontStyle = FontStyle.Italic;
            cSubText.alignment = TextAnchor.MiddleLeft;
            cSubText.color = new Color(0.35f, 0.75f, 1.0f, 1.0f);
            uiOverlay.detailSubtitleText = cSubText;

            // Card Description
            GameObject cDescObj = new GameObject("CardDescription");
            cDescObj.transform.SetParent(cardPanel.transform, false);
            RectTransform cDescRect = cDescObj.AddComponent<RectTransform>();
            cDescRect.anchoredPosition = new Vector2(0f, -25f);
            cDescRect.sizeDelta = new Vector2(340f, 100f);
            Text cDescText = cDescObj.AddComponent<Text>();
            cDescText.font = defaultFont;
            cDescText.fontSize = 14;
            cDescText.lineSpacing = 1.25f;
            cDescText.alignment = TextAnchor.UpperLeft;
            cDescText.color = new Color(0.85f, 0.88f, 0.92f, 1.0f);
            uiOverlay.detailDescriptionText = cDescText;

            uiOverlay.detailCardPanel = cardPanel;

            // --- 3. Keybind Hint Text ---
            GameObject hintObj = new GameObject("KeybindHint");
            hintObj.transform.SetParent(canvasObj.transform, false);
            RectTransform hintRect = hintObj.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 5f);
            hintRect.sizeDelta = new Vector2(800f, 20f);
            Text hintText = hintObj.AddComponent<Text>();
            hintText.font = defaultFont;
            hintText.fontSize = 12;
            hintText.alignment = TextAnchor.MiddleCenter;
            hintText.color = new Color(0.7f, 0.75f, 0.8f, 0.75f);
            uiOverlay.keybindHintText = hintText;

            cardPanel.SetActive(false);
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Font font)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.30f, 0.95f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.28f, 0.40f, 0.60f, 1f);
            cb.pressedColor = new Color(0.12f, 0.20f, 0.35f, 1f);
            btn.colors = cb;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = size;
            Text txt = textObj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 16;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = label;

            return btnObj;
        }
    }
}
#endif
