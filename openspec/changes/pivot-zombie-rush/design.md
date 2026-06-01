## Context

El juego actual está montado *code-first*: `GameBootstrap` construye la escena `Game` por código, los enemigos caen hacia un héroe único que dispara con auto-aim al más cercano (`AutoShooter` recorre `Enemy.All`), y toda la progresión vive en la meta-tienda (`Economy`/`Upgrades`/`Weapons` sobre PlayerPrefs). Este cambio pivota a un **shooter de multitud con gates** (ver `proposal.md`) y reorienta la meta-tienda. Se mantiene la filosofía del proyecto: primitivas y placeholders generados por código, validar el loop antes de pulir.

Las capacidades a cubrir están en `specs/`: `squad`, `enemies-combat`, `course-elements`, `level-flow`, `meta-shop`.

## Goals / Non-Goals

**Goals:**
- Sustituir el héroe único por un **escuadrón-multitud** con disparo recto y crecimiento/pérdida de unidades dentro del nivel.
- Convertir la arena estática en un **runner vertical** con estructura de nivel y generación procedural de 100 niveles.
- Reorientar la meta-tienda a **punto de partida permanente**.
- Reaprovechar el máximo del código actual (enemigos, economía, FX, datos) y mantener el estilo code-first/placeholder-first.
- Validar pronto la diversión con un *vertical slice* antes de construir los 100 niveles.

**Non-Goals:**
- Arte/audio definitivos (seguimos con `PixelArt`/`Sfx`/`Music` procedurales).
- Migración a uGUI (la UI sigue en IMGUI por ahora).
- Monetización, ads, publicación en tienda (otra fase).
- Multijugador, base, 4X (fuera del alcance del juego).

## Decisions

### 1. Representación del escuadrón: GameObjects agrupados + pooling
Cada soldado es un GameObject ligero (sprite primitivo) gestionado por un `SquadController` que mantiene el recuento y recoloca la formación. El recuento de unidades es la **fuente de verdad**; los GameObjects son su representación visual, tomados de un pool.
- **Alternativa descartada (de momento)**: un único objeto "blob" con número abstracto y proxies puramente visuales. Más eficiente pero menos legible para iterar; se reconsiderará si el rendimiento lo exige.
- **Por qué**: code-first y legible para un proyecto en validación; el pool acota el coste.

### 2. Presupuesto de balas: pooling + fuego agregado si hace falta
Las balas salen de un pool. Si N unidades × cadencia genera demasiados proyectiles en móvil, el fuego se **agrega** (p. ej. solo dispara una franja representativa del frente, o el daño/anchura escala con el recuento sin una bala por soldado).
- **Por qué**: el feel de "muro de balas" no requiere una bala literal por soldado; desacoplar fuego de recuento es la palanca de rendimiento clave.

### 3. Scroll del mundo reutilizando el modelo actual "todo cae hacia el jugador"
El escuadrón permanece abajo; los elementos del recorrido y los zombies se desplazan hacia él, igual que hoy caen los enemigos en `EnemySpawner`. Un `LevelRunner` consume una `LevelDefinition` (secuencia ordenada por distancia) y va instanciando gates/jaulas/barreras/hordas a medida que "avanza" el nivel.
- **Por qué**: minimiza el cambio respecto al código actual y evita mover cámara/mundo real.

### 4. Generación procedural determinista
Un generador toma `(índiceNivel, semilla)` y produce una `LevelDefinition` con `System.Random` sembrado (no `Random.value`, para garantizar determinismo). La dificultad es función del índice (1..100) a partir de parámetros en un ScriptableObject (`GenParams`): densidad de hordas, fuerza de gates, frecuencia de jaulas/barreras, tipo de clímax.
- **Por qué**: 100 niveles a mano es inviable; determinismo permite reproducir y depurar un nivel.

### 5. Meta-tienda reorientada (reusa `Economy`, reemplaza `Upgrades`)
`Economy` (banco de monedas en PlayerPrefs) se mantiene. Las mejoras de % (`Upgrades`/`UpgradeData`) se **reemplazan** por compras de punto de partida (unidades iniciales, arma base, rendimiento de gates) persistidas en PlayerPrefs. `Weapons` se conserva/adapta como "arma base" del escuadrón.
- **Migración de datos**: limpiar las keys antiguas `upg_*` al actualizar; conservar `coins`.

### 6. Combate 1:1 por colisión en el frente
El contacto zombie↔escuadrón se resuelve con un trigger en el frente del blob: cada zombie que entra resta 1 unidad y muere. El "escudo de la fila delantera" **emerge** del orden de la formación (se elimina primero la unidad más adelantada).
- **Por qué**: regla simple y legible; encaja con el modelo de triggers 2D ya usado (bala/enemigo/pickup).

### 7. Mapa de reutilización del código
- **Reescribe**: `AutoShooter` → disparo recto por unidad/agregado; `GameManager` → victoria (superar clímax) / derrota (recuento 0) y avance de nivel.
- **Reorienta**: `Enemy`/`EnemySpawner` (hordas dentro del scroll, regla 1:1, jefe de clímax), `MenuUI`/`Hud` (mostrar nº de unidades y nivel), `Economy`/`Weapons`.
- **Nuevo**: `SquadController`, `LevelRunner`, generador + `LevelDefinition`/`GenParams`, gates, barreras, jaulas, sistema de pooling.
- **Reusa tal cual**: `Prims`/`PixelArt`/`FX`/`Sfx`/`Music`, patrón ScriptableObject en `Assets/Resources`.

### 8. Rename Zombie Dash → Zombie Rush
Actualizar README, CLAUDE.md, `PlayerSettings` (`productName`, `applicationIdentifier` → `com.luismiguel.zombierush`), nombres visibles y doc de Notion. Como la app **no está publicada**, cambiar el package id no rompe actualizaciones.

## Risks / Trade-offs

- **Rendimiento en móvil (unidades + balas + zombies)** → Pooling obligatorio desde el principio; desacoplar fuego del recuento (decisión 2); perfilar en el Pixel pronto.
- **Scope grande (pivote de género + 100 niveles)** → Construir primero un *vertical slice* con primitivas (1 nivel, escuadrón, disparo recto, 1:1, scroll) y validar diversión antes del generador. No construir los 100 niveles hasta que el loop enganche.
- **Determinismo del generador entre plataformas** → Usar RNG sembrado explícito; no usar `Random.value`/`Time` en la generación.
- **Rename rompe referencias** (nombres de escena, keys de PlayerPrefs) → Revisar con grep; mantener nombres de escena o actualizar la lista de escenas del build (`BuildAndroid` hoy solo empaqueta `Game.unity`).
- **Pérdida del juego anterior** → Es un pivote sobre repo personal; etiquetar (`git tag`) el estado pre-pivote para poder volver.

## Migration Plan

Por fases, cada una compilable y jugable (filosofía del proyecto):
1. **Vertical slice**: `SquadController` + disparo recto + scroll + 1 nivel hardcodeado + combate 1:1, con primitivas. → ¿es divertido?
2. **Elementos de recorrido**: gates en carriles, jaulas, barreras.
3. **Generador**: `LevelDefinition` + `GenParams` + curva de dificultad + 100 niveles + victoria/derrota/avance.
4. **Meta-tienda reorientada**: punto de partida; migración de keys PlayerPrefs.
5. **Rename + pulido + build**: Zombie Rush en todos los sitios; ajustar `BuildAndroid`/escenas; APK de prueba en dispositivo.

**Rollback**: `git tag pre-zombie-rush` antes de empezar; revertir a ese tag restaura el juego anterior.

## Open Questions

- **Formación del blob**: geometría exacta (¿círculo/“pila”/rejilla suelta?) y cómo se mapea recuento → ancho y → orden de bajas.
- **Generador**: parámetros concretos y forma de la curva de dificultad a lo largo de 100 niveles; ¿niveles-jefe especiales cada N?
- **Armas**: ¿sobreviven pistola/escopeta y cómo encajan con "unidades" (arma global del escuadrón vs por unidad)?
- **Balance numérico**: vida de zombies, cadencia, valores de gates, longitud de nivel.
- **Presupuesto de balas**: ¿una bala por unidad hasta cierto N y luego fuego agregado? Punto de corte a perfilar en dispositivo.
