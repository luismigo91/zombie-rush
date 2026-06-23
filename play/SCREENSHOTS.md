# Assets de Play Store — plan de captura

## Icono de app (512×512)
Ya se genera por código en `Assets/Editor/AppIconGen.cs` y se aplica en el build.
Verificar que el icono adaptativo (foreground/background) se ve bien en el launcher
del dispositivo (ya instalado).

## Feature graphic (1024×500)
Pendiente. Opciones:
- Generarlo por código (estilo neón del juego: título "ZOMBIE RUSH" + escuadrón +
  horda). Se puede añadir un método a `AppIconGen` que renderice a 1024×500.
- O componerlo en una herramienta de imagen a partir de `docs/banner.png`.

## Screenshots (mínimo 2, ideal 3–8) — portrait 1080×1920
Capturar del dispositivo instalado con `adb exec-out screencap`:
```bash
~/Library/Android/sdk/platform-tools/adb exec-out screencap -p > play/screenshots/01_menu.png
```
Plan de tomas:
1. **Menú principal** (banner + tienda de punto de partida + banco).
2. **Combate temprano** (escuadrón disparando a una horda, HUD con chips).
3. **Gate** (escuadrón cruzando un par de gates +N / ×N).
4. **Power-up** (recogiendo un power-up con el indicador activo).
5. **Jefe** (barra de vida de jefe + adds).
6. **Pantalla de victoria** (estrellas + recompensa).

## Video (opcional, 30s)
Trailer de gameplay capturado del dispositivo; no imprescindible para el primer
release en internal test.
