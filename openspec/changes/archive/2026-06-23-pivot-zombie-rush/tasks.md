## 1. Preparación y red de seguridad

- [x] 1.1 Etiquetar el estado actual: `git tag pre-zombie-rush` (rollback al juego anterior)
- [x] 1.2 Pooling de balas (`Bullet` con pool estático, fase 6). Unidades/zombies (menos numerosos) sin poolear de momento
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

- [x] 4.1 Modelo `LevelDefinition` + `LevelEvent` (encuentros por tiempo: horda/gates/jaula/barrera)
- [x] 4.2 Presupuestos `D(n)`/`G(n)` embebidos como constantes en `LevelGenerator` (en vez de SO, para no depender del editor; migrable a `GenParams` SO)
- [x] 4.3 `LevelGenerator` híbrido determinista: beats amenaza/recompensa en onda; semilla GLOBAL constante (100 fijos), `seed=f(global,n)` con `System.Random`
- [x] 4.4 Actos de 10: mecánicas introducidas progresivamente (tutorial implícito); jefe cada 10; `Campaign` persiste el nivel
- [ ] 4.5 Verificar (en Unity) determinismo y que cada nivel sea superable desde el punto de partida MÍNIMO ← requiere playtest

## 5. Meta-tienda reorientada (capacidad `meta-shop`)

- [x] 5.1 `StartingPoint` reemplaza los % stats: compras de soldados iniciales y arma base (tiers en `Weapons`). (El "rendimiento de gates" como compra queda fuera de alcance por ahora.)
- [x] 5.2 Punto de partida aplicado al inicio de cada nivel (`Squad` usa `StartUnits`; `GameManager` el tier base)
- [x] 5.3 Migración PlayerPrefs (constructor estático de `StartingPoint`): borra `upg_*`/`wpn_*`, conserva `coins`
- [x] 5.4 `MenuUI` reescrito a la tienda de punto de partida; `Economy` (banco) sigue funcionando; monedas por kill/nivel

## 6. Rendimiento y balance

- [x] 6.1 Pooling de balas (`Bullet`); decidido fuego por streams (no una bala por unidad). Pooling de unidades/zombies + perfilado en el Pixel: pendiente si hace falta
- [ ] 6.2 Tunear balance (vida zombies, cadencia, valores de gates, longitud, tope de ancho) ← requiere playtest

## 7. Rename a Zombie Rush

- [x] 7.1 `ProjectSettings`: `productName` = "Zombie Rush", `applicationIdentifier` = `com.luismiguel.zombierush` (y en `BuildAndroid`)
- [x] 7.2 `README.md` y `CLAUDE.md` reescritos para Zombie Rush
- [x] 7.3 Nombres visibles: `MenuUI` ("ZOMBIE RUSH") y menús de editor (`Zombie Rush → ...`)
- [x] 7.4 `BuildAndroid`: ambas escenas en el APK (menú+juego), salida `ZombieRush.apk`; `openspec/config.yaml` actualizado
- [x] 7.5 Memoria del proyecto y **Notion** actualizados (título → Zombie Rush + aviso de pivote; GDD/Plan/Roadmap quedan como histórico)

## 8. Cierre del cambio

- [x] 8.1 APK del pivote construido (`Builds/ZombieRush.apk`, BUILD_OK) e instalado en el dispositivo. Jugarlo/validar = hito 2.9
- [ ] 8.2 Commit por hito y, al terminar, archivar el cambio (`/opsx:archive`)
