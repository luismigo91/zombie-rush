using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Explosión de "partículas" hecha con primitivas (sin ParticleSystem ni
/// texturas): unos cuadraditos que salen disparados, se encogen y se desvanecen.
///
/// FxParticle usa un POOL estático: cada impacto genera varias partículas y las
/// hordas generan muchos impactos, así que reutilizar evita el GC de
/// instanciar/destruir. El pool tolera la recarga de escena (nulos descartados).
/// </summary>
public static class HitEffect
{
    public static void Burst(Vector3 pos, Color color, int count, float speed, float size, float life)
    {
        for (int i = 0; i < count; i++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (speed * Random.Range(0.5f, 1f));
            var p = FxParticle.Spawn(pos, color, size);
            p.Init(vel, life);
        }
    }
}

/// <summary>Una partícula primitiva: se mueve, frena, se encoge y se desvanece.</summary>
public class FxParticle : MonoBehaviour
{
    static readonly Stack<FxParticle> pool = new Stack<FxParticle>();

    SpriteRenderer sr;
    Vector3 baseScale;
    Vector2 vel;
    float life = 0.3f;
    float age;

    public static FxParticle Spawn(Vector3 pos, Color color, float size)
    {
        FxParticle p = null;
        while (pool.Count > 0)
        {
            p = pool.Pop();
            if (p != null) break; // descarta nulos (destruidos por recarga de escena)
            p = null;
        }

        if (p == null)
        {
            GameObject go = Prims.Make("FX", color, new Vector2(size, size), pos, sortingOrder: 6);
            p = go.AddComponent<FxParticle>();
            p.sr = go.GetComponent<SpriteRenderer>();
        }
        else
        {
            p.transform.position = pos;
            if (p.sr != null) p.sr.color = color;
            if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
        }

        p.baseScale = new Vector3(size, size, 1f);
        p.transform.localScale = p.baseScale;
        return p;
    }

    public void Init(Vector2 velocity, float lifetime)
    {
        vel = velocity;
        life = lifetime;
        age = 0f;
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

        if (age >= life) Despawn();
    }

    void Despawn()
    {
        gameObject.SetActive(false);
        pool.Push(this);
    }
}
