# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Zombie Dash es un juego móvil arcade 2D *dodge & shoot* en **Unity 6000.4.9f1** (C#, plantilla 2D, Built-in Render Pipeline), target **Android vertical**. El usuario es buen programador pero **principiante con el editor de Unity**: al explicar, prioriza el funcionamiento del motor (escenas, componentes, Inspector, build) sobre la sintaxis de C#. Documentación de diseño completa en Notion ("Zombie Dash — MVP", fases F0–F6).

## Convención de idioma (importante)

Código y comentarios **en español**; identificadores de tipos/variables y términos técnicos en su forma original (inglés). Mantén este estilo en cualquier código nuevo.

## Comandos

No hay framework de tests ni `.asmdef`: todo compila en el assembly por defecto (`Assembly-CSharp`). El "build" y el "run" pasan por Unity, no por una toolchain de línea de comandos al uso.

**Abrir / jugar:** Unity Hub → *Add* esta carpeta → abrir con 6000.4.9f1. Escena de entrada `Assets/Scenes/MainMenu.unity` (flujo completo) o `Assets/Scenes/Game.unity` (directo a partida). Pulsar Play.

**Compilar APK (CLI headless):**
```bash
/Applications/Unity/Hub/Editor/6000.4.9f1/Unity.app/Contents/MacOS/Unity \
  -quit -batchmode -nographics -projectPath "$(pwd)" \
  -buildTarget Android -executeMethod BuildAndroid.BuildAPK -logFile -
```
Genera `Builds/ZombieDash.apk` (IL2CPP + ARM64, portrait, minSdk 25, dev build, package `com.luismiguel.zombiedash`). En el editor: menú *Zombie Dash → Build APK (Android)*. Busca `BUILD_OK` / `BUILD_FAILED` en el log.

**Instalar en dispositivo:** `~/Library/Android/sdk/platform-tools/adb install -r Builds/ZombieDash.apk`.

**Menús del editor** (`Assets/Editor/`, también invocables por `-executeMethod`):
- `ZombieDashSetup.CreateGameScene` / `CreateMenuScene` → regeneran las escenas desde cero.
- `CreateGameData.CreateData` → (re)genera los ScriptableObjects de `Assets/Resources/`.

## Arquitectura (lee esto antes de tocar nada)

**Code-first, placeholder-first.** Nada se cablea en el Inspector. Cada escena contiene **un único GameObject** con un *bootstrap* que construye todo lo demás por código en `Awake`:
- `Core/GameBootstrap` (escena Game) → cámara, `GameManager`, jugador (`PlayerController`+`AutoShooter`), `EnemySpawner`, `Hud`, `FloatingTextManager`.
- `Core/MenuBootstrap` (escena MainMenu) → cámara, música, `MenuUI`.

Si añades una entidad o sistema, **instánciala desde el bootstrap correspondiente**, no esperes encontrarla en la jerarquía de la escena.

**Estado de la run vs. persistencia** — dos capas separadas:
- `Core/GameManager` (singleton, `Instance`) mantiene el estado *de la partida actual*: `State`, `Kills`, `Coins` (de la run), `RunTime`, `CurrentWave`. Es efímero: se recrea al recargar la escena.
- Persistencia entre partidas vía **clases estáticas sobre PlayerPrefs**: `Core/Economy` (banco de monedas, key `coins`), `Core/Upgrades` (niveles, keys `upg_<StatId>`), `Core/Weapons` (posesión/equipada, keys `wpn_<WeaponId>` y `wpn_equipped`).
- El puente: al morir, `GameManager.OnPlayerDied()` vuelca `Coins` de la run al banco con `Economy.Add()`. `Upgrades.ResetAll()` borra todo el progreso (lo usa el botón de pruebas del menú).

**Targeting sin física.** `AutoShooter` no usa overlaps/raycasts: recorre la lista estática **`Enemy.All`** para hallar el más cercano. Los enemigos se registran/desregistran solos en esa lista. Las colisiones reales (bala↔enemigo, jugador↔enemigo, pickup↔jugador) sí usan triggers 2D (`BoxCollider2D` + `Rigidbody2D` kinematic).

**Data-driven con una única fuente de verdad.** Los ScriptableObjects viven en `Assets/Resources/` y se cargan por código con `Resources.LoadAll` (no por referencia en el Inspector):
- `Resources/Enemies/` (`EnemyData`), `Resources/Waves/` (`WaveData`, se reproducen ordenadas por nombre de archivo y la última se repite en bucle), `Resources/Upgrades/` (`UpgradeData`).
- La curva de mejoras tiene **doble respaldo**: `Upgrades.Defaults` (tabla en código) es la fuente de verdad; `CreateGameData.CreateData` genera los `.asset` a partir de ella, y `Upgrades` usa el asset si existe o cae al Default si no. Para cambiar la progresión, edita `Upgrades.Defaults` y regenera los datos — no edites los `.asset` a mano salvo para tuneo puntual (los sobreescribe el generador).

**Todo el "vestido" es procedural (sin assets binarios).** Sprites pixel-art (`FX/PixelArt`, dibujados en `Texture2D`), SFX (`FX/Sfx`) y música chiptune (`FX/Music`) se generan en código; no hay `.png`/`.wav` en el repo, por eso **no se necesita Git LFS todavía**. La UI es **IMGUI (`OnGUI`)** en `UI/Hud` y `UI/MenuUI` — pendiente de migrar a uGUI. Si metes assets reales o Canvas uGUI, ahí cambia esta premisa (y habría que instalar Git LFS).

## Trampas conocidas

- **El APK solo empaqueta `Game.unity`.** `BuildAndroid` fija `scenes = { Game.unity }`, aunque *EditorBuildSettings* incluya ambas. En el APK, los saltos de escena a `MainMenu` (`GameManager.GoToMenu()`) fallarían: el build de prueba es para validar el gameplay, no el flujo de menú. Si necesitas el menú en el APK, añade `MainMenu.unity` a la lista de `BuildAndroid`.
- El balance jugable se tunea en campos públicos de `AutoShooter` / `EnemySpawner` / `PlayerController` (cadencia, daño, oleadas, jefes) o en los ScriptableObjects; varios `[Header]` avisan de lo que es placeholder "hasta WeaponData".

## Flujo de trabajo

- **Git:** repo en GitHub (`luismigo91/zombie-dash`, privado), rama `main`, commit por hito. Haz push/commit solo cuando el usuario lo pida.
- **OpenSpec** (`openspec/`, schema `spec-driven`): desarrollo guiado por specs. `openspec/config.yaml` contiene el contexto del proyecto que se inyecta al crear artefactos. Skills `opsx:*` para proponer/aplicar/archivar cambios.
- **Filosofía:** empezar simple, iterar, validar el loop jugable antes de pulir; nada de arquitectura prematura. Cada cambio jugable se compila a APK y se prueba en dispositivo real (Pixel) por adb.
