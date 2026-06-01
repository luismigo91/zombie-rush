using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú principal (IMGUI): banco de monedas, nivel de campaña, botón JUGAR y la
/// tienda de PUNTO DE PARTIDA (soldados iniciales y arma base). Compra gastando
/// del banco persistente. Incluye toggle de música y reinicio de progreso.
///
/// Sustituye a las mejoras de % del juego anterior (Zombie Rush: el poder dentro
/// de la run viene de gates/jaulas; la tienda solo fija el punto de partida).
/// Provisional con OnGUI; en pulido se sustituye por uGUI.
/// </summary>
public class MenuUI : MonoBehaviour
{
    void OnGUI()
    {
        float h = Screen.height, w = Screen.width;
        float u = h / 1280f;

        var title = Style((int)(70 * u), TextAnchor.MiddleCenter, FontStyle.Bold);
        var info = Style((int)(34 * u), TextAnchor.MiddleCenter);
        var header = Style((int)(40 * u), TextAnchor.MiddleCenter, FontStyle.Bold);
        var rowName = Style((int)(32 * u), TextAnchor.MiddleLeft);
        var rowVal = Style((int)(26 * u), TextAnchor.MiddleLeft);
        var bigBtn = ButtonStyle((int)(44 * u));
        var buyBtn = ButtonStyle((int)(30 * u));
        var smallBtn = ButtonStyle((int)(24 * u));

        float margin = w * 0.06f;
        float contentW = w - margin * 2f;

        GUI.Label(new Rect(0, h * 0.04f, w, 90 * u), "ZOMBIE RUSH", title);
        GUI.Label(new Rect(0, h * 0.12f, w, 50 * u), $"Banco: {Economy.Coins}    ·    Nivel {Campaign.Current}/100", info);

        float playW = w * 0.6f, playH = 120 * u;
        if (GUI.Button(new Rect((w - playW) * 0.5f, h * 0.17f, playW, playH), "JUGAR", bigBtn))
        {
            Sfx.Click();
            SceneManager.LoadScene("Game");
        }

        float y = h * 0.33f;
        GUI.Label(new Rect(0, y, w, 50 * u), "PUNTO DE PARTIDA", header);
        y += 70 * u;

        float rowH = 100 * u;
        DrawShopRow(StartStat.Units, margin, y, contentW, rowH, u, rowName, rowVal, buyBtn); y += rowH + 14 * u;
        DrawShopRow(StartStat.Weapon, margin, y, contentW, rowH, u, rowName, rowVal, buyBtn); y += rowH + 22 * u;

        float rw = w * 0.5f, rh = 70 * u;
        string mLabel = Music.Muted ? "Música: OFF" : "Música: ON";
        if (GUI.Button(new Rect((w - rw) * 0.5f, y, rw, rh), mLabel, smallBtn))
        {
            Sfx.Click();
            Music.Muted = !Music.Muted;
        }
        y += rh + 12 * u;

        if (GUI.Button(new Rect((w - rw) * 0.5f, y, rw, rh), "Reiniciar progreso", smallBtn))
            StartingPoint.ResetAll();
    }

    void DrawShopRow(StartStat stat, float x, float y, float width, float height, float u,
                     GUIStyle nameStyle, GUIStyle valStyle, GUIStyle buyStyle)
    {
        GUI.color = new Color(1f, 1f, 1f, 0.07f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float padX = 18 * u;
        int level = StartingPoint.Level(stat);

        GUI.Label(new Rect(x + padX, y + 8 * u, width * 0.62f, height * 0.5f),
            $"{StartingPoint.Name(stat)}   Nv {level}/{StartingPoint.MaxLevel(stat)}", nameStyle);
        GUI.Label(new Rect(x + padX, y + height * 0.5f, width * 0.62f, height * 0.45f),
            $"Actual: {StartingPoint.ValueText(stat)}", valStyle);

        float bw = width * 0.32f, bh = height * 0.66f;
        var bRect = new Rect(x + width - bw - padX, y + (height - bh) * 0.5f, bw, bh);

        if (StartingPoint.IsMaxed(stat))
        {
            GUI.enabled = false;
            GUI.Button(bRect, "MAX", buyStyle);
            GUI.enabled = true;
        }
        else
        {
            int cost = StartingPoint.NextCost(stat);
            GUI.enabled = Economy.Coins >= cost;
            if (GUI.Button(bRect, $"{cost} ⤴", buyStyle))
            {
                Sfx.Click();
                StartingPoint.TryBuy(stat);
            }
            GUI.enabled = true;
        }
    }

    static GUIStyle Style(int fontSize, TextAnchor anchor, FontStyle fs = FontStyle.Normal)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = anchor, fontStyle = fs };
        s.normal.textColor = Color.white;
        return s;
    }

    static GUIStyle ButtonStyle(int fontSize)
        => new GUIStyle(GUI.skin.button) { fontSize = fontSize };
}
