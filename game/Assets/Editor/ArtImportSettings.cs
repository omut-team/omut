using UnityEditor;
using UnityEngine;

namespace Omut.EditorTools
{
    // Настройки импорта картинок из Assets/Art ставятся сами (docs/ART.md):
    // Sprite, 32 Pixels Per Unit, Point, без сжатия. Работает только для новых файлов,
    // поэтому если что-то поменять руками в инспекторе, это не перезапишется.
    public class ArtImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/") || !assetImporter.importSettingsMissing)
                return;

            var importer = (TextureImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = file.StartsWith("fx_") ? FilterMode.Bilinear : FilterMode.Point;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (file.StartsWith("tile_"))
            {
                // Тайлы растягиваются плиткой (Draw Mode: Tiled), для этого нужен Full Rect и Repeat.
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.wrapMode = TextureWrapMode.Repeat;
            }
            else
            {
                settings.wrapMode = TextureWrapMode.Clamp;
            }

            // Вид 45° (docs/ART.md): у стоящих объектов точка опоры внизу, по ней идёт сортировка по Y.
            if (StandsOnGround(file))
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spritePivot = new Vector2(0.5f, 0f);
            }

            if (Borders.TryGetValue(file, out Vector4 border))
                settings.spriteBorder = border;

            importer.SetTextureSettings(settings);
        }

        // Края для растяжения (9-slice) картинок интерфейса: слева, снизу, справа, сверху (в пикселях).
        public static readonly System.Collections.Generic.Dictionary<string, Vector4> Borders =
            new System.Collections.Generic.Dictionary<string, Vector4>
            {
                { "ui_panel", new Vector4(28, 28, 28, 28) },
                { "ui_button_plank", new Vector4(110, 22, 110, 22) },
                { "ui_button_wood", new Vector4(14, 12, 14, 12) },
                { "ui_slot", new Vector4(10, 10, 10, 10) },
                { "ui_slot_selected", new Vector4(8, 8, 8, 8) },
                { "ui_hud", new Vector4(12, 12, 12, 12) },
                { "ui_bar", new Vector4(8, 8, 8, 8) },
                { "ui_tag", new Vector4(12, 10, 12, 10) },
            };

        // Проставить края уже импортированным картинкам (вызывает сборщик сцены).
        public static void FixBorders()
        {
            foreach (var pair in Borders)
            {
                foreach (string guid in AssetDatabase.FindAssets(pair.Key + " t:Texture2D", new[] { "Assets/Art" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(path) != pair.Key || !(AssetImporter.GetAtPath(path) is TextureImporter importer))
                        continue;
                    if (importer.spriteBorder == pair.Value)
                        continue;
                    importer.spriteBorder = pair.Value;
                    importer.SaveAndReimport();
                }
            }
        }

        // Объекты, которые "стоят" на земле или воде: опора внизу (вид 45°).
        public static bool StandsOnGround(string file)
        {
            if (file.EndsWith("_portrait"))
                return false;
            return file.StartsWith("obj_") && file != "obj_bobber" || file.StartsWith("boat_")
                   || file.StartsWith("npc_") || file.StartsWith("hero_") || file == "env_reed";
        }

        // Поправить уже импортированные картинки (меню сборщика вызывает сам).
        public static void FixPivots()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!StandsOnGround(file) || !(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    continue;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteAlignment == (int)SpriteAlignment.BottomCenter)
                    continue;

                settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                settings.spritePivot = new Vector2(0.5f, 0f);
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }
    }
}
