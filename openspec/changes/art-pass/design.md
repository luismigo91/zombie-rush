## Context

Zombie Rush se construyó **code-first, placeholder-first, sin assets binarios**: todo el arte (sprites de personajes, suelo, props, viñeta, glow) se genera en `PixelArt.cs` y `Environment.cs` con `Texture2D`+`SetPixels`; los VFX son cuadrados tintados en `Vfx.cs`; la UI está en `OnGUI` (`MenuUI.cs`, `Hud.cs`, `PauseMenu.cs`) pese a existir ya una librería uGUI completa (`UGui.cs`) sin estrenar. El pipeline es **Built-in RP, color space Gamma**, sin post-proceso ni luces.

El mood visual objetivo ("noche apocalíptica neón") vive hoy solo en la **paleta** (`#14122A`, `#FFD23A`, `#3DD6F5`, `#FF3B3B`…) esparcida en los headers de `PixelArt`/`Environment`/`Vfx`/`UGui`. Faltan las tres cosas que lo harían parecer real: **Linear color space**, **post-proceso (bloom/viñeta)** y **profundidad de fondo**. El usuario quiere un art pass con assets gratuitos y ha aceptado romper la convención "sin assets binarios" (con Git LFS) y migrar a URP.

Stack: Unity 6000.4.9f1, 2D, Android portrait. Bootstraps montan todo en `Awake`; nada se cablea en el Inspector. Compilación APK por CLI (`BuildAndroid.BuildAPK`, IL2CPP+ARM64) y prueba en Pixel real.

## Goals / Non-Goals

**Goals:**
- Dar al juego un look "de tienda" coherente con el mood neón apocalíptico, sin rehacer gameplay.
- Migrar a **URP 2D + Linear** con un volumen de post-proceso (Bloom, Vignette, Color Adjustments, Film Grain) y `Light2D` puntuales.
- Introducir **assets binarios gratuitos** (CC0/CC4) cargados por código desde `Resources/`, con Git LFS.
- Fondo de **ciudad en parallax** en varias capas, reemplazando las losas procedurales.
- **Spritesheets animados** de soldado/zombie/boss con más frames, manteniendo el tinte por tipo de zombie.
- Migrar la **UI de OnGUI a uGUI** empezando por `MenuUI` (primera impresión del jugador), después `Hud` y `PauseMenu`.
- Mantener la filosofía **code-first**: los bootstraps instancian/configuran todo; los assets se piden por nombre, no se cablean.

**Non-Goals:**
- No se rediseña el gameplay, el balance ni la curva de niveles.
- No se sustituye el audio procedural (`Sfx.cs`/`Music.cs`) por assets de sonido en este cambio (queda para otro).
- No se hace un *re-skin* total de marca/logo ni icono de app (se roza solo si un asset lo pide).
- No se migran los ScriptableObjects de `Assets/Resources` (Enemies/Waves/Upgrades) — son datos, no arte.
- No se eliminan `PlayerController`/`EnemySpawner` y demás dormantes del juego anterior (fuera de alcance).

## Decisions

### D1 — URP 2D (no Built-in + shader propio, no HDRP)
**Decisión:** Migrar a **URP con 2D Renderer** (paquete `com.unity.universalrp` + `com.unity.2d.common`/`com.unity.2d.sprite`/`com.unity.2d.animation` según haga falta).
**Por qué:** El mood neón necesita **Bloom** y **Vignette** de GPU; el Built-in RP no los trae cómodos en Unity 6. URP 2D es el camino soportado para 2D móvil, con `Light2D` nativa y post-proceso por *Volume*. HDRP se descarta (overkill para 2D móvil). Un shader propio en Built-in (vía `CommandBuffer`) se descarta por maintenance y porque URP ya lo resuelve.
**Alternativas descartadas:** Built-in + shader de bloom casero (deuda técnica), HDRP (no orientado a 2D móvil).
**Configuración:** `UniversalRendererData` con renderer 2D + `UniversalRenderPipelineAsset` configurado para Android (portrait, MSAA off o 2x, HDR off si_fill-rate lo pide). El URP Asset se crea en `Assets/Settings/` y se asigna en `GraphicsSettings`; dado el code-first, se puede **regenerar por editor script** (`ZombieDashSetup` o uno nuevo) para evitar tocar el Inspector a mano.

### D2 — Linear color space
**Decisión:** `PlayerSettings.colorSpace = ColorSpace.Linear` (en `ProjectSettings` y, por costumbre, forzado en `AppIconGen`/setup si hace falta).
**Por qué:** En Gamma los degradados y la mezcla de sprites tintados se ven sucios; Linear es el estándar para iluminación/bloom correctos. Es un cambio "casi gratis" que mejora todo lo existente.
**Trade-off:** Los `SpriteRenderer.color` se reinterpretan (Linear espera color en espacio lineal, no sRGB); hay que **revisar la paleta visualmente** tras el switch. Texturas marcadas como sRGB se ajustan automáticamente; las generadas por código (`Texture2D`) son lineales por defecto → posibles colores más apagados de lo esperado → retoque de paleta.

### D3 — Assets cargados por código desde `Resources/` (no Addressables, no Inspector)
**Decisión:** Los PNG/spritesheets se colocan en `Assets/Resources/Art/<categoria>/` y se cargan con `Resources.Load<Sprite>(...)` o `Resources.LoadAll<Sprite>(...)`, cacheados en un `ArtCache` estático. Los bootstraps piden los sprites por nombre.
**Por qué:** Mantiene **code-first** (nada se cablea en el Inspector) y la filosofía "el bootstrap construye el mundo". `Resources/` es simple y suficiente para el volumen de un móvil arcade; Addressables añade complejidad de build/carga asincrona que no se necesita ahora.
**Nombres:** convención `<categoria>/<nombre>_<variante>_<frame>` (p. ej. `enemies/zombie_shamble_0`). Los sheets se importan con *Sprite Mode = Multiple* y `Sprite.Create` o se rebanan por código con `Sprite.Create` sub-rects si viene como una sola textura.
**Alternativas descartadas:** Addressables (prematuro), cableado en Inspector (rompe code-first).

### D4 — Git LFS para los binarios
**Decisión:** Activar Git LFS con `.gitattributes` para `*.png`, `*.psd`, `*.jpg`, `*.tga`, `*.tif`, `*.wav` (futuro audio) y `*.psb` (PSB para 2D Animation si se usa).
**Por qué:** Los PNG de tilesets/spritesheets superan el umbral de "commit cómodo" en git plano; sin LFS el repo se hincha y los diffs binarios son inútiles. GitHub soporta LFS (el repo es privado en `luismigo91/zombie-dash`).
**Trade-off:** Requiere `git lfs install` y que todo clone del repo tenga LFS; el usuario ya trabaja solo en el repo, así que es bajo impacto.

### D5 — Fuentes de assets (CC0 preferidas)
**Decisión:** Prioridad **Kenney.nl (CC0)** para tilesets y props de ciudad (sin atribución obligatoria). Para personajes, si no hay Kenney adecuado, **itch.io free packs con licencia CC0 o CC4-BY** (atribución documentada en `Assets/Resources/Art/ATTRIBUTION.md`). Se evita cualquier asset con *no-derivatives* (CC-ND) porque puede haber re-tinte.
**Por qué:** CC0 minimiza fricción legal en Play Store; CC4-BY es seguro con atribución. Kenney es el estándar de facto para jam/arcade.
**Categorías a buscar:** `city/tileset`, `road/tileset`, `post-apocalypse/tileset`, `characters/zombie`, `characters/soldier`, `effects/particles` (opcional).

### D6 — Environment en capas (parallax), reciclado por código
**Decisión:** `Environment.cs` mantiene su arquitectura de scroll+reciclado por código pero pasa de "2 losas + 3 props" a **5–6 capas**: cielo (URP background o sprite), skyline lejano, edificios medios, calle/props cercanos, suelo con carriles, niebla/polvillo. Cada capa tiene su `sortingOrder` y factor de parallax.
**Por qué:** Un runner de multitud vive de profundidad; las capas dan масштаб sin tocar gameplay. Reutiliza el reciclado existente (props pool, losas) por lo que el coste es sobre todo **cargar sprites nuevos**.
**Viñeta:** se elimina el sprite de viñeta de `Environment.cs` y se usa la **Vignette del volume URP**.

### D7 — Spritesheets de personajes con tinte por tipo conservado
**Decisión:** Para el zombie, buscar un sheet en **grises** o con colores neutros y tintar por tipo (normal/runner/tank) con `SpriteRenderer.color`, igual que hoy. Si solo hay sheets a color, se eligen 3 paletas de tintado o 3 sheets (decisión menor, aplazada a la fase de assets). Soldado y boss usan sus colores finales del sheet.
**Animación:** se reemplaza el `Sprite[]` de `PixelArt` por un array cargado de `Resources/`; `SpriteAnim.cs` ya existe y se reutiliza para el ciclo. Frames: caminar (4–8), disparar (2–4), morir (3–5) si el sheet los trae.
**`PixelArt.cs`** se conserva durante el cambio como fallback y se elimina al final si todos los sprites tienen reemplazo (decisión de limpieza al cierre).

### D8 — UI: uGUI con `UGui.cs`, menú primero
**Decisión:** Migrar en este orden dentro del cambio: `MenuUI` (menú + tienda) → `Hud` → `PauseMenu`. Se usa `UGui.cs` (canvas scaler 720×1280, match 0.5, TMP, paneles/botones/barras/iconos). Los `OnGUI` se eliminan a medida que cada componente migra.
**Por qué:** `MenuUI` es la **pantalla de entrada** del jugador (y la que ve Google Play en capturas); `Hud` es menos crítica visualmente y más de lectura en partida; `PauseMenu` es el más simple. Migrar en este orden maximiza la primera impresión por esfuerzo.
**`UGui.cs` ya escrito:** canvas, `Rect`, `AddImage`, `Text`, `Button`, `Icon`, `ProgressBar`, `MakeRounded` — se reutiliza tal cual; solo se añaden builders si hacen falta (p. ej. tienda con grid de items).

### D9 — Post-proceso volume por código
**Decisión:** El `Volume` de URP se crea por código desde un editor-script de setup o desde el bootstrap del juego (con `VolumeComponent` inyectados) para mantener code-first. Overrides: `Bloom` (threshold alto para que solo brille el neón), `Vignette` (intensidad media, forma portrait), `ColorAdjustments` (contraste +5, saturación -5 para mood nocturno), `FilmGrain` (sutil, 0.3).
**Por qué:** Evita cablear el volume en el Inspector; el setup queda reproducible. Si URP no permite crear el volume 100% por código cómodamente, se cae a un prefab `.prefab` en `Resources/` cargado al bootstrap (excepción controlada a code-first).

### D10 — Orden de implementación (fases)
**Decisión:** Fases en este orden, cada una compila APK y se valida en Pixel antes de la siguiente:
1. **Linear color space** (1h) — revisión de paleta.
2. **UI uGUI: MenuUI** (1–2d) — primera impresión.
3. **Environment: ciudad en parallax** (1d) — con Git LFS ya activado.
4. **URP 2D + post-proceso** (1–2d) — migración de pipeline, volume, `Light2D`.
5. **Personajes: spritesheets** (2d) — soldado, zombie, boss.
6. **UI uGUI: Hud + PauseMenu** (1d) — cierre de UI.
7. **Limpieza** (0.5d) — eliminar viñeta/glow falsos, decidir sobre `PixelArt.cs`.
**Por qué:** Lineal es casi gratis y mejora todo; UI da la primera impresión sin depender del pipeline; Environment introduce LFS y assets antes de URP (así el bloom tiene qué iluminar); URP va después de tener assets para ver el efecto real; personajes al final (más integración de anim); Hud/Pause cierran UI.

## Risks / Trade-offs

- **[Linear reinterpreta la paleta existente]** → revisión visual de todos los `SpriteRenderer.color` y colores de `Texture2D` generados por código; retocar hex si quedan apagados. Hacer este cambio aislado en su propia fase y validar en Pixel antes de seguir.
- **[Migración URP rompe build Android]** → compilar APK tras la migración y antes de tocar nada más; mantener el commit previo como punto de rollback. Probar en Pixel (no solo editor).
- **[URP Bloom cuesta fill-rate en móvil]** → threshold alto (solo neón), MSAA off, HDR off si hace falta; medir FPS en Pixel. Si regresa, bajar intensidad o usar *Bloom Fast*.
- **[Assets CC4-BY con atribución olvidada]** → `Assets/Resources/Art/ATTRIBUTION.md` obligatorio desde la fase 3; cualquier asset sin atribución clara se descarta.
- **[Git LFS no configurado en un clone]** → documentar en CLAUDE.md que `git lfs install` es obligatorio; añadir check en el setup script.
- **[Spritesheets con frames no uniformes]** → loader tolerante: si un sheet no se rebana bien con `Sprite.Create` múltiple, caer a sub-rects manuales por código con coordenadas documentadas.
- **[Zombie en gris no encontrado]** → fallback: sheet a color + 3 paletas de tintado por tipo (decisión aplazada a fase 5).
- **[Volumen URP no creable 100% por código]** → excepción controlada: prefab en `Resources/` cargado por el bootstrap. Aceptado porque no rompe code-first (el prefab se carga, no se cablea).

## Migration Plan

1. **Pre**: commit limpio en `main`, tag `pre-art-pass` como rollback (igual que `pre-zombie-rush`).
2. **Fase 1 (Linear)**: switch + revisión visual + commit. Rollback trivial (revert ProjectSettings).
3. **Fase 2 (UI MenuUI)**: nueva `MenuUI` en uGUI convive con `OnGUI` solo durante la migración; al terminar se elimina el `OnGUI`. Commit.
4. **Fase 3 (Environment)**: activar Git LFS + `.gitattributes` **antes** de cualquier PNG. Importar tilesets a `Resources/Art/`. Reescribir `Environment.cs` por capas. Commit.
5. **Fase 4 (URP)**: instalar paquete, crear URP Asset + 2D Renderer (por editor script), asignar en `GraphicsSettings`. Migrar materiales (casi ninguno). Volumen de post-proceso. `Light2D` en farolas/jefe. Build APK + prueba Pixel. Commit.
6. **Fase 5 (Personajes)**: importar spritesheets, rebanar, loader + `SpriteAnim`. Sustituir uso de `PixelArt` en `Squad`/`Enemy`/boss. Commit.
7. **Fase 6 (Hud/Pause uGUI)**: migrar y eliminar `OnGUI`. Commit.
8. **Fase 7 (Limpieza)**: borrar viñeta sprite y quads de glow falso de `Vfx.cs`; decidir si `PixelArt.cs` se queda como fallback o se elimina. Commit final.
9. **Validación**: APK en Pixel por `adb install -r Builds/ZombieRush.apk` tras cada fase jugable.

Rollback por fase: revert del commit correspondiente. Rollback total: `git reset --hard pre-art-pass`.

## Open Questions

- **¿Sheet de zombie en grises o a color?** Se resuelve en fase 5 al evaluar lo que Kenney/itch ofrecen. Si hay gris, se mantiene el tinte por tipo; si no, 3 paletas.
- **¿`PixelArt.cs` se elimina al final?** Decisión de limpieza en fase 7. Probablemente sí si todo tiene reemplazo; si algún sprite (p. ej. bala/muzzle) no tiene asset gratis decente, se conserva para esos.
- **¿`Light2D` en farolas o solo en jefe/acentos?** Probar fill-rate en Pixel; si las farolas pesan, limitar a jefe y muzzle (muzzle via bloom, no Light2D).
- **¿Regenerar URP Asset por editor script o guardarlo en `Assets/Settings/`?** Intentar por script; si es frágil, guardarlo como asset versionado (excepción a "sin assets en Inspector", pero no a code-first porque el bootstrap lo referencia por path).
