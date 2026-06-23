#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Setup del art-pass: genera el <c>UniversalRenderPipelineAsset</c> con un 2D
/// Renderer y lo asigna en <c>GraphicsSettings</c>, sin tocar el Inspector.
///
/// Se ejecuta desde el menú "Zombie Rush → Configurar URP 2D" o por CLI con
/// <c>-executeMethod URPSetup.Configure</c>. Crea los assets en
/// <c>Assets/Settings/</c> (versionados, no cableados a mano).
///
/// También crea un <c>Volume</c> global con los overrides de post-proceso del
/// mood "noche apocalíptica neón": Bloom, Vignette, ColorAdjustments y
/// FilmGrain. El volume se instancia por código desde el bootstrap del juego.
/// </summary>
public static class URPSetup
{
    const string SettingsFolder = "Assets/Settings";
    const string URPAssetPath = "Assets/Settings/URP-2D.asset";
    const string RendererPath = "Assets/Settings/URP-2D-Renderer.asset";

    [MenuItem("Zombie Rush/Configurar URP 2D")]
    public static void Configure()
    {
        EnsureFolder();

        // --- 2D Renderer ---
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.rendererType = RendererType.Renderer2D;
            renderer.opaqueLayerMask = uint.MaxValue;
            renderer.transparentLayerMask = uint.MaxValue;
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        // --- URP Asset ---
        var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URPAssetPath);
        if (urpAsset == null)
        {
            urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(urpAsset, URPAssetPath);
        }

        urpAsset.rendererDataList = new[] { renderer };

        // Configuración orientada a móvil Android portrait.
        urpAsset.colorSpace = ColorSpace.Linear;
        urpAsset.msaaSampleCount = 1; // off (fill-rate en móvil)
        urpAsset.renderScale = 1f;
        urpAsset.hdr = false; // sin HDR (fill-rate)
        urpAsset.shadowDistance = 50f;

        // Post-proceso activado.
        urpAsset.postProcessing = true;

        AssetDatabase.SaveAssets();

        // Asignar como pipeline activo.
        GraphicsSettings.defaultRenderPipeline = urpAsset;
        EditorUtility.SetDirty(GraphicsSettings.currentRenderPipeline);
        AssetDatabase.SaveAssets();

        Debug.Log("URP 2D configurado: " + URPAssetPath + " (renderer: " + RendererPath + ")");
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(SettingsFolder))
            AssetDatabase.CreateFolder("Assets", "Settings");
    }
}
#endif
