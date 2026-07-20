#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Compila la versión web (WebGL) por código, análogo a BuildAndroid:
///
///  - BuildWebGL: genera `Builds/Web/` con las dos escenas (menú + juego),
///    listo para servir desde cualquier hosting estático (GitHub Pages,
///    itch.io, `python3 -m http.server`...).
///
/// Usa la plantilla propia `Assets/WebGLTemplates/ZombieRush` (canvas 9:16
/// centrado con letterbox, viewport móvil sin zoom) y compresión gzip CON
/// fallback de descompresión en JS: así el build funciona aunque el servidor
/// no envíe la cabecera `Content-Encoding: gzip`.
///
/// Menú editor: "Zombie Rush → Build Web (WebGL)".
/// Requiere el módulo "WebGL Build Support" instalado desde Unity Hub.
/// </summary>
public static class BuildWeb
{
    // El build incluye ambas escenas: arranca en el menú y puede cargar el juego.
    static readonly string[] Scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" };

    [MenuItem("Zombie Rush/Build Web (WebGL)")]
    public static void BuildWebGL()
    {
        // --- Identidad (aparece en el título de la página y el loader) ---
        PlayerSettings.companyName = "LuisMiguel";
        PlayerSettings.productName = "Zombie Rush";

        // --- Plantilla propia: letterbox 9:16 + controles táctiles/ratón ---
        PlayerSettings.WebGL.template = "PROJECT:ZombieRush";

        // --- Compresión: gzip + fallback JS = cero configuración de servidor ---
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        // Solo excepciones explícitas (default de Unity): build más pequeño.
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

        string outPath = Path.Combine("Builds", "Web");
        Directory.CreateDirectory(outPath);

        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"BUILD_OK ruta={outPath} bytes={summary.totalSize}");
        }
        else
        {
            Debug.LogError($"BUILD_FAILED resultado={summary.result} errores={summary.totalErrors}");
            // En headless (-batchmode), el proceso debe salir con código de error
            // para que un build roto no pase por bueno en la shell.
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
        }
    }
}
#endif
