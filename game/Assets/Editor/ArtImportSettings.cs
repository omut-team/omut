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

            importer.SetTextureSettings(settings);
        }
    }
}
