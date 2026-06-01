using UnityEngine;

/// <summary>
/// HUD de la partida (Zombie Rush) con IMGUI (OnGUI), tamaños proporcionales a la
/// altura de pantalla para leerse en móvil. Muestra nº de unidades, nivel y una
/// barra de progreso del nivel, y las pantallas de VICTORIA / DERROTA.
///
/// Provisional: en pulido se sustituye por un Canvas de uGUI.
/// </summary>
public class Hud : MonoBehaviour
{
    static float damageFlash;

    /// <summary>Destello rojo de pantalla (lo conserva PlayerController dormante).</summary>
    public static void FlashDamage() => damageFlash = 0.25f;

    void Update()
    {
        if (damageFlash > 0f) damageFlash -= Time.deltaTime;
    }

    void OnGUI()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        float h = Screen.height, w = Screen.width;

        if (damageFlash > 0f)
        {
            GUI.color = new Color(1f, 0f, 0f, 0.4f * Mathf.Clamp01(damageFlash / 0.25f));
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        float u = h / 1280f;
        int count = gm.Squad != null ? gm.Squad.Count : 0;

        var label = Style((int)(40 * u), TextAnchor.UpperLeft);
        float x = 24 * u, lh = 50 * u;
        GUI.Label(new Rect(x, 16 * u, w, lh), $"Unidades: {count}", label);
        GUI.Label(new Rect(x, 16 * u + lh, w, lh), $"Nivel: {gm.Level}", label);

        DrawProgressBar(gm, w, u);

        if (gm.State == GameState.GameOver)
            DrawEndScreen(gm, w, h, u, "DERROTA", new Color(1f, 0.4f, 0.4f), "REINTENTAR");
        else if (gm.State == GameState.Won)
            DrawEndScreen(gm, w, h, u, "¡NIVEL SUPERADO!", new Color(0.5f, 1f, 0.6f), "SIGUIENTE NIVEL");
    }

    void DrawProgressBar(GameManager gm, float w, float u)
    {
        float barW = w * 0.6f, barH = 18 * u;
        float bx = (w - barW) * 0.5f, by = 24 * u;

        GUI.color = new Color(1f, 1f, 1f, 0.18f);
        GUI.DrawTexture(new Rect(bx, by, barW, barH), Texture2D.whiteTexture);
        GUI.color = new Color(0.5f, 0.85f, 1f, 0.9f);
        GUI.DrawTexture(new Rect(bx, by, barW * Mathf.Clamp01(gm.LevelProgress), barH), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void DrawEndScreen(GameManager gm, float w, float h, float u, string title, Color titleColor, string continueLabel)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var big = Style((int)(64 * u), TextAnchor.MiddleCenter);
        big.normal.textColor = titleColor;
        var btn = ButtonStyle((int)(38 * u));

        GUI.Label(new Rect(0, h * 0.28f, w, 90 * u), title, big);

        float bw = w * 0.62f, bh = 110 * u, bx = (w - bw) * 0.5f;
        if (GUI.Button(new Rect(bx, h * 0.50f, bw, bh), continueLabel, btn))
        {
            Sfx.Click();
            gm.Restart();
        }
        if (GUI.Button(new Rect(bx, h * 0.50f + bh + 20 * u, bw, bh), "MENÚ", btn))
        {
            Sfx.Click();
            gm.GoToMenu();
        }
    }

    static GUIStyle Style(int fontSize, TextAnchor anchor)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = anchor };
        s.normal.textColor = Color.white;
        return s;
    }

    static GUIStyle ButtonStyle(int fontSize)
        => new GUIStyle(GUI.skin.button) { fontSize = fontSize };
}
