## ADDED Requirements

### Requirement: Render pipeline URP 2D
El proyecto SHALL usar **Universal Render Pipeline** con un **2D Renderer** como pipeline activo, reemplazando el Built-in RP. El URP Asset y el Renderer Data MUST generarse por editor-script (o, si no es factible, versionarse en `Assets/Settings/` y referenciarse por path desde el setup), sin cableado manual en el Inspector.

#### Scenario: Build Android con URP
- **WHEN** se ejecuta `BuildAndroid.BuildAPK` por CLI
- **THEN** el APK se genera con URP 2D como pipeline y arranca sin errores en dispositivo Pixel

#### Scenario: Editor sin Inspector
- **WHEN** se abre el proyecto en el editor tras un clone limpio
- **THEN** `GraphicsSettings.m_CustomRenderPipeline` apunta al URP Asset sin que el usuario lo asigne a mano

### Requirement: Color space Linear
El proyecto SHALL usar `ColorSpace.Linear` en `PlayerSettings`. La paleta existente (en `PixelArt`, `Environment`, `Vfx`, `UGui`) MUST revisarse visualmente tras el cambio y retocar los hex/colores que queden apagados.

#### Scenario: Degradados correctos
- **WHEN** se renderiza el cielo de `Environment` en Linear
- **THEN** el degradado `#14122A → #241A3A` se muestra sin banding sucio

#### Scenario: Sprites tintados
- **WHEN** un zombie tintado por tipo se renderiza en Linear
- **THEN** el tinte se percibe con la saturación esperada (no apagado); si no, se retoca el hex en el código

### Requirement: Volumen de post-proceso
El juego SHALL tener un `Volume` URP (global) con overrides de **Bloom**, **Vignette**, **ColorAdjustments** y **FilmGrain**. El volume MUST crearse por código desde el bootstrap o cargarse desde un prefab en `Resources/` (excepción controlada a code-first).

#### Scenario: Bloom en neón
- **WHEN** se dispara una bala o se muestra el muzzle
- **THEN** el brillo amarillo/naranja produce halo de Bloom (threshold alto para que solo brille el neón, no el fondo)

#### Scenario: Vignette reemplaza sprite
- **WHEN** se renderiza el frame
- **THEN** el oscurecimiento de bordes viene de la Vignette del volume URP, no de un sprite en `Environment.cs`

### Requirement: Light2D para acentos
El juego MAY usar `Light2D` en farolas del entorno y en el jefe. Si el fill-rate en Pixel regresa, se limita a jefe y muzzle (muzzle via Bloom, no Light2D).

#### Scenario: Farola ilumina
- **WHEN** una farola del entorno está en pantalla
- **THEN** proyecta una luz ámbar tenue sobre el suelo vía `Light2D` (no un sprite de halo)

#### Scenario: Rendimiento móvil
- **WHEN** hay varias `Light2D` activas en Pixel
- **THEN** el FPS se mantiene ≥ 30; si baja, se reducen luces hasta estabilizar
