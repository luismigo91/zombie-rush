#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Genera el BRIEF VISUAL de Zombie Rush para construir la web del juego.

Lee los assets reales del repo (Assets/Branding, Assets/Resources/Art), aplica
los tintes exactos que usa el juego (skins, bestiario, noche) y produce un HTML
A4 listo para imprimir a PDF con Chrome headless:

    python3 build_brief.py
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless \
        --no-pdf-header-footer --print-to-pdf=ZombieRush-Web-Visual-Brief.pdf \
        file://$PWD/ZombieRush-WebBrief.html
"""
import base64
import io
import os

from PIL import Image, ImageChops

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ART = os.path.join(ROOT, "Assets", "Resources", "Art")
BRAND = os.path.join(ROOT, "Assets", "Branding")
OUT = os.path.join(HERE, "ZombieRush-WebBrief.html")

# ---------------------------------------------------------------- utilidades

def uri_file(path):
    with open(path, "rb") as f:
        return "data:image/png;base64," + base64.b64encode(f.read()).decode()

def art(rel):
    return uri_file(os.path.join(ART, rel))

def brand(rel):
    return uri_file(os.path.join(BRAND, rel))

def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))

def tinted(path, hexc):
    """Multiplica el sprite por un color, igual que SpriteRenderer.color en Unity."""
    img = Image.open(path).convert("RGBA")
    r, g, b = hex_rgb(hexc)
    solid = Image.new("RGBA", img.size, (r, g, b, 255))
    out = ImageChops.multiply(img, solid)
    buf = io.BytesIO()
    out.save(buf, format="PNG")
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()

def art_tinted(rel, hexc):
    return tinted(os.path.join(ART, rel), hexc)

# ---------------------------------------------------------------- paleta real (extraída del código)

PAL_BRAND = [
    ("Cyan Neón", "#3DD6F5", "Color de marca. Acentos UI, bordes de panel, barra de progreso, 'RUSH' del logo."),
    ("Bone", "#F4F1E8", "Texto principal sobre fondos oscuros ('ZOMBIE' del logo)."),
    ("Magenta", "#FF4D8D", "Acento secundario. Ventanas del skyline, alertas especiales."),
    ("Panel", "#1A1830", "Fondo de paneles y tarjetas de UI (uGUI, 85–90% alpha)."),
    ("Dark", "#14141C", "Fondo de botones secundarios y overlays."),
    ("Gun Gray", "#2E2E36", "Gris de armas y elementos neutros."),
]
PAL_WORLD = [
    ("Cielo (alto)", "#14122A", "Tope del degradado del cielo. Fondo base de toda la estética."),
    ("Cielo (bajo)", "#241A3A", "Base del degradado del cielo, hacia el horizonte."),
    ("Niebla", "#3A3060", "Bandas de niebla semitransparente que derivan en pantalla."),
    ("Farola ámbar", "#E8A23A", "Light2D de las farolas de cada bioma (brilla más de noche)."),
    ("Tinte noche · cielo", "#7A7AB8", "Multiplicador nocturno del cielo (niveles x7–x9)."),
    ("Tinte noche · suelo", "#8F8FC2", "Multiplicador nocturno de calzada y arcenes."),
]
PAL_PLAY = [
    ("Gate bueno", "#3DE0C8", "Puertas que multiplican/suman escuadrón. Verde-cian confiable."),
    ("Gate malo", "#FF5A3C", "Puertas que restan. Naranja-rojo de peligro."),
    ("Oro", "#FFD23A", "Monedas, récords, Desafío Diario, texto de daño."),
    ("Lima", "#5BD66A", "Confirmaciones y positivos."),
    ("Rojo zombie", "#FF3B3B", "Daño recibido, derrota, ojos de la horda."),
    ("Power-up SLOW", "#5C8FFF", "Chip de ralentización de horda en el HUD."),
    ("Power-up HIELO", "#8CD9FF", "Chip de congelación en el HUD (y habilidad Congelación)."),
    ("Ácido escupidor", "#8CF24D", "Proyectil enemigo esquivable (EnemyShot)."),
]

# Bestiario: tinte, tamaño de mundo, notas — valores reales de LevelRunner/Enemy.
BESTIARY = [
    ("Zombie", "#80B04F", 0.55, "×1.0 vel · ×1.0 vida", "La carne de la horda. Contacto: −1 soldado."),
    ("Corredor", "#E8C74A", 0.45, "×1.7 vel · ×0.7 vida", "Pequeño y rapidísimo: prioriza derribarlo."),
    ("Tanque", "#9C5CD6", 0.85, "×0.7 vel · ×2.2 vida", "Lento y masivo. Esponja de balas."),
    ("Explotador", "#FF8C33", 0.70, "×0.95 vel · ×1.6 vida", "Mátalo LEJOS: al contacto revienta y resta 3."),
    ("Escupidor", "#8CF259", 0.50, "×0.9 vel · ×1.1 vida", "Se planta y dispara ácido esquivable (no derribable)."),
    ("Chillón", "#F24DD9", 0.60, "×0.9 vel · ×1.3 vida", "Grita y acelera a la horda cercana: derríbalo pronto."),
]

SKINS = [
    ("CLÁSICO", None, "Gratis"),
    ("DESIERTO", "#E8D1A0", "250 monedas"),
    ("ÁRTICO", "#A8C7E8", "250 monedas"),
    ("SOMBRA", "#9194A8", "500 monedas"),
    ("ORO", "#FFD13B", "1000 monedas"),
]

WEAPONS = [
    ("Pistola", "×1.0", "×1.0", "—", "—", "Arma inicial, fiable."),
    ("Subfusil", "×0.7", "×2.0", "—", "—", "Cadencia pura."),
    ("Escopeta", "×1.6", "×0.9", "+2", "2", "Pega duro y atraviesa."),
    ("Rifle", "×1.3", "×1.4", "+1", "1", "Equilibrio total."),
    ("Minigun", "×0.85", "×2.8", "+2", "1", "Muro de plomo."),
    ("Láser", "×2.2", "×1.3", "+3", "4", "Tier final: perfora la horda entera."),
]

PERKS = [
    ("POTENCIA", "+12% de daño por disparo"),
    ("CADENCIA", "+8% de cadencia de fuego"),
    ("PERFORANTE", "Las balas atraviesan +1 enemigo"),
    ("IMÁN", "+30% de radio para recoger power-ups"),
    ("BLINDAJE", "+2.5s de escudo al empezar cada nivel"),
    ("REFUERZOS", "+2 soldados iniciales"),
    ("SUERTE", "+33% de probabilidad de power-up"),
]

ABILITIES = [
    ("GRANADA", "#FF9940", "Explosión en área sobre la horda. Daño instantáneo."),
    ("CONGELACIÓN", "#8CD9FF", "Congela a la horda unos segundos. Control de masas."),
    ("CENTINELA", "#3DD6F5", "Torreta aliada temporal que dispara sola."),
]

BIOMES = [
    ("suburbs", "Suburbios", "Actos I–II · Niveles 1–20",
     ["coche", "farola ✦", "valla", "arbusto", "señal", "tapa", "cono", "neumáticos"]),
    ("downtown", "Centro urbano", "Actos III–IV · Niveles 21–40",
     ["bus", "escombros", "barricada", "semáforo", "neón ✦", "cartel", "basura", "tablas"]),
    ("cemetery", "Cementerio", "Actos V–VI · Niveles 41–60",
     ["lápida", "cruz", "árbol", "verja", "mausoleo", "farol ✦", "banco", "pozo"]),
    ("industrial", "Zona industrial", "Actos VII–VIII · Niveles 61–80",
     ["contenedor", "bidón", "palé", "tubería", "charco", "tubo", "foco ✦", "bidones"]),
    ("lab", "Laboratorio", "Actos IX–X · Niveles 81–100",
     ["tanque", "luz ✦", "cables", "pantalla", "cápsula", "torreta", "valla", "puerta"]),
]

UI_ICONS = [
    ("icon_play", "Jugar"), ("icon_pause", "Pausa"), ("icon_restart", "Reiniciar"),
    ("icon_home", "Menú"), ("icon_settings", "Ajustes"), ("icon_music", "Música"),
    ("icon_music_off", "Música off"), ("icon_sfx", "SFX"), ("icon_sfx_off", "SFX off"),
    ("icon_vibration", "Vibración"), ("icon_coin", "Moneda"), ("icon_star", "Estrella"),
    ("icon_skull", "Calavera"), ("icon_unit", "Soldado"), ("icon_weapon", "Arma"),
    ("icon_lock", "Bloqueado"),
]

SOLDIER_POSES = [
    ("soldier_stand", "stand"), ("soldier_hold", "hold"), ("soldier_gun", "gun"),
    ("soldier_reload", "reload"), ("soldier_silencer", "silencer"), ("soldier_machine", "machine"),
]
ZOMBIE_POSES = [
    ("zombie_stand", "stand"), ("zombie_hold", "hold"), ("zombie_gun", "gun"),
    ("zombie_reload", "reload"), ("zombie_silencer", "silencer"),
]
SURVIVOR_POSES = [("survivor_stand", "stand"), ("survivor_hold", "hold"), ("survivor_gun", "gun")]

# ---------------------------------------------------------------- helpers HTML

def swatch(name, hexc, use):
    return f'''<div class="sw">
      <div class="sw-c" style="background:{hexc}"></div>
      <div class="sw-t"><b>{name}</b><code>{hexc}</code><span>{use}</span></div>
    </div>'''

def fig(u, label, wmm, sub=None, cls=""):
    s = f'<span class="sub">{sub}</span>' if sub else ""
    return f'''<figure class="fig {cls}"><div class="fig-box"><img src="{u}" style="width:{wmm}mm"></div>
      <figcaption>{label}{s}</figcaption></figure>'''

def h2(title, sub=""):
    s = f'<p class="h2sub">{sub}</p>' if sub else ""
    return f'<div class="h2w"><h2>{title}</h2>{s}</div>'

def page(num, total, body, cls=""):
    return f'''<div class="page {cls}">
      <div class="phead"><span class="ph-brand">ZOMBIE&nbsp;<i>RUSH</i></span><span class="ph-doc">Brief visual para la web</span></div>
      {body}
      <div class="pfoot"><span>Assets reales del repo · colores extraídos del código</span><span>{num} / {total}</span></div>
    </div>'''

# ---------------------------------------------------------------- carga de imágenes

print("Cargando assets…")

IMG = {
    "wordmark": brand("wordmark_1024x256.png"),
    "icon": brand("app_icon_512.png"),
    "icon_fg": brand("app_icon_fg_432.png"),
    "icon_bg": brand("app_icon_bg_432.png"),
    "feature": brand("feature_graphic_1024x500.png"),
    "social": brand("social_preview_1280x640.png"),
    "splash": brand("splash_1024x512.png"),
    "bullet": art("combat/bullet.png"),
    "muzzle": art("fx/muzzle.png"),
    "gate_good": art("combat/gate_good.png"),
    "gate_bad": art("combat/gate_bad.png"),
    "cage": art("combat/cage.png"),
    "cage_broken": art("combat/cage_broken.png"),
    "barrier_full": art("combat/barrier_full.png"),
    "barrier_damaged": art("combat/barrier_damaged.png"),
    "coin": art("items/coin.png"),
    "chest": art("items/chest.png"),
    "boss": art("characters/zombie_machine.png"),
}
for name, _ in SOLDIER_POSES + ZOMBIE_POSES + SURVIVOR_POSES:
    IMG[name] = art(f"characters/{name}.png")
for i in range(1, 7):
    IMG[f"coin_{i}"] = art(f"items/coin_spin/coin_0{i}.png")
for icon, _ in UI_ICONS:
    IMG[icon] = art(f"ui/{icon}.png")
for key, _, _, _ in BIOMES:
    IMG[f"sky_{key}"] = art(f"environment/skyline_{key}.png")
    IMG[f"edge_{key}"] = art(f"environment/edge_{key}.png")
    for i in range(1, 5):
        IMG[f"road_{key}_{i}"] = art(f"environment/road_{key}_0{i}.png")
    for i in range(1, 9):
        IMG[f"prop_{key}_{i}"] = art(f"environment/prop_{key}_0{i}.png")

print("Aplicando tintes del juego…")
for name, hexc, _size, _stats, _note in BESTIARY:
    IMG[f"z_{name}"] = art_tinted("characters/zombie_stand.png", hexc)
for name, hexc, _cost in SKINS:
    IMG[f"skin_{name}"] = art_tinted("characters/soldier_stand.png", hexc) if hexc else IMG["soldier_stand"]

# Escena día/noche (tintes multiplicativos reales de Environment)
IMG["night_road"] = art_tinted("environment/road_suburbs_02.png", "#8F8FC2")
IMG["night_prop"] = art_tinted("environment/prop_suburbs_02.png", "#9E9ED9")
IMG["day_road"] = IMG["road_suburbs_2"]
IMG["day_prop"] = IMG["prop_suburbs_2"]

# ---------------------------------------------------------------- CSS

CSS = """
* { margin:0; padding:0; box-sizing:border-box; }
:root { --sky1:#14122A; --sky2:#241A3A; --panel:#1A1830; --cyan:#3DD6F5; --bone:#F4F1E8;
        --magenta:#FF4D8D; --gold:#FFD23A; --bad:#FF5A3C; --good:#3DE0C8; --dark:#14141C; }
html,body { -webkit-print-color-adjust:exact; print-color-adjust:exact; background:#0C0B18;
  font-family:'Avenir Next','Helvetica Neue',Arial,sans-serif; color:var(--bone); }
@page { size:A4; margin:0; }
.page { width:210mm; height:297mm; padding:12mm 13mm 9mm; overflow:hidden; position:relative;
  page-break-after:always; background:linear-gradient(180deg,#14122A 0%,#1B1631 55%,#241A3A 100%); }
.phead { display:flex; justify-content:space-between; align-items:baseline;
  border-bottom:0.5mm solid rgba(61,214,245,0.35); padding-bottom:2mm; margin-bottom:5mm; }
.ph-brand { font-weight:800; font-style:italic; letter-spacing:0.8mm; font-size:11px; color:var(--bone); }
.ph-brand i { color:var(--cyan); }
.ph-doc { font-size:9px; color:rgba(244,241,232,0.55); letter-spacing:0.5mm; text-transform:uppercase; }
.pfoot { position:absolute; bottom:6mm; left:13mm; right:13mm; display:flex; justify-content:space-between;
  font-size:8px; color:rgba(244,241,232,0.4); }
.h2w { margin:1mm 0 4mm; }
h2 { font-size:21px; font-weight:800; font-style:italic; letter-spacing:0.3mm; color:var(--bone); text-transform:uppercase; }
h2::after { content:""; display:block; width:22mm; height:1.1mm; background:var(--cyan);
  border-radius:1mm; margin-top:1.2mm; box-shadow:0 0 3mm rgba(61,214,245,0.8); }
.h2sub { font-size:10.5px; color:rgba(244,241,232,0.7); margin-top:2mm; max-width:165mm; line-height:1.45; }
h3 { font-size:12.5px; font-weight:700; color:var(--cyan); margin:4mm 0 2.5mm; text-transform:uppercase; letter-spacing:0.4mm; }
p, li { font-size:10.5px; line-height:1.5; color:rgba(244,241,232,0.85); }
code { font-family:Menlo,monospace; font-size:9px; color:var(--cyan); }
.panel { background:rgba(26,24,48,0.88); border:0.4mm solid rgba(61,214,245,0.5); border-radius:2.5mm; padding:4mm; }
.muted { color:rgba(244,241,232,0.55); }
.grid { display:grid; gap:3mm; }
.fig { text-align:center; }
.fig-box { background:rgba(26,24,48,0.65); border:0.3mm solid rgba(61,214,245,0.25); border-radius:2mm;
  padding:2.5mm; display:flex; align-items:center; justify-content:center; }
.fig-box img { display:block; max-width:100%; }
.fig figcaption { font-size:8.5px; color:rgba(244,241,232,0.75); margin-top:1.4mm; font-weight:600; }
.fig .sub { display:block; font-size:7.5px; color:rgba(244,241,232,0.45); font-weight:400; }
.fig-light .fig-box { background:#F4F1E8; }
.sw { display:flex; gap:2.5mm; align-items:center; background:rgba(26,24,48,0.65);
  border:0.3mm solid rgba(61,214,245,0.2); border-radius:2mm; padding:2.2mm; }
.sw-c { width:11mm; height:11mm; border-radius:1.6mm; flex:none; border:0.3mm solid rgba(255,255,255,0.25); }
.sw-t { display:flex; flex-direction:column; gap:0.4mm; min-width:0; }
.sw-t b { font-size:9.5px; }
.sw-t span { font-size:7.6px; color:rgba(244,241,232,0.6); line-height:1.35; }
.chip { display:inline-block; padding:1mm 2.6mm; border-radius:5mm; font-size:9px; font-weight:700;
  border:0.35mm solid; margin:0 1.2mm 1.2mm 0; }
table { border-collapse:collapse; width:100%; }
th { font-size:8.5px; text-transform:uppercase; letter-spacing:0.3mm; color:var(--cyan); text-align:left;
  padding:1.6mm 2mm; border-bottom:0.4mm solid rgba(61,214,245,0.45); }
td { font-size:9.5px; padding:1.7mm 2mm; border-bottom:0.25mm solid rgba(244,241,232,0.12); vertical-align:top; }
td b { color:var(--bone); }
.two { display:grid; grid-template-columns:1fr 1fr; gap:5mm; }
.hero-note { font-size:9px; color:rgba(244,241,232,0.5); margin-top:1.5mm; }
"""

# ---------------------------------------------------------------- páginas

pages_body = []

# --- 1 · PORTADA
pages_body.append((f'''
<div style="height:245mm; display:flex; flex-direction:column; align-items:center; justify-content:center; text-align:center; position:relative;">
  <img src="{IMG['sky_downtown']}" style="position:absolute; bottom:-6mm; left:-13mm; width:210mm; opacity:0.5;">
  <img src="{IMG['sky_suburbs']}" style="position:absolute; bottom:14mm; left:-13mm; width:210mm; opacity:0.22;">
  <div style="position:relative;">
    <img src="{IMG['icon']}" style="width:46mm; border-radius:10mm; box-shadow:0 0 14mm rgba(61,214,245,0.45); margin-bottom:10mm;">
    <img src="{IMG['wordmark']}" style="width:150mm; display:block; margin:0 auto;">
    <p style="font-size:16px; font-weight:600; color:rgba(244,241,232,0.92); margin-top:6mm; letter-spacing:0.3mm;">
      Haz crecer tu escuadrón, elige bien tus puertas y arrasa con la horda.</p>
    <p style="font-size:11px; color:rgba(244,241,232,0.6); margin-top:2.5mm;">
      Shooter arcade de multitud · 100 niveles · jefes · perks · modos sin fin</p>
    <div style="margin-top:11mm;">
      <span class="chip" style="color:#3DD6F5; border-color:#3DD6F5;">ANDROID · VERTICAL 9:16</span>
      <span class="chip" style="color:#3DE0C8; border-color:#3DE0C8;">UNITY 6 · URP 2D</span>
      <span class="chip" style="color:#FFD23A; border-color:#FFD23A;">GRATIS</span>
    </div>
    <p style="font-size:9.5px; color:rgba(244,241,232,0.45); margin-top:13mm; max-width:130mm; line-height:1.6;">
      BRIEF VISUAL PARA LA WEB OFICIAL — identidad, paleta, personajes, biomas, UI y contenido,
      con los assets reales del juego y los colores exactos extraídos del código. Julio 2026.</p>
  </div>
</div>'''))

# --- 2 · EL JUEGO
feat_rows = [
    ("🧟", "100 niveles fijos", "Campaña determinista en 10 actos: cada partida del nivel N es idéntica y aprendible."),
    ("🚪", "Gates que multiplican", "Puertas ×2 / +N contra puertas trampa: la decisión constante que define el género."),
    ("👥", "El escuadrón es la vida", "Cada soldado cuenta: la horda erosiona tu frente y las jaulas te refuerzan."),
    ("👹", "Jefes cada 10 niveles", "4 patrones distintos — invocador, embestida, escupidor y combinado con enrage."),
    ("🔫", "6 armas por tiers", "De la Pistola al Láser perforante, subiendo con gates de ARMA+ en plena carrera."),
    ("⭐", "Perks permanentes", "Al ganar un nivel eliges 1 de 3 mejoras que quedan para siempre."),
    ("🌙", "Ciclo día / noche", "Los niveles x7–x9 de cada acto anochecen: tintes fríos, farolas y tensión extra."),
    ("♾️", "Modos sin fin", "Supervivencia por oleadas y Desafío Diario con semilla por fecha y récords."),
]
feat_html = "".join(
    f'<div class="panel" style="padding:3mm 3.5mm; display:flex; gap:2.5mm; align-items:flex-start;">'
    f'<span style="font-size:15px; line-height:1;">{e}</span>'
    f'<div><b style="font-size:10px;">{t}</b><p style="font-size:8.6px; line-height:1.4; margin-top:0.8mm;">{d}</p></div></div>'
    for e, t, d in feat_rows)
pages_body.append(h2("El juego", "Texto y ganchos listos para el hero y las secciones de features de la web.") + f'''
<div class="panel" style="margin-bottom:4mm;">
  <p style="font-size:12px; line-height:1.55;"><b style="color:var(--cyan);">Zombie Rush</b> es un shooter arcade de multitud:
  arrastras un <b>escuadrón que dispara solo</b> por una carretera infestada, cruzas <b>gates</b> que lo multiplican
  (o lo castigan), liberas supervivientes enjaulados y aguantas la erosión de la horda hasta el jefe.
  Corto, táctil y con progresión rogue-lite: <b>perks</b>, <b>armas</b>, <b>habilidades</b> y una meta-tienda
  que fija tu punto de partida.</p>
</div>
<h3>Cómo se juega (4 pasos para la web)</h3>
<div class="grid" style="grid-template-columns:repeat(4,1fr); margin-bottom:4mm;">
  <div class="panel" style="padding:3mm;"><b style="color:var(--cyan); font-size:12px;">1 · DESLIZA</b><p style="font-size:8.8px;">Mueve el escuadrón en horizontal con un dedo. El disparo es automático.</p></div>
  <div class="panel" style="padding:3mm;"><b style="color:var(--good); font-size:12px;">2 · RECLUTA</b><p style="font-size:8.8px;">Elige la puerta buena de cada par y rompe jaulas para crecer.</p></div>
  <div class="panel" style="padding:3mm;"><b style="color:var(--bad); font-size:12px;">3 · AGUANTA</b><p style="font-size:8.8px;">La horda muerde tu frente: cada zombie que llega resta soldados.</p></div>
  <div class="panel" style="padding:3mm;"><b style="color:var(--gold); font-size:12px;">4 · MEJORA</b><p style="font-size:8.8px;">Gana perks, monedas y sube tu punto de partida en la tienda.</p></div>
</div>
<h3>Features para la web</h3>
<div class="grid" style="grid-template-columns:repeat(2,1fr);">{feat_html}</div>
<p class="hero-note">El banner inferior (feature graphic 1024×500) funciona como imagen hero. Variante 1280×640 disponible para Open Graph / social.</p>
<img src="{IMG['feature']}" style="width:76%; display:block; margin:2mm auto 0; border-radius:2.5mm; border:0.3mm solid rgba(61,214,245,0.3);">
''')

# --- 3 · IDENTIDAD
pages_body.append(h2("Identidad de marca", "Todos los archivos viven en Assets/Branding/ (PNG con transparencia donde aplica).") + f'''
<div class="two" style="margin-bottom:4mm;">
  <div>
    <h3>Wordmark</h3>
    {fig(IMG['wordmark'], "wordmark_1024x256.png · sobre oscuro", 78, "«ZOMBIE» en Bone + «RUSH» en Cyan Neón, itálica black, outline oscuro y glow")}
    <div style="height:2.5mm;"></div>
    {fig(IMG['wordmark'], "El wordmark también funciona sobre claro", 78, "conserva el outline: legible en ambos fondos", "fig-light")}
  </div>
  <div>
    <h3>Icono de app</h3>
    <div class="grid" style="grid-template-columns:1fr 1fr 1fr;">
      {fig(IMG['icon'], "app_icon_512.png", 26, "icono maestro")}
      {fig(IMG['icon_fg'], "app_icon_fg_432.png", 26, "adaptive · foreground")}
      {fig(IMG['icon_bg'], "app_icon_bg_432.png", 26, "adaptive · background")}
    </div>
    <p style="font-size:8.8px; margin-top:2mm;" class="muted">Motivo de marca: <b style="color:var(--bone);">zombie en la retícula</b> —
    cara verde de ojos rojos dentro de un visor cian neón sobre skyline nocturno. La retícula puede reutilizarse
    en la web como elemento decorativo (favicon, bullets, cursores, separadores).</p>
  </div>
</div>
<div class="two">
  <div>
    <h3>Splash (fondo claro)</h3>
    {fig(IMG['splash'], "splash_1024x512.png", 78, "única pieza pensada para fondo blanco")}
  </div>
  <div>
    <h3>Social / Open Graph</h3>
    {fig(IMG['social'], "social_preview_1280x640.png", 78, "1280×640 · GitHub social preview y og:image")}
  </div>
</div>
<h3>Reglas rápidas</h3>
<ul style="padding-left:5mm;">
  <li>Fondo por defecto de la web: degradado <code>#14122A → #241A3A</code> con skylines como siluetas decorativas.</li>
  <li>El glow cian (<code>#3DD6F5</code> con blur/alpha) es la firma: úsalo en CTAs, bordes y el wordmark.</li>
  <li>No recolorear el wordmark ni el icono; sí se pueden escalar (PNG a resolución generosa).</li>
</ul>
''')

# --- 4 · COLOR
def swgrid(items, cols=3):
    return f'<div class="grid" style="grid-template-columns:repeat({cols},1fr);">' + "".join(
        swatch(n, h, u) for n, h, u in items) + "</div>"

pages_body.append(h2("Paleta de color", "Colores exactos extraídos del código (UGui, Environment, LevelRunner). El mood es «noche apocalíptica neón»: fondos morados muy oscuros + neones fríos + acentos cálidos de peligro/recompensa.") + f'''
<h3>Marca & interfaz</h3>{swgrid(PAL_BRAND)}
<h3>Mundo & atmósfera</h3>{swgrid(PAL_WORLD)}
<h3>Gameplay & feedback</h3>{swgrid(PAL_PLAY, 4)}
<h3>Degradado de cielo (fondo canónico)</h3>
<div style="height:16mm; border-radius:2.5mm; background:linear-gradient(180deg,#14122A,#241A3A); border:0.3mm solid rgba(61,214,245,0.3); display:flex; align-items:center; justify-content:space-between; padding:0 4mm;">
  <code style="font-size:10px;">#14122A</code><span style="font-size:9px; color:rgba(244,241,232,0.6);">SKY_TOP → SKY_BOT · vertical</span><code style="font-size:10px;">#241A3A</code>
</div>
<p style="font-size:9px; margin-top:3mm;" class="muted">Sugerencia para la web: texto sobre estos fondos siempre en Bone <code>#F4F1E8</code>;
enlaces y foco en Cyan Neón <code>#3DD6F5</code>; éxito/CTA primario en Gate bueno <code>#3DE0C8</code>; alertas en Gate malo <code>#FF5A3C</code>.</p>
''')

# --- 5 · ESCUADRÓN
poses_html = "".join(fig(IMG[n], l, 17) for n, l in SOLDIER_POSES)
skins_html = "".join(
    fig(IMG[f"skin_{n}"], n, 15, c) for n, _h, c in SKINS)
surv_html = "".join(fig(IMG[n], l, 15) for n, l in SURVIVOR_POSES)
pages_body.append(h2("El escuadrón", "El jugador ES el escuadrón: un disco de soldados (ancho ∝ √N) que se mueve en X y dispara recto por streams. Sprites top-down 96×96 en Assets/Resources/Art/characters/.") + f'''
<h3>Poses del soldado (pseudo-ciclos de 2–3 frames)</h3>
<div class="grid" style="grid-template-columns:repeat(6,1fr);">{poses_html}</div>
<p style="font-size:8.8px; margin-top:1.5mm;" class="muted">Marcha = stand→hold→gun · Disparo = gun→hold. La animación alterna poses, no hay spritesheet clásico.</p>
<h3>Skins (tinte multiplicativo real del juego, se compran en la meta-tienda)</h3>
<div class="grid" style="grid-template-columns:repeat(5,1fr);">{skins_html}</div>
<div class="two" style="margin-top:4mm;">
  <div>
    <h3>Superviviente (jaulas)</h3>
    <div class="grid" style="grid-template-columns:repeat(3,1fr);">{surv_html}</div>
    <p style="font-size:8.8px; margin-top:1.5mm;" class="muted">Encerrados en jaulas por el recorrido: dispara al candado y se unen al escuadrón.</p>
  </div>
  <div>
    <h3>Escalas de mundo (referencia)</h3>
    <div class="panel">
      <table>
        <tr><th>Entidad</th><th>Tamaño</th><th>Nota</th></tr>
        <tr><td><b>Soldado</b></td><td>0.32 u</td><td>unidad base del disco</td></tr>
        <tr><td><b>Zombie</b></td><td>0.45–0.85 u</td><td>según variante</td></tr>
        <tr><td><b>Jefe</b></td><td>1.7 u</td><td>≈ 5 soldados de alto</td></tr>
      </table>
      <p style="font-size:8.6px; margin-top:2mm;" class="muted">Todos los sprites se normalizan a ~1×1 unidad por código (ArtCache) y se escalan después: en la web pueden mezclarse respetando estas proporciones.</p>
    </div>
  </div>
</div>
''')

# --- 6 · HORDA
zposes_html = "".join(fig(IMG[n], l, 15) for n, l in ZOMBIE_POSES)
best_html = "".join(f'''<div class="panel" style="padding:3mm; text-align:center;">
    <img src="{IMG[f"z_{n}"]}" style="width:{10 + s * 18:.0f}mm;">
    <div style="margin-top:1.5mm;"><span class="chip" style="color:{h}; border-color:{h};">{n.upper()}</span></div>
    <p style="font-size:8.2px; color:rgba(244,241,232,0.55);">{st}</p>
    <p style="font-size:8.6px; margin-top:0.8mm;">{note}</p>
  </div>''' for n, h, s, st, note in BESTIARY)
pages_body.append(h2("La horda", "El sprite base del zombie es GRIS y el juego lo tiñe por variante (tintes exactos reproducidos aquí). Cada tipo introduce counterplay propio y se presenta escalonado a lo largo de la campaña.") + f'''
<h3>Poses base (gris tintable)</h3>
<div class="grid" style="grid-template-columns:repeat(5,1fr);">{zposes_html}</div>
<h3>Bestiario (color, tamaño relativo y rasgo)</h3>
<div class="grid" style="grid-template-columns:repeat(3,1fr);">{best_html}</div>
<div class="two" style="margin-top:4mm;">
  <div class="panel" style="display:flex; gap:4mm; align-items:center;">
    <img src="{IMG['boss']}" style="width:34mm; flex:none;">
    <div>
      <span class="chip" style="color:#73B333; border-color:#73B333;">JEFE · CADA 10 NIVELES</span>
      <p style="font-size:8.8px; margin-top:1.5mm;">Silueta masiva (zombie_machine, 256×256, escala ×1.7). Un patrón por acto:
      <b>Invocador</b>, <b>Embestida</b>, <b>Escupidor</b> y <b>Combinado con enrage</b>. Al contacto resta 4 soldados y rebota. La victoria del acto llega al derribarlo.</p>
    </div>
  </div>
  <div class="panel">
    <b style="font-size:10px; color:var(--cyan);">PROYECTIL ENEMIGO</b>
    <p style="font-size:8.8px; margin-top:1.5mm;">El Escupidor dispara glóbulos de ácido <code>#8CF24D</code>:
    <b>esquivables pero no derribables</b> — el único ataque a distancia de la horda, y la razón para no dejar de moverse.
    En la web puede representarse como puntos verdes brillantes con estela.</p>
    <div style="margin-top:2mm; display:flex; gap:2mm; align-items:center;">
      <span style="width:6mm; height:6mm; border-radius:50%; background:#8CF24D; box-shadow:0 0 3mm #8CF24D; display:inline-block;"></span>
      <span style="width:4.5mm; height:4.5mm; border-radius:50%; background:#8CF24D; opacity:0.7; box-shadow:0 0 2mm #8CF24D; display:inline-block;"></span>
      <span style="width:3mm; height:3mm; border-radius:50%; background:#8CF24D; opacity:0.4; display:inline-block;"></span>
    </div>
  </div>
</div>
''')

# --- 7/8/9 · BIOMAS
def biome_block(key, title, when, props):
    roads = "".join(fig(IMG[f"road_{key}_{i}"], f"road_0{i}", 16) for i in range(1, 5))
    edge = fig(IMG[f"edge_{key}"], "edge (arcén)", 16)
    props_html = "".join(fig(IMG[f"prop_{key}_{i}"], props[i - 1], 13) for i in range(1, 9))
    return f'''
  <div style="margin-bottom:5mm;">
    <div style="display:flex; justify-content:space-between; align-items:baseline; margin-bottom:1.6mm;">
      <h3 style="margin:0;">{title}</h3><span style="font-size:8.5px; color:var(--gold);">{when}</span>
    </div>
    <img src="{IMG[f'sky_{key}']}" style="width:100%; border-radius:2mm; border:0.3mm solid rgba(61,214,245,0.25); background:linear-gradient(180deg,#14122A,#241A3A); margin-bottom:2mm;">
    <div class="grid" style="grid-template-columns:repeat(5,1fr); margin-bottom:2mm;">{roads}{edge}</div>
    <div class="grid" style="grid-template-columns:repeat(8,1fr);">{props_html}</div>
  </div>'''

pages_body.append(h2("Biomas · I", "5 localizaciones, una cada 2 actos. Cada bioma aporta skyline de horizonte (1024×256), 4 tiles de calzada, tile de arcén y 8 props laterales. Los props marcados con ✦ llevan Light2D ámbar #E8A23A.")
    + biome_block(*[(b[0], b[1], b[2], b[3]) for b in BIOMES][0])
    + biome_block(*[(b[0], b[1], b[2], b[3]) for b in BIOMES][1]))

pages_body.append(h2("Biomas · II")
    + biome_block(*[(b[0], b[1], b[2], b[3]) for b in BIOMES][2])
    + biome_block(*[(b[0], b[1], b[2], b[3]) for b in BIOMES][3]))

day_night = f'''
<div class="two" style="margin-top:1mm;">
  <div>
    <h3 style="margin-top:0;">Día (niveles x1–x6, x10)</h3>
    <div style="position:relative; height:52mm; border-radius:2.5mm; overflow:hidden; border:0.3mm solid rgba(61,214,245,0.3);
         background-image:url('{IMG["day_road"]}'); background-size:26mm;">
      <img src="{IMG['day_prop']}" style="position:absolute; width:20mm; left:6mm; bottom:8mm;">
      <img src="{IMG['prop_suburbs_1']}" style="position:absolute; width:26mm; right:5mm; top:6mm;">
    </div>
  </div>
  <div>
    <h3 style="margin-top:0;">Noche (niveles x7–x9)</h3>
    <div style="position:relative; height:52mm; border-radius:2.5mm; overflow:hidden; border:0.3mm solid rgba(61,214,245,0.3);
         background-image:url('{IMG["night_road"]}'); background-size:26mm;">
      <img src="{art_tinted('environment/prop_suburbs_01.png', '#9E9ED9')}" style="position:absolute; width:26mm; right:5mm; top:6mm;">
      <div style="position:absolute; inset:0; background:rgba(15,15,46,0.22);"></div>
      <div style="position:absolute; width:34mm; height:34mm; left:0; bottom:0; background:radial-gradient(circle at 16mm 22mm, rgba(232,162,58,0.55), rgba(232,162,58,0) 60%);"></div>
      <img src="{IMG['night_prop']}" style="position:absolute; width:20mm; left:6mm; bottom:8mm;">
    </div>
  </div>
</div>
<p style="font-size:8.8px; margin-top:2mm;" class="muted">La noche se logra con <b>tintes multiplicativos</b> (cielo <code>#7A7AB8</code>, suelo <code>#8F8FC2</code>, props <code>#9E9ED9</code>)
+ un velo azul <code>#0F0F2E</code> al 22% sobre el gameplay + farolas ámbar con más radio. Reproducible en la web con CSS
(filter/blend) sobre los mismos assets. El HUD anuncia «ANOCHECE — la horda avanza en la penumbra».</p>'''

pages_body.append(h2("Biomas · III + ciclo día/noche")
    + biome_block(*[(b[0], b[1], b[2], b[3]) for b in BIOMES][4])
    + day_night)

# --- 10 · COMBATE Y OBJETOS
coins_html = "".join(fig(IMG[f"coin_{i}"], f"{i}", 9) for i in range(1, 7))
pages_body.append(h2("Combate, gates y objetos", "Todo lo «disparable» del recorrido. Los gates van en pares por carriles: elegir bien es el corazón del juego.") + f'''
<div class="two" style="margin-bottom:4mm;">
  <div>
    <h3>Gates (decisión constante)</h3>
    <div class="grid" style="grid-template-columns:1fr 1fr;">
      {fig(IMG['gate_good'], "gate_good.png", 34, "×2 / +N soldados · #3DE0C8")}
      {fig(IMG['gate_bad'], "gate_bad.png", 34, "resta escuadrón · #FF5A3C")}
    </div>
    <p style="font-size:8.8px; margin-top:1.5mm;" class="muted">Dispararles mejora su valor antes de cruzar. También hay gates de ARMA+ que suben el tier.</p>
  </div>
  <div>
    <h3>Jaulas y barreras</h3>
    <div class="grid" style="grid-template-columns:repeat(2,1fr);">
      {fig(IMG['cage'], "cage.png", 20, "superviviente dentro")}
      {fig(IMG['cage_broken'], "cage_broken.png", 20, "rota a tiros → refuerzo")}
      {fig(IMG['barrier_full'], "barrier_full.png", 34, "bloquea el carril")}
      {fig(IMG['barrier_damaged'], "barrier_damaged.png", 34, "se astilla con daño")}
    </div>
  </div>
</div>
<div class="two">
  <div>
    <h3>Proyectil y fogonazo</h3>
    <div class="grid" style="grid-template-columns:1fr 1fr;">
      {fig(IMG['bullet'], "bullet.png", 8, "bala del escuadrón (32×64)")}
      {fig(IMG['muzzle'], "muzzle.png", 18, "muzzle flash (96×96)")}
    </div>
    <p style="font-size:8.8px; margin-top:1.5mm;" class="muted">Las balas salen en <b>streams</b> paralelos repartidos a lo ancho del disco — en la web, ráfagas doradas ascendentes.</p>
  </div>
  <div>
    <h3>Economía</h3>
    <div class="grid" style="grid-template-columns:1fr 1fr;">
      {fig(IMG['coin'], "coin.png", 13, "moneda (drop de la horda)")}
      {fig(IMG['chest'], "chest.png", 15, "cofre (bonus de recorrido)")}
    </div>
    <h3 style="margin-top:3mm;">Animación coin_spin (6 frames)</h3>
    <div class="grid" style="grid-template-columns:repeat(6,1fr);">{coins_html}</div>
  </div>
</div>
''')

# --- 11 · UI KIT
icons_html = "".join(fig(IMG[i], f"{lbl}", 10, f"{i}.png") for i, lbl in UI_ICONS)
pages_body.append(h2("Kit de interfaz", "Iconos 128×128 en Assets/Resources/Art/ui/ + el lenguaje de paneles del juego (uGUI), recreado aquí con CSS para copiarlo en la web.") + f'''
<h3>Iconos</h3>
<div class="grid" style="grid-template-columns:repeat(8,1fr); margin-bottom:4mm;">{icons_html}</div>
<div class="two">
  <div>
    <h3>Componentes canónicos</h3>
    <div class="panel" style="display:flex; flex-direction:column; gap:3mm;">
      <div style="background:#1A1830; border:0.5mm solid #3DD6F5; border-radius:2.5mm; padding:3mm 5mm; text-align:center;
           box-shadow:0 0 4mm rgba(61,214,245,0.4); font-weight:800; font-style:italic; letter-spacing:0.5mm; color:#F4F1E8;">JUGAR ▶</div>
      <div style="background:#14141C; border:0.4mm solid rgba(61,214,245,0.35); border-radius:2.5mm; padding:2.5mm 5mm; text-align:center;
           font-weight:700; color:rgba(244,241,232,0.8); font-size:10px;">Botón secundario</div>
      <div>
        <div style="font-size:8px; color:rgba(244,241,232,0.55); margin-bottom:1mm;">PROGRESO DE NIVEL</div>
        <div style="background:#0A0D1A; border-radius:3mm; height:4.5mm; overflow:hidden;">
          <div style="width:62%; height:100%; background:#3DD6F5; box-shadow:0 0 3mm rgba(61,214,245,0.8);"></div>
        </div>
      </div>
      <div>
        <span class="chip" style="color:#5C8FFF; border-color:#5C8FFF;">SLOW</span>
        <span class="chip" style="color:#8CD9FF; border-color:#8CD9FF;">HIELO</span>
        <span class="chip" style="color:#FFD23A; border-color:#FFD23A;">🪙 1.250</span>
      </div>
    </div>
    <p style="font-size:8.6px; margin-top:2mm;" class="muted">Receta del panel: fondo <code>#1A1830</code> al 88% + borde <code>#3DD6F5</code> (90%) redondeado + sombra dura de texto 2px. Los overlays de derrota/victoria oscurecen con negro al 62%.</p>
  </div>
  <div>
    <h3>Tipografía</h3>
    <div class="panel">
      <p style="font-size:8.8px;">El juego rotula con una <b>sans black en itálica</b> con outline (ver wordmark) y usa TextMeshPro para HUD. Para la web:</p>
      <table style="margin-top:2mm;">
        <tr><th>Uso</th><th>Recomendación</th></tr>
        <tr><td><b>Display / títulos</b></td><td>Archivo Black itálica o Nunito 900 italic, mayúsculas, tracking amplio</td></tr>
        <tr><td><b>Cuerpo</b></td><td>Inter / Nunito Sans 400–600</td></tr>
        <tr><td><b>Números HUD</b></td><td>tabulares, peso 800, con glow del color de contexto</td></tr>
      </table>
      <div style="margin-top:3mm; background:linear-gradient(180deg,#14122A,#241A3A); border-radius:2mm; padding:3mm; text-align:center; border:0.3mm solid rgba(61,214,245,0.3);">
        <span style="font-weight:800; font-style:italic; font-size:19px; letter-spacing:0.6mm; color:#F4F1E8;">ZOMBIE <span style="color:#3DD6F5; text-shadow:0 0 3mm rgba(61,214,245,0.9);">RUSH</span></span>
        <div style="font-size:7.6px; color:rgba(244,241,232,0.5); margin-top:1mm;">titular tipo: black italic + glow cian solo en la palabra acento</div>
      </div>
    </div>
    <h3 style="margin-top:3mm;">Texto flotante de combate</h3>
    <p style="font-size:8.8px;" class="muted">Daño en oro <code>#FFF280</code>, oleadas en cian, avisos de élite en su color
    (¡EXPLOTADOR! naranja, ¡ESCUPIDOR! verde, ¡CHILLÓN! magenta) — útiles como microcopys animados en la web.</p>
  </div>
</div>
''')

# --- 12 · CONTENIDO
weapons_rows = "".join(
    f"<tr><td><b>{i}. {n}</b></td><td>{d}</td><td>{c}</td><td>{s}</td><td>{p}</td><td class='muted'>{note}</td></tr>"
    for i, (n, d, c, s, p, note) in enumerate(WEAPONS, 1))
perks_html = "".join(
    f'<div class="panel" style="padding:2.5mm 3mm;"><b style="font-size:9.5px; color:var(--gold);">{n}</b>'
    f'<p style="font-size:8.4px;">{d}</p></div>' for n, d in PERKS)
ab_html = "".join(
    f'<div class="panel" style="padding:3mm; border-color:{c};"><b style="font-size:10px; color:{c};">{n}</b>'
    f'<p style="font-size:8.6px; margin-top:1mm;">{d}</p></div>' for n, c, d in ABILITIES)
pages_body.append(h2("Contenido jugable", "Material para las secciones «Arsenal», «Progresión» y «Modos» de la web. Datos reales de Weapons.cs, Perks.cs y Loadout.cs.") + f'''
<h3>Arsenal · 6 tiers globales del escuadrón</h3>
<div class="panel" style="padding:2mm 3mm; margin-bottom:4mm;">
<table>
  <tr><th>Arma</th><th>Daño</th><th>Cadencia</th><th>Streams</th><th>Perfora</th><th>Carácter</th></tr>
  {weapons_rows}
</table>
</div>
<div class="two" style="margin-bottom:4mm;">
  <div>
    <h3>Habilidad activa equipada (botón en HUD)</h3>
    <div class="grid" style="grid-template-columns:1fr;">{ab_html}</div>
  </div>
  <div>
    <h3>Perks permanentes (elige 1 de 3 al ganar)</h3>
    <div class="grid" style="grid-template-columns:1fr 1fr;">{perks_html}</div>
  </div>
</div>
<h3>Modos y meta-progresión</h3>
<div class="grid" style="grid-template-columns:repeat(3,1fr);">
  <div class="panel" style="padding:3mm;"><b style="color:var(--cyan); font-size:10px;">CAMPAÑA</b>
    <p style="font-size:8.6px;">100 niveles deterministas en 10 actos · jefe cada 10 · noche en x7–x9 · revive 1×/run pagando monedas.</p></div>
  <div class="panel" style="padding:3mm;"><b style="color:var(--cyan); font-size:10px;">SUPERVIVENCIA</b>
    <p style="font-size:8.6px;">Oleadas encadenadas con +4% de vida por oleada. Récord local (surv_best).</p></div>
  <div class="panel" style="padding:3mm;"><b style="color:var(--gold); font-size:10px;">DESAFÍO DIARIO</b>
    <p style="font-size:8.6px;">La fecha es la semilla: el mismo reto para todo el mundo, récord por día.</p></div>
</div>
<p style="font-size:9px; margin-top:3mm;" class="muted">Meta-tienda (punto de partida): soldados iniciales, arma base, línea repetible de +3% daño/nivel,
skins y héroe francotirador de apoyo. Economía en monedas que suelta la horda y guardan los cofres.</p>
''')

# --- 13 · LICENCIAS Y ARCHIVOS
pages_body.append(h2("Licencias, archivos y specs", "Todo lo que la web puede usar y de dónde sale.") + f'''
<div class="two" style="margin-bottom:4mm;">
  <div class="panel">
    <h3 style="margin-top:0;">Licencias</h3>
    <p style="font-size:9px;"><b>Arte de juego</b> (personajes, entorno, combate, iconos): assets <b>CC0 1.0</b> de
    <b>Kenney</b> (kenney.nl) — uso libre, también comercial y sin atribución obligatoria (acreditar es cortesía y se recomienda
    en el footer de la web).</p>
    <p style="font-size:9px; margin-top:2mm;"><b>Branding</b> (icono, wordmark, feature graphic, splash, social): elaboración propia del proyecto.</p>
    <p style="font-size:9px; margin-top:2mm;"><b>Audio</b>: música y SFX proceduralmente generados por código — no hay archivos de audio que licenciar.</p>
  </div>
  <div class="panel">
    <h3 style="margin-top:0;">Specs del producto</h3>
    <table>
      <tr><td><b>Título</b></td><td>Zombie Rush</td></tr>
      <tr><td><b>Género</b></td><td>Crowd shooter arcade con gates (runner vertical)</td></tr>
      <tr><td><b>Plataforma</b></td><td>Android · portrait 9:16 · IL2CPP + ARM64</td></tr>
      <tr><td><b>Package</b></td><td><code>com.luismiguel.zombierush</code></td></tr>
      <tr><td><b>Motor</b></td><td>Unity 6000.4.9f1 · URP 2D · espacio Linear</td></tr>
      <tr><td><b>Idioma</b></td><td>Español</td></tr>
    </table>
  </div>
</div>
<h3>Índice de assets en el repo</h3>
<div class="panel" style="padding:2.5mm 3.5mm;">
<table>
  <tr><th>Ruta</th><th>Contenido</th></tr>
  <tr><td><code>Assets/Branding/</code></td><td>Icono (512 + adaptive fg/bg 432), wordmark 1024×256, feature graphic 1024×500, social 1280×640, splash 1024×512</td></tr>
  <tr><td><code>Assets/Resources/Art/characters/</code></td><td>Poses 96×96 de soldier / zombie / survivor + zombie_machine 256×256 (jefe)</td></tr>
  <tr><td><code>Assets/Resources/Art/environment/</code></td><td>Por bioma: skyline_* 1024×256, road_*_01..04 256², edge_* 256², prop_*_01..08 + props genéricos y farolas</td></tr>
  <tr><td><code>Assets/Resources/Art/combat/</code></td><td>gate_good/bad 256×128, cage(+broken) 128², barrier full/damaged 256×96, bullet 32×64</td></tr>
  <tr><td><code>Assets/Resources/Art/items/</code></td><td>coin 64², coin_spin/ (6 frames), chest 96²</td></tr>
  <tr><td><code>Assets/Resources/Art/ui/</code></td><td>16 iconos 128×128 + wordmark</td></tr>
  <tr><td><code>docs/banner.png</code></td><td>Banner del repositorio (GitHub)</td></tr>
</table>
</div>
<p style="font-size:8.8px; margin-top:3mm;" class="muted">⚠ El repo usa <b>Git LFS</b> para los PNG: tras clonar, <code>git lfs pull</code> antes de exportar assets.
Los tintes de este documento (skins, bestiario, noche) son multiplicativos idénticos a los del juego; el resto de imágenes están sin retocar.
Documento generado por <code>docs/web-brief/build_brief.py</code>.</p>
''')

# ---------------------------------------------------------------- ensamblado

total = len(pages_body)
html = ("<!doctype html><html><head><meta charset='utf-8'>"
        "<title>Zombie Rush — Brief visual para la web</title>"
        f"<style>{CSS}</style></head><body>"
        + "".join(page(i + 1, total, b) for i, b in enumerate(pages_body))
        + "</body></html>")

with open(OUT, "w", encoding="utf-8") as f:
    f.write(html)
print(f"OK → {OUT} ({os.path.getsize(OUT) / 1e6:.1f} MB, {total} páginas)")
