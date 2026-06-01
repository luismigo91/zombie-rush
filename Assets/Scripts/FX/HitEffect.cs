using UnityEngine;

/// <summary>
/// Explosión de "partículas" hecha con primitivas (sin ParticleSystem ni
/// texturas): unos cuadraditos que salen disparados, se encogen y se desvanecen.
/// Suficiente para dar impacto en la fase gris. En pulido final se puede pasar
/// a un ParticleSystem con sprites.
/// </summary>
public static class HitEffect
{
    public static void Burst(Vector3 pos, Color color, int count, float speed, float size, float life)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = Prims.Make("FX", color, new Vector2(size, size), pos, sortingOrder: 6);
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (speed * Random.Range(0.5f, 1f));
            go.AddComponent<FxParticle>().Init(vel, life);
        }
    }
}

/// <summary>Una partícula primitiva: se mueve, frena, se encoge y se desvanece.</summary>
public class FxParticle : MonoBehaviour
{
    Vector2 vel;
    float life = 0.3f;
    float age;
    SpriteRenderer sr;
    Vector3 baseScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    public void Init(Vector2 velocity, float lifetime)
    {
        vel = velocity;
        life = lifetime;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;

        transform.position += (Vector3)(vel * dt);
        vel *= 1f - 3f * dt; // fricción

        float t = Mathf.Clamp01(age / life);
        transform.localScale = baseScale * (1f - t);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;
        }

        if (age >= life) Destroy(gameObject);
    }
}
