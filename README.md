# 🧟 Zombie Dash

Juego móvil arcade *dodge & shoot* en Unity 2D. El jugador se mueve lateralmente
para esquivar hordas de zombies mientras dispara en automático, y mejora su
arsenal entre partidas. Sesiones de 1–3 minutos, pensado para móvil vertical.

> Documentación completa (GDD, plan técnico, roadmap) en Notion → "Zombie Dash".

## Estado actual: Fase 1 — Core loop jugable (gris)

Implementado el bucle básico con primitivas (cubos de colores), sin arte todavía:

- ✅ Jugador que se mueve lateralmente arrastrando el dedo / ratón.
- ✅ Disparo automático al enemigo más cercano.
- ✅ Enemigos que avanzan hacia el jugador y le hacen daño de contacto.
- ✅ Spawner con dificultad creciente.
- ✅ Vida del jugador + game over con reinicio.
- ✅ HUD provisional (vida, kills, tiempo).

## Cómo abrirlo y jugar

1. Abre **Unity Hub** → *Add* → selecciona esta carpeta (`zombie-dash`).
   Usa **Unity 6000.4.9f1** (o el editor que tengas; te pedirá la versión).
2. En el editor, abre la escena **`Assets/Scenes/Game.unity`** (doble clic).
3. Pulsa **Play**.
   - **Ratón:** mantén pulsado y arrastra horizontalmente para mover al jugador (cubo verde).
   - Los zombies (cubos rojos) bajan desde arriba; las balas amarillas salen solas.
   - Si te alcanzan, pierdes vida. Al morir → *Game Over* → clic para reintentar.

> 💡 Para que se vea como en móvil, en la ventana *Game* elige una resolución
> vertical (p. ej. 1080x1920 o 9:16).

### Hito de esta fase
La pregunta a responder jugando: **¿es mínimamente divertido con cubos?**
Si no lo es, ajustamos velocidades/cadencia/daño antes de añadir arte.

## Arquitectura (code-first en la fase gris)

Todo se monta por código desde `GameBootstrap` (un único objeto en la escena),
así no hay que cablear nada en el editor todavía. Scripts en `Assets/Scripts/`:

| Script | Responsabilidad |
|---|---|
| `Core/GameBootstrap` | Crea cámara, jugador, spawner y HUD al arrancar. |
| `Core/GameManager` | Estado de la run, kills, tiempo, game over (singleton). |
| `Core/Prims` | Fábrica de sprites primitivos (cuadrados de color). |
| `Player/PlayerController` | Movimiento por arrastre + vida. |
| `Player/AutoShooter` | Busca el enemigo más cercano y dispara. |
| `Combat/Bullet` | Proyectil: se mueve y daña al impactar. |
| `Enemies/Enemy` | Zombie: avanza, daña al contacto, muere por balas. |
| `Enemies/EnemySpawner` | Genera enemigos con ritmo creciente. |
| `UI/Hud` | HUD provisional con IMGUI. |
| `Editor/ZombieDashSetup` | Menú *Zombie Dash → Crear escena de juego*. |

Para tunear el balance, ajusta los campos públicos de `AutoShooter`,
`EnemySpawner` y `PlayerController` (cadencia, daño, velocidad, vida...).

## Siguientes pasos (roadmap)

- **Fase 2:** tipos de enemigo (normal/corredor/tanque) y oleadas con `ScriptableObject`,
  pickups de monedas, HUD propio con uGUI.
- **Fase 3:** meta-progresión (monedas persistentes con PlayerPrefs, pantalla de mejoras).
- **Fase 4:** juicy (partículas, números de daño, SFX) y sustituir primitivas por sprites.
- **Fase 5:** build APK y test en dispositivo real.

## Notas técnicas

- Unity **6000.4.9f1**, plantilla 2D, Built-in Render Pipeline.
- Input Manager clásico (sin paquete extra): ratón en editor, tacto en móvil.
- Cuando se añadan assets binarios (sprites, audio), instalar **Git LFS**
  (`brew install git-lfs && git lfs install`) y trackear `*.png`, `*.wav`, etc.
