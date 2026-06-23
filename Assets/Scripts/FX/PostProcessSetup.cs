using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Crea por código el <c>Volume</c> global de post-proceso del mood "noche
/// apocalíptica neón": Bloom (brillo en balas/monedas/muzzle), Vignette
/// (oscurece bordes, reemplaza al sprite de viñeta de Environment), Color
/// Adjustments (contraste/saturación nocturnos) y Film Grain sutil.
///
/// Se instancia desde los bootstraps (GameBootstrap y MenuBootstrap) en Awake,
/// manteniendo code-first: nada se cablea en el Inspector. El volume es global
/// (afecta a toda la cámara) y sobrevive a la escena mientras el componente
/// exista.
/// </summary>
public class PostProcessSetup : MonoBehaviour
{
    /// <summary>Instancia el volume en la escena actual. Llamar desde el bootstrap.</summary>
    public static void Build()
    {
        // Evita duplicados.
        var existing = FindFirstObjectByType<PostProcessSetup>();
        if (existing != null) return;

        var go = new GameObject("PostProcessVolume");
        var v = go.AddComponent<PostProcessSetup>();
        v.Setup();
    }

    void Setup()
    {
        var vol = gameObject.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "NeonApocalypse";

        // --- Bloom: brillo neón (threshold alto para que solo brillen los
        // emisivos: balas, muzzle, monedas, ojos de zombie/jefe). ---
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(1.2f);
        bloom.scatter.Override(0.7f);
        bloom.tint.Override(new Color(1f, 0.95f, 0.8f, 1f)); // leve tinte cálido

        // --- Vignette: oscurece bordes (reemplaza el sprite de Environment). ---
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.55f);
        vignette.smoothness.Override(0.4f);
        vignette.color.Override(new Color(0.02f, 0.01f, 0.05f, 1f)); // casi negro con tinte violeta

        // --- Color Adjustments: contraste+ligero, saturación-ligera (mood nocturno). ---
        var colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.postExposure.Override(0.1f);
        colorAdj.contrast.Override(10f);
        colorAdj.saturation.Override(-8f);
        colorAdj.colorFilter.Override(new Color(0.95f, 0.93f, 1f, 1f)); // leve tinte frío

        // --- Film Grain: sutil, da textura de "película". ---
        var grain = profile.Add<FilmGrain>(true);
        grain.intensity.Override(0.3f);
        grain.type.Override(FilmGrainLookup.Thin2);
        grain.response.Override(0.8f);

        vol.profile = profile;
    }
}
