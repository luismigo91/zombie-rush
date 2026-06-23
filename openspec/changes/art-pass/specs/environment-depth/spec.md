## ADDED Requirements

### Requirement: Fondo de ciudad en capas de parallax
`Environment` SHALL construir el fondo en **al menos 5 capas** con `sortingOrder` y factor de parallax distintos: cielo, skyline lejano, edificios medios, calle/props cercanos, suelo con carriles. Una capa opcional de niebla/polvillo MAY añadirse por encima.

#### Scenario: Capas visibles en pantalla
- **WHEN** se juega en portrait 9:16
- **THEN** se distinguen al menos cielo, skyline, edificios y suelo a la vez, dando sensación de profundidad

#### Scenario: Parallax diferenciado
- **WHEN** el scroll avanza a velocidad `v`
- **THEN** el skyline se mueve más lento que los edificios medios y estos más lento que el suelo (factores de parallax escalonados)

### Requirement: Scroll y reciclado por código
El scroll vertical SHALL gestionarse por código (como hoy): los elementos bajan y se reciclan al salir por abajo. El gating por `GameState.Playing` (juego) o continuo (menú) MUST conservarse. La cámara no se mueve en Y.

#### Scenario: Bucle infinito sin costuras
- **WHEN** el jugador avanza indefinidamente
- **THEN** las capas se reciclan sin huecos ni saltos visibles

#### Scenario: Pausa en menú/juego
- **WHEN** `GameManager.State != Playing` en la escena de juego
- **THEN** el scroll se detiene; en la escena de menú el scroll sigue lento

### Requirement: Sprites cargados, no generados
Las capas (skyline, edificios, calle, props) SHALL usar sprites cargados desde `Resources/Art/environment/` (vía `ArtCache`), no generados por `Texture2D`. El asfalto y líneas de carril MAY seguir procedurales si encajan con el tileset, o migrar al tileset.

#### Scenario: Props desde Resources
- **WHEN** `Environment` recicla un prop (árbol/farola/escombro)
- **THEN** su sprite viene de `ArtCache`, no de un `MakeDeadTreeSprite`/`MakeLampSprite`/`MakeRubbleSprite` procedural

#### Scenario: Suelo tileset
- **WHEN** se renderiza el suelo
- **THEN** usa el tileset de `Resources/Art/environment/` (o el asfalto procedural si se decide conservarlo)

### Requirement: Viñeta por URP, no por sprite
La viñeta SHALL venir del **Vignette del volume URP** (`render-pipeline`). El sprite de viñeta actual en `Environment.cs` MUST eliminarse.

#### Scenario: Sin sprite de viñeta
- **WHEN** se inspecciona la jerarquía del `Environment`
- **THEN** no existe un GameObject "Vignette"; el oscurecimiento de bordes lo aporta el volume URP

### Requirement: Light2D en farolas
Las farolas del entorno MAY proyectar luz ámbar tenue vía `Light2D` (paquete `render-pipeline`). Si el rendimiento en Pixel lo impide, se limita al jefe o se elimina.

#### Scenario: Farola con Light2D
- **WHEN** una farola se recicla al borde superior
- **THEN** lleva un `Light2D` hijo configurado en color ámbar `#E8A23A` y radio moderado

#### Scenario: Sin Light2D si baja FPS
- **WHEN** el FPS en Pixel baja de 30 con varias `Light2D`
- **THEN** se desactivan las `Light2D` de farolas y se conserva solo la del jefe (o ninguna)
