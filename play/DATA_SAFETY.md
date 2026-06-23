# Data Safety — Zombie Rush

## Resumen
Zombie Rush **no recopila, comparte ni vende datos personales del usuario**. No hay
analíticas, no hay SDKs de terceros, no hay anuncios, no hay inicio de sesión.

## Datos almacenados (solo en el dispositivo)
El juego usa `UnityEngine.PlayerPrefs` para guardar progreso localmente en el dispositivo:

| Clave (prefijo)        | Uso                                  | Tipo   |
|------------------------|--------------------------------------|--------|
| `coins`                | Banco de monedas persistente         | int    |
| `sp_Units`, `sp_Weapon`| Punto de partida comprado (meta-tienda) | int |
| `level`                | Nivel actual de la campaña (1..100)  | int    |
| `seen_tutorial`        | Flag "ya vio el tutorial de arrastre"| int    |
| `musicOn`, `sfxOn`     | Preferencias de audio                | int    |
| `migrated_v2`          | Flag de migración de economía        | int    |

Estos datos **no se envían a ningún servidor**. Se pueden borrar desde el botón
"Reiniciar progreso" del menú o desinstalando la app.

## Permisos
- Ninguno en tiempo de ejecución. (Vibración háptica vía `Android.vibrate`; no requiere
  permiso RUN_TIME en Android 13+ para vibración básica — se declara internamente pero
  no se solicita al usuario.)

## Cifrado
No aplica: no se transmite ni almacena datos personales fuera del dispositivo.

## Cumplimiento familiar
El juego está dirigido a público general (Teen / violencia fantástica no realista).
No está diseñado para menores de 13 sin supervisión; no recopila datos de menores.
