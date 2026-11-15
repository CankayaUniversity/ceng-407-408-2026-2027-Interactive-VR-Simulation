using System;
using System.Collections.Generic;
using UnityEngine;
using WorkplaceStudio.Core;

namespace WorkplaceStudio.Desktop
{
    // Local deterministic display, not a photo or network feed. Values are illustrative.
    public sealed class EquipmentDisplay : MonoBehaviour
    {
        const int W = 512, H = 320;
        Texture2D texture;
        Material material;
        Color32[] pixels = new Color32[W * H];
        EquipmentState state;
        string kind;
        float nextFrame;
        static readonly Color32 Background = new Color32(9, 19, 27, 255), Muted = new Color32(142, 174, 189, 255), White = new Color32(233, 247, 248, 255), Green = new Color32(65, 235, 155, 255), Cyan = new Color32(75, 206, 239, 255), Yellow = new Color32(246, 202, 92, 255);
        static readonly Dictionary<char, string> Font = new Dictionary<char, string>
        {
            ['A']="01110100011000111111100011000110001", ['B']="11110100011000111110100011000111110", ['C']="01111100001000010000100001000001111", ['D']="11110100011000110001100011000111110",
            ['E']="11111100001000011110100001000011111", ['F']="11111100001000011110100001000010000", ['G']="01111100001000010111100011000101111", ['H']="10001100011000111111100011000110001",
            ['I']="11111001000010000100001000010011111", ['J']="00111000100001000010100101001001100", ['K']="10001100101010011000101001001010001", ['L']="10000100001000010000100001000011111",
            ['M']="10001110111010110101100011000110001", ['N']="10001110011010110011100011000110001", ['O']="01110100011000110001100011000101110", ['P']="11110100011000111110100001000010000",
            ['Q']="01110100011000110001101011001001101", ['R']="11110100011000111110101001001010001", ['S']="01111100001000001110000010000111110", ['T']="11111001000010000100001000010000100",
            ['U']="10001100011000110001100011000101110", ['V']="10001100011000110001100010101000100", ['W']="10001100011000110101101011101110001", ['X']="10001100010101000100010101000110001",
            ['Y']="10001100010101000100001000010000100", ['Z']="11111000010001000100010001000011111",
            ['0']="01110100011001110101110011000101110", ['1']="00100011000010000100001000010001110", ['2']="01110100010000100010001000100011111", ['3']="11110000010000101110000010000111110",
            ['4']="00010001100101010010111110001000010", ['5']="11111100001000011110000010000111110", ['6']="01110100001000011110100011000101110", ['7']="11111000010001000100010000100001000",
            ['8']="01110100011000101110100011000101110", ['9']="01110100011000101111000010000101110", ['/']="00001000100001000100010000100010000", ['-']="00000000000000011111000000000000000", ['.']="00000000000000000000000000011000110", [':']="00000001100011000000001100011000000"
        };
        public static EquipmentDisplay Create(GameObject node, string kind)
        {
            var display = node.AddComponent<EquipmentDisplay>(); display.kind = kind;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("The equipment display shader is missing.");
            display.texture = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "Local equipment display", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            display.material = new Material(shader) { name = "Equipment display - " + kind };
            display.material.SetTexture("_BaseMap", display.texture); display.material.SetColor("_BaseColor", Color.white);
            node.GetComponent<Renderer>().sharedMaterial = display.material;
            return display;
        }
        public void SetState(EquipmentState value) { state = value; }
        void Pixel(int x, int y, Color32 color) { if (x >= 0 && x < W && y >= 0 && y < H) pixels[(H - 1 - y) * W + x] = color; }
        void Rect(int x, int y, int w, int h, Color32 color) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Pixel(i, j, color); }
        void Text(string text, int x, int y, int size, Color32 color)
        {
            foreach (char c in text)
            {
                if (Font.TryGetValue(char.ToUpperInvariant(c), out var glyph))
                    for (int j = 0; j < 7; j++) for (int i = 0; i < 5; i++) if (glyph[j * 5 + i] == '1') Rect(x + i * size, y + j * size, size, size, color);
                x += size * 6;
            }
        }
        void Wave(int y, Color32 color, int channel)
        {
            for (int x = 14; x < 345; x++)
            {
                float phase = Mathf.Repeat(x / 110f - Time.unscaledTime * .8f, 1);
                float wave = channel == 0 ? (phase < .08f ? -Mathf.Sin(phase / .08f * Mathf.PI) * 28 : phase < .14f ? Mathf.Sin((phase - .08f) / .06f * Mathf.PI) * 13 : Mathf.Sin(phase * Mathf.PI * 2) * 2)
                    : Mathf.Sin(phase * Mathf.PI * 2) * (channel == 1 ? 16 : 8);
                int py = y + (int)wave; Rect(x, py, 2, 2, color);
            }
        }
        void Update()
        {
            if (state == null || Time.unscaledTime < nextFrame || texture == null) return;
            nextFrame = Time.unscaledTime + .08f;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Background;
            Rect(0, 0, W, 38, new Color32(22, 48, 62, 255)); Text(kind == "monitor" ? "PATIENT MONITOR" : "INFUSION PUMP", 14, 11, 2, White);
            if (!state.Powered) { Text("STANDBY", 75, 125, 6, Muted); Text("DEMO EQUIPMENT", 140, 275, 2, Muted); }
            else if (kind == "monitor")
            {
                Text("ECG", 14, 54, 2, Green); Text("72", 373, 72, 7, Green); Wave(106, Green, 0);
                Text("SPO2", 14, 134, 2, Cyan); Text("98", 373, 155, 7, Cyan); Wave(187, Cyan, 1);
                Text("RESP", 14, 217, 2, Yellow); Text("16", 373, 236, 7, Yellow); Wave(263, Yellow, 2);
                Text("DEMO  -  ILLUSTRATIVE VALUES", 14, 300, 2, Muted);
            }
            else
            {
                Text(state.DoorOpen ? "DOOR OPEN" : state.Running ? "RUNNING" : "READY", 22, 65, 5, state.Running ? Green : Yellow);
                Text("PRACTICE MODE", 22, 140, 3, White); Text("NO FLUID DELIVERY", 22, 182, 3, Muted);
                Rect(22, 242, 468, 14, new Color32(31, 59, 70, 255));
                if (state.Running) Rect(22, 242, (int)(Mathf.Repeat(Time.unscaledTime * .25f, 1) * 468), 14, Green);
                Text("DEMO", 22, 282, 3, Cyan);
            }
            texture.SetPixels32(pixels); texture.Apply(false);
        }
        void OnDestroy() { if (texture != null) Destroy(texture); if (material != null) Destroy(material); }
    }
}
