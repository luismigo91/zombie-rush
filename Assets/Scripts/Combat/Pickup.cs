using UnityEngine;

/// <summary>
/// Pickup que cae desde donde nace (moneda o cofre). El jugador lo recoge
/// automáticamente al tocarlo (GDD: recoger al contacto), sumando monedas a la
/// run. Si cae por debajo de la pantalla sin recogerse, se destruye: hay que
/// moverse para atraparlo mientras esquivas → tensión riesgo/recompensa.
/// </summary>
public class Pickup : MonoBehaviour
{
    int coinValue;
    float fallSpeed;
    float killY; // por debajo de esta Y se destruye

    /// <summary>Moneda pequeña (la sueltan los enemigos al morir).</summary>
    public static Pickup SpawnCoin(Vector3 pos, int value)
        => Spawn(pos, value, PixelArt.Coin, new Vector2(0.42f, 0.42f), 3f);

    /// <summary>Cofre dorado, vale más monedas (cae periódicamente).</summary>
    public static Pickup SpawnChest(Vector3 pos, int value)
        => Spawn(pos, value, PixelArt.Chest, new Vector2(0.6f, 0.55f), 2.5f);

    public static Pickup Spawn(Vector3 pos, int value, Sprite sprite, Vector2 size, float fallSpeed)
    {
        GameObject go = Prims.MakeSprite("Pickup", sprite, Color.white, size, pos, sortingOrder: 3);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.useFullKinematicContacts = true;

        var p = go.AddComponent<Pickup>();
        p.coinValue = value;
        p.fallSpeed = fallSpeed;

        Camera cam = Camera.main;
        p.killY = cam != null ? -cam.orthographicSize - 1f : -10f;

        return p;
    }

    void Update()
    {
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
        if (transform.position.y < killY)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddCoins(coinValue);
            Sfx.Coin();
            Destroy(gameObject);
        }
    }
}
