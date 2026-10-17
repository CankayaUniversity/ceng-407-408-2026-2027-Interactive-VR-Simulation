using System;
using System.Collections.Generic;
using UnityEngine;

namespace Simulation.Interaction
{
    public class CockpitInspectionManager : MonoBehaviour
    {
        public static CockpitInspectionManager Instance { get; private set; }

        [Header("Camera & Rig")]
        public Camera targetCamera;
        public float moveSmoothTime = 0.45f;
        public float rotateSpeed = 8.0f;
        public float fovSmoothTime = 0.35f;

        [Header("Overview Views")]
        public Vector3 defaultOverviewPos = new Vector3(0.0f, 0.48f, -0.05f);
        public Vector3 defaultOverviewLookAt = new Vector3(0.0f, 0.85f, 0.90f);
        public float defaultOverviewFOV = 70f;

        public Vector3 captainSeatPos = new Vector3(0.46f, 0.42f, 0.15f);
        public Vector3 captainSeatLookAt = new Vector3(0.34f, 0.85f, 0.90f);

        [Header("Hotspot Database")]
        public List<CockpitHotspotData> hotspots = new List<CockpitHotspotData>();

        [Header("State")]
        public int currentHotspotIndex = -1; // -1 = Overview
        public bool isInspecting = false;

        // Camera interpolation variables
        private Vector3 targetPos;
        private Quaternion targetRot;
        private float targetFOV;
        private Vector3 posVelocity;
        private float fovVelocity;

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
            ResetToOverview(true);
        }

        private void Update()
        {
            HandleKeyboardInput();
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
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            {
                ResetToOverview();
            }

            // Quick jump with number keys 1-9
            for (int i = 1; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    SelectHotspot(i);
                }
            }
        }

        private void UpdateCameraMovement()
        {
            if (targetCamera == null) return;

            // Position SmoothDamp
            targetCamera.transform.position = Vector3.SmoothDamp(
                targetCamera.transform.position,
                targetPos,
                ref posVelocity,
                moveSmoothTime
            );

            // Rotation Slerp
            targetCamera.transform.rotation = Quaternion.Slerp(
                targetCamera.transform.rotation,
                targetRot,
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

            CockpitHotspotData data = hotspots[index];
            targetPos = data.cameraPosition;
            Vector3 forward = (data.cameraLookAt - data.cameraPosition).normalized;
            if (forward != Vector3.zero)
            {
                targetRot = Quaternion.LookRotation(forward);
            }
            targetFOV = data.fieldOfView;

            Debug.Log($"[CockpitInspection] Focusing on #{data.id}: {data.title}");
            OnHotspotChanged?.Invoke(data);
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

            targetPos = defaultOverviewPos;
            Vector3 forward = (defaultOverviewLookAt - defaultOverviewPos).normalized;
            if (forward != Vector3.zero)
            {
                targetRot = Quaternion.LookRotation(forward);
            }
            targetFOV = defaultOverviewFOV;

            if (immediate && targetCamera != null)
            {
                targetCamera.transform.position = targetPos;
                targetCamera.transform.rotation = targetRot;
                targetCamera.fieldOfView = targetFOV;
            }

            OnOverviewReset?.Invoke();
        }

        public CockpitHotspotData GetCurrentData()
        {
            if (currentHotspotIndex >= 0 && currentHotspotIndex < hotspots.Count)
            {
                return hotspots[currentHotspotIndex];
            }
            return null;
        }

        private void InitializeDefaultHotspots()
        {
            if (hotspots != null && hotspots.Count == 22) return;

            hotspots = new List<CockpitHotspotData>
            {
                new CockpitHotspotData(1, "01_Cyclic_Captain", "Cyclic (Captain)", "Primary Flight Control Stick",
                    "Controls helicopter pitch and roll attitude. Features contoured grip with 4-way trim hat, autopilot disconnect, and PTT trigger.",
                    new Vector3(0.46f, 0.58f, 0.12f), new Vector3(0.46f, 0.36f, 0.36f), 40f),

                new CockpitHotspotData(2, "02_Collective_Captain", "Collective Lever (Captain)", "Main Rotor Thrust & Power",
                    "Controls main rotor blade pitch angle collectively. Includes twist-grip throttle friction collar and multi-switch control head.",
                    new Vector3(0.24f, 0.52f, -0.06f), new Vector3(0.24f, 0.28f, 0.30f), 38f),

                new CockpitHotspotData(3, "03_YawPedals_Captain", "Yaw Pedals (Captain)", "Antitorque / Tail Rotor Control",
                    "Controls heading and yaw axis via tail rotor pitch. Dual adjustable ribbed metal pedals with differential toe braking.",
                    new Vector3(0.46f, 0.45f, 0.55f), new Vector3(0.46f, 0.10f, 0.88f), 35f),

                new CockpitHotspotData(4, "04_MFD_Captain", "MFD Captain (PFD)", "Primary Flight Display",
                    "Helionix electronic flight instrument display presenting artificial horizon, airspeed tape, altitude tape, vertical speed, and compass rose.",
                    new Vector3(0.34f, 0.90f, 0.55f), new Vector3(0.34f, 0.90f, 0.90f), 35f),

                new CockpitHotspotData(5, "05_VMS_Display", "VMS (Vehicle Monitoring System)", "Vehicle monitoring system (engine indicators)",
                    "Displays dual engine torque (TRQ 39.0%), turbine outlet temperature (TOT 535°C), N1/N2 rotor speed, hydraulic pressures, engine & gearbox oil status, fuel quantity, and gross weight (3000 Kg).",
                    new Vector3(0.00f, 0.90f, 0.55f), new Vector3(0.00f, 0.90f, 0.90f), 35f),

                new CockpitHotspotData(6, "06_IEASI", "IEASI (Standby Indicator)", "Integrated Electronic Attitude Situation Indicator",
                    "Independent solid-state backup artificial horizon with digital barometric altimeter (1017 hPa), airspeed tape, and slip indicator.",
                    new Vector3(-0.17f, 0.98f, 0.70f), new Vector3(-0.17f, 0.98f, 0.98f), 30f),

                new CockpitHotspotData(7, "07_MFD_CoPilot_Upper", "MFD Co-Pilot (PFD)", "Co-Pilot Multi-Function Display",
                    "Left primary flight display duplicating captain flight parameters with independent sensor selection, navigation mapping, and synthetic vision.",
                    new Vector3(-0.38f, 0.90f, 0.55f), new Vector3(-0.38f, 0.90f, 0.90f), 35f),

                new CockpitHotspotData(8, "08_MFD_Center_Dual", "Center Dual Navigation Displays", "Tactical Navigation & Radar",
                    "Dual high-contrast displays on slanted console showing moving map, tactical radar sweep, waypoints, and terrain elevation.",
                    new Vector3(0.00f, 0.72f, 0.38f), new Vector3(0.00f, 0.62f, 0.52f), 35f),

                new CockpitHotspotData(9, "09_Audio_Panels", "Audio Control Panels", "Communications & Intercom (ICS)",
                    "Dual audio control units managing VHF/UHF radios, passenger intercom, crew isolation modes, and volume levels.",
                    new Vector3(0.00f, 0.62f, 0.32f), new Vector3(0.00f, 0.54f, 0.44f), 32f),

                new CockpitHotspotData(10, "10_Remote_Compass_Slave", "Remote Compass Slave", "Directional Gyro / Flux Calibrator",
                    "Magnetic compass slaving controller providing manual and automatic synchronization between directional gyros and external flux gate.",
                    new Vector3(-0.07f, 0.54f, 0.30f), new Vector3(-0.07f, 0.47f, 0.35f), 30f),

                new CockpitHotspotData(11, "11_Autopilot_Control_Panel", "Autopilot Control Panel (AFCS)", "4-Axis Automatic Flight Control",
                    "Selects automatic flight control modes: Heading (HDG), Altitude Hold (ALT), Navigation (NAV), Vertical Speed (VS), and Go-Around (GA).",
                    new Vector3(-0.07f, 0.48f, 0.28f), new Vector3(-0.07f, 0.42f, 0.25f), 30f),

                new CockpitHotspotData(12, "12_I_Drive_Control", "I-Drive Rotary Controller", "Multi-Function Input Device",
                    "Large fluted rotary encoder used for rapid scrolling, menu selection, and flight plan waypoint entry across all cockpit screens.",
                    new Vector3(-0.08f, 0.42f, 0.24f), new Vector3(-0.08f, 0.36f, 0.12f), 30f),

                new CockpitHotspotData(13, "13_Weather_Radar_Panel", "Weather Radar Controller", "Digital Radar Gain & Scan Control",
                    "Controls weather radar range rings, gain, tilt angle, ground clutter suppression, and storm cell detection modes.",
                    new Vector3(-0.07f, 0.36f, 0.20f), new Vector3(-0.07f, 0.36f, 0.01f), 30f),

                new CockpitHotspotData(14, "14_ELT_Switch", "ELT Emergency Locator Switch", "Emergency Locator Transmitter (406 MHz)",
                    "Tri-state guarded switch (ARM / ON / TEST) controlling satellite distress beacon with armed g-force impact trigger.",
                    new Vector3(0.07f, 0.60f, 0.35f), new Vector3(0.07f, 0.54f, 0.44f), 28f),

                new CockpitHotspotData(15, "15_Engine_Main_Switches", "Engine Main Switches", "Dual FADEC Engine Control Levers",
                    "Main powerplant control panel with dual yellow lever toggle switches (FLIGHT / IDLE / OFF) protected by red hinged safety guards.",
                    new Vector3(0.09f, 0.78f, 0.50f), new Vector3(0.09f, 0.72f, 0.60f), 30f),

                new CockpitHotspotData(16, "16_FMS_Selectors", "FMS Mode Selectors", "Flight Management System Selectors",
                    "Rotary selector knobs and pushbuttons for navigation mode switching, barometric reference setting, and flight plan stepping.",
                    new Vector3(-0.11f, 0.78f, 0.50f), new Vector3(-0.11f, 0.72f, 0.60f), 30f),

                new CockpitHotspotData(17, "17_Fire_Warning_Light", "Engine Fire Warning Light", "Master Fire Alarm & Extinguisher",
                    "High-intensity illuminated red warning indicator alerting crew of engine bay overheat or fire, with built-in push action to arm discharge.",
                    new Vector3(0.13f, 1.10f, 0.75f), new Vector3(0.13f, 1.05f, 0.60f), 28f),

                new CockpitHotspotData(18, "18_Caution_Warnings_Panel", "Caution & Warning Annunciator", "Master Caution LED Strip",
                    "Central LED warning bar displaying amber caution and green advisory annunciations across electrical, hydraulic, and rotor systems.",
                    new Vector3(0.00f, 1.10f, 0.75f), new Vector3(0.00f, 1.05f, 0.60f), 32f),

                new CockpitHotspotData(19, "19_Magnetic_Compass", "Standby Magnetic Compass", "Direct-Reading Wet Compass",
                    "Liquid-filled magnetic compass mounted centrally on the vertical windshield center post, independent of electrical power.",
                    new Vector3(0.00f, 1.18f, 0.85f), new Vector3(0.00f, 1.18f, 0.48f), 30f),

                new CockpitHotspotData(20, "20_Collective_CoPilot", "Collective Lever (Co-Pilot)", "Co-Pilot Rotor Thrust Control",
                    "Left-hand collective pitch control lever for Co-pilot seat, mechanically synchronized with captain collective.",
                    new Vector3(-0.68f, 0.52f, -0.06f), new Vector3(-0.68f, 0.28f, 0.30f), 38f),

                new CockpitHotspotData(21, "21_YawPedals_CoPilot", "Yaw Pedals (Co-Pilot)", "Co-Pilot Antitorque Pedals",
                    "Dual foot pedals on left cockpit floor governing tail rotor antitorque and directional heading.",
                    new Vector3(-0.46f, 0.45f, 0.55f), new Vector3(-0.46f, 0.10f, 0.88f), 35f),

                new CockpitHotspotData(22, "22_Cyclic_CoPilot", "Cyclic (Co-Pilot)", "Co-Pilot Flight Control Stick",
                    "Dual-control cyclic stick in front of Co-pilot seat with ergonomic flight grip, trim switches, and AP override.",
                    new Vector3(-0.46f, 0.58f, 0.12f), new Vector3(-0.46f, 0.36f, 0.36f), 40f)
            };
        }
    }
}
