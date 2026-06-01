using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dibuja "números de daño" flotantes en el mundo usando IMGUI: se proyecta la
/// posición de mundo a pantalla, el texto sube y se desvanece. Code-only, sin
/// fuentes ni TextMesh. Lo crea GameBootstrap.
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }

    class Item
    {
        public Vector3 world;
        public string text;
        public Color color;
        public float size;
        public float life;
        public float age;
    }

    readonly List<Item> items = new List<Item>();
    Camera cam;

    void Awake()
    {
        Instance = this;
        cam = Camera.main;
    }

    public static void Spawn(Vector3 world, string text, Color color, float size = 30f, float life = 0.7f)
    {
        if (Instance == null) return;
        Instance.items.Add(new Item { world = world, text = text, color = color, size = size, life = life });
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var it = items[i];
            it.age += dt;
            it.world += Vector3.up * (1.6f * dt); // sube
            if (it.age >= it.life) items.RemoveAt(i);
        }
    }

    void OnGUI()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float u = Screen.height / 1280f;

        DrawBossBars(u);

        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            Vector3 sp = cam.WorldToScreenPoint(it.world);
            if (sp.z < 0f) continue; // detrás de la cámara

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(10, (int)(it.size * u)),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            Color c = it.color;
            c.a = 1f - (it.age / it.life);
            style.normal.textColor = c;

            float w = 140f * u, h = 44f * u;
            // GUI usa Y=0 arriba; WorldToScreenPoint usa Y=0 abajo.
            var rect = new Rect(sp.x - w * 0.5f, (Screen.height - sp.y) - h * 0.5f, w, h);
            GUI.Label(rect, it.text, style);
        }
    }

    /// <summary>Barra de vida flotante encima de cada mini-jefe.</summary>
    void DrawBossBars(float u)
    {
        var enemies = Enemy.All;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || !e.isBoss) continue;

            Vector3 sp = cam.WorldToScreenPoint(e.transform.position);
            if (sp.z < 0f) continue;

            float bw = 190f * u, bh = 16f * u;
            float gx = sp.x - bw * 0.5f;
            float gy = (Screen.height - sp.y) - 100f * u;

            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(gx, gy, bw, bh), Texture2D.whiteTexture);

            float frac = Mathf.Clamp01(e.Health / Mathf.Max(1f, e.maxHealth));
            GUI.color = new Color(0.85f, 0.2f, 0.2f);
            GUI.DrawTexture(new Rect(gx + 2f * u, gy + 2f * u, (bw - 4f * u) * frac, bh - 4f * u), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
