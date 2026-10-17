using System.Runtime.InteropServices;
using UnityEngine;

namespace Simulation.WebBridge
{
    public class WebBridgeService : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void NotifyWebUnityReady();

        [DllImport("__Internal")]
        private static extern void SendStepProgressToWeb(string jsonPayload);

        [DllImport("__Internal")]
        private static extern void SendSimulationResultToWeb(string jsonPayload);

        private void Start()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            NotifyWebUnityReady();
            #else
            Debug.Log("[WebBridge Mock -> Web] Unity Simulation Ready!");
            #endif
        }

        public void ReportStepCompleted(string stepId, bool isSuccess, float timeSpent)
        {
            string json = $"{{\"stepId\":\"{stepId}\",\"isSuccess\":{isSuccess.ToString().ToLower()},\"timeSpent\":{timeSpent:F2}}}";
            #if UNITY_WEBGL && !UNITY_EDITOR
            SendStepProgressToWeb(json);
            #else
            Debug.Log($"[WebBridge Mock -> Web] Step Completed: {json}");
            #endif
        }

        public void ReportSimulationFinished(int totalScore, int mistakeCount, float totalDuration)
        {
            string json = $"{{\"totalScore\":{totalScore},\"mistakeCount\":{mistakeCount},\"totalDuration\":{totalDuration:F2}}}";
            #if UNITY_WEBGL && !UNITY_EDITOR
            SendSimulationResultToWeb(json);
            #else
            Debug.Log($"[WebBridge Mock -> Web] Simulation Finished: {json}");
            #endif
        }

        // Web Tarafından Çağrılır: unityInstance.SendMessage("WebBridgeReceiver", "InitializeSimulationFromWeb", jsonConfig);
        public void InitializeSimulationFromWeb(string jsonConfig)
        {
            Debug.Log($"[Web -> Unity] Başlangıç verisi alındı: {jsonConfig}");
        }

        // Web Tarafından Çağrılır: unityInstance.SendMessage("WebBridgeReceiver", "SetWebGLKeyboardCapture", 0 veya 1);
        public void SetWebGLKeyboardCapture(int capture)
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput = (capture == 1);
            #endif
        }
    }
}
