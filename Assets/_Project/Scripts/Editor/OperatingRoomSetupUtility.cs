#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Simulation.Interactables;
using Simulation.UI;
using Simulation.Player;

namespace Simulation.Editor
{
    [InitializeOnLoad]
    public static class OperatingRoomSetupUtility
    {
        private const string FbxPath = "Assets/_Project/Art/Models/OperatingRoom/OP_Mitte.fbx";
        private const string TexturesFolder = "Assets/_Project/Art/Models/OperatingRoom";
        private const string MaterialsFolder = "Assets/_Project/Art/Models/OperatingRoom/Materials";
        private const string ScenePath = "Assets/_Project/Scenes/02_TrainingLab.unity";

        // [InitializeOnLoadMethod]
        // private static void AutoRunOnEditorLoad()
        // {
        //     EditorApplication.delayCall += () =>
        //     {
        //         if (GameObject.Find("OperatingRoom_Environment") == null)
        //         {
        //             Debug.Log("[ORSetup] Auto-executing SetupOperatingRoomScene()...");
        //             SetupOperatingRoomScene();
        //         }
        //     };
        // }

        [MenuItem("Simulation/Setup Operating Room Environment")]
        public static void SetupOperatingRoomScene()
        {
            // 1. Open Scene
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Debug.Log("[ORSetup] Beginning Operating Room Setup (Step 1-4)...");

            // 2. Prepare Materials Folder
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art/Models/OperatingRoom", "Materials");
            }

            // 3. Configure Model Importer for OP_Mitte.fbx
            ModelImporter importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer != null)
            {
                importer.addCollider = true;
                importer.SaveAndReimport();
            }

            // 4. Create URP Materials for the 4 textures
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            Material[] orMaterials = new Material[4];
            string[] texNames = new string[] {
                "Volumetric_OR_Room1.jpg",
                "Volumetric_OR_Room2.jpg",
                "Volumetric_OR_Room3.jpg",
                "Volumetric_OR_Room4.jpg"
            };

            for (int i = 0; i < 4; i++)
            {
                string matPath = $"{MaterialsFolder}/Mat_OP_Room{i + 1}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(urpShader);
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                else
                {
                    mat.shader = urpShader;
                }

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesFolder}/{texNames[i]}");
                if (tex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
                }
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
                EditorUtility.SetDirty(mat);
                orMaterials[i] = mat;
            }
            AssetDatabase.SaveAssets();

            // 5. Clean up old scene objects
            string[] objectsToRemove = new string[]
            {
                "Floor",
                "EmergencyButton",
                "Cube",
                "Helicopter_Cockpit_Modular",
                "CockpitInspectionManager",
                "Cockpit_Hotspot_Pins",
                "Cockpit_UI_Canvas",
                "OperatingRoom_Environment",
                "Medical_Devices_Root"
            };

            foreach (var name in objectsToRemove)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                {
                    Debug.Log($"[ORSetup] Removing old object: {name}");
                    Undo.DestroyObjectImmediate(obj);
                }
            }

            // 6. Instantiate OP_Mitte.fbx in scene
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbxPrefab == null)
            {
                Debug.LogError($"[ORSetup] Failed to load FBX at {FbxPath}");
                return;
            }

            GameObject orObj = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
            orObj.name = "OperatingRoom_Environment";
            orObj.transform.position = Vector3.zero;
            orObj.transform.rotation = Quaternion.identity;
            orObj.transform.localScale = Vector3.one;

            // Assign URP materials & MeshColliders to all meshes
            var renderers = orObj.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in renderers)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    var mc = mr.gameObject.GetComponent<MeshCollider>();
                    if (mc == null) mc = mr.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                }

                var mats = mr.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] != null)
                    {
                        mats[m] = ResolveMaterial(mats[m].name, orMaterials);
                    }
                    else
                    {
                        mats[m] = orMaterials[m % 4];
                    }
                }
                mr.sharedMaterials = mats;
            }

            // 7. Setup UI on Canvas
            SetupMedicalDeviceUI();

            // 8. Create 6 Medical Device Hotspots & Orbs
            CreateMedicalDeviceHotspots();

            // 9. Position Player
            PositionPlayer();

            // 10. Save Scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[ORSetup] SUCCESS! Charité Operating Room and 6 Medical Devices fully integrated!");
        }

        private static Material ResolveMaterial(string originalMatName, Material[] materials)
        {
            if (originalMatName.Contains("Tex1")) return materials[0];
            if (originalMatName.Contains("Tex2")) return materials[1];
            if (originalMatName.Contains("Tex3")) return materials[2];
            if (originalMatName.Contains("Tex4")) return materials[3];
            return materials[0];
        }

        private static void SetupMedicalDeviceUI()
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Check or create DeviceInfoPanel
            Transform existingPanel = canvasObj.transform.Find("DeviceInfoPanel");
            GameObject panelObj;
            if (existingPanel != null)
            {
                panelObj = existingPanel.gameObject;
            }
            else
            {
                panelObj = new GameObject("DeviceInfoPanel");
                panelObj.transform.SetParent(canvasObj.transform, false);

                RectTransform rect = panelObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-40f, -40f);
                rect.sizeDelta = new Vector2(480f, 340f);

                Image bg = panelObj.AddComponent<Image>();
                bg.color = new Color(0.04f, 0.07f, 0.12f, 0.92f); // Dark semi-transparent
            }

            // Header Text
            Transform headerTrans = panelObj.transform.Find("HeaderText");
            TextMeshProUGUI headerText;
            if (headerTrans == null)
            {
                GameObject hObj = new GameObject("HeaderText");
                hObj.transform.SetParent(panelObj.transform, false);
                RectTransform hr = hObj.AddComponent<RectTransform>();
                hr.anchorMin = new Vector2(0f, 1f);
                hr.anchorMax = new Vector2(1f, 1f);
                hr.pivot = new Vector2(0.5f, 1f);
                hr.anchoredPosition = new Vector2(0f, -16f);
                hr.sizeDelta = new Vector2(-40f, 36f);

                headerText = hObj.AddComponent<TextMeshProUGUI>();
                headerText.fontSize = 20;
                headerText.fontStyle = FontStyles.Bold;
                headerText.color = Color.white;
                headerText.alignment = TextAlignmentOptions.TopLeft;
            }
            else
            {
                headerText = headerTrans.GetComponent<TextMeshProUGUI>();
            }

            // Mode Badge
            Transform badgeTrans = panelObj.transform.Find("ModeBadgeText");
            TextMeshProUGUI badgeText;
            if (badgeTrans == null)
            {
                GameObject bObj = new GameObject("ModeBadgeText");
                bObj.transform.SetParent(panelObj.transform, false);
                RectTransform br = bObj.AddComponent<RectTransform>();
                br.anchorMin = new Vector2(0f, 1f);
                br.anchorMax = new Vector2(1f, 1f);
                br.pivot = new Vector2(0.5f, 1f);
                br.anchoredPosition = new Vector2(0f, -54f);
                br.sizeDelta = new Vector2(-40f, 26f);

                badgeText = bObj.AddComponent<TextMeshProUGUI>();
                badgeText.fontSize = 12;
                badgeText.fontStyle = FontStyles.Bold;
                badgeText.color = new Color(0.0f, 0.9f, 1.0f, 1.0f); // Cyan
                badgeText.alignment = TextAlignmentOptions.TopLeft;
            }
            else
            {
                badgeText = badgeTrans.GetComponent<TextMeshProUGUI>();
            }

            // Description Text
            Transform descTrans = panelObj.transform.Find("DescriptionText");
            TextMeshProUGUI descText;
            if (descTrans == null)
            {
                GameObject dObj = new GameObject("DescriptionText");
                dObj.transform.SetParent(panelObj.transform, false);
                RectTransform dr = dObj.AddComponent<RectTransform>();
                dr.anchorMin = new Vector2(0f, 0f);
                dr.anchorMax = new Vector2(1f, 1f);
                dr.pivot = new Vector2(0.5f, 0.5f);
                dr.anchoredPosition = new Vector2(0f, -15f);
                dr.sizeDelta = new Vector2(-40f, -160f);

                descText = dObj.AddComponent<TextMeshProUGUI>();
                descText.fontSize = 14;
                descText.lineSpacing = 18;
                descText.color = new Color(0.88f, 0.92f, 0.96f, 1.0f);
                descText.alignment = TextAlignmentOptions.TopLeft;
                descText.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                descText = descTrans.GetComponent<TextMeshProUGUI>();
            }

            // Instruction Footer Text
            Transform footerTrans = panelObj.transform.Find("InstructionFooterText");
            TextMeshProUGUI footerText;
            if (footerTrans == null)
            {
                GameObject fObj = new GameObject("InstructionFooterText");
                fObj.transform.SetParent(panelObj.transform, false);
                RectTransform fr = fObj.AddComponent<RectTransform>();
                fr.anchorMin = new Vector2(0f, 0f);
                fr.anchorMax = new Vector2(1f, 0f);
                fr.pivot = new Vector2(0.5f, 0f);
                fr.anchoredPosition = new Vector2(0f, 14f);
                fr.sizeDelta = new Vector2(-40f, 44f);

                footerText = fObj.AddComponent<TextMeshProUGUI>();
                footerText.fontSize = 12;
                footerText.fontStyle = FontStyles.Italic;
                footerText.color = new Color(0.45f, 0.85f, 1.0f, 1.0f);
                footerText.alignment = TextAlignmentOptions.BottomLeft;
                footerText.textWrappingMode = TextWrappingModes.Normal;
            }
            else
            {
                footerText = footerTrans.GetComponent<TextMeshProUGUI>();
            }

            // Attach MedicalDeviceUIController to Canvas
            MedicalDeviceUIController uiCtrl = canvasObj.GetComponent<MedicalDeviceUIController>();
            if (uiCtrl == null) uiCtrl = canvasObj.AddComponent<MedicalDeviceUIController>();

            // Wire up serialized fields via SerializedObject
            SerializedObject so = new SerializedObject(uiCtrl);
            so.FindProperty("deviceInfoPanel").objectReferenceValue = panelObj;
            so.FindProperty("headerText").objectReferenceValue = headerText;
            so.FindProperty("modeBadgeText").objectReferenceValue = badgeText;
            so.FindProperty("descriptionText").objectReferenceValue = descText;
            so.FindProperty("instructionFooterText").objectReferenceValue = footerText;
            so.ApplyModifiedProperties();

            panelObj.SetActive(false);
            Debug.Log("[ORSetup] Configured MedicalDeviceUIController on Canvas.");
        }

        private static void CreateMedicalDeviceHotspots()
        {
            GameObject root = new GameObject("Medical_Devices_Root");
            root.transform.position = Vector3.zero;

            // Shared materials for hotspots and orbs
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            Material markerMat = new Material(urpShader);
            markerMat.name = "Mat_DeviceHotspot_Marker";
            if (markerMat.HasProperty("_BaseColor")) markerMat.SetColor("_BaseColor", new Color(0.1f, 0.6f, 1.0f, 0.45f));
            if (markerMat.HasProperty("_Smoothness")) markerMat.SetFloat("_Smoothness", 0.5f);

            Material orbMat = new Material(urpShader);
            orbMat.name = "Mat_DeviceHowItWorks_Orb";
            Color cyanColor = new Color(0.0f, 0.85f, 1.0f, 1.0f);
            if (orbMat.HasProperty("_BaseColor")) orbMat.SetColor("_BaseColor", cyanColor);
            if (orbMat.HasProperty("_EmissionColor"))
            {
                orbMat.EnableKeyword("_EMISSION");
                orbMat.SetColor("_EmissionColor", cyanColor * 1.5f);
            }

            // 6 Devices data
            var devices = new[]
            {
                new {
                    number = 1,
                    name = "Operating Table",
                    pos = new Vector3(0.0f, 1.15f, -0.15f),
                    orbOffset = new Vector3(0.45f, 0.10f, 0.0f),
                    whatItDoes = "Positions and stabilizes the patient safely during surgical procedures, allowing surgeons optimal anatomical access to the target operative site.",
                    howItWorks = "Uses electro-hydraulic actuators and motorized joints controlled via a hand remote to adjust height, Trendelenburg tilt, lateral roll, and radiolucent tabletop sections for C-Arm imaging."
                },
                new {
                    number = 2,
                    name = "Anesthesia Machine",
                    pos = new Vector3(-1.10f, 1.40f, 1.70f),
                    orbOffset = new Vector3(0.40f, 0.0f, 0.0f),
                    whatItDoes = "Delivers a precisely controlled mixture of medical gases (oxygen, air, nitrous oxide) and volatile anesthetic vapors to keep the patient unconscious, pain-free, and ventilated.",
                    howItWorks = "Regulates high-pressure gas supplies through flowmeters and precision vaporizers, while a mechanical bellows ventilator cycles breaths through a closed circle breathing circuit with CO2 absorption."
                },
                new {
                    number = 3,
                    name = "Overhead Surgical Lights",
                    pos = new Vector3(0.00f, 2.70f, -0.10f),
                    orbOffset = new Vector3(0.35f, -0.25f, 0.0f),
                    whatItDoes = "Provides high-intensity, shadow-free, color-accurate illumination directly into deep surgical cavities without generating excessive heat on exposed tissues.",
                    howItWorks = "Arranges multiple LED modules in a parabolic ring so light beams converge from different angles, automatically compensating for obstructions caused by the surgeon's head or hands."
                },
                new {
                    number = 4,
                    name = "Patient Vital Signs Monitor",
                    pos = new Vector3(-1.40f, 1.85f, 0.35f),
                    orbOffset = new Vector3(0.35f, 0.0f, 0.0f),
                    whatItDoes = "Continuously tracks and displays the patient's critical physiological parameters in real time, alerting the surgical team to any hemodynamic or respiratory instability.",
                    howItWorks = "Processes electrical and optical signals from body sensors: ECG electrodes measure cardiac electrical activity, pulse oximetry uses infrared light absorption for SpO2, and oscillometric cuffs measure blood pressure."
                },
                new {
                    number = 5,
                    name = "Electrosurgical Unit (ESU)",
                    pos = new Vector3(2.10f, 1.65f, -0.80f),
                    orbOffset = new Vector3(-0.35f, 0.0f, 0.0f),
                    whatItDoes = "Cuts biological tissue and coagulates bleeding blood vessels simultaneously during surgery to minimize blood loss and maintain a clear operative field.",
                    howItWorks = "Generates high-frequency alternating radiofrequency (RF) electrical current (300 kHz to 3 MHz) delivered through a monopolar or bipolar active electrode, rapidly heating intracellular water to vaporize or seal tissue."
                },
                new {
                    number = 6,
                    name = "Mayo Stand & Sterile Instrument Trolley",
                    pos = new Vector3(-2.40f, 0.95f, -0.80f),
                    orbOffset = new Vector3(0.35f, 0.15f, 0.0f),
                    whatItDoes = "Holds and organizes sterilized surgical instruments (scalpels, forceps, retractors, sutures) within immediate reach of the scrub nurse and operating surgeon.",
                    howItWorks = "Maintains a strict aseptic barrier using sterile drapes over a height-adjustable stainless-steel tray, organized systematically by surgical phase (incision, dissection, hemostasis, closure)."
                }
            };

            foreach (var d in devices)
            {
                // Main Hotspot Sphere
                GameObject hotspotObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hotspotObj.name = $"Hotspot_Device_{d.number}_{d.name.Replace(" ", "_")}";
                hotspotObj.transform.SetParent(root.transform, false);
                hotspotObj.transform.position = d.pos;
                hotspotObj.transform.localScale = Vector3.one * 0.35f;

                Renderer hotspotRenderer = hotspotObj.GetComponent<Renderer>();
                hotspotRenderer.sharedMaterial = markerMat;

                // 3D Billboard Label
                GameObject labelObj = new GameObject("Billboard_Label");
                labelObj.transform.SetParent(hotspotObj.transform, false);
                labelObj.transform.localPosition = new Vector3(0f, 0.42f, 0f);

                TextMeshPro labelTmp = labelObj.AddComponent<TextMeshPro>();
                labelTmp.text = $"[{d.number}]";
                labelTmp.fontSize = 5.5f;
                labelTmp.alignment = TextAlignmentOptions.Center;
                labelTmp.color = new Color(0.0f, 0.95f, 1.0f, 1.0f);
                labelTmp.outlineWidth = 0.25f;
                labelTmp.outlineColor = Color.black;

                // Child Orb for Stage 2
                GameObject orbObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orbObj.name = $"Orb_HowItWorks_Device_{d.number}";
                orbObj.transform.SetParent(hotspotObj.transform, false);
                orbObj.transform.localPosition = d.orbOffset;
                orbObj.transform.localScale = Vector3.one * 0.22f;

                Renderer orbRenderer = orbObj.GetComponent<Renderer>();
                orbRenderer.sharedMaterial = orbMat;

                DeviceHowItWorksOrb orbScript = orbObj.AddComponent<DeviceHowItWorksOrb>();
                orbScript.deviceNumber = d.number;
                orbScript.deviceNameEnglish = d.name;
                orbScript.howItWorksEnglish = d.howItWorks;

                // Add MedicalDeviceHotspot component
                MedicalDeviceHotspot hotspotScript = hotspotObj.AddComponent<MedicalDeviceHotspot>();
                hotspotScript.deviceNumber = d.number;
                hotspotScript.deviceNameEnglish = d.name;
                hotspotScript.whatItDoesEnglish = d.whatItDoes;
                hotspotScript.howItWorksEnglish = d.howItWorks;
                hotspotScript.howItWorksOrb = orbScript;

                SerializedObject sHotspot = new SerializedObject(hotspotScript);
                sHotspot.FindProperty("billboardTransform").objectReferenceValue = labelObj.transform;
                sHotspot.FindProperty("labelText").objectReferenceValue = labelTmp;
                sHotspot.FindProperty("markerRenderer").objectReferenceValue = hotspotRenderer;
                sHotspot.FindProperty("howItWorksOrb").objectReferenceValue = orbScript;
                sHotspot.ApplyModifiedProperties();

                // Initially hide Stage 2 orb
                orbObj.SetActive(false);
            }

            Debug.Log("[ORSetup] Created 6 Medical Device Hotspots and How-It-Works Orbs.");
        }

        private static void PositionPlayer()
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                Debug.LogWarning("[ORSetup] Player object not found in scene!");
                return;
            }

            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Standing in front of Operating Table facing towards +Z
            playerObj.transform.position = new Vector3(0.0f, 0.08f, -2.6f);
            playerObj.transform.rotation = Quaternion.identity;

            if (cc != null)
            {
                cc.enabled = true;
                cc.height = 1.8f;
                cc.center = new Vector3(0f, 0.9f, 0f);
            }

            // Ensure DesktopPlayerController is active
            DesktopPlayerController pc = playerObj.GetComponent<DesktopPlayerController>();
            if (pc != null)
            {
                pc.enabled = true;
            }

            // Ensure DesktopRayInteractor is active and wired up
            DesktopRayInteractor interactor = playerObj.GetComponent<DesktopRayInteractor>();
            if (interactor != null)
            {
                interactor.enabled = true;
                SerializedObject so = new SerializedObject(interactor);
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    so.FindProperty("playerCamera").objectReferenceValue = mainCam;
                }
                GameObject promptObj = GameObject.Find("PromptText");
                if (promptObj != null)
                {
                    so.FindProperty("promptText").objectReferenceValue = promptObj.GetComponent<TextMeshProUGUI>();
                }
                so.ApplyModifiedProperties();
            }

            Debug.Log("[ORSetup] Positioned Player in Operating Room facing Operating Table.");
        }
    }
}
#endif
