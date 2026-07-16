using UnityEngine;

/// <summary>
/// Sacudida de cámara (screen shake). Va en la Main Camera. Llama
/// CameraShake.Shake(intensidad, duración) para disparar un temblor que decae.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    Vector3 basePos;
    float magnitude;
    float decayPerSec;

    void Awake()
    {
        Instance = this;
        basePos = transform.localPosition;
    }

    // Confort visual (playtest: "vibración de pantalla, no se ve qué pasa"):
    // atenuación global, TECHO de magnitud (los eventos no se apilan sin límite)
    // y ruido PERLIN suave en vez de saltos aleatorios por frame (ruido blanco).
    const float Attenuation = 0.6f;  // todos los shakes llegan al 60%
    const float MaxMagnitude = 0.14f; // techo en unidades de mundo (~1.4% del alto)
    const float NoiseFreq = 14f;      // Hz aparente del temblor (suave, legible)

    public static void Shake(float intensity, float duration)
    {
        if (Instance != null) Instance.Begin(intensity * Attenuation, duration);
    }

    void Begin(float intensity, float duration)
    {
        intensity = Mathf.Min(intensity, MaxMagnitude);
        magnitude = Mathf.Max(magnitude, intensity);
        decayPerSec = intensity / Mathf.Max(0.01f, duration);
    }

    void LateUpdate()
    {
        if (magnitude <= 0f) return;

        // Perlin recorrido en el tiempo: la cámara "vaga" suavemente en vez de
        // teletransportarse a un punto aleatorio cada frame.
        float t = Time.unscaledTime * NoiseFreq;
        float ox = (Mathf.PerlinNoise(t, 0.37f) * 2f - 1f) * magnitude;
        float oy = (Mathf.PerlinNoise(0.73f, t) * 2f - 1f) * magnitude;
        transform.localPosition = basePos + new Vector3(ox, oy, 0f);

        magnitude -= decayPerSec * Time.deltaTime;
        if (magnitude <= 0f)
        {
            magnitude = 0f;
            transform.localPosition = basePos;
        }
    }
}
