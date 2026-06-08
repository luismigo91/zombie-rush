using UnityEngine;

/// <summary>
/// Componente que cicla un array de Sprite[] sobre el SpriteRenderer del propio
/// GameObject, generando animación por código (sin Animator ni clips). Cada
/// instancia arranca con un offset de fase aleatorio para que formaciones u
/// hordas no marchen sincronizadas.
///
/// No crea SpriteRenderer: si el GameObject no tiene uno, Play es un no-op
/// seguro. Reutiliza el componente si ya existe en el GameObject (Configure),
/// evitando duplicados al re-llamar a Play.
/// </summary>
public class SpriteAnim : MonoBehaviour
{
    Sprite[] _frames;     // fotogramas a ciclar
    float _fps;           // velocidad de reproducción
    bool _loop;           // si true repite; si no, se queda en el último y se desactiva
    float _phase;         // offset de fase (segundos) para desincronizar instancias
    float _t;             // tiempo acumulado
    SpriteRenderer _sr;   // renderer del GameObject (se cachea)

    /// <summary>
    /// Reproduce los frames sobre el SpriteRenderer de <paramref name="go"/>.
    /// No-op seguro si go es null, frames null/vacío o el GameObject no tiene
    /// SpriteRenderer. Reutiliza un SpriteAnim existente. Firma del contrato.
    /// </summary>
    public static SpriteAnim Play(GameObject go, Sprite[] frames, float fps, bool loop = true)
    {
        if (go == null || frames == null || frames.Length == 0) return null;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) return null; // no creamos renderer, sólo lo usamos

        var anim = go.GetComponent<SpriteAnim>();
        if (anim == null) anim = go.AddComponent<SpriteAnim>();
        anim.Configure(sr, frames, fps, loop);
        return anim;
    }

    /// <summary>Configura (o reconfigura) la animación reutilizando el componente.</summary>
    void Configure(SpriteRenderer sr, Sprite[] frames, float fps, bool loop)
    {
        _sr = sr;
        _frames = frames;
        _fps = Mathf.Max(0.01f, fps);
        _loop = loop;
        _t = 0f;
        // Offset de fase aleatorio dentro de un ciclo completo (desincroniza).
        _phase = Random.value * (_frames.Length / _fps);
        enabled = true;
        // Pinta ya el primer frame correcto (evita un fotograma de retardo).
        Apply();
    }

    void Update()
    {
        _t += Time.deltaTime;
        Apply();
    }

    void Apply()
    {
        if (_sr == null || _frames == null || _frames.Length == 0) return;

        int n = _frames.Length;
        int idx = Mathf.FloorToInt((_t + _phase) * _fps);

        if (_loop)
        {
            idx %= n;
            if (idx < 0) idx += n; // por si la fase diese negativo
        }
        else if (idx >= n)
        {
            // Animación de una pasada terminada: fija el último frame y se apaga.
            _sr.sprite = _frames[n - 1];
            enabled = false;
            return;
        }

        _sr.sprite = _frames[idx];
    }
}
