## Why

El loop de Zombie Rush ya está validado, pero el look **no hace justicia al mood "noche apocalíptica neón"** con el que se diseñó: el neón está pintado a mano en sprites planos (sin bloom), el color space es Gamma (degradados sucios), el fondo son 2 losas de asfalto + 3 props silueta, los personajes son de 44px con 3 frames, y la UI sigue en `OnGUI` pese a tener `UGui.cs` ya escrito. Para competir en Play Store el juego necesita un **art pass** que eleve la primera impresión y el game feel sin rehacer el gameplay.

## What Changes

- **BREAKING** — Migración del **Built-in RP al URP 2D** (Universal Render Pipeline + 2D Renderer): post-proceso real (Bloom, Vignette, Color Adjustments, Film Grain), `Light2D` para farolas/jefe, y sustitución de los "workarounds" de `Environment.cs`/`Vfx.cs` que fingen viñeta y glow con sprites.
- **BREAKING** — **Color space Gamma → Linear** (Project Settings): degradados y mezclas de color correctas. Requiere revisión visual de la paleta existente (sprites tintados).
- **Assets binarios**: se introducen **tilesets y spritesheets gratuitos (CC0/CC4)** cargados por código desde `Resources/` (sin cableado en Inspector). Implica **Git LFS** para los PNG.
- **Environment**: fondo de **ciudad en capas de parallax** (skyline lejano, edificios medios, calle/props cercanos, suelo) reemplazando las losas procedurales. Mantiene el scroll/reciclado por código de `Environment.cs`, solo cambia la fuente de los sprites.
- **Personajes**: spritesheets de soldado/zombie/boss con más frames (caminar, disparar, morir) cargados desde `Resources/`. El zombie mantiene el esquema **gris tintable** por tipo (se elige un sheet en grises o se tintan los colores fijos por tipo).
- **UI**: migración de `MenuUI` (menú + tienda, punto de entrada del jugador) de `OnGUI` a uGUI usando la librería `UGui.cs` existente. `Hud` y `PauseMenu` migran después en el mismo cambio.
- **Post-proceso / mood**: volumen URP con Bloom (muzzle, balas, monedas, jefe), Vignette (sustituye al sprite de viñeta), Color Adjustments (contraste/tinta nocturna) y Film Grain sutil.
- **Limpieza**: eliminación de la viñeta sprite y los quads de "glow falso" de `Vfx.cs` cuando el post-proceso URP los cubra.

## Capabilities

### New Capabilities
- `render-pipeline`: migración a URP 2D + Linear color space + volumen de post-proceso (Bloom, Vignette, Color Adjustments, Film Grain) y `Light2D` para acentos del entorno.
- `art-assets`: integración de assets binarios gratuitos (tilesets, spritesheets, iconos) cargados por código desde `Resources/`, con Git LFS. Fuentes, licencias y convención de nombres.
- `environment-depth`: fondo de ciudad en capas de parallax (cielo, skyline, edificios, calle, props, niebla) con scroll/reciclado por código, reemplazando las losas procedurales de asfalto.
- `character-sprites`: spritesheets animados de soldado, zombie y boss (caminar/disparar/morir) cargados desde `Resources/`, manteniendo el tinte por tipo de zombie.
- `ugui-migration`: migración de `MenuUI`, `Hud` y `PauseMenu` de `OnGUI` a uGUI usando `UGui.cs` (canvas scaler portrait, TMP, botones/barras/iconos).

### Modified Capabilities
<!-- No hay specs previas en openspec/specs/ (el único cambio archivado, pivot-zombie-rush, no dejó specs publicadas). -->

## Impact

- **Pipeline/Proyecto**: `GraphicsSettings` (URP Asset + 2D Renderer), `ProjectSettings` (color space Linear), `Packages/manifest.json` (`com.unity.universalrp` + `com.unity.2d.*`). Revalidar build Android (IL2CPP+ARM64).
- **Código reescrito**: `Environment.cs` (carga de sprites de ciudad en vez de generarlos, capas de parallax, sin viñeta sprite), `Vfx.cs` (sin quads de glow falsos, delega al Bloom URP), `MenuUI.cs`/`Hud.cs`/`PauseMenu.cs` (OnGUI → uGUI).
- **Código nuevo**: loader de sprites desde `Resources/` (con caché), mapeador de frames de spritesheet a `SpriteRenderer`/animación, wrapper de volumen URP por código (sin Inspector).
- **Código reorientado**: `PixelArt.cs` se conserva como fallback procedural para lo que no tenga asset (o se elimina por completo al final del cambio — decisión de diseño).
- **Assets**: nuevos PNG en `Assets/Resources/Art/` (tilesets, spritesheets, iconos). **Git LFS obligatorio** (`.gitattributes` para `*.png`, `*.psd`, etc.).
- **Filosofía**: se mantiene **code-first** (los bootstraps instancian y configuran todo por código; los assets se cargan por nombre desde `Resources/`, no se cablean en Inspector). Se rompe **"sin assets binarios"** explícitamente, asumido por el usuario.
- **Rendimiento**: URP 2D está pensado para móvil; revisar fill-rate del Bloom en portrait. Sin regresión esperada vs. Built-in.
- **Validación**: cada hito se compila a APK y se prueba en dispositivo real (Pixel) como ya se hace.
