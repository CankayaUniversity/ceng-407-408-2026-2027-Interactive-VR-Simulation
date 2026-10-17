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
        public float minFOV = 20f;
        public float maxFOV = 85f;

        [Header("Overview Views (World Space relative to Cockpit)")]
        // Comfortable, wide viewpoint between pilot seats framing full cockpit
        public Vector3 overviewEyeOffset = new Vector3(0.0f, 0.98f, -0.85f); 
        public Vector3 overviewLookAtOffset = new Vector3(0.0f, 0.90f, 0.65f); 
        public Vector3 defaultOverviewPos { get => overviewEyeOffset; set => overviewEyeOffset = value; }
        public Vector3 defaultOverviewLookAt { get => overviewLookAtOffset; set => overviewLookAtOffset = value; }
        public float defaultOverviewFOV = 70f;

        [Header("Hotspot Database")]
        public List<CockpitHotspotData> hotspots = new List<CockpitHotspotData>();

        [Header("Runtime State")]
        public int currentHotspotIndex = -1; // -1 = Overview
        public bool isInspecting = false;

        private Transform cockpitTransform;
        private Transform currentTargetTransform;
        private Vector3 currentLookAtPoint;
        private Vector3 currentBaseOffset;
        private float distanceMultiplier = 1.0f;

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

            if (targetCamera != null)
            {
                targetCamera.orthographic = false;
                targetCamera.nearClipPlane = 0.02f;
                targetCamera.farClipPlane = 200f;
            }

            InitializeDefaultHotspots(true);
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
            // Ensure cursor stays visible and unlocked during inspection
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Mouse wheel dolly zoom (distance & FOV scaling)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                distanceMultiplier = Mathf.Clamp(distanceMultiplier - scroll * 1.5f, 0.45f, 2.2f);
                targetFOV = Mathf.Clamp(targetFOV - scroll * 10f, minFOV, maxFOV);
                RecalculateTargetView();
            }

            // Right-click drag OR left-click drag on empty background to orbit around component
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

                    orbitYaw += delta.x * orbitSensitivity * 0.08f;
                    orbitPitch -= delta.y * orbitSensitivity * 0.08f;
                    orbitPitch = Mathf.Clamp(orbitPitch, -55f, 70f);

                    RecalculateTargetView();
                }
            }
            else
            {
                isOrbiting = false;
            }
        }

        private void RecalculateTargetView()
        {
            if (isInspecting)
            {
                Vector3 baseOffsetWorld = TransformCockpitOffset(currentBaseOffset) * distanceMultiplier;
                Quaternion orbitRot = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
                Vector3 finalOffset = orbitRot * baseOffsetWorld;
                targetCamPos = currentLookAtPoint + finalOffset;
                targetCamRot = Quaternion.LookRotation((currentLookAtPoint - targetCamPos).normalized);
            }
            else
            {
                Vector3 basePos = cockpitTransform != null ? cockpitTransform.position : Vector3.zero;
                Vector3 baseOffset = overviewEyeOffset * distanceMultiplier;
                Quaternion orbitRot = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
                targetCamPos = basePos + orbitRot * baseOffset;
                currentLookAtPoint = basePos + overviewLookAtOffset;
                targetCamRot = Quaternion.LookRotation((currentLookAtPoint - targetCamPos).normalized);
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
            distanceMultiplier = 1.0f;

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

            // Calculate optimal natural offset from component
            currentBaseOffset = ComputeOptimalOffset(data.id);
            targetFOV = data.fieldOfView;

            RecalculateTargetView();

            Debug.Log($"[CockpitInspection] Focusing on #{data.id}: {data.title} at {targetCenter}");
            OnHotspotChanged?.Invoke(data);
        }

        private Vector3 ComputeOptimalOffset(int id)
        {
            // Natural, comfortable viewing distances (0.70m - 0.85m) from component
            float distBack = 0.78f;
            float distUp = 0.25f;
            float distX = 0f;

            switch (id)
            {
                // Flight Controls
                case 1:  // Cyclic Captain
                case 22: // Cyclic Co-Pilot
                    distBack = 0.70f;
                    distUp = 0.50f;
                    break;

                case 2:  // Collective Captain
                    distBack = 0.58f;
                    distUp = 0.45f;
                    distX = 0.22f; // Looking slightly from pilot seat
                    break;

                case 20: // Collective Co-Pilot
                    distBack = 0.58f;
                    distUp = 0.45f;
                    distX = -0.22f;
                    break;

                case 3:  // Yaw Pedals Captain
                case 21: // Yaw Pedals Co-Pilot
                    distBack = 0.82f;
                    distUp = 0.70f;
                    break;

                // Main Upper Flight Displays
                case 4: // Captain PFD
                case 5: // VMS Display
                case 6: // IEASI
                case 7: // Co-Pilot PFD
                    distBack = 0.80f;
                    distUp = 0.08f;
                    break;

                // Glare Shield & Compass
                case 17: // Fire Warning Light
                case 18: // Caution Annunciator
                case 19: // Magnetic Compass
                    distBack = 0.75f;
                    distUp = -0.08f;
                    break;

                // Lower Dashboard Switches & Selectors
                case 15: // Engine Main Switches
                case 16: // FMS Selectors
                    distBack = 0.76f;
                    distUp = 0.36f;
                    break;

                // Slanted Center Pedestal Modules (Ergonomic pilot eye level looking down)
                case 8:  // Dual Nav MFD
                case 9:  // Audio Panels
                case 10: // Remote Compass Slave
                case 11: // Autopilot Control Panel
                case 12: // I-Drive Rotary
                case 13: // Weather Radar Panel
                case 14: // ELT Switch
                    distBack = 0.80f;
                    distUp = 0.48f; // Eye level looking down at slanted console
                    break;

                default:
                    distBack = 0.78f;
                    distUp = 0.30f;
                    break;
            }

            return new Vector3(distX, distUp, -distBack);
        }

        private Vector3 TransformCockpitOffset(Vector3 localOffset)
        {
            if (cockpitTransform != null)
            {
                return cockpitTransform.TransformDirection(localOffset);
            }
            return localOffset;
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
            distanceMultiplier = 1.0f;

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
                targetCamera.orthographic = false;
                targetCamera.nearClipPlane = 0.02f;
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

        public void InitializeDefaultHotspots(bool force = false)
        {
            if (!force && hotspots != null && hotspots.Count == 22) return;

            hotspots = new List<CockpitHotspotData>
            {
                new CockpitHotspotData(1, "01_Cyclic_Captain", "Cyclic (Captain)", "Primary Flight Control Stick",
                    "Controls helicopter pitch and roll attitude. Features contoured grip with 4-way trim hat, autopilot disconnect, and PTT trigger.",
                    Vector3.zero, Vector3.zero, 56f),

                new CockpitHotspotData(2, "02_Collective_Captain", "Collective Lever (Captain)", "Main Rotor Thrust & Power",
                    "Controls main rotor blade pitch angle collectively. Includes twist-grip throttle friction collar and multi-switch control head.",
                    Vector3.zero, Vector3.zero, 56f),

                new CockpitHotspotData(3, "03_YawPedals_Captain", "Yaw Pedals (Captain)", "Antitorque / Tail Rotor Control",
                    "Controls heading and yaw axis via tail rotor pitch. Dual adjustable ribbed metal pedals with differential toe braking.",
                    Vector3.zero, Vector3.zero, 60f),

                new CockpitHotspotData(4, "04_MFD_Captain", "MFD Captain (PFD)", "Primary Flight Display",
                    "Helionix electronic flight instrument display presenting artificial horizon, airspeed tape, altitude tape, vertical speed, and compass rose.",
                    Vector3.zero, Vector3.zero, 54f),

                new CockpitHotspotData(5, "05_VMS_Display", "VMS (Vehicle Monitoring System)", "Vehicle monitoring system (engine indicators)",
                    "Displays dual engine torque (TRQ 39.0%), turbine outlet temperature (TOT 535°C), N1/N2 rotor speed, hydraulic pressures, engine & gearbox oil status, fuel quantity, and gross weight (3000 Kg).",
                    Vector3.zero, Vector3.zero, 54f),

                new CockpitHotspotData(6, "06_IEASI", "IEASI (Standby Indicator)", "Integrated Electronic Attitude Situation Indicator",
                    "Independent solid-state backup artificial horizon with digital barometric altimeter (1017 hPa), airspeed tape, and slip indicator.",
                    Vector3.zero, Vector3.zero, 52f),

                new CockpitHotspotData(7, "07_MFD_CoPilot_Upper", "MFD Co-Pilot (PFD)", "Co-Pilot Multi-Function Display",
                    "Left primary flight display duplicating captain flight parameters with independent sensor selection, navigation mapping, and synthetic vision.",
                    Vector3.zero, Vector3.zero, 54f),

                new CockpitHotspotData(8, "08_MFD_Center_Dual", "Center Dual Navigation Displays", "Tactical Navigation & Radar",
                    "Dual high-contrast displays on slanted console showing moving map, tactical radar sweep, waypoints, and terrain elevation.",
                    Vector3.zero, Vector3.zero, 55f),

                new CockpitHotspotData(9, "09_Audio_Panels", "Audio Control Panels", "Communications & Intercom (ICS)",
                    "Dual audio control units managing VHF/UHF radios, passenger intercom, crew isolation modes, and volume levels.",
                    Vector3.zero, Vector3.zero, 55f),

                new CockpitHotspotData(10, "10_Remote_Compass_Slave", "Remote Compass Slave", "Directional Gyro / Flux Calibrator",
                    "Magnetic compass slaving controller providing manual and automatic synchronization between directional gyros and external flux gate.",
                    Vector3.zero, Vector3.zero, 55f),

                new CockpitHotspotData(11, "11_Autopilot_Control_Panel", "Autopilot Control Panel (AFCS)", "4-Axis Automatic Flight Control",
                    "Selects automatic flight control modes: Heading (HDG), Altitude Hold (ALT), Navigation (NAV), Vertical Speed (VS), and Go-Around (GA).",
                    Vector3.zero, Vector3.zero, 55f),

                new CockpitHotspotData(12, "12_I_Drive_Control", "I-Drive Rotary Controller", "Multi-Function Input Device",
                    "Large fluted rotary encoder used for rapid scrolling, menu selection, and flight plan waypoint entry across all cockpit screens.",
                    Vector3.zero, Vector3.zero, 55f),

                new CockpitHotspotData(13, "13_Weather_Radar_Panel", "Weather Radar Controller", "Digital Radar Gain & Scan Control",
                    "Controls weather radar range rings, gain, tilt angle, ground clutter suppression, and storm cell detection modes.",
                    Vector3.zero, Vector3.zero, 55f),

                new CockpitHotspotData(14, "14_ELT_Switch", "ELT Emergency Locator Switch", "Emergency Locator Transmitter (406 MHz)",
                    "Tri-state guarded switch (ARM / ON / TEST) controlling satellite distress beacon with armed g-force impact trigger.",
                    Vector3.zero, Vector3.zero, 55f),

                new CockpitHotspotData(15, "15_Engine_Main_Switches", "Engine Main Switches", "Dual FADEC Engine Control Levers",
                    "Main powerplant control panel with dual yellow lever toggle switches (FLIGHT / IDLE / OFF) protected by red hinged safety guards.",
                    Vector3.zero, Vector3.zero, 54f),

                new CockpitHotspotData(16, "16_FMS_Selectors", "FMS Mode Selectors", "Flight Management System Selectors",
                    "Rotary selector knobs and pushbuttons for navigation mode switching, barometric reference setting, and flight plan stepping.",
                    Vector3.zero, Vector3.zero, 54f),

                new CockpitHotspotData(17, "17_Fire_Warning_Light", "Engine Fire Warning Light", "Master Fire Alarm & Extinguisher",
                    "High-intensity illuminated red warning indicator alerting crew of engine bay overheat or fire, with built-in push action to arm discharge.",
                    Vector3.zero, Vector3.zero, 52f),

                new CockpitHotspotData(18, "18_Caution_Warnings_Panel", "Caution & Warning Annunciator", "Master Caution LED Strip",
                    "Central LED warning bar displaying amber caution and green advisory annunciations across electrical, hydraulic, and rotor systems.",
                    Vector3.zero, Vector3.zero, 52f),

                new CockpitHotspotData(19, "19_Magnetic_Compass", "Standby Magnetic Compass", "Direct-Reading Wet Compass",
                    "Liquid-filled magnetic compass mounted centrally on the vertical windshield center post, independent of electrical power.",
                    Vector3.zero, Vector3.zero, 52f),

                new CockpitHotspotData(20, "20_Collective_CoPilot", "Collective Lever (Co-Pilot)", "Co-Pilot Rotor Thrust Control",
                    "Left-hand collective pitch control lever for Co-pilot seat, mechanically synchronized with captain collective.",
                    Vector3.zero, Vector3.zero, 56f),

                new CockpitHotspotData(21, "21_YawPedals_CoPilot", "Yaw Pedals (Co-Pilot)", "Co-Pilot Antitorque Pedals",
                    "Dual foot pedals on left cockpit floor governing tail rotor antitorque and directional heading.",
                    Vector3.zero, Vector3.zero, 60f),

                new CockpitHotspotData(22, "22_Cyclic_CoPilot", "Cyclic (Co-Pilot)", "Co-Pilot Flight Control Stick",
                    "Dual-control cyclic stick in front of Co-pilot seat with ergonomic flight grip, trim switches, and AP override.",
                    Vector3.zero, Vector3.zero, 56f)
            };
        }
    }
}
