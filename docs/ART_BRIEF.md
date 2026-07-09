# 🧟 Zombie Rush — Brief de arte para Claude Design

> **Cómo usar este documento:** pégalo (o adjúntalo) entero en Claude Design como
> contexto y pide los assets **por lotes** (Lote 1, Lote 2…). Es autocontenido: no
> hace falta acceso al repositorio. Si puedes, adjunta también `docs/banner.png`
> como referencia de mood/paleta. Al final hay un prompt de arranque sugerido.

---

## 1. El juego en corto

**Zombie Rush** es un *crowd shooter* arcade para móvil (Android, **vertical 9:16**),
estilo *Last Z / Crowd Evolution*: controlas un **escuadrón de soldados** (una multitud
de unidades pequeñas) que avanza por una carretera con scroll, se mueve solo en
horizontal y **dispara recto hacia arriba**. Creces cruzando **gates** (+N / ×N),
rescatando supervivientes de **jaulas** y rompiendo **barreras**, mientras una **horda
de zombies** baja hacia ti y te erosiona. 100 niveles, jefe cada 10.

- **Vista:** cenital pura (top-down). Los personajes se ven desde arriba: cabeza,
  hombros y arma. Muchas unidades pequeñas en pantalla a la vez (hasta ~200 sprites).
- **Cámara:** ortográfica; el área visible mide ~5.6 × 10 "unidades de mundo"
  (~193 px de pantalla por unidad en 1080×1920).
- **Mood:** *noche apocalíptica neón* — oscuro, saturado en los acentos, legible.

## 2. Dirección de arte

- **Estilo:** *flat vector* con sombreado sutil (1–2 tonos por forma), siluetas
  redondeadas y limpias, contorno oscuro fino u omitido (elige y sé consistente).
  NO pixel-art. NO fotorealismo. Debe leerse perfectamente a ~60 px en pantalla.
- **Luz:** única y global, desde arriba-izquierda. Sombras integradas en el sprite
  (no sombras proyectadas sueltas; el juego no las compone).
- **Regla de oro:** silueta reconocible a tamaño pequeño > detalle interno.
- **Nada de texto horneado** en los sprites (números, letreros): el juego los
  renderiza por código encima.
- **Originalidad:** arte 100 % original (sin IPs de terceros). Se usará en un juego
  con posible publicación comercial en Google Play.

### Paleta maestra (usa EXACTAMENTE estos hex como base)

| Uso | Hex |
|---|---|
| Cielo (degradado arriba → abajo) | `#14122A` → `#241A3A` |
| Asfalto / suelo | `#2A2740` (líneas de carril `#4A4668`) |
| Soldado: cuerpo / sombra / contorno | `#5BD66A` / `#34A347` / `#0C2A14` |
| Soldado: casco y arma | `#2E2E36` |
| Zombie base (tintable): claros desaturados | cuerpo `#EAEAEA`, sombra `#A8A8A8`, contorno `#1E1E1E` |
| Zombie: ojos | `#FF3B3B` |
| Tintes por tipo (los aplica el juego) | normal `#7FB04E` · runner `#E8C84A` · tank `#9B5BD6` |
| Jefe | `#4A7A20` (ojos `#FF2A2A`) |
| Bala: núcleo / punta / estela | `#FFD23A` / `#FFF7CC` / `#FF7A1A` |
| Moneda: oro / brillo / contorno | `#FFD23A` / `#FFF0A0` / `#7A5212` |
| Gate BUENO / Gate MALO | `#3DE0C8` / `#FF5A3C` |
| UI: cian / magenta / texto / panel | `#3DD6F5` / `#FF4D8D` / `#F4F1E8` / `#1A1830` |

## 3. Especificaciones técnicas de entrega (obligatorias)

1. **Formato:** PNG-32 con **fondo transparente** (sin halos blancos ni fondos de
   ajedrez). Un archivo por sprite/pose (nada de spritesheets).
2. **Nombres de archivo EXACTOS** de las tablas siguientes (minúsculas,
   `snake_case`, sin espacios). El juego carga por esos nombres: si el nombre
   coincide, el asset entra solo, sin tocar código.
3. **Orientación de personajes:** mirando a la **DERECHA** en el archivo (el motor
   los rota: los soldados a arriba, los zombies a abajo).
4. **Encuadre:** el sujeto centrado y ocupando ~85–90 % del lienzo; el pivote se
   toma en el centro del archivo. En animaciones (poses de un mismo personaje),
   **mismo lienzo y mismo anclaje** en todas las poses para que no "salte" al ciclar.
5. **Tamaños de lienzo** (px): indicados por tabla. En caso de duda: personajes
   96×96, props 128–256, tiles 256×256, iconos UI 128×128.
6. **Tiles de suelo:** repetibles sin costura (seamless) en **vertical y horizontal**.
7. Los zombies base van en **claros desaturados** (casi grises): el juego los
   **tinta** por tipo. Evita colores saturados en su cuerpo (los ojos rojos sí).

---

## 4. LOTE 1 — Personajes

Vista cenital, mirando a la derecha, lienzo cuadrado 96×96 salvo indicación.
Las "poses" son frames de animación: el juego cicla `stand → hold → gun`.

### Soldado (verde, con casco y fusil)

| Archivo | Contenido |
|---|---|
| `characters/soldier_stand.png` | De pie, fusil sujeto en ristre |
| `characters/soldier_hold.png` | Paso de marcha, fusil ligeramente adelantado |
| `characters/soldier_gun.png` | Disparando: fusil extendido (sin fogonazo, va aparte) |
| `characters/soldier_reload.png` | Recargando (codos abiertos, cargador) |
| `characters/soldier_silencer.png` | Variante con arma alargada (tier de arma superior) |
| `characters/soldier_machine.png` | Variante con ametralladora pesada (tier máximo) |

### Zombie base (CLAROS DESATURADOS, tintable; postura encorvada, brazos al frente)

| Archivo | Contenido |
|---|---|
| `characters/zombie_stand.png` | Arrastre, brazos extendidos |
| `characters/zombie_hold.png` | Paso de arrastre, ladeado a un lado |
| `characters/zombie_gun.png` | Paso de arrastre, ladeado al otro lado |
| `characters/zombie_reload.png` | Zarpazo (frame de ataque) |
| `characters/zombie_silencer.png` | Variante flaca (base del "runner") |
| `characters/zombie_machine.png` | **JEFE**: zombie masivo y amenazador, lienzo **256×256**, en color final `#4A7A20` (no tintable), ojos `#FF2A2A` |

### Superviviente (civil desarmado, tonos apagados)

| Archivo | Contenido |
|---|---|
| `characters/survivor_stand.png` | De pie, asustado |
| `characters/survivor_hold.png` | Brazos arriba / gesto de alivio |
| `characters/survivor_gun.png` | Corriendo hacia el escuadrón |

## 5. LOTE 2 — Objetos de gameplay

| Archivo | Lienzo | Contenido |
|---|---|---|
| `combat/bullet.png` | 32×64 | Proyectil vertical: punta brillante `#FFF7CC`, núcleo `#FFD23A`, estela `#FF7A1A` (punta hacia ARRIBA) |
| `fx/muzzle.png` | 96×96 | Fogonazo de boca de arma (estrella/destello amarillo-blanco) |
| `items/coin.png` | 64×64 | Moneda de oro con brillo |
| `items/coin_spin/coin_01.png` … `coin_06.png` | 64×64 | Ciclo de giro de la moneda (6 frames: de cara → canto → cara) |
| `items/chest.png` | 96×96 | Cofre de madera con herrajes dorados |
| `combat/gate_good.png` | 256×128 | Arco/portal translúcido cian `#3DE0C8`, marco tecnológico brillante. **Centro despejado** (el juego pinta "+5" / "×2" encima) |
| `combat/gate_bad.png` | 256×128 | Igual pero rojo `#FF5A3C`, aspecto de peligro |
| `combat/cage.png` | 128×128 | Jaula con barrotes (se ve gente dentro, siluetas) |
| `combat/cage_broken.png` | 128×128 | La misma jaula rota/abierta |
| `combat/barrier_full.png` | 256×96 | Muro/barricada militar intacta (hormigón + metal), tileable en horizontal |
| `combat/barrier_damaged.png` | 256×96 | La misma con grietas y trozos caídos |

## 6. LOTE 3 — Localizaciones (entornos por actos)

El recorrido es una **carretera vertical** con scroll: suelo en tiles + props a los
lados (en parallax) + skyline lejano opcional. La campaña tiene 10 actos; queremos
**5 temas** (uno cada 2 actos). **Empieza por el Tema 1** (es el MVP); los demás
pueden llegar después.

**Por CADA tema entrega:**

| Archivo (patrón) | Lienzo | Contenido |
|---|---|---|
| `environment/road_<tema>_01.png` … `_04.png` | 256×256 | 4 variantes de tile de suelo, seamless, mismo tono base con detalles distintos (grietas, manchas, tapas) |
| `environment/edge_<tema>.png` | 256×256 | Tile del borde/arcén (transición carretera → lateral), seamless vertical |
| `environment/prop_<tema>_XX.png` × 8 | 128–256 | 8 props laterales del tema (ver lista por tema) |
| `environment/skyline_<tema>.png` | 1024×256 | Franja de horizonte lejano, seamless horizontal, silueta oscura sobre el cielo |

**Los 5 temas** (todos de noche, paleta base + un acento propio):

1. **`suburbs`** — Carretera de los suburbios (actos 1–2, acento cian `#3DD6F5`):
   coches abandonados, farolas encendidas (halo cálido), vallas caídas, arbustos
   secos, señales de tráfico torcidas, cubos de basura, conos, neumáticos.
2. **`downtown`** — Centro urbano en ruinas (actos 3–4, acento magenta `#FF4D8D`):
   neones rotos, escaparates tapiados, barricadas militares, sacos terreros,
   semáforos muertos, autobús cruzado, cabina, papeleras volcadas.
3. **`cemetery`** — Cementerio y parque viejo (actos 5–6, acento verde `#7FB04E`):
   lápidas, cruces, árboles muertos, verjas de hierro, mausoleos, faroles de gas,
   bancos rotos, estatuas.
4. **`industrial`** — Zona industrial/puerto (actos 7–8, acento naranja `#FF7A1A`):
   contenedores, bidones tóxicos (brillo verde), grúas al fondo, palés, tuberías,
   charcos con reflejo, vallas de obra, focos industriales.
5. **`lab`** — Epicentro / laboratorio (actos 9–10, acento rojo `#FF3B3B`):
   estructuras high-tech, tanques de cultivo rotos, luces de emergencia, cables,
   pantallas rotas, cápsulas, torretas apagadas, marcas de cuarentena.

> Nota: hoy el juego usa tiles llamados `environment/road_asphalt01..08` y props
> `environment/prop_*` genéricos. Si entregas el Tema 1 con el patrón de arriba,
> el equipo cablea los nombres nuevos; también vale entregar el Tema 1 con los
> nombres antiguos (`road_asphalt01..08`) para reemplazo directo.

## 7. LOTE 4 — Iconos de UI

Lienzo **128×128**, trazo grueso y legible a 48 px, monocolor hueso `#F4F1E8`
sobre transparente (el juego los tinta si hace falta):

`ui/icon_unit.png` (soldado/casco) · `ui/icon_coin.png` · `ui/icon_skull.png` (jefe) ·
`ui/icon_pause.png` · `ui/icon_play.png` · `ui/icon_home.png` · `ui/icon_restart.png` ·
`ui/icon_music.png` · `ui/icon_music_off.png` · `ui/icon_sfx.png` · `ui/icon_sfx_off.png` ·
`ui/icon_vibration.png` · `ui/icon_settings.png` · `ui/icon_star.png` (victoria) ·
`ui/icon_lock.png` · `ui/icon_weapon.png` (tier de arma)

(Paneles, botones y barras ya se generan por código con la paleta; no hacen falta.)

## 8. LOTE 5 — Branding y tienda

| Archivo | Lienzo | Contenido |
|---|---|---|
| `branding/app_icon_512.png` | 512×512 | Icono de app: zombie o casco de soldado + mira, sobre `#1A1830`/degradado del cielo, borde neón. SIN texto |
| `branding/app_icon_fg_432.png` | 432×432 | Capa *foreground* para icono adaptativo Android (el motivo centrado en el 66 % central, resto transparente) |
| `branding/app_icon_bg_432.png` | 432×432 | Capa *background* (color/degradado plano, sin motivo) |
| `branding/wordmark_1024x256.png` | 1024×256 | Logotipo "ZOMBIE RUSH": "ZOMBIE" hueso `#F4F1E8` + "RUSH" cian `#3DD6F5`, con glow, transparente |
| `branding/splash_1024x512.png` | 1024×512 | Wordmark + motivo mínimo, sobre transparente (se muestra sobre `#14122A`) |
| `branding/feature_graphic_1024x500.png` | 1024×500 | Gráfico de ficha de Google Play: escena escuadrón vs horda + wordmark |
| `branding/social_preview_1280x640.png` | 1280×640 | Imagen social del repo de GitHub (composición similar al banner) |

## 9. Criterios de aceptación (checklist final)

- [ ] PNG con transparencia real, sin halos ni fondos.
- [ ] Nombres de archivo y carpetas EXACTOS a las tablas.
- [ ] Personajes mirando a la derecha; poses del mismo personaje con el mismo
      lienzo/anclaje (sin "saltos" entre frames).
- [ ] Zombies base en claros desaturados (tintables); jefe en color final.
- [ ] Tiles seamless comprobados (repetidos 2×2 no se ve la costura).
- [ ] Gates con el centro despejado para el texto del juego.
- [ ] Silueta legible al 50 % del tamaño.
- [ ] Sin texto horneado (salvo wordmark/branding).
- [ ] Paleta respetada (los hex de la sección 2).
- [ ] Arte original (sin IPs de terceros).

## 10. Prompt de arranque sugerido para Claude Design

> Eres el artista de "Zombie Rush", un crowd-shooter móvil vertical con vista
> cenital y mood de noche apocalíptica neón. Te paso el brief completo del juego
> (documento adjunto): dirección de arte, paleta hex obligatoria y el inventario
> de assets con nombres de archivo y tamaños exactos. Empieza por el **LOTE 1
> (personajes)**: genera las 6 poses del soldado, las 6 del zombie base (en claros
> desaturados tintables, salvo el jefe en color) y las 3 del superviviente,
> respetando la sección 3 (specs de entrega) y la checklist de la sección 9.
> Cuando lo valide, seguimos con los lotes 2–5.

---

*Documento generado desde el repo `zombie-rush` (los nombres de archivo se
corresponden con las claves que carga `ArtCache` desde `Assets/Resources/Art/`).
Los assets entregados sustituyen a los placeholders CC0 de Kenney actuales;
recuerda actualizar `Assets/Resources/Art/ATTRIBUTION.md` al integrarlos.*
