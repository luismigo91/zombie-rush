## ADDED Requirements

### Requirement: Migración de MenuUI a uGUI
`MenuUI` (menú principal + tienda) SHALL reconstruirse con uGUI usando la librería `UGui.cs` (canvas scaler 720×1280, match 0.5, TMP, paneles/botones/barras/iconos). La implementación en `OnGUI` MUST eliminarse al terminar la migración de este componente.

#### Scenario: Menú se dibuja con uGUI
- **WHEN** se entra en la escena `MainMenu`
- **THEN** el menú se renderiza con `Canvas` + `Image` + `TextMeshProUGUI` (no con `OnGUI`)

#### Scenario: Tienda funcional
- **WHEN** el jugador abre la tienda desde el menú
- **THEN** los items, precios y botones comprables se muestran con uGUI y responden a toque con `Button.onClick`

#### Scenario: Sin OnGUI en MenuUI
- **WHEN** se inspecciona `MenuUI.cs` tras la migración
- **THEN** no queda ningún `OnGUI()` ni `GUI.*`; todo es uGUI

### Requirement: Migración de Hud a uGUI
`Hud` (HUD de partida: nivel, monedas, unidades, barra de progreso) SHALL migrarse a uGUI con `UGui.cs`. El `OnGUI` de `Hud` MUST eliminarse al terminar.

#### Scenario: HUD legible en partida
- **WHEN** se juega un nivel
- **THEN** los contadores y la barra de progreso se dibujan con uGUI (TMP nítido, escalado por CanvasScaler)

#### Scenario: HUD actualiza en vivo
- **WHEN** el jugador pierde/gana unidades o monedas
- **THEN** los textos de uGUI se actualizan en el mismo frame (no hay lag visual)

### Requirement: Migración de PauseMenu a uGUI
`PauseMenu` SHALL migrarse a uGUI con `UGui.cs`. El `OnGUI` MUST eliminarse al terminar.

#### Scenario: Pausa abre menú uGUI
- **WHEN** el jugador pausa
- **THEN** se muestra un panel uGUI con botones (Reanudar, Reiniciar, Salir) que responden a toque

### Requirement: Uso de la librería UGui existente
La migración SHALL usar los builders de `UGui.cs` (`MakeCanvas`, `Rect`, `AddImage`, `Text`, `Button`, `Icon`, `ProgressBar`, `WithShadow`). Si hacen falta builders nuevos (p. ej. grid de tienda), se añaden a `UGui.cs` siguiendo su estilo.

#### Scenario: Reutilización de builders
- **WHEN** se construye un botón del menú
- **THEN** se usa `UGui.Button(...)` (no se crea el `Button` a mano con `AddComponent` suelto)

#### Scenario: Builder nuevo
- **WHEN** la tienda necesita un grid de items que `UGui` no cubre
- **THEN** se añade un builder (p. ej. `UGui.ShopGrid(...)`) en `UGui.cs` con el mismo estilo (paleta, sombras, scaler)

### Requirement: Canvas scaler portrait responsive
Todo Canvas SHALL usar `CanvasScaler` con referencia 720×1280 y `matchWidthOrHeight = 0.5` (lo que ya hace `UGui.MakeCanvas`). La UI MUST verse correcta en portrait 9:16 y no deformarse en distintos DPIs.

#### Scenario: Distintos tamaños
- **WHEN** se ejecuta en un teléfono con DPI/aspect distinto al Pixel de referencia
- **THEN** la UI escala sin deformación de texto ni botones fuera de pantalla

### Requirement: Sin cableado en Inspector
Los Canvas y sus elementos SHALL crearse por código desde los bootstraps (`MenuBootstrap`, `GameBootstrap`). No se arrastra nada a campos del Inspector; los prefabs (si los hay) se cargan desde `Resources/` por nombre.

#### Scenario: Bootstrap construye el menú
- **WHEN** `MenuBootstrap.Awake` se ejecuta
- **THEN** instancia el Canvas y los paneles del menú llamando a `UGui.*` y `MenuUI.*` por código

#### Scenario: Sin referencias Inspector
- **WHEN** se inspecciona `MenuUI` en el editor
- **THEN** no hay campos `Image`/`Button`/`TextMeshProUGUI` arrastrados a mano
