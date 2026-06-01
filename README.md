# 🧟 Zombie Dash

Juego móvil arcade *dodge & shoot* en Unity 2D. El jugador se mueve lateralmente
para esquivar hordas de zombies mientras dispara en automático, y mejora su
arsenal entre partidas. Sesiones de 1–3 minutos, pensado para móvil vertical.

> Documentación completa (GDD, plan técnico, roadmap) en Notion → "Zombie Dash".

## Estado actual: Fases 0–4 completas (build jugable de extremo a extremo)

El juego está **mecánicamente completo**: tiene menú, tienda, progresión, varios
tipos de enemigo, dos armas, mini-jefes, feedback y sonido. Lo que todavía es
*placeholder* es el **vestido**: todo el arte, el audio y la UI están generados
por código (no hay assets binarios). Es bonito y honesto para validar el juego,
pero no es el aspecto final.

Implementado hasta ahora:

- ✅ **Core loop** — moverte arrastrando el dedo/ratón, disparo automático al
  zombie más cercano, daño de contacto, vida y *game over*.
- ✅ **Enemigos data-driven** — tipos Normal / Corredor / Tanque (ScriptableObjects)
  y oleadas con composición y ritmo propios.
- ✅ **Mini-jefe** — zombie enorme que aparece a los ~35 s y luego cada ~55 s.
- ✅ **Pickups** — monedas que sueltan los enemigos y cofres que caen; se recogen
  al contacto (tensión riesgo/recompensa: hay que ir a por ellos esquivando).
- ✅ **Meta-progresión** — banco de monedas persistente (PlayerPrefs), tienda de
  mejoras (daño, cadencia, vida, velocidad) con curva de coste creciente.
- ✅ **Armas** — Pistola (siempre) y Escopeta (se compra y equipa en la tienda).
- ✅ **Juicy** — sacudida de cámara, "partículas" de impacto, números de daño.
- ✅ **Audio** — SFX sintetizados y música chiptune en bucle (con toggle on/off).
- ✅ **Arte pixel** — sprites de pixel-art dibujados por código (jugador, zombies,
  balas, monedas, cofres) sustituyendo a las primitivas cuadradas.
- ✅ **Soporte de build Android** — APK de prueba generable desde el editor.

## Cómo abrirlo y jugar

1. Abre **Unity Hub** → *Add* → selecciona esta carpeta (`zombie-dash`).
   Usa **Unity 6000.4.9f1**.
2. En el editor, abre una escena (doble clic):
   - **`Assets/Scenes/MainMenu.unity`** → arranca en el menú con la tienda
     (recomendado para ver el flujo completo).
   - **`Assets/Scenes/Game.unity`** → entra directo a una partida.
3. Pulsa **Play**.
   - **Menú:** botón *JUGAR*, panel de *MEJORAS* y *ARMA* (compras gastando
     monedas del banco), toggle de música y *Reiniciar progreso* (para pruebas).
   - **En partida (ratón):** mantén pulsado y arrastra horizontalmente para mover
     al jugador. Los zombies bajan desde arriba; las balas salen solas. Recoge
     monedas y cofres. Si te alcanzan, pierdes vida; al morir vuelves al menú con
     las monedas ganadas en el banco.

> 💡 Para que se vea como en móvil, en la ventana *Game* elige una resolución
> vertical (p. ej. 1080x1920 o 9:16).

### La pregunta de fondo
El hito original era: **¿es mínimamente divertido?** Ahora hay mucho más con qué
responderla (progresión, variedad, jefes). Si algo no engancha, se ajusta el
balance antes de invertir en assets reales.

## Arquitectura (code-first)

Cada escena se monta por código desde su *bootstrap* (un único objeto), así no
hay que cablear nada en el editor. El balance se tunea con los campos públicos de
los componentes (`AutoShooter`, `EnemySpawner`, `PlayerController`) o, para datos,
editando los ScriptableObjects de `Assets/Resources/`.

Scripts en `Assets/Scripts/`:

| Script | Responsabilidad |
|---|---|
| **Core** | |
| `Core/GameBootstrap` | Monta la escena de juego (cámara, jugador, spawner, HUD y FX). |
| `Core/MenuBootstrap` | Monta la escena de menú (cámara, música y `MenuUI`). |
| `Core/GameManager` | Estado de la run: kills, tiempo, monedas, vida, game over (singleton). |
| `Core/Economy` | Banco de monedas persistente entre partidas (PlayerPrefs). |
| `Core/Upgrades` | Niveles de mejora persistentes; calcula valores y costes (usa `UpgradeData` o una tabla por defecto). |
| `Core/Weapons` | Armas desbloqueables (Pistola/Escopeta): comprar, equipar, persistir. |
| `Core/Prims` | Fábrica de objetos/sprites primitivos. |
| **Data** (ScriptableObjects) | |
| `Data/EnemyData` | Stats y aspecto de un tipo de zombie. |
| `Data/WaveData` | Composición (por peso) y ritmo de una oleada. |
| `Data/UpgradeData` | Curva de una mejora (valor base/por nivel y coste). |
| **Player** | |
| `Player/PlayerController` | Movimiento por arrastre + vida. |
| `Player/AutoShooter` | Apunta al enemigo más cercano y dispara según el arma equipada. |
| **Combat** | |
| `Combat/Bullet` | Proyectil: se mueve y daña al impactar. |
| `Combat/Pickup` | Monedas y cofres que caen y se recogen al contacto. |
| **Enemies** | |
| `Enemies/Enemy` | Zombie: avanza, daña al contacto, muere por balas, suelta monedas. Incluye variante mini-jefe. |
| `Enemies/EnemySpawner` | Reproduce las oleadas (`WaveData`) con dificultad creciente y lanza mini-jefes. |
| **FX** | |
| `FX/CameraShake` | Sacudida de cámara (screen shake). |
| `FX/HitEffect` | "Partículas" de impacto hechas con primitivas. |
| `FX/FloatingTextManager` | Números de daño flotantes (IMGUI). |
| `FX/Sfx` | Efectos de sonido sintetizados por código. |
| `FX/Music` | Loop chiptune procedural con toggle on/off. |
| `FX/PixelArt` | Sprites de pixel-art dibujados por código en `Texture2D`. |
| **UI** | |
| `UI/Hud` | HUD de la run (vida, kills, tiempo, monedas) con IMGUI. |
| `UI/MenuUI` | Menú principal: banco, jugar, tienda de mejoras y armas, música, reset (IMGUI). |
| **Editor** | |
| `Editor/ZombieDashSetup` | Menús *Zombie Dash → Crear escena de juego / de menú*. |
| `Editor/CreateGameData` | Menú *Crear datos de juego (enemigos y oleadas)*. |
| `Editor/BuildAndroid` | Menú *Build APK (Android)*. |

### Datos del juego
Los ScriptableObjects generados viven en `Assets/Resources/` y se cargan por
código con `Resources.LoadAll`:

- `Resources/Enemies/` → Normal, Corredor, Tanque.
- `Resources/Waves/` → Wave_01…Wave_04 (se reproducen en orden por nombre).
- `Resources/Upgrades/` → Damage, FireRate, MaxHealth, MoveSpeed.

Se regeneran con el menú *Zombie Dash → Crear datos de juego*.

## Siguientes pasos (roadmap)

Con las Fases 0–4 cerradas, lo que queda es **publicar** y **pulir el vestido**:

- **Fase 5 — Publicación:** probar en dispositivo Android real, firma de la app,
  ficha de Google Play (capturas, textos), *testing* cerrado y rollout.
- **Pulido de UI:** sustituir los menús/HUD de IMGUI (`OnGUI`) por un Canvas de
  uGUI propio.
- **Assets reales (opcional):** cambiar el arte pixel y el audio procedurales por
  un pack de sprites y SFX/música reales (la arquitectura ya lo deja enchufar).
- **Profundidad de juego:** más armas/enemigos, variedad y retención.

## Notas técnicas

- Unity **6000.4.9f1**, plantilla 2D, Built-in Render Pipeline.
- Input Manager clásico (sin paquete extra): ratón en editor, tacto en móvil.
- **Arte, audio y UI son procedurales (code-only)**: no hay assets binarios
  todavía, así que el repo no necesita Git LFS por ahora.
- Cuando se añadan assets binarios (sprites, audio), instalar **Git LFS**
  (`brew install git-lfs && git lfs install`) y trackear `*.png`, `*.wav`, etc.
- Desarrollo guiado por specs con **OpenSpec** (`openspec/`).
