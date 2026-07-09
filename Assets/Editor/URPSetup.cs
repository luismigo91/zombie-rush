#if UNITY_EDITOR
using UnityEditor;
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

        // --- 2D Renderer Data ---
        var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }

        // --- URP Asset (la API de Unity 6 crea el asset con el renderer dado) ---
        var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URPAssetPath);
        if (urpAsset == null)
        {
            urpAsset = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(urpAsset, URPAssetPath);
        }

        // Configuración orientada a móvil Android portrait.
        urpAsset.renderScale = 1f;
        urpAsset.msaaSampleCount = 1; // off (fill-rate en móvil)
        urpAsset.supportsHDR = false; // sin HDR (fill-rate)
        urpAsset.shadowDistance = 50f;

        AssetDatabase.SaveAssets();

        // Asignar como pipeline activo.
        GraphicsSettings.defaultRenderPipeline = urpAsset;
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
