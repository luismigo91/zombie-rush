## 1. Preparación y red de seguridad

- [ ] 1.1 Etiquetar el estado actual: `git tag pre-zombie-rush` (rollback al juego anterior)
- [ ] 1.2 Crear un sistema de pooling reutilizable (balas, unidades, zombies) en `Assets/Scripts/Core`
- [ ] 1.3 Decidir dónde vive el vertical slice (reusar escena `Game` con un bootstrap nuevo)

## 2. Vertical slice — loop jugable mínimo (squad + disparo recto + scroll + 1:1)

- [ ] 2.1 `SquadController`: recuento de unidades + formación blob con primitivas (resolver geometría del blob: recuento → ancho y → orden de bajas)
- [ ] 2.2 Movimiento del escuadrón solo en X con límites de pantalla (reusar input de `PlayerController`)
- [ ] 2.3 Reescribir `AutoShooter`: disparo recto automático hacia +Y (una bala por unidad de momento, desde el pool)
- [ ] 2.4 `LevelRunner` mínimo: scroll del mundo con un nivel hardcodeado
- [ ] 2.5 Adaptar `Enemy`/`EnemySpawner` a hordas que aparecen dentro del scroll
- [ ] 2.6 Combate 1:1: trigger en el frente del blob → resta 1 unidad y mata al zombie; escudo de la fila delantera emergente del orden
- [ ] 2.7 `GameManager`: derrota al llegar a 0 unidades, victoria al superar el clímax
- [ ] 2.8 HUD mínimo (IMGUI): nº de unidades y nivel actual
- [ ] 2.9 Compilar APK y **validar en el Pixel si el loop es divertido** antes de seguir

## 3. Elementos de recorrido (capacidad `course-elements`)

- [ ] 3.1 Gates en carriles con efectos: suma (+8), multiplicación (×2) y trampa (−5)
- [ ] 3.2 Elección por alineación: el escuadrón cruza el gate del carril con el que está alineado
- [ ] 3.3 Jaulas de supervivientes: liberar → suma unidades al recuento
- [ ] 3.4 Barreras destructibles con vida que bloquean hasta ser derribadas a tiros
- [ ] 3.5 Gate de arma: sube el tier del arma global durante el nivel (eje de calidad)

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
