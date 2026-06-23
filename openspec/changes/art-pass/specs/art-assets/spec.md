## ADDED Requirements

### Requirement: Assets cargados por código desde Resources
Los assets de arte (PNG, spritesheets) SHALL colocarse en `Assets/Resources/Art/<categoria>/` y cargarse con `Resources.Load`/`Resources.LoadAll`, cacheados en un `ArtCache` estático. Los bootstraps MUST pedir los sprites por nombre. Nada se cablea en el Inspector.

#### Scenario: Bootstrap pide un sprite
- **WHEN** `GameBootstrap` necesita el sprite del suelo
- **THEN** llama a `ArtCache.Get("environment/road_tile")` y recibe el `Sprite` cacheado o cargado desde `Resources/Art/environment/`

#### Scenario: Sin cableado Inspector
- **WHEN** se inspecciona un bootstrap en el editor
- **THEN** no hay campos `Sprite`/`Material` arrastrados a mano; todo se resuelve por nombre en tiempo de ejecución

### Requirement: Git LFS para binarios
El repo SHALL tener Git LFS activado con `.gitattributes` que cubra `*.png`, `*.psd`, `*.jpg`, `*.tga`, `*.tif`, `*.wav`, `*.psb`. CLAUDE.md MUST documentar que `git lfs install` es obligatorio tras un clone.

#### Scenario: PNG commiteado via LFS
- **WHEN** se añade un nuevo tileset PNG al repo
- **THEN** `git lfs ls-files` lo lista y el commit no incluye el binario en plano

#### Scenario: Clone fresco
- **WHEN** un desarrollador clona el repo y corre `git lfs install` + `git lfs pull`
- **THEN** todos los PNG de `Assets/Resources/Art/` están presentes y el proyecto compila

### Requirement: Licencias y atribución
Todo asset externo SHALL ser **CC0** o **CC4-BY** (con atribución). Se prohíben licencias *No-Derivatives* (CC-ND). Un archivo `Assets/Resources/Art/ATTRIBUTION.md` MUST listar cada asset, su autor, licencia y URL.

#### Scenario: Asset CC0
- **WHEN** se importa un tileset de Kenney (CC0)
- **THEN** se añade una entrada a `ATTRIBUTION.md` con autor "Kenney" y licencia "CC0"

#### Scenario: Asset CC4-BY
- **WHEN** se importa un spritesheet de itch.io con licencia CC4-BY
- **THEN** `ATTRIBUTION.md` incluye autor, URL y texto de atribución requerido

#### Scenario: Asset sin licencia clara
- **WHEN** se encuentra un asset sin licencia declarada o con CC-ND
- **THEN** se descarta y no se commitea

### Requirement: Caché de sprites
`ArtCache` SHALL cachear los sprites cargados para evitar `Resources.Load` repetido. Si un sprite solicitado no existe, MUST devolver un fallback procedural (de `PixelArt` mientras exista) o `null` y loguear un warning.

#### Scenario: Caché hit
- **WHEN** se pide un sprite ya cargado
- **THEN** se devuelve desde el caché sin acceder a disco otra vez

#### Scenario: Sprite ausente
- **WHEN** se pide `ArtCache.Get("no_existe")`
- **THEN** se loguea un warning y se devuelve un fallback visible (no un crash)
