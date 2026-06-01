#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Compila el APK de Android por código. Se puede ejecutar desde el menú
/// "Zombie Dash → Build APK (Android)" o por línea de comandos con
/// -buildTarget Android -executeMethod BuildAndroid.BuildAPK.
///
/// Config: vertical (portrait), IL2CPP + ARM64 (compatible con móviles
/// actuales) y build de desarrollo para poder depurar. El APK se deja en
/// Builds/ (ignorado por git).
/// </summary>
public static class BuildAndroid
{
    // El APK incluye ambas escenas: arranca en el menú y puede cargar el juego.
    static readonly string[] Scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" };

    [MenuItem("Zombie Rush/Build APK (Android)")]
    public static void BuildAPK()
    {
        // --- Identidad de la app ---
        PlayerSettings.companyName = "LuisMiguel";
        PlayerSettings.productName = "Zombie Rush";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.luismiguel.zombierush");

        // --- Orientación vertical fija (juego portrait) ---
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        // --- Backend y arquitectura ---
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25; // Android 7.1 (mínimo soportado)

        // Queremos un APK instalable directamente (no un AAB de Play).
        EditorUserBuildSettings.buildAppBundle = false;

        Directory.CreateDirectory("Builds");
        string apkPath = Path.Combine("Builds", "ZombieRush.apk");

        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = apkPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"BUILD_OK ruta={apkPath} bytes={summary.totalSize}");
        else
            Debug.LogError($"BUILD_FAILED resultado={summary.result} errores={summary.totalErrors}");
    }
}
#endif
