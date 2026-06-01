## 1. Preparación y red de seguridad

- [x] 1.1 Etiquetar el estado actual: `git tag pre-zombie-rush` (rollback al juego anterior)
- [ ] 1.2 Crear un sistema de pooling reutilizable (balas, unidades, zombies) — APLAZADO al slice: el fuego por streams ya acota las balas; el pooling es de la fase de rendimiento
- [x] 1.3 Decidir dónde vive el vertical slice (se reescribe `GameBootstrap` en la escena `Game`)

## 2. Vertical slice — loop jugable mínimo (squad + disparo recto + scroll + 1:1)

- [x] 2.1 `Squad`: recuento de unidades + formación blob disco (√N) con primitivas; erosión por el frente (máxima Y)
- [x] 2.2 Movimiento del escuadrón solo en X con límites de pantalla (arrastre, reutilizando el patrón de `PlayerController`)
- [x] 2.3 `SquadShooter` (reemplaza a `AutoShooter`): disparo recto hacia +Y por streams con daño escalado por densidad
- [x] 2.4 `LevelRunner` mínimo: scroll del mundo con un nivel hardcodeado (duración fija → victoria)
- [x] 2.5 Adaptar `Enemy` a hordas que bajan hacia el escuadrón (spawn desde `LevelRunner`)
- [x] 2.6 Combate 1:1: al alcanzar el frente del blob → resta 1 unidad y muere el zombie; escudo frontal emergente del orden
- [x] 2.7 `GameManager`: derrota al llegar a 0 unidades, victoria al completar el nivel (estado `Won`)
- [x] 2.8 HUD mínimo (IMGUI): nº de unidades, nivel y barra de progreso; pantallas de victoria/derrota
- [ ] 2.9 Compilar/abrir en Unity y **validar si el loop es divertido** antes de seguir

## 3. Elementos de recorrido (capacidad `course-elements`)

- [x] 3.1 Gates con efectos: suma (+N), multiplicación (×N) y trampa (−N) — `Gate`/`GateEffect`
- [x] 3.2 Elección por alineación: el gate aplica solo si el escuadrón está alineado con su carril
- [x] 3.3 Jaulas de supervivientes (`Cage`): derribar a tiros → suma unidades
- [x] 3.4 Barreras destructibles (`Barrier`) con vida; si llegan intactas arrasan parte del frente
- [x] 3.5 Gate de arma: sube el tier del arma global (`GameManager.RaiseWeaponTier`)

## 4. Generación procedural de niveles (capacidad `level-flow`)

- [ ] 4.1 Modelo `LevelDefinition` (encuentros por distancia) + tabla de ~8-12 *trozos* de encuentro reutilizables
- [ ] 4.2 `GenParams` (ScriptableObject): presupuestos `D(n)` (amenaza) y `G(n)` (crecimiento) por nivel
- [ ] 4.3 Generador híbrido determinista: encadena trozos escalados por `D/G` e intercala amenaza/recompensa en onda; semilla GLOBAL constante (100 niveles fijos), `seed = f(global, n)`, nunca `Random.value`
- [ ] 4.4 Macro-estructura por actos de 10: acto 1 introduce mecánicas de una en una (tutorial implícito); nivel-jefe cada 10
- [ ] 4.5 Verificar determinismo (mismo n → misma disposición) y que cada nivel sea superable desde el punto de partida MÍNIMO

## 5. Meta-tienda reorientada (capacidad `meta-shop`)

- [ ] 5.1 Reemplazar `Upgrades`/`UpgradeData` (% stats) por compras de punto de partida: unidades iniciales, arma base (tier inicial del arma global; pistola/escopeta como tiers) y rendimiento de gates
- [ ] 5.2 Aplicar el punto de partida al inicio de cada nivel (reset del escuadrón)
- [ ] 5.3 Migración PlayerPrefs: limpiar keys `upg_*`, conservar `coins`
- [ ] 5.4 Actualizar `MenuUI` al catálogo nuevo; mantener `Economy` (banco) funcionando

## 6. Rendimiento y balance

- [ ] 6.1 Asegurar pooling de unidades, balas y zombies; decidir punto de corte "una bala por unidad" vs fuego agregado perfilando en el Pixel
- [ ] 6.2 Tunear balance: vida de zombies, cadencia, valores de gates y longitud de nivel

## 7. Rename a Zombie Rush

- [ ] 7.1 `PlayerSettings`: `productName` = "Zombie Rush", `applicationIdentifier` = `com.luismiguel.zombierush`
- [ ] 7.2 Actualizar `README.md` y `CLAUDE.md`
- [ ] 7.3 Nombres visibles en UI y en los menús de editor (`Zombie Dash → ...` → `Zombie Rush → ...`)
- [ ] 7.4 Revisar nombres de escena y la lista de escenas de `BuildAndroid` (incluir `MainMenu` si el menú debe ir en el APK)
- [ ] 7.5 Actualizar la doc de Notion y la memoria del proyecto

## 8. Cierre del cambio

- [ ] 8.1 Compilar el APK del pivote y probarlo en dispositivo real
- [ ] 8.2 Commit por hito y, al terminar, archivar el cambio (`/opsx:archive`)
