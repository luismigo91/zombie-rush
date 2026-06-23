## 1. Preparación y rollback

- [x] 1.1 Commit limpio en `main` y crear tag `pre-art-pass` como punto de rollback
- [x] 1.2 Activar **Git LFS** (`git lfs install`) y añadir `.gitattributes` para `*.png *.psd *.jpg *.tga *.tif *.wav *.psb`
- [x] 1.3 Actualizar `CLAUDE.md`: documentar que `git lfs install` es obligatorio tras clone y anotar el cambio activo `art-pass`

## 2. Color space Linear (fase 1)

- [x] 2.1 Cambiar `PlayerSettings.colorSpace = Linear` (Project Settings) y forzarlo en el setup script si hace falta
- [ ] 2.2 Revisión visual de la paleta en editor: `PixelArt`, `Environment`, `Vfx`, `UGui`; retocar hex que queden apagados en Linear
- [ ] 2.3 Compilar APK y validar en Pixel que degradados y sprites tintados se ven correctos
- [x] 2.4 Commit "art-pass: Linear color space"

## 3. UI uGUI — MenuUI (fase 2) — ✓ YA HECHO en el trabajo del pivote

> `MenuUI.cs`, `Hud.cs` y `PauseMenu.cs` ya están migrados a uGUI (uso de `UGui.*`, `TextMeshProUGUI`, `Button.onClick`, sin `OnGUI()`). Detectado al commitear el trabajo pendiente del pivote. Las fases 3 y 7 se marcan completas.

- [x] 3.1 Releer `UGui.cs` y `MenuUI.cs` (OnGUI) para mapear pantallas/elementos a migrar
- [x] 3.2 Añadir a `UGui.cs` los builders que falten (p. ej. `ShopGrid` para items de tienda) siguiendo su estilo
- [x] 3.3 Reconstruir `MenuUI` con uGUI (menú principal + tienda) usando `UGui.*`; cablear botones con `Button.onClick`
- [x] 3.4 Eliminar el `OnGUI()` de `MenuUI` (y helpers IMGUI que ya no se usen)
- [x] 3.5 Validar en editor: tocar botones, abrir tienda, comprar; comprobar escalado portrait
- [x] 3.6 Compilar APK y validar flujo de menú+tienda en Pixel
- [x] 3.7 Commit "art-pass: migrar MenuUI a uGUI" — ya estaba en el commit del pivote

## 4. Environment — ciudad en parallax (fase 3)

- [x] 4.1 Crear `Assets/Resources/Art/environment/` y `Assets/Resources/Art/ATTRIBUTION.md`
- [x] 4.2 Descargar tilesets/props CC0 de Kenney (city/road/post-apocalypse) y colocarlos en `Resources/Art/environment/`; rebanar sheets con Sprite Mode=Multiple
- [x] 4.3 Implementar `ArtCache` estático (`Resources.Load`/`LoadAll` con caché y fallback a `PixelArt`)
- [x] 4.4 Reescribir `Environment.cs` en capas: cielo, skyline, edificios medios, calle/props cercanos, suelo, niebla; con parallax escalonado y reciclado por código
- [x] 4.5 Cargar sprites de props desde `ArtCache` (árbol/farola/escombro) en vez de `MakeDeadTreeSprite`/`MakeLampSprite`/`MakeRubbleSprite`
- [x] 4.6 Eliminar el sprite de viñeta de `Environment.cs` (la aporta URP en la fase 5; provisionalmente sin viñeta)
- [ ] 4.7 Validar scroll infinito sin costuras en editor portrait y en Pixel
- [ ] 4.8 Commit "art-pass: environment ciudad en parallax + ArtCache + LFS"

## 5. URP 2D + post-proceso (fase 4)

- [ ] 5.1 Instalar paquetes URP en `Packages/manifest.json` (`com.unity.universalrp` + `com.unity.2d.*` necesarios)
- [ ] 5.2 Crear editor-script que genere el `UniversalRenderPipelineAsset` + `UniversalRendererData` (2D Renderer) y lo asigne en `GraphicsSettings` (sin Inspector); si no es factible, versionar en `Assets/Settings/` y referenciar por path
- [ ] 5.3 Revisar materiales: el default de sprites sigue válido; migrar lo que haga falta
- [ ] 5.4 Crear el `Volume` global con overrides: `Bloom` (threshold alto), `Vignette`, `ColorAdjustments` (contraste/saturación nocturnos), `FilmGrain` sutil; por código o prefab en `Resources/`
- [ ] 5.5 Añadir `Light2D` a farolas del entorno (color ámbar `#E8A23A`); medir FPS en Pixel; si regresa, limitar a jefe/eliminar
- [ ] 5.6 Compilar APK y validar look neón + FPS en Pixel; ajustar Bloom/Vignette según dispositivo
- [ ] 5.7 Commit "art-pass: migrar a URP 2D + post-proceso"

## 6. Personajes — spritesheets (fase 5)

- [ ] 6.1 Crear `Assets/Resources/Art/characters/`; descargar spritesheets CC0/CC4-BY de soldado, zombie y boss (Kenney/itch); actualizar `ATTRIBUTION.md`
- [ ] 6.2 Rebanar sheets (`Sprite Mode=Multiple`) o documentar sub-rects para `Sprite.Create` por código si vienen como textura única
- [ ] 6.3 Extender `ArtCache` para servir arrays de `Sprite` por animación (`soldier_march`, `soldier_shoot`, `zombie_shamble`, `zombie_die`, `boss_idle`…)
- [ ] 6.4 Elegir estrategia de tinte por tipo de zombie: sheet en grises + `SpriteRenderer.color`, o 3 paletas/sheets; documentar la decisión
- [ ] 6.5 Actualizar `Squad` para que use `ArtCache` en vez de `PixelArt.SoldierMarch`/`SoldierShoot`; conservar `SpriteAnim`
- [ ] 6.6 Actualizar `Enemy` para que use `ArtCache` para shamble (con tinte por tipo) y muerte si el sheet la trae; `Vfx.Gore` se superpone
- [ ] 6.7 Actualizar jefe para usar el sheet de boss (idle o estático)
- [ ] 6.8 Compilar APK y validar animaciones + tinte en Pixel
- [ ] 6.9 Commit "art-pass: spritesheets de personajes"

## 7. UI uGUI — Hud y PauseMenu (fase 6) — ✓ YA HECHO en el trabajo del pivote

- [x] 7.1 Migrar `Hud` a uGUI con `UGui.*` (nivel, monedas, unidades, barra de progreso); actualizar textos en vivo
- [x] 7.2 Eliminar `OnGUI()` de `Hud`
- [x] 7.3 Migrar `PauseMenu` a uGUI (panel con Reanudar/Reiniciar/Salir); eliminar su `OnGUI()`
- [x] 7.4 Validar escalado portrait y respuesta a toque en editor y Pixel
- [x] 7.5 Commit "art-pass: migrar Hud y PauseMenu a uGUI" — ya estaba en el commit del pivote

## 8. Limpieza y cierre (fase 7)

- [ ] 8.1 Eliminar de `Vfx.cs` los quads de "glow falso" que el Bloom URP ya cubre (muzzle halo, etc.); conservar partículas de gore/impact/coin/confetti
- [ ] 8.2 Decidir sobre `PixelArt.cs`: eliminar si todos los sprites tienen reemplazo, o conservar como fallback para bala/muzzle sin asset; documentar la decisión
- [ ] 8.3 Compilar APK final y validar look completo en Pixel (neón, parallax, animaciones, UI)
- [ ] 8.4 Actualizar `CLAUDE.md` para reflejar el nuevo stack (URP, LFS, `Resources/Art/`) y archivar el cambio `art-pass`
- [ ] 8.5 Commit final "art-pass: cierre y limpieza"
