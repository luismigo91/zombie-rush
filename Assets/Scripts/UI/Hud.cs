using UnityEngine;

/// <summary>
/// HUD de la partida dibujado con IMGUI (OnGUI), con tamaños proporcionales a
/// la altura de pantalla para que se lea bien en móvil. Muestra vida, oleada y
/// monedas, y la pantalla de game over con botones Reintentar / Menú.
///
/// Provisional: en una fase de pulido se sustituye por un Canvas de uGUI.
/// </summary>
public class Hud : MonoBehaviour
{
    static float damageFlash;

    /// <summary>Dispara el destello rojo de pantalla (al recibir daño el jugador).</summary>
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

        // Destello rojo al recibir daño.
        if (damageFlash > 0f)
        {
            GUI.color = new Color(1f, 0f, 0f, 0.4f * Mathf.Clamp01(damageFlash / 0.25f));
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        float u = h / 1280f; // unidad de escala (referencia ~720x1280)

        var label = Style((int)(34 * u), TextAnchor.UpperLeft);
        float hp = gm.Player != null ? gm.Player.Health : 0f;
        float x = 24 * u, lh = 44 * u;
        GUI.Label(new Rect(x, 16 * u, w, lh), $"Vida: {hp:0}", label);
        GUI.Label(new Rect(x, 16 * u + lh, w, lh), $"Oleada: {gm.CurrentWave}", label);
        GUI.Label(new Rect(x, 16 * u + lh * 2, w, lh), $"Monedas: {gm.Coins}", label);
        GUI.Label(new Rect(x, 16 * u + lh * 3, w, lh), $"Kills: {gm.Kills}", label);

        if (gm.State == GameState.GameOver)
            DrawGameOver(gm, w, h, u);
    }

    void DrawGameOver(GameManager gm, float w, float h, float u)
    {
        // Velo semitransparente.
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var big = Style((int)(64 * u), TextAnchor.MiddleCenter);
        var mid = Style((int)(34 * u), TextAnchor.MiddleCenter);
        var btn = ButtonStyle((int)(38 * u));

        GUI.Label(new Rect(0, h * 0.24f, w, 90 * u), "GAME OVER", big);
        GUI.Label(new Rect(0, h * 0.36f, w, 50 * u), $"Oleada {gm.CurrentWave}   ·   {gm.Coins} monedas", mid);
        GUI.Label(new Rect(0, h * 0.41f, w, 50 * u), $"Kills: {gm.Kills}    Tiempo: {gm.RunTime:0}s", mid);
        GUI.Label(new Rect(0, h * 0.47f, w, 50 * u), $"Banco total: {Economy.Coins} monedas", mid);

        float bw = w * 0.62f, bh = 110 * u, bx = (w - bw) * 0.5f;
        if (GUI.Button(new Rect(bx, h * 0.58f, bw, bh), "REINTENTAR", btn))
        {
            Sfx.Click();
            gm.Restart();
        }
        if (GUI.Button(new Rect(bx, h * 0.58f + bh + 20 * u, bw, bh), "MENÚ / MEJORAS", btn))
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
    {
        var s = new GUIStyle(GUI.skin.button) { fontSize = fontSize };
        return s;
    }
}
