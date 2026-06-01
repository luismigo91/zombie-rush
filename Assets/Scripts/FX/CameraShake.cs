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

    public static void Shake(float intensity, float duration)
    {
        if (Instance != null) Instance.Begin(intensity, duration);
    }

    void Begin(float intensity, float duration)
    {
        magnitude = Mathf.Max(magnitude, intensity);
        decayPerSec = intensity / Mathf.Max(0.01f, duration);
    }

    void LateUpdate()
    {
        if (magnitude <= 0f) return;

        Vector2 offset = Random.insideUnitCircle * magnitude;
        transform.localPosition = basePos + new Vector3(offset.x, offset.y, 0f);

        magnitude -= decayPerSec * Time.deltaTime;
        if (magnitude <= 0f)
        {
            magnitude = 0f;
            transform.localPosition = basePos;
        }
    }
}
