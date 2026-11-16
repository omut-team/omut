using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Omut.EditorTools
{
    // Снимок всего озера в PNG (для документации и проверки карты).
    // Меню Unity: Omut > Снимок карты в PNG. Файл: game/Logs/omut_map.png (+ крупный план причала).
    public static class OmutSnapshot
    {
        [MenuItem("Omut/Снимок карты в PNG")]
        public static void RenderMap()
        {
            EditorSceneManager.OpenScene(OmutSceneBuilder.MainScenePath, OpenSceneMode.Single);
            Directory.CreateDirectory("Logs");
            Render(new Vector2(0f, 0f), 36f, 1800, 1200, "Logs/omut_map.png");
            Render(new Vector2(-40f, 22f), 9f, 1600, 1000, "Logs/omut_dock.png");
            Debug.Log("[OmutSnapshot] Снимки в " + Path.GetFullPath("Logs"));
        }

        private static void Render(Vector2 center, float size, int width, int height, string file)
        {
            var go = new GameObject("SnapshotCamera");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.transform.position = new Vector3(center.x, center.y, -10f);
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;

            var rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(file, tex.EncodeToPNG());

            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }
    }
}
