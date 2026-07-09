#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Compila para Android por código. Dos caminos:
///
///  - BuildAPK:  APK de desarrollo (instalable directamente para testear en dispositivo).
///  - BuildAAB:  AAB de release FIRMADO para Google Play (Play App Signing usa esta
///               clave como upload key). targetSdk actual, IL2CPP+ARM64, sin dev flags.
///
/// Ambos incluyen las dos escenas (menú + juego), portrait, IL2CPP+ARM64.
/// Menú editor: "Zombie Rush → Build APK (Android)" / "Build AAB (Play, signed)".
///
/// La firma lee credenciales de keystore/keystore.json (gitignored, NUNCA al repo).
/// Si falta el fichero, BuildAAB avisa y aborta (no firma con debug).
/// </summary>
public static class BuildAndroid
{
    // El build incluye ambas escenas: arranca en el menú y puede cargar el juego.
    static readonly string[] Scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Game.unity" };

    const string KeystoreRelPath = "keystore/zombierush.keystore";
    const string KeystoreJsonRelPath = "keystore/keystore.json";

    [MenuItem("Zombie Rush/Build APK (Android)")]
    public static void BuildAPK()
    {
        ApplyCommonAndroidSettings(development: true);
        EditorUserBuildSettings.buildAppBundle = false; // APK instalable directo

        // El APK de desarrollo firma con la debug key de Unity, no con el
        // keystore custom (que puede no tener password en sesión headless).
        PlayerSettings.Android.useCustomKeystore = false;

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
        LogResult(report, apkPath);
    }

    [MenuItem("Zombie Rush/Build AAB (Play, signed)")]
    public static void BuildAAB()
    {
        if (!TryConfigureSigning())
        {
            Debug.LogError("BUILD_FAILED: falta keystore/keystore.json. Genera uno con keytool (ver docs) antes de compilar el AAB de release.");
            return;
        }

        ApplyCommonAndroidSettings(development: false);
        EditorUserBuildSettings.buildAppBundle = true; // .aab para Play

        // versionCode auto-incremental por build de release; versionName legible.
        int vc = PlayerSettings.Android.bundleVersionCode;
        if (vc < 1) vc = 1;
        PlayerSettings.Android.bundleVersionCode = vc + 1;

        Directory.CreateDirectory("Builds");
        string aabPath = Path.Combine("Builds", "ZombieRush.aab");

        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = aabPath,
            target = BuildTarget.Android,
            options = BuildOptions.None // release, sin dev flags
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        LogResult(report, aabPath);
    }

    /// <summary>Configuración común de identidad, orientación, backend y SDK.</summary>
    static void ApplyCommonAndroidSettings(bool development)
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

        // --- SDK: minSdk amplio (Android 7.0) y targetSdk al nivel actual que exige Play ---
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24; // Android 7.0
        // API 35 (Android 15): requisito de Play para apps nuevas a partir de ago-2025.
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;

        // Icono, splash y versión generados por código (look de app publicable).
        AppIconGen.Apply();

        // En release desactivamos el debugging de editor.
        if (!development)
            EditorUserBuildSettings.allowDebugging = false;
    }

    /// <summary>Carga keystore/keystore.json y aplica la firma al PlayerSettings.</summary>
    static bool TryConfigureSigning()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (projectRoot == null) return false;

        string jsonPath = Path.Combine(projectRoot, KeystoreJsonRelPath);
        if (!File.Exists(jsonPath)) return false;

        string keystorePath = Path.Combine(projectRoot, KeystoreRelPath);
        if (!File.Exists(keystorePath)) return false;

        var data = JsonUtility.FromJson<KeystoreData>(File.ReadAllText(jsonPath));
        if (string.IsNullOrEmpty(data.alias) || string.IsNullOrEmpty(data.storePass)) return false;

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = data.storePass;
        PlayerSettings.Android.keyaliasName = data.alias;
        PlayerSettings.Android.keyaliasPass = string.IsNullOrEmpty(data.keyPass) ? data.storePass : data.keyPass;
        return true;
    }

    [System.Serializable]
    struct KeystoreData
    {
        public string alias;
        public string storePass;
        public string keyPass;
    }

    static void LogResult(BuildReport report, string path)
    {
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"BUILD_OK ruta={path} bytes={summary.totalSize} vc={PlayerSettings.Android.bundleVersionCode}");
        else
            Debug.LogError($"BUILD_FAILED resultado={summary.result} errores={summary.totalErrors}");
    }
}
#endif
