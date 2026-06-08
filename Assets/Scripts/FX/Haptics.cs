using UnityEngine;

/// <summary>
/// Vibración háptica del juego con tres intensidades (Light/Medium/Heavy).
/// Respeta el ajuste del usuario (SettingsStore.VibrationOn): si está desactivada,
/// no hace nada. En el editor es no-op (no hay motor de vibración fiable). En
/// Android usa el Vibrator del sistema vía AndroidJavaObject, con VibrationEffect
/// (API >= 26) y duración/amplitud por nivel; en APIs viejas cae a vibrate(ms) y,
/// como último recurso, a Handheld.Vibrate(). El Vibrator se cachea para no
/// recrearlo en cada llamada. Todo va envuelto en try/catch para no crashear.
/// </summary>
public static class Haptics
{
    // Duraciones por nivel (milisegundos) y amplitud (0..255).
    const long LightMs = 10;
    const long MediumMs = 25;
    const long HeavyMs = 50;

    /// <summary>Vibración muy corta (disparo, recoger moneda...).</summary>
    public static void Light() => Vibrate(LightMs, 90);

    /// <summary>Vibración media (cruzar gate, impacto notable...).</summary>
    public static void Medium() => Vibrate(MediumMs, 160);

    /// <summary>Vibración fuerte (muerte de jefe, derrota...).</summary>
    public static void Heavy() => Vibrate(HeavyMs, 255);

    /// <summary>
    /// Disparo común: comprueba el ajuste del usuario y delega a la plataforma.
    /// </summary>
    static void Vibrate(long milliseconds, int amplitude)
    {
        // Respeta el ajuste del usuario (no vibrar si lo desactivó).
        if (!SettingsStore.VibrationOn) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(milliseconds, amplitude);
#else
        // En editor y otras plataformas: no-op (evita ruido al desarrollar).
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject _vibrator;   // android.os.Vibrator cacheado
    static int _sdkInt = -1;              // Build.VERSION.SDK_INT cacheado
    static bool _initFailed;

    /// <summary>Obtiene (y cachea) el servicio Vibrator del Context de la app.</summary>
    static AndroidJavaObject GetVibrator()
    {
        if (_vibrator != null) return _vibrator;
        if (_initFailed) return null;
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                // Context.VIBRATOR_SERVICE == "vibrator".
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                _sdkInt = version.GetStatic<int>("SDK_INT");
            }
        }
        catch (System.Exception e)
        {
            _initFailed = true;
            Debug.LogWarning("Haptics: no se pudo obtener el Vibrator: " + e.Message);
        }
        return _vibrator;
    }

    static void VibrateAndroid(long milliseconds, int amplitude)
    {
        try
        {
            var vib = GetVibrator();
            if (vib == null) { Handheld.Vibrate(); return; }

            // Algunos dispositivos no tienen vibrador físico.
            bool has = true;
            try { has = vib.Call<bool>("hasVibrator"); } catch { /* asumimos que sí */ }
            if (!has) return;

            if (_sdkInt >= 26)
            {
                // VibrationEffect.createOneShot(ms, amplitude) — API 26+.
                amplitude = Mathf.Clamp(amplitude, 1, 255);
                using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amplitude))
                {
                    vib.Call("vibrate", effect);
                }
            }
            else
            {
                // API < 26: vibrate(long milliseconds) (deprecado pero válido).
                vib.Call("vibrate", milliseconds);
            }
        }
        catch (System.Exception e)
        {
            // Último recurso: el pulso por defecto de Unity.
            Debug.LogWarning("Haptics: fallo al vibrar, fallback a Handheld.Vibrate(). " + e.Message);
            try { Handheld.Vibrate(); } catch { /* nada más que hacer */ }
        }
    }
#endif
}
