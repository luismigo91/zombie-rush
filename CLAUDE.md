# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Zombie Rush es un juego móvil arcade 2D en **Unity 6000.4.9f1** (C#, 2D, **URP 2D** + Linear), target **Android vertical**. Es un *shooter de multitud con gates* (al estilo Last Z): controlas un escuadrón que se mueve en X y dispara recto, y creces con gates/jaulas por un recorrido con scroll mientras una horda te erosiona. El usuario es buen programador pero **principiante con el editor de Unity**: al explicar, prioriza el motor (escenas, componentes, Play, build) sobre la sintaxis de C#.

> El proyecto pivotó desde "Zombie Dash" (un dodge & shoot de héroe único). El diseño del pivote está archivado en `openspec/changes/archive/2026-06-23-pivot-zombie-rush/`; el juego anterior se conserva en el tag de git `pre-zombie-rush`. El cambio activo es `art-pass` (mejora estética: URP 2D, Linear, assets CC0, parallax, spritesheets, uGUI); su tag de rollback es `pre-art-pass`.

## Convención de idioma (importante)

Código y comentarios **en español**; identificadores de tipos/variables y términos técnicos en inglés. Mantén este estilo en cualquier código nuevo.

## Comandos

No hay framework de tests ni `.asmdef`: todo compila en `Assembly-CSharp`. El "build" y el "run" pasan por Unity.

**Abrir / jugar:** Unity Hub → la carpeta (6000.4.9f1). Escena de entrada `Assets/Scenes/MainMenu.unity` (menú + tienda) o `Assets/Scenes/Game.unity` (directo). Play en vertical 9:16.

**Compilar APK (CLI headless):**
```bash
/Applications/Unity/Hub/Editor/6000.4.9f1/Unity.app/Contents/MacOS/Unity \
  -quit -batchmode -nographics -projectPath "$(pwd)" \
  -buildTarget Android -executeMethod BuildAndroid.BuildAPK -logFile -
```
Genera `Builds/ZombieRush.apk` (IL2CPP+ARM64, portrait, package `com.luismiguel.zombierush`, **ambas escenas**). En el editor: menú *Zombie Rush → Build APK (Android)*. Busca `BUILD_OK`/`BUILD_FAILED` en el log.

**Instalar:** `~/Library/Android/sdk/platform-tools/adb install -r Builds/ZombieRush.apk`.

**Menús de editor** (`Assets/Editor/`, también por `-executeMethod`): `ZombieDashSetup.CreateGameScene`/`CreateMenuScene` (regeneran escenas), `URPSetup.Configure` (genera URP Asset + 2D Renderer y lo asigna en GraphicsSettings). `CreateGameData.CreateData` está dormante tras el pivote.

## Arquitectura (lee esto antes de tocar nada)

**Code-first, placeholder-first.** Nada se cablea en el Inspector. Cada escena tiene **un único GameObject** con un *bootstrap* que construye el resto en `Awake`:
- `Core/GameBootstrap` (Game) → cámara, `GameManager`, `Squad`+`SquadShooter`, `LevelRunner`, `Hud`.
- `Core/MenuBootstrap` (MainMenu) → cámara, música, `MenuUI`.

Si añades una entidad/sistema, **instánciala desde el bootstrap**, no la busques en la jerarquía.

**El escuadrón es el jugador** (`Player/Squad`): mantiene el recuento de unidades (recurso central), las coloca en un **disco** (ancho ∝ √N hasta un tope; exceso → densidad) como hijos, se mueve solo en X por arrastre y **erosiona por el frente** (la unidad de mayor Y) al perder. `Player/SquadShooter` dispara **recto por streams** repartidos a lo ancho, con daño escalado por densidad y modulado por el **tier de arma** (`Core/Weapons`). No hay una bala por soldado.

**Combate y "disparables".** Las balas (`Combat/Bullet`, con **pool** estático) dañan a cualquier `Combat/IShootable`: `Enemy`, `Cage`, `Barrier`. Los zombies (`Enemies/Enemy`) bajan hacia el escuadrón y, al alcanzar su frente, aplican **contacto 1:1** (−1 soldado, mueren) por comprobación de distancia (no por collider del escuadrón). Derrota = escuadrón a 0; victoria = completar el nivel.

**Niveles data-driven y deterministas.** `Core/LevelGenerator` produce una `Core/LevelDefinition` (lista de encuentros por tiempo: hordas, pares de gates en carriles, jaulas, barreras) para el nivel n con una **semilla GLOBAL constante** → los **100 niveles son fijos**. `Core/LevelRunner` la "reproduce" con el scroll y spawnea los encuentros; en niveles múltiplos de 10 spawnea un jefe y la victoria llega al derribarlo. La dificultad escala con presupuestos `D(n)/G(n)` (constantes en `LevelGenerator`, no en un ScriptableObject). `Core/Campaign` persiste el nivel actual.

**Persistencia (PlayerPrefs):** `Core/Economy` (banco de monedas, `coins`), `Core/StartingPoint` (punto de partida permanente: soldados iniciales y arma base; keys `sp_*`), `Core/Campaign` (`level`). La meta-tienda (`UI/MenuUI`) ya **no** vende % de stats: solo fija el punto de partida; la run crece con gates/jaulas. `StartingPoint` tiene un constructor estático que **migra** la economía pre-pivote (borra `upg_*`/`wpn_*`, conserva `coins`).

## Trampas conocidas / contexto

- **Arte/audio/UI:** arte de personajes y entorno con assets CC0 de Kenney en `Assets/Resources/Art/` (cargados por código vía `ArtCache`); bala/moneda/cofre/muzzle siguen procedurales (`PixelArt` como fallback de `ArtCache`). Audio procedural (`Sfx`, `Music`). UI en uGUI (`UGui.cs`, `MenuUI`, `Hud`, `PauseMenu`); `FloatingTextManager` aún usa `OnGUI` para texto flotante de combate.
- **Render pipeline: URP 2D + Linear.** Post-proceso via `PostProcessSetup` (Bloom, Vignette, ColorAdjustments, FilmGrain). `Light2D` en farolas del entorno. Configurar URP con menú *Zombie Rush → Configurar URP 2D* (`URPSetup.Configure`).
- **Git LFS obligatorio:** tras un clone fresco, ejecuta `brew install git-lfs && git lfs install && git lfs pull`. Las reglas están en `.gitattributes` (`*.png`, `*.psd`, `*.wav`, etc.). Sin LFS, los assets binarios no se descargan.
- El balance vive como valores por defecto en `Squad`, `SquadShooter`, `LevelGenerator` (se montan por código, no se ven en el Inspector): para tunear, edita esos defaults.

## Flujo de trabajo

- **Git:** repo en GitHub (`luismigo91/zombie-dash`), rama `main`, commit por hito. Push/commit solo cuando el usuario lo pida. Tag `pre-zombie-rush` = rollback al juego anterior; tag `pre-art-pass` = rollback antes del art pass.
- **OpenSpec** (`openspec/`, schema `spec-driven`): el cambio activo es `art-pass`. Skills `opsx:*` para proponer/aplicar/archivar.
- **Filosofía:** empezar simple, iterar, validar el loop con primitivas antes de pulir; cada cambio jugable se compila a APK y se prueba en dispositivo real.
