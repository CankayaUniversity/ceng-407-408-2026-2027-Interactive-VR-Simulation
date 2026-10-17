using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Simulation.Interaction
{
    public class CockpitInspectionManager : MonoBehaviour
    {
        public static CockpitInspectionManager Instance { get; private set; }

        [Header("Target Camera")]
        public Camera targetCamera;
        public float moveSmoothTime = 0.35f;
        public float rotateSpeed = 10.0f;
        public float fovSmoothTime = 0.30f;

        [Header("Orbit & Free Look Controls")]
        public float orbitSensitivity = 3.5f;
        public float zoomSensitivity = 8.0f;
        public float minFOV = 15f;
        public float maxFOV = 85f;

        [Header("Overview Views (World Space relative to Cockpit)")]
        public Vector3 overviewEyeOffset = new Vector3(0.0f, 0.94f, -0.65f); // Between pilot seats at eye level
        public Vector3 overviewLookAtOffset = new Vector3(0.0f, 0.92f, 0.65f); // Looking forward at dashboard center
        public Vector3 defaultOverviewPos { get => overviewEyeOffset; set => overviewEyeOffset = value; }
        public Vector3 defaultOverviewLookAt { get => overviewLookAtOffset; set => overviewLookAtOffset = value; }
        public float defaultOverviewFOV = 68f;

        [Header("Hotspot Database")]
        public List<CockpitHotspotData> hotspots = new List<CockpitHotspotData>();

        [Header("Runtime State")]
        public int currentHotspotIndex = -1; // -1 = Overview
        public bool isInspecting = false;

        private Transform cockpitTransform;
        private Transform currentTargetTransform;
        private Vector3 currentLookAtPoint;

        // Camera interpolation targets
        private Vector3 targetCamPos;
        private Quaternion targetCamRot;
        private float targetFOV;
        private Vector3 posVelocity;
        private float fovVelocity;

        // Orbit rotation offsets
        private float orbitYaw = 0f;
        private float orbitPitch = 0f;
        private Vector3 lastMousePos;
        private bool isOrbiting = false;

        public event Action<CockpitHotspotData> OnHotspotChanged;
        public event Action OnOverviewReset;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            InitializeDefaultHotspots();
        }

        private void Start()
        {
            // Find cockpit in scene
            GameObject cockpitObj = GameObject.Find("Helicopter_Cockpit_Modular");
            if (cockpitObj != null)
            {
                cockpitTransform = cockpitObj.transform;
            }

            // Disable conflicting player controllers (FPS mouse look)
            DisableConflictingControllers();

            // Ensure cursor is free and visible
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            ResetToOverview(true);
        }

        private void DisableConflictingControllers()
        {
            // Disable DesktopPlayerController if present so it doesn't fight the inspection camera
            var playerControllers = FindObjectsByType<MonoBehaviour>();
            foreach (var mb in playerControllers)
            {
                if (mb == null) continue;
                string typeName = mb.GetType().Name;
                if (typeName == "DesktopPlayerController" || typeName == "FirstPersonController")
                {
                    mb.enabled = false;
                    Debug.Log($"[CockpitInspection] Suspended conflicting controller: {typeName}");
                }
            }
        }

        private void Update()
        {
            HandleKeyboardInput();
            HandleMouseOrbitAndZoom();
            UpdateCameraMovement();
        }

        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                NextHotspot();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                PreviousHotspot();
            }
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha0))
            {
                ResetToOverview();
            }

            // Number keys 1-9
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    SelectHotspot(i);
                }
            }
        }

        private void HandleMouseOrbitAndZoom()
        {
            // Ensure cursor stays visible
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Mouse wheel zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetFOV = Mathf.Clamp(targetFOV - scroll * zoomSensitivity, minFOV, maxFOV);
            }

            // Right-click drag OR left-click drag on empty background to orbit
            bool isRightDown = Input.GetMouseButton(1);
            bool isLeftDown = Input.GetMouseButton(0);
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            bool shouldOrbit = isRightDown || (isLeftDown && !overUI && isInspecting);

            if (shouldOrbit)
            {
                if (!isOrbiting)
                {
                    isOrbiting = true;
                    lastMousePos = Input.mousePosition;
                }
                else
                {
                    Vector3 delta = Input.mousePosition - lastMousePos;
                    lastMousePos = Input.mousePosition;

                    orbitYaw += delta.x * orbitSensitivity * 0.05f;
                    orbitPitch -= delta.y * orbitSensitivity * 0.05f;
                    orbitPitch = Mathf.Clamp(orbitPitch, -60f, 60f);

                    // Recompute target camera rotation with orbit
                    Quaternion baseRot = Quaternion.LookRotation((currentLookAtPoint - targetCamPos).normalized);
                    targetCamRot = Quaternion.Euler(baseRot.eulerAngles.x + orbitPitch, baseRot.eulerAngles.y + orbitYaw, 0f);
                }
            }
            else
            {
                isOrbiting = false;
            }
        }

        private void UpdateCameraMovement()
        {
            if (targetCamera == null) return;

            // Position SmoothDamp
            targetCamera.transform.position = Vector3.SmoothDamp(
                targetCamera.transform.position,
                targetCamPos,
                ref posVelocity,
                moveSmoothTime
            );

            // Rotation Slerp
            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                targetCamRot,
                Time.deltaTime * rotateSpeed
            );

            // FOV SmoothDamp
            targetCamera.fieldOfView = Mathf.SmoothDamp(
                targetCamera.fieldOfView,
                targetFOV,
                ref fovVelocity,
                fovSmoothTime
            );
        }

        public void SelectHotspot(int id)
        {
            int index = hotspots.FindIndex(h => h.id == id);
            if (index < 0) return;

            currentHotspotIndex = index;
            isInspecting = true;
            orbitYaw = 0f;
            orbitPitch = 0f;

            CockpitHotspotData data = hotspots[index];

            // Resolve real runtime position of component
            Transform compTransform = FindComponentTransform(data.objectName);
            Vector3 targetCenter;

            if (compTransform != null)
            {
                MeshRenderer mr = compTransform.GetComponent<MeshRenderer>();
                targetCenter = mr != null ? mr.bounds.center : compTransform.position;
            }
            else
            {
                Vector3 basePos = cockpitTransform != null ? cockpitTransform.position : Vector3.zero;
                targetCenter = basePos + data.cameraLookAt;
            }

            currentLookAtPoint = targetCenter;
            currentTargetTransform = compTransform;

            // Calculate optimal close-up viewpoint
            targetCamPos = ComputeOptimalViewpoint(data.id, targetCenter);
            targetCamRot = Quaternion.LookRotation((targetCenter - targetCamPos).normalized);
            targetFOV = data.fieldOfView;

            Debug.Log($"[CockpitInspection] Focusing on #{data.id}: {data.title} at {targetCenter}");
            OnHotspotChanged?.Invoke(data);
        }

        private Vector3 ComputeOptimalViewpoint(int id, Vector3 targetCenter)
        {
            // Specialized close-up camera offsets based on component ergonomics:
            switch (id)
            {
                // Vertical Instrument Panels (Look straight along +Z)
                case 4: // Captain PFD
                case 5: // VMS Display
                case 7: // Co-Pilot PFD
                    return new Vector3(targetCenter.x, targetCenter.y, targetCenter.z - 0.36f);

                case 6: // IEASI
                    return new Vector3(targetCenter.x, targetCenter.y, targetCenter.z - 0.28f);

                case 17: // Fire Warning Light
                case 18: // Caution Annunciator
                case 19: // Magnetic Compass
                    return new Vector3(targetCenter.x, targetCenter.y - 0.04f, targetCenter.z - 0.32f);

                // Lower Dashboard & Slanted Panels (Look down at angle)
                case 15: // Engine Main Switches
                case 16: // FMS Selectors
                    return new Vector3(targetCenter.x, targetCenter.y + 0.16f, targetCenter.z - 0.32f);

                // Slanted Center Pedestal Modules
                case 8:  // Dual Nav MFD
                    return new Vector3(targetCenter.x, targetCenter.y + 0.20f, targetCenter.z - 0.32f);

                case 9:  // Audio Panels
                case 10: // Remote Compass
                case 11: // Autopilot
                case 14: // ELT Switch
                    return new Vector3(targetCenter.x, targetCenter.y + 0.18f, targetCenter.z - 0.28f);

                case 12: // I-Drive Rotary
                case 13: // Weather Radar
                    return new Vector3(targetCenter.x, targetCenter.y + 0.20f, targetCenter.z - 0.25f);

                // Flight Controls
                case 1:  // Cyclic Captain
                case 22: // Cyclic Co-Pilot
                    return new Vector3(targetCenter.x, targetCenter.y + 0.28f, targetCenter.z - 0.35f);

                case 2:  // Collective Captain
                case 20: // Collective Co-Pilot
                    return new Vector3(targetCenter.x + (id == 2 ? -0.15f : 0.15f), targetCenter.y + 0.22f, targetCenter.z - 0.25f);

                case 3:  // Yaw Pedals Captain
                case 21: // Yaw Pedals Co-Pilot
                    return new Vector3(targetCenter.x, targetCenter.y + 0.42f, targetCenter.z - 0.40f);

                default:
                    return new Vector3(targetCenter.x, targetCenter.y + 0.15f, targetCenter.z - 0.35f);
            }
        }

        public void NextHotspot()
        {
            if (hotspots.Count == 0) return;
            int nextIndex = (currentHotspotIndex + 1) % hotspots.Count;
            SelectHotspot(hotspots[nextIndex].id);
        }

        public void PreviousHotspot()
        {
            if (hotspots.Count == 0) return;
            int prevIndex = currentHotspotIndex - 1;
            if (prevIndex < 0) prevIndex = hotspots.Count - 1;
            SelectHotspot(hotspots[prevIndex].id);
        }

        public void ResetToOverview(bool immediate = false)
        {
            currentHotspotIndex = -1;
            isInspecting = false;
            orbitYaw = 0f;
            orbitPitch = 0f;

            if (cockpitTransform == null)
            {
                GameObject cockpitObj = GameObject.Find("Helicopter_Cockpit_Modular");
                if (cockpitObj != null) cockpitTransform = cockpitObj.transform;
            }

            Vector3 basePos = cockpitTransform != null ? cockpitTransform.position : Vector3.zero;
            targetCamPos = basePos + overviewEyeOffset;
            currentLookAtPoint = basePos + overviewLookAtOffset;
            targetCamRot = Quaternion.LookRotation((currentLookAtPoint - targetCamPos).normalized);
            targetFOV = defaultOverviewFOV;

            if (immediate && targetCamera != null)
            {
                targetCamera.transform.position = targetCamPos;
                targetCamera.transform.rotation = targetCamRot;
                targetCamera.fieldOfView = targetFOV;
            }

            OnOverviewReset?.Invoke();
        }

        public Transform FindComponentTransform(string compName)
        {
            if (cockpitTransform != null)
            {
                Transform inter = cockpitTransform.Find("Interactive_Controls");
                if (inter != null)
                {
                    Transform target = inter.Find(compName);
                    if (target != null) return target;
                }

                // Deep search
                var all = cockpitTransform.GetComponentsInChildren<Transform>(true);
                foreach (var t in all)
                {
                    if (t.name == compName) return t;
                }
            }

            GameObject directObj = GameObject.Find(compName);
            return directObj != null ? directObj.transform : null;
        }

        public void InitializeDefaultHotspots()
        {
            if (hotspots != null && hotspots.Count == 22) return;

            hotspots = new List<CockpitHotspotData>
            {
                new CockpitHotspotData(1, "01_Cyclic_Captain", "Cyclic (Captain)", "Primary Flight Control Stick",
                    "Controls helicopter pitch and roll attitude. Features contoured grip with 4-way trim hat, autopilot disconnect, and PTT trigger.",
                    Vector3.zero, Vector3.zero, 38f),

                new CockpitHotspotData(2, "02_Collective_Captain", "Collective Lever (Captain)", "Main Rotor Thrust & Power",
                    "Controls main rotor blade pitch angle collectively. Includes twist-grip throttle friction collar and multi-switch control head.",
                    Vector3.zero, Vector3.zero, 36f),

                new CockpitHotspotData(3, "03_YawPedals_Captain", "Yaw Pedals (Captain)", "Antitorque / Tail Rotor Control",
                    "Controls heading and yaw axis via tail rotor pitch. Dual adjustable ribbed metal pedals with differential toe braking.",
                    Vector3.zero, Vector3.zero, 35f),

                new CockpitHotspotData(4, "04_MFD_Captain", "MFD Captain (PFD)", "Primary Flight Display",
                    "Helionix electronic flight instrument display presenting artificial horizon, airspeed tape, altitude tape, vertical speed, and compass rose.",
                    Vector3.zero, Vector3.zero, 32f),

                new CockpitHotspotData(5, "05_VMS_Display", "VMS (Vehicle Monitoring System)", "Vehicle monitoring system (engine indicators)",
                    "Displays dual engine torque (TRQ 39.0%), turbine outlet temperature (TOT 535°C), N1/N2 rotor speed, hydraulic pressures, engine & gearbox oil status, fuel quantity, and gross weight (3000 Kg).",
                    Vector3.zero, Vector3.zero, 32f),

                new CockpitHotspotData(6, "06_IEASI", "IEASI (Standby Indicator)", "Integrated Electronic Attitude Situation Indicator",
                    "Independent solid-state backup artificial horizon with digital barometric altimeter (1017 hPa), airspeed tape, and slip indicator.",
                    Vector3.zero, Vector3.zero, 28f),

                new CockpitHotspotData(7, "07_MFD_CoPilot_Upper", "MFD Co-Pilot (PFD)", "Co-Pilot Multi-Function Display",
                    "Left primary flight display duplicating captain flight parameters with independent sensor selection, navigation mapping, and synthetic vision.",
                    Vector3.zero, Vector3.zero, 32f),

                new CockpitHotspotData(8, "08_MFD_Center_Dual", "Center Dual Navigation Displays", "Tactical Navigation & Radar",
                    "Dual high-contrast displays on slanted console showing moving map, tactical radar sweep, waypoints, and terrain elevation.",
                    Vector3.zero, Vector3.zero, 32f),

                new CockpitHotspotData(9, "09_Audio_Panels", "Audio Control Panels", "Communications & Intercom (ICS)",
                    "Dual audio control units managing VHF/UHF radios, passenger intercom, crew isolation modes, and volume levels.",
                    Vector3.zero, Vector3.zero, 30f),

                new CockpitHotspotData(10, "10_Remote_Compass_Slave", "Remote Compass Slave", "Directional Gyro / Flux Calibrator",
                    "Magnetic compass slaving controller providing manual and automatic synchronization between directional gyros and external flux gate.",
                    Vector3.zero, Vector3.zero, 28f),

                new CockpitHotspotData(11, "11_Autopilot_Control_Panel", "Autopilot Control Panel (AFCS)", "4-Axis Automatic Flight Control",
                    "Selects automatic flight control modes: Heading (HDG), Altitude Hold (ALT), Navigation (NAV), Vertical Speed (VS), and Go-Around (GA).",
                    Vector3.zero, Vector3.zero, 28f),

                new CockpitHotspotData(12, "12_I_Drive_Control", "I-Drive Rotary Controller", "Multi-Function Input Device",
                    "Large fluted rotary encoder used for rapid scrolling, menu selection, and flight plan waypoint entry across all cockpit screens.",
                    Vector3.zero, Vector3.zero, 28f),

                new CockpitHotspotData(13, "13_Weather_Radar_Panel", "Weather Radar Controller", "Digital Radar Gain & Scan Control",
                    "Controls weather radar range rings, gain, tilt angle, ground clutter suppression, and storm cell detection modes.",
                    Vector3.zero, Vector3.zero, 28f),

                new CockpitHotspotData(14, "14_ELT_Switch", "ELT Emergency Locator Switch", "Emergency Locator Transmitter (406 MHz)",
                    "Tri-state guarded switch (ARM / ON / TEST) controlling satellite distress beacon with armed g-force impact trigger.",
                    Vector3.zero, Vector3.zero, 28f),

                new CockpitHotspotData(15, "15_Engine_Main_Switches", "Engine Main Switches", "Dual FADEC Engine Control Levers",
                    "Main powerplant control panel with dual yellow lever toggle switches (FLIGHT / IDLE / OFF) protected by red hinged safety guards.",
                    Vector3.zero, Vector3.zero, 28f),

                new CockpitHotspotData(16, "16_FMS_Selectors", "FMS Mode Selectors", "Flight Management System Selectors",
                    "Rotary selector knobs and pushbuttons for navigation mode switching, barometric reference setting, and flight plan stepping.",
                    Vector3.zero, Vector3.zero, 28f),

                new CockpitHotspotData(17, "17_Fire_Warning_Light", "Engine Fire Warning Light", "Master Fire Alarm & Extinguisher",
                    "High-intensity illuminated red warning indicator alerting crew of engine bay overheat or fire, with built-in push action to arm discharge.",
                    Vector3.zero, Vector3.zero, 26f),

                new CockpitHotspotData(18, "18_Caution_Warnings_Panel", "Caution & Warning Annunciator", "Master Caution LED Strip",
                    "Central LED warning bar displaying amber caution and green advisory annunciations across electrical, hydraulic, and rotor systems.",
                    Vector3.zero, Vector3.zero, 28f),

                new CockpitHotspotData(19, "19_Magnetic_Compass", "Standby Magnetic Compass", "Direct-Reading Wet Compass",
                    "Liquid-filled magnetic compass mounted centrally on the vertical windshield center post, independent of electrical power.",
                    Vector3.zero, Vector3.zero, 28f),

                new CockpitHotspotData(20, "20_Collective_CoPilot", "Collective Lever (Co-Pilot)", "Co-Pilot Rotor Thrust Control",
                    "Left-hand collective pitch control lever for Co-pilot seat, mechanically synchronized with captain collective.",
                    Vector3.zero, Vector3.zero, 36f),

                new CockpitHotspotData(21, "21_YawPedals_CoPilot", "Yaw Pedals (Co-Pilot)", "Co-Pilot Antitorque Pedals",
                    "Dual foot pedals on left cockpit floor governing tail rotor antitorque and directional heading.",
                    Vector3.zero, Vector3.zero, 35f),

                new CockpitHotspotData(22, "22_Cyclic_CoPilot", "Cyclic (Co-Pilot)", "Co-Pilot Flight Control Stick",
                    "Dual-control cyclic stick in front of Co-pilot seat with ergonomic flight grip, trim switches, and AP override.",
                    Vector3.zero, Vector3.zero, 38f)
            };
        }
    }
}
