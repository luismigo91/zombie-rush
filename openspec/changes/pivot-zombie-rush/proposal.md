## Why

El juego actual (Zombie Dash) tiene una **run plana**: la única progresión vive en la meta-tienda y dentro de la partida solo sube la dificultad, nunca tu poder. Eso deja sin responder la pregunta de fondo del proyecto ("¿es divertido?") porque falta la fantasía central del subgénero que nos inspira (Last Z): empezar débil y convertirte en una marea de fuego durante la propia partida. Este cambio pivota el juego hacia un **shooter de multitud con gates** (runner vertical) y lo renombra a **Zombie Rush**, donde el verbo central deja de ser *esquivar* y pasa a ser *crecer + arrasar*.

## What Changes

- **BREAKING** — Cambio de género: de "un héroe que esquiva con auto-aim" a un **escuadrón de unidades en formación de multitud** que dispara recto hacia arriba y crece/pierde unidades durante el nivel.
- **BREAKING** — La meta-tienda deja de dar boosts de % (daño/cadencia/vida/velocidad) y pasa a definir el **punto de partida permanente** (unidades iniciales, arma base, rendimiento de gates). La run amplifica ese punto de partida.
- Mundo con **scroll vertical** y estructura de nivel (inicio → recorrido → clímax con jefe/horda final), en lugar de la arena estática endless actual.
- **100 niveles generados** proceduralmente con una curva de dificultad (generador con semilla, no mapas a mano). El escuadrón **reinicia** al punto de partida en cada nivel.
- Elementos de recorrido nuevos: **gates en carriles** (×2, +8, trampas −), **jaulas** con supervivientes que se rescatan (+unidades) y **barreras con vida** que se derriban a tiros.
- **Combate 1:1**: cada zombie que alcanza el frente del escuadrón mata exactamente 1 soldado (y el zombie muere); los soldados de delante escudan a los de detrás. Derrota = escuadrón a 0; victoria = superar el nivel.
- **Rename** del proyecto y la app: "Zombie Dash" → "Zombie Rush" (README, CLAUDE.md, identidad de la app/package, doc en Notion).
- **Técnico**: el *object pooling* deja de ser opcional (unidades + balas + zombies simultáneos en móvil).

## Capabilities

### New Capabilities
- `squad`: el escuadrón controlable — formación de multitud (blob), movimiento solo en X, disparo recto automático, recuento de unidades como recurso central y reinicio por nivel.
- `enemies-combat`: hordas de zombies y sus tipos dentro del recorrido, regla de contacto 1:1 (1 zombie = −1 soldado, escudo de la fila delantera) y jefe/horda final de nivel.
- `course-elements`: elementos del recorrido que modifican el escuadrón — gates en carriles (×/+/trampa), jaulas de supervivientes (rescate) y barreras destructibles.
- `level-flow`: scroll vertical, estructura de nivel (inicio/recorrido/clímax), generación procedural de 100 niveles con curva de dificultad, y condiciones de victoria/derrota.
- `meta-shop`: meta-progresión persistente reorientada al punto de partida permanente (unidades iniciales, arma base, mejoras de rendimiento de gates).

### Modified Capabilities
<!-- No existen specs previas en openspec/specs/; no hay capacidades a modificar. -->

## Impact

- **Código que se reescribe**: `AutoShooter` (de auto-aim al más cercano → disparo recto por unidad), condiciones de derrota/victoria en `GameManager`.
- **Código que se reorienta**: `Economy`/`Upgrades`/`Weapons` (de % stats → punto de partida), `EnemySpawner`/`Enemy` (hordas dentro del scroll, regla 1:1), `MenuUI`/`Hud` (nuevos datos: nº de unidades, nivel actual).
- **Código nuevo**: sistema de escuadrón (formación blob, alta/baja de unidades), scroll del mundo + `LevelData`/generador procedural, gates, barreras y jaulas.
- **Datos**: nuevos ScriptableObjects en `Assets/Resources` (parámetros de generación, gates, supervivientes); revisión de los actuales (Upgrades, Waves).
- **Identidad/branding**: rename a Zombie Rush en README, CLAUDE.md, `PlayerSettings` (productName, applicationIdentifier), escenas y doc de Notion.
- **Rendimiento**: introducción de object pooling (antes en backlog "solo si hace falta").
- **Filosofía intacta**: code-first/placeholder-first, comentarios en español, empezar simple e iterar; validar el nuevo loop con primitivas antes de pulir arte.
