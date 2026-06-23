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

### 1. Representación y geometría del escuadrón: GameObjects + pooling, formación disco/blob (√N)
Cada soldado es un GameObject ligero (sprite primitivo) gestionado por un `SquadController` que mantiene el recuento y recoloca la formación. El recuento de unidades es la **fuente de verdad**; los GameObjects son su representación visual, tomados de un pool.

**Formación: disco/blob.** Las unidades se distribuyen en un disco cuyo **ancho (diámetro) crece con √N hasta un tope** (~70-80% del ancho jugable). Mientras el ancho no llega al tope, más unidades = más cobertura horizontal de fuego; superado el tope, el ancho se mantiene y el exceso de unidades **aumenta la densidad** por columna (ver decisión 2). Las bajas erosionan el blob **por el frente** (se elimina la unidad más adelantada de la columna impactada), de donde emerge el escudo de la fila delantera. El movimiento lateral usa un suavizado ligero (cada soldado persigue su hueco) para dar sensación de masa viva.
- **Alternativas descartadas**: *banda* (ancho ∝ N) → se sale de pantalla a N grande; *rejilla rígida* → no se siente multitud; *cuña* → frente demasiado estrecho. El disco ∝ √N crece suave, se mantiene en pantalla y da erosión legible.
- **Por qué GameObjects+pool**: code-first y legible para iterar; el pool acota el coste. Se reconsideraría un blob abstracto con proxies solo si el rendimiento lo exige.

### 2. Fuego por columna ocupada con densidad (resuelve el presupuesto de balas)
El escuadrón **no dispara una bala por soldado**: emite fuego **por columna ocupada del blob**, y el daño/cadencia de cada columna **escala con cuántas unidades hay detrás** en esa columna (la densidad). Las balas salen de un pool.
- **Por qué**: con la formación disco ∝ √N muchos soldados comparten columna; una bala por soldado sería redundante y cara. El "muro de balas" se siente igual y el coste queda acotado al nº de columnas, no al nº de soldados. Esto ata la geometría (decisión 1) con el rendimiento.

### 3. Scroll del mundo reutilizando el modelo actual "todo cae hacia el jugador"
El escuadrón permanece abajo; los elementos del recorrido y los zombies se desplazan hacia él, igual que hoy caen los enemigos en `EnemySpawner`. Un `LevelRunner` consume una `LevelDefinition` (secuencia ordenada por distancia) y va instanciando gates/jaulas/barreras/hordas a medida que "avanza" el nivel.
- **Por qué**: minimiza el cambio respecto al código actual y evita mover cámara/mundo real.

### 4. Generación procedural: híbrido de trozos, 100 niveles fijos, jefes cada 10
Un generador toma `(n, semillaGlobal)` y produce una `LevelDefinition` (secuencia de encuentros ordenados por distancia) de forma **determinista** con `System.Random` sembrado por nivel (`seed = f(semillaGlobal, n)`, nunca `Random.value`). La **semilla global es constante** → los 100 niveles son **fijos**: una campaña que todos juegan igual, se puede balancear y comentar (no un re-roll por partida).

**Enfoque híbrido (gramática de trozos).** Se diseñan ~8-12 *trozos* de encuentro con buen ritmo interno (p. ej. "encrucijada de gates dobles", "sala de rescate con horda", "embudo de tanques", "pasillo de barreras"). El generador **encadena** trozos elegidos según dificultad y **escala sus parámetros internos** con dos presupuestos por nivel:
- `D(n)` = presupuesto de **amenaza** (zombies, dureza de barreras, % de gates-trampa).
- `G(n)` = presupuesto de **crecimiento** (gates +/×, jaulas).

La dificultad percibida ≈ `D(n)/G(n)`. Amenaza y recompensa se **intercalan en onda** (tensión → alivio → tensión mayor → clímax), no de forma uniforme. Presupuestos y tabla de trozos viven en un ScriptableObject `GenParams`.

**Macro-estructura por actos de 10.** El acto 1 (niveles 1-10) introduce las mecánicas **de una en una** (gates → jaulas → barreras → tanques → primer jefe), como **tutorial implícito** sin texto. Cada 10 niveles hay un **nivel-jefe** especial (hito). La curva `D(n)` sube **escalonada por acto**, no en recta.

**Balancear contra el punto de partida MÍNIMO.** Como el escuadrón reinicia cada nivel y los gates son el gran ecualizador, los niveles MUST ser superables desde el punto de partida base (sin compras). Lo comprado en la meta-tienda da **margen**, no es requisito.
- **Por qué**: 100 niveles a mano es inviable; el híbrido da ritmo diseñado + escalado automático; el determinismo permite reproducir/depurar un nivel concreto; los presupuestos `D/G` dan una sola palanca de dificultad.

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

### 8. Armas: arma global del escuadrón por tiers (segundo eje de crecimiento)
El arma es **global al escuadrón**, no por unidad: todos los soldados disparan el mismo arma activa y el **daño/cadencia por columna escala con la densidad** (decisión 2). El arma tiene **tiers** (pistola → … → escopeta) que varían daño/cadencia/perforación manteniendo el **fuego hacia el frente** (sin auto-aim). Pistola y escopeta actuales sobreviven como tiers (la escopeta = tier alto con más cobertura/perforación, reusando el código existente).

Esto introduce un **segundo eje de crecimiento** en la run, además de la cantidad de unidades:
- **Cantidad** (nº de soldados) → ancho/densidad → cobertura y DPS bruto. Crece con gates +/× y jaulas.
- **Calidad** (tier de arma) → potencia por disparo. Crece con **gates de arma** (un tipo de gate que sube el tier durante el nivel) y con el arma base comprada en la meta-tienda (punto de partida).

La decisión por segundo se enriquece: a veces el jugador elige entre un gate de "+unidades" y uno de "subir arma" (ensanchar el muro vs mejorar cada disparo). El presupuesto de crecimiento `G(n)` del generador reparte entre ambos ejes.
- **Alternativa descartada (arma por unidad)**: un escuadrón con armas mezcladas rompe el "fuego por columna con densidad" (¿qué dispara una columna mixta?), reintroduce el problema de presupuesto de balas y añade microgestión (perder un soldado "valioso"). Demasiada complejidad antes de validar el core; se podría explorar tras el MVP.
- **Por qué**: encaja con la densidad ya decidida, reusa pistola/escopeta, mantiene el escuadrón legible y **cumple la petición original de "mejorar las armas en la misma run"** como eje ortogonal a las unidades.

### 9. Rename Zombie Dash → Zombie Rush
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

- **Balance numérico** (tuning, no arquitectura): vida de zombies, cadencia, valores de gates, longitud de nivel, tope de ancho del blob, tiers de arma, y la forma concreta de las curvas `D(n)`/`G(n)` + diseño de los ~8-12 trozos del generador.
- **Tuning de formación**: factor exacto de √N, suavizado del movimiento y nº de columnas de disparo (a perfilar en dispositivo).
