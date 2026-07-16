using UnityEngine;

/// <summary>
/// Héroe FRANCOTIRADOR (compra única en el menú, Loadout.SniperOwned): una unidad
/// dorada destacada en el centro del escuadrón que dispara por su cuenta con
/// auto-apuntado al enemigo con MÁS vida en pantalla — exactamente el objetivo
/// que peor lleva el fuego recto del escuadrón (tanks, élites, jefe).
///
/// No cuenta como soldado (no es recurso, no muere): es un arma pasiva. Vive en
/// el GameObject del Squad (lo añade GameBootstrap si está comprado).
/// </summary>
public class SniperHero : MonoBehaviour
{
    const float FireInterval = 1.6f;
    static readonly Color GoldTint = new Color(1f, 0.82f, 0.23f);

    float fireT = 1f; // primer disparo con un respiro
    Transform visual;

    void Start()
    {
        // Unidad visual destacada en el centro del blob (no está en Squad.units:
        // no cuenta para el recuento ni puede morir por contacto).
        var go = Prims.MakeSprite("SniperHero", ArtCache.Soldier, GoldTint,
            new Vector2(0.44f, 0.44f), transform.position, sortingOrder: 3);
        go.transform.SetParent(transform, worldPositionStays: false);
        go.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // cabeza arriba
        SpriteAnim.Play(go, ArtCache.SoldierMarch, 6f, true);
        visual = go.transform;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (gm.Squad == null || gm.Squad.Count <= 0) return; // sin escuadrón no hay héroe

        fireT -= Time.deltaTime;
        if (fireT > 0f) return;

        // Objetivo: el enemigo VISIBLE con más vida restante (tank/élite/jefe).
        float top = Camera.main != null ? Camera.main.orthographicSize : 5f;
        Enemy target = null;
        float best = 0f;
        var all = Enemy.All;
        for (int i = 0; i < all.Count; i++)
        {
            var e = all[i];
            if (e == null || e.transform.position.y > top) continue;
            if (e.Health > best) { best = e.Health; target = e; }
        }
        if (target == null) return; // sin objetivo no gasta cadencia

        fireT = FireInterval;

        // Bala dirigida al objetivo (sin homing: apunta a dónde está AHORA; a
        // 22 u/s el error de puntería es mínimo). Daño alto de un solo objetivo.
        Vector3 muzzle = visual != null ? visual.position : transform.position;
        Vector2 dir = ((Vector2)(target.transform.position - muzzle)).normalized;
        float damage = (25f + 5f * gm.Level) * Perks.DamageMult * StartingPoint.DamageMult;
        Bullet.Spawn(muzzle + (Vector3)(dir * 0.35f), dir, 22f, damage, pierce: 0);
        Vfx.Muzzle(muzzle);
        Sfx.Shoot();
    }
}
