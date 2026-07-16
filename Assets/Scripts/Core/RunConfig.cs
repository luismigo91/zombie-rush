using UnityEngine;

/// <summary>Modo de la próxima run: campaña (por defecto), supervivencia o desafío diario.</summary>
public enum GameMode { Campaign, Survival, Daily }

/// <summary>Modificador del desafío diario (uno por día, derivado de la fecha).</summary>
public enum DailyMod { DoubleCoins, FastHorde, ExplosiveOutbreak, RunnersOnly, NoPowerUps }

/// <summary>
/// Configuración de la run que el menú fija ANTES de cargar la escena Game
/// (estática, no persistente: cada botón de jugar la vuelve a fijar) y récords
/// de los modos sin fin (mejor oleada), persistidos con PlayerPrefs.
///
/// - Survival: oleadas infinitas reutilizando la curva de los 100 niveles, con un
///   multiplicador extra de vida por oleada (LevelRunner) para que nunca se plancha.
/// - Daily: supervivencia con semilla del DÍA (mismo recorrido en todos los
///   intentos de hoy, distinto mañana) y récord diario separado.
/// </summary>
public static class RunConfig
{
    public static GameMode Mode = GameMode.Campaign;

    public static bool Endless => Mode != GameMode.Campaign;

    /// <summary>Semilla del desafío de hoy (yyyyMMdd como entero).</summary>
    public static int DailySeed
    {
        get { var d = System.DateTime.Now; return d.Year * 10000 + d.Month * 100 + d.Day; }
    }

    /// <summary>Oleada en la que arranca cada modo (el diario empieza con chicha).</summary>
    public static int StartWave => Mode == GameMode.Daily ? 9 : 4;

    /// <summary>Modificador de HOY (hash de la fecha → uno de los DailyMod).</summary>
    public static DailyMod DailyModifier
    {
        get
        {
            int h;
            unchecked { h = DailySeed * 1103515245 + 12345; }
            return (DailyMod)(Mathf.Abs(h) % 5);
        }
    }

    /// <summary>Nombre corto del modificador para menú/HUD.</summary>
    public static string DailyModName => DailyModifier switch
    {
        DailyMod.DoubleCoins => "MONEDAS ×2",
        DailyMod.FastHorde => "HORDA VELOZ",
        DailyMod.ExplosiveOutbreak => "PLAGA EXPLOSIVA",
        DailyMod.RunnersOnly => "SOLO CORREDORES",
        _ => "SIN POWER-UPS",
    };

    /// <summary>¿Está jugándose el diario con este modificador? (las mecánicas consultan aquí).</summary>
    public static bool DailyModActive(DailyMod m) => Mode == GameMode.Daily && DailyModifier == m;

    const string SurvKey = "surv_best";
    static string DailyKey => "daily_best_" + DailySeed;

    public static int SurvivalBest => PlayerPrefs.GetInt(SurvKey, 0);
    public static int DailyBest => PlayerPrefs.GetInt(DailyKey, 0);

    /// <summary>Registra el final de una run sin fin; true si es récord nuevo.</summary>
    public static bool ReportEnd(int wave)
    {
        if (!Endless) return false;
        string key = Mode == GameMode.Daily ? DailyKey : SurvKey;
        if (wave <= PlayerPrefs.GetInt(key, 0)) return false;
        PlayerPrefs.SetInt(key, wave);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Borra los récords (reinicio de progreso). Los diarios pasados quedan inertes.</summary>
    public static void ResetRecords()
    {
        PlayerPrefs.DeleteKey(SurvKey);
        PlayerPrefs.DeleteKey(DailyKey);
        PlayerPrefs.Save();
    }
}
