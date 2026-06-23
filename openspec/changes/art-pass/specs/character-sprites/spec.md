## ADDED Requirements

### Requirement: Spritesheets animados de personajes
El soldado, el zombie y el jefe SHALL usar spritesheets cargados desde `Resources/Art/characters/` con más frames que el procedural actual (mínimo 4–8 de caminar, 2–4 de disparar, 3–5 de morir si el sheet los trae). La animación MUST reproducirse vía `SpriteAnim` (ya existente) consumiendo el array de `Sprite` cargado.

#### Scenario: Soldado camina
- **WHEN** el escuadrón se mueve en X
- **THEN** cada unidad reproduce el ciclo de marcha del spritesheet (no el `SoldierMarch` procedural de 3 frames)

#### Scenario: Zombie camina
- **WHEN** un zombie baja por la pantalla
- **THEN** reproduce el ciclo de shamble del spritesheet cargado

#### Scenario: Boss idle
- **WHEN** el jefe aparece en un nivel múltiplo de 10
- **THEN** muestra un ciclo de idle (o el frame estático del sheet si no hay anim)

### Requirement: Tinte por tipo de zombie conservado
Los tres tipos de zombie (normal, runner, tank) SHALL distinguirse por color, igual que hoy. Si el sheet elegido es en grises, se tinta con `SpriteRenderer.color` por tipo; si el sheet es a color, se usan 3 paletas de tintado o 3 sheets.

#### Scenario: Zombie normal verde
- **WHEN** se spawnea un zombie normal
- **THEN** su `SpriteRenderer.color` lo tiñe de verde (`#5BD66A` o equivalente)

#### Scenario: Zombie runner amarillo
- **WHEN** se spawnea un zombie runner
- **THEN** su tinte es amarillo (distinto al normal)

#### Scenario: Zombie tank morado
- **WHEN** se spawnea un zombie tank
- **THEN** su tinte es morado (distinto a los anteriores)

### Requirement: Frames cargados desde Resources
Los frames de los sheets SHALL cargarse con `Resources.LoadAll<Sprite>` (sheet rebana­do por Unity) o con `Sprite.Create` sub-rects por código si el sheet viene como textura única. El resultado MUST cachearse en `ArtCache` y servir arrays de `Sprite` por nombre de animación.

#### Scenario: LoadAll de un sheet Multiple
- **WHEN** `ArtCache` carga `characters/zombie_shamble`
- **THEN** `Resources.LoadAll<Sprite>` devuelve los frames en orden y se cachea como `Sprite[]`

#### Scenario: Sheet como textura única
- **WHEN** un sheet viene sin rebana­do (una sola textura)
- **THEN** el loader lo corta por código con `Sprite.Create` sub-rects según coordenadas documentadas y devuelve el `Sprite[]`

### Requirement: Reemplazo de PixelArt en gameplay
`Squad`, `Enemy` y el jefe SHALL obtener sus sprites de `ArtCache` (spritesheets), no de `PixelArt`. `PixelArt.cs` MAY conservarse como fallback para sprites sin reemplazo (p. ej. bala, muzzle) y se decide su eliminación al cierre del cambio.

#### Scenario: Squad usa ArtCache
- **WHEN** `Squad` instancia una unidad
- **THEN** su `SpriteRenderer.sprite` viene de `ArtCache.Get("characters/soldier_march")`, no de `PixelArt.SoldierMarch`

#### Scenario: Fallback si falta asset
- **WHEN** un sprite de personaje no existe en `Resources/Art/characters/`
- **THEN** se usa el equivalente de `PixelArt` y se loguea un warning (no crash)

### Requirement: Animaciones de muerte (si el sheet las trae)
Si los sheets incluyen frames de muerte, `Enemy` y soldado SHALL reproducir una animación de muerte breve antes de desaparecer (en vez del `Gore` instantáneo actual, que se conserva como VFX superpuesto).

#### Scenario: Zombie muere con anim
- **WHEN** un zombie pierde toda su vida
- **THEN** reproduce 3–5 frames de muerte antes de desactivarse, con `Vfx.Gore` superpuesto

#### Scenario: Sheet sin muerte
- **WHEN** el sheet de un personaje no tiene frames de muerte
- **THEN** se conserva el comportamiento actual (desaparación + `Vfx.Gore`)
