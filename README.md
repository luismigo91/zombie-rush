# 🧟 Zombie Rush

Juego móvil arcade *shooter de multitud con gates* en Unity 2D (al estilo de
*Last Z*). Controlas un **escuadrón** que se mueve lateralmente y dispara recto
hacia arriba; por el recorrido **creces** cruzando gates y rescatando
supervivientes, mientras una horda de zombies te erosiona. Runner vertical, un
dedo, sesiones de 1–3 minutos, móvil portrait.

> Antes se llamaba *Zombie Dash* (un "dodge & shoot" de héroe único). El juego
> pivotó de género; el diseño completo está en `openspec/changes/pivot-zombie-rush/`.
> El juego anterior queda preservado en el tag de git **`pre-zombie-rush`**.

## Estado actual: pivote implementado (sin validar aún)

Implementadas por código las fases 1–7 de la propuesta `pivot-zombie-rush`:

- ✅ **Escuadrón-multitud**: formación disco (ancho ∝ √N), movimiento en X, disparo
  recto por *streams* con daño por densidad.
- ✅ **Recorrido con scroll**: hordas que bajan, **gates en carriles** (+ / × /
  trampa / arma), **jaulas** de supervivientes y **barreras** destructibles.
- ✅ **Combate 1:1**: cada zombie que llega al frente mata 1 soldado (escudo
  frontal emergente). Derrota a 0; victoria al completar el nivel.
- ✅ **100 niveles generados** proceduralmente (deterministas, fijos), con jefe
  cada 10 y mecánicas introducidas poco a poco (tutorial implícito).
- ✅ **Meta-tienda = punto de partida**: soldados iniciales y arma base (tiers),
  comprados con monedas del banco.
- ✅ Pooling de balas; arte/audio aún procedurales (placeholder).

⚠️ **Pendiente (requiere a ti)**: abrir en Unity para **compilar** (se escribió sin
poder compilar), **jugar y validar** que engancha (hito 2.9), tunear balance
(6.2) y hacer el **build APK + prueba en el móvil** (8.1).

## Cómo abrirlo y jugar

1. Abre **Unity Hub → la carpeta** (Unity 6000.4.9f1). Al abrir, Unity compila y
   genera los `.meta` de los scripts nuevos. **Revisa la Console** por si hay
   `error CS` (y commitea los `.meta` que genere).
2. Abre **`Assets/Scenes/MainMenu.unity`** (menú + tienda) o **`Game.unity`**
   (directo a jugar) y pulsa **Play** (resolución vertical 9:16).
3. **Arrastra** para mover el escuadrón. Alinéate con los gates buenos, rescata
   jaulas, derriba barreras y sobrevive a la horda hasta el final del nivel.

## Arquitectura (code-first)

Cada escena se monta por código desde su *bootstrap* (un único GameObject):
`GameBootstrap` (Game) y `MenuBootstrap` (MainMenu). Scripts en `Assets/Scripts/`:

| Script | Responsabilidad |
|---|---|
| **Core** | |
| `Core/GameBootstrap` | Monta la partida (cámara, GameManager, escuadrón, LevelRunner, HUD). |
| `Core/MenuBootstrap` | Monta el menú (cámara, música, MenuUI). |
| `Core/GameManager` | Estado de la run, nivel, tier de arma, victoria/derrota (singleton). |
| `Core/Campaign` | Nivel actual de la campaña (1..100), persistente. |
| `Core/LevelGenerator` | Generador híbrido determinista de los 100 niveles. |
| `Core/LevelDefinition` | Modelo de nivel (encuentros por tiempo). |
| `Core/LevelRunner` | Reproduce el nivel: scroll y spawn de encuentros + jefe. |
| `Core/Economy` | Banco de monedas persistente. |
| `Core/StartingPoint` | Meta-tienda: punto de partida permanente (unidades, arma). |
| `Core/Weapons` | Arma global por tiers (daño/cadencia/streams). |
| `Core/Prims` | Fábrica de primitivas 2D. |
| **Player** | |
| `Player/Squad` | Escuadrón-multitud: recuento, formación √N, movimiento, erosión. |
| `Player/SquadShooter` | Disparo recto por streams con densidad y tier de arma. |
| **Combat** | |
| `Combat/Bullet` | Proyectil (con pool) que daña a cualquier `IShootable`. |
| `Combat/IShootable` | Interfaz de "disparable" (zombie/jaula/barrera). |
| `Combat/Gate` | Gate en carril: efecto +/×/trampa/arma al alinearse. |
| `Combat/Cage` | Jaula: se rompe a tiros → +unidades. |
| `Combat/Barrier` | Muro destructible; si llega intacto arrasa el frente. |
| **Enemies** | |
| `Enemies/Enemy` | Zombie: baja hacia el escuadrón; contacto 1:1; jefe. |
| **FX / UI** | |
| `FX/*` | Pixel-art, SFX, música, sacudida, números de daño (procedurales). |
| `UI/Hud` | Unidades, nivel, barra de progreso, victoria/derrota (IMGUI). |
| `UI/MenuUI` | Menú y tienda de punto de partida (IMGUI). |
| **Editor** | |
| `Editor/ZombieDashSetup` | Menús *Zombie Rush → Crear escena de juego / de menú*. |
| `Editor/CreateGameData` | Menú *Zombie Rush → Crear datos de juego*. |
| `Editor/BuildAndroid` | Menú *Zombie Rush → Build APK (Android)*. |

> Dormantes del juego anterior (compilan, sin uso): `Player/PlayerController`,
> `Enemies/EnemySpawner`, `Combat/Pickup`, `Core/Upgrades`, `Data/UpgradeData`.

## Compilar APK (CLI)

```bash
/Applications/Unity/Hub/Editor/6000.4.9f1/Unity.app/Contents/MacOS/Unity \
  -quit -batchmode -nographics -projectPath "$(pwd)" \
  -buildTarget Android -executeMethod BuildAndroid.BuildAPK -logFile -
```
Genera `Builds/ZombieRush.apk` (IL2CPP+ARM64, portrait, package `com.luismiguel.zombierush`),
con **ambas escenas** (menú + juego). Instalar: `adb install -r Builds/ZombieRush.apk`.

## Siguientes pasos

- Validar diversión y **tunear balance** (curvas D/G, valores de gates, vida/cadencia).
- Pulir UI (IMGUI → uGUI) y meter arte/audio reales (la arquitectura ya lo permite).
- Publicación en Google Play (firma, ficha, testing cerrado).

## Notas técnicas

- Unity **6000.4.9f1**, 2D, Built-in RP, Android portrait.
- Arte/audio/UI **procedurales** (sin assets binarios → sin Git LFS aún).
- Desarrollo guiado por specs con **OpenSpec** (`openspec/`).
