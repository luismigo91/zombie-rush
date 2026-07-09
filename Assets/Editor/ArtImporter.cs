#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Importador automático del arte de <c>Assets/Resources/Art/</c>: aplica ajustes
/// deterministas a cada PNG en el momento de importarlo, para que soltar assets
/// nuevos "simplemente funcione" sin tocar nada en el editor (filosofía code-first).
///
/// - Todo se importa como Sprite (Single), sin mipmaps, filtro bilineal (el arte
///   es vector plano, no pixel-art) y PPU 100 (la escala real la normaliza
///   ArtCache al cargar).
/// - Los tiles de suelo (environment/road_*, edge_*) usan wrapMode Repeat porque
///   Environment los repite vía material (SetTextureScale).
/// - Assets/Branding/ se importa legible y sin comprimir (AppIconGen lee los
///   píxeles para icono/splash).
/// </summary>
public class ArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        bool isArt = assetPath.StartsWith("Assets/Resources/Art/");
        bool isBranding = assetPath.StartsWith("Assets/Branding/");
        if (!isArt && !isBranding) return;

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Bilinear;
        ti.alphaIsTransparency = true;

        // Tiles repetibles: el material del suelo/arcén repite la textura.
        bool isTile = isArt && (assetPath.Contains("/road_") || assetPath.Contains("/edge_"));
        ti.wrapMode = isTile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        if (isBranding)
        {
            // AppIconGen necesita leer píxeles y asignar la textura a PlayerSettings.
            ti.isReadable = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
#endif
