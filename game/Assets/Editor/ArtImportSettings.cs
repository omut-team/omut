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

            importer.SetTextureSettings(settings);
        }

        // Объекты, которые "стоят" на земле или воде (кроме лодок: у них опора по центру).
        public static bool StandsOnGround(string file)
        {
            if (file.EndsWith("_portrait"))
                return false;
            return file.StartsWith("obj_") && file != "obj_bobber"
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
