"""Generate unique SVG card art based on card type, effects, and archetype."""
import os, json, math, random
random.seed(42)

with open("Data/cards.json", "r", encoding="utf-8") as f:
    cards_data = json.load(f)

CARD_DIR = "Assets/Cards"
os.makedirs(CARD_DIR, exist_ok=True)
W, H = 200, 280

# ── Color palettes ──
IRON = {"bg": "#120808", "fg": "#cc3333", "ac": "#ff5544", "name": "iron"}
PHAN = {"bg": "#080812", "fg": "#8855cc", "ac": "#bb77ee", "name": "phantom"}
STORM = {"bg": "#080a14", "fg": "#4488dd", "ac": "#66bbff", "name": "storm"}

# Effect colors
EFF_COLORS = {
    "Damage": "#fca5a5", "GainBlock": "#93c5fd", "ApplyVulnerable": "#e9d5ff",
    "DrawCards": "#a5f3fc", "GainStrength": "#fca5a5", "GainEnergy": "#fde68a",
    "Heal": "#86efac", "DiscardCards": "#d1d5db"
}
EFF_ICONS = {
    "Damage": "sword", "GainBlock": "shield", "ApplyVulnerable": "crack",
    "DrawCards": "card", "GainStrength": "fist", "GainEnergy": "spark",
    "Heal": "cross", "DiscardCards": "arrow"
}

# ── Archetype assignment ──
def pal(cid):
    iron = {"heavy_slash","bash","twin_strike","crushing_blow","sword_boomerang","immolate",
            "hemoplague","executioner","demon_form","offering","body_slam","reckless_charge",
            "juggernaut","blood_for_blood","iron_skin","battle_focus","blood_pact",
            "berserker_form","death_chorus","fortress_stance","rend_armor","glass_cannon",
            "reaper_touch","war_cry","shield_bash","iron_wall","second_wind","fortify","rending_wave"}
    if cid in iron or cid in ("strike","defend"): return IRON
    phantom = {"quick_slash","bone_shrapnel","chain_lightning","dagger_spray","acrobatics",
               "shadow_cloak","skewer","mortal_strike","calculated_gamble","thousand_cuts",
               "grand_finale","flash_knives","deadly_poison","evasion","backflip","venom_strike",
               "triage","tactical_step","adrenaline_rush","hand_overflow","grave_whisper","ember_wheel"}
    if cid in phantom: return PHAN
    storm = {"arcane_barrage","meteor_shower","infinite_fireball","whirlwind","firestorm",
             "thunder_strike","static_discharge","frost_bolt","apocalypse","spark_loop",
             "arcane_recycle","mana_turbine","overclock","mana_storm","echo_form","time_warp",
             "crystal_shield","arcane_surge","lightning_cascade","energy_shield",
             "phoenix_cycle","meditate"}
    if cid in storm: return STORM
    return IRON

# ── SVG generators ──
def svg_base(bg, fg, ac):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}">\n'
            f'<defs><filter id="g"><feGaussianBlur stdDeviation="2.5"/><feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge></filter></defs>\n'
            f'<rect width="{W}" height="{H}" fill="{bg}" rx="10"/>\n'
            f'<rect x="2" y="2" width="{W-4}" height="{H-4}" fill="none" stroke="{fg}" stroke-width="1.5" rx="8" opacity="0.25"/>\n'
            f'<rect x="6" y="6" width="{W-12}" height="{H-12}" fill="none" stroke="{ac}" stroke-width="0.5" rx="6" opacity="0.12"/>\n')

def svg_end():
    return '</svg>'

def sword_icon(cx, cy, ac, fg, scale=1.0):
    s = scale
    return (f'<rect x="{cx-2*s}" y="{cy-35*s}" width="{4*s}" height="{55*s}" fill="{ac}" rx="{1*s}" opacity="0.7" filter="url(#g)"/>\n'
            f'<rect x="{cx-14*s}" y="{cy-39*s}" width="{28*s}" height="{8*s}" fill="{ac}" rx="{2*s}" opacity="0.6"/>\n'
            f'<rect x="{cx-16*s}" y="{cy+18*s}" width="{32*s}" height="{4*s}" fill="{fg}" rx="{1*s}" opacity="0.4"/>\n')

def shield_icon(cx, cy, ac, fg, scale=1.0):
    s = scale
    return (f'<path d="M{cx-22*s} {cy-28*s} L{cx} {cy-40*s} L{cx+22*s} {cy-28*s} L{cx+18*s} {cy+22*s} Q{cx-18*s} {cy+30*s} {cx-18*s} {cy+22*s} Z" fill="none" stroke="{ac}" stroke-width="{2.5*s}" opacity="0.7" filter="url(#g)"/>\n'
            f'<line x1="{cx}" y1="{cy-18*s}" x2="{cx}" y2="{cy+10*s}" stroke="{fg}" stroke-width="{1.5*s}" opacity="0.4"/>\n')

def spark_icon(cx, cy, ac, fg=None, scale=1.0):
    s = scale
    svg = f'<circle cx="{cx}" cy="{cy}" r="{10*s}" fill="{ac}" opacity="0.12"/>\n'
    svg += f'<circle cx="{cx}" cy="{cy}" r="{5*s}" fill="{ac}" opacity="0.35" filter="url(#g)"/>\n'
    svg += f'<circle cx="{cx}" cy="{cy}" r="{2*s}" fill="white" opacity="0.6"/>\n'
    for a in [0, 60, 120, 180, 240, 300]:
        rad = math.radians(a)
        x1 = cx + 14*s*math.cos(rad)
        y1 = cy + 14*s*math.sin(rad)
        x2 = cx + 28*s*math.cos(rad)
        y2 = cy + 28*s*math.sin(rad)
        svg += f'<line x1="{x1:.0f}" y1="{y1:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="{ac}" stroke-width="{1.2*s}" opacity="0.35"/>\n'
    return svg

def crack_icon(cx, cy, ac, fg=None, scale=1.0):
    s = scale
    return (f'<circle cx="{cx}" cy="{cy}" r="{20*s}" fill="none" stroke="{ac}" stroke-width="{2*s}" opacity="0.5"/>\n'
            f'<line x1="{cx-8*s}" y1="{cy-4*s}" x2="{cx+2*s}" y2="{cy+6*s}" stroke="{ac}" stroke-width="{1.8*s}" opacity="0.5"/>\n'
            f'<line x1="{cx+2*s}" y1="{cy+6*s}" x2="{cx-3*s}" y2="{cy+14*s}" stroke="{ac}" stroke-width="{1.4*s}" opacity="0.4"/>\n'
            f'<line x1="{cx+12*s}" y1="{cy-10*s}" x2="{cx+6*s}" y2="{cy-2*s}" stroke="{ac}" stroke-width="{1.5*s}" opacity="0.4"/>\n')

def card_icon(cx, cy, ac, fg=None, scale=1.0):
    s = scale
    return (f'<rect x="{cx-18*s}" y="{cy-24*s}" width="{36*s}" height="{48*s}" fill="none" stroke="{ac}" stroke-width="{2*s}" rx="{3*s}" opacity="0.6"/>\n'
            f'<rect x="{cx-12*s}" y="{cy-14*s}" width="{24*s}" height="{2*s}" fill="{ac}" opacity="0.35" rx="1"/>\n'
            f'<rect x="{cx-12*s}" y="{cy-6*s}" width="{18*s}" height="{2*s}" fill="{ac}" opacity="0.25" rx="1"/>\n'
            f'<rect x="{cx-12*s}" y="{cy+2*s}" width="{20*s}" height="{2*s}" fill="{ac}" opacity="0.25" rx="1"/>\n')

def fist_icon(cx, cy, ac, fg=None, scale=1.0):
    s = scale
    return (f'<circle cx="{cx}" cy="{cy}" r="{18*s}" fill="none" stroke="{ac}" stroke-width="{3*s}" opacity="0.7" filter="url(#g)"/>\n'
            f'<rect x="{cx-8*s}" y="{cy-22*s}" width="{16*s}" height="{6*s}" fill="{ac}" rx="{2*s}" opacity="0.5"/>\n'
            f'<line x1="{cx-12*s}" y1="{cy-10*s}" x2="{cx+12*s}" y2="{cy-10*s}" stroke="{ac}" stroke-width="{1.5*s}" opacity="0.4"/>\n'
            f'<line x1="{cx-10*s}" y1="{cy-2*s}" x2="{cx+10*s}" y2="{cy-2*s}" stroke="{ac}" stroke-width="{1.5*s}" opacity="0.4"/>\n'
            f'<line x1="{cx-8*s}" y1="{cy+6*s}" x2="{cx+8*s}" y2="{cy+6*s}" stroke="{ac}" stroke-width="{1.5*s}" opacity="0.4"/>\n')

def cross_icon(cx, cy, ac, fg=None, scale=1.0):
    s = scale
    return (f'<rect x="{cx-5*s}" y="{cy-20*s}" width="{10*s}" height="{40*s}" fill="{ac}" rx="{3*s}" opacity="0.7" filter="url(#g)"/>\n'
            f'<rect x="{cx-18*s}" y="{cy-5*s}" width="{36*s}" height="{10*s}" fill="{ac}" rx="{3*s}" opacity="0.6"/>\n')

def arrow_icon(cx, cy, ac, fg=None, scale=1.0):
    s = scale
    svg = f'<path d="M{cx-20*s} {cy} Q{cx} {cy-25*s} {cx+20*s} {cy}" fill="none" stroke="{ac}" stroke-width="{2.5*s}" opacity="0.7" filter="url(#g)"/>\n'
    svg += f'<polygon points="{cx+20*s},{cy} {cx+12*s},{cy-8*s} {cx+12*s},{cy+8*s}" fill="{ac}" opacity="0.7"/>\n'
    svg += f'<path d="M{cx-20*s} {cy+20*s} Q{cx} {cy-5*s} {cx+20*s} {cy+20*s}" fill="none" stroke="{ac}" stroke-width="{1.5*s}" opacity="0.3"/>\n'
    return svg

ICON_FN = {"sword": sword_icon, "shield": shield_icon, "spark": spark_icon,
           "crack": crack_icon, "card": card_icon, "fist": fist_icon,
           "cross": cross_icon, "arrow": arrow_icon}

# ── Generate per-card art ──
print("Generating unique card art...")
for card in cards_data["cards"]:
    cid = card["id"]
    p = pal(cid)
    bg, fg, ac = p["bg"], p["fg"], p["ac"]
    kind = card["kind"]  # "Attack" or "Skill"
    effects = card["effects"]
    is_attack = kind == "Attack"

    # Primary effect determines the central icon
    primary_effect = effects[0]["type"] if effects else "Damage"
    icon_name = EFF_ICONS.get(primary_effect, "spark")
    icon_fn = ICON_FN.get(icon_name, spark_icon)

    # Secondary effects determine decorative elements
    has_secondary = len(effects) > 1
    sec_names = [e["type"] for e in effects[1:]] if has_secondary else []

    seed = sum(ord(c)*i for i, c in enumerate(cid))
    rng = random.Random(seed)

    svg = svg_base(bg, fg, ac)

    # ── TOP BANNER: kind indicator ──
    banner_color = "#fca5a5" if is_attack else "#93c5fd"
    banner_label = "ATTACK" if is_attack else "SKILL"
    svg += f'<rect x="10" y="12" width="{W-20}" height="20" fill="{banner_color}" opacity="0.12" rx="4"/>\n'
    svg += f'<text x="{W/2}" y="27" text-anchor="middle" fill="{banner_color}" font-size="9" font-family="monospace" opacity="0.7">{banner_label}</text>\n'

    # ── CENTER: primary icon ──
    icon_scale = 1.0
    if is_attack: icon_scale = 1.15  # Attacks get slightly larger icons
    svg += icon_fn(W/2, 80, ac, fg, icon_scale)

    # ── SECONDARY: small indicator icons ──
    sec_y = 118
    for i, sn in enumerate(sec_names[:3]):  # max 3 secondary indicators
        sec_ac = EFF_COLORS.get(sn, fg)
        x = 40 + i * 60
        sn_icon = EFF_ICONS.get(sn, "spark")
        sn_fn = ICON_FN.get(sn_icon, spark_icon)
        svg += sn_fn(x, sec_y, sec_ac, fg, 0.35)

    # ── DECORATIVE: attack vs skill patterns ──
    if is_attack:
        # Angular slash lines
        for i in range(5):
            x1 = rng.randint(20, W-20)
            y1 = rng.randint(140, H-40)
            angle = rng.uniform(-30, 30)
            length = rng.randint(20, 50)
            x2 = x1 + length * math.cos(math.radians(angle))
            y2 = y1 + length * math.sin(math.radians(angle))
            svg += f'<line x1="{x1:.0f}" y1="{y1:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="{ac}" stroke-width="{rng.uniform(0.5,1.5):.1f}" opacity="{rng.uniform(0.1,0.3):.2f}"/>\n'
    else:
        # Flowing curved particles
        for i in range(7):
            x = rng.randint(15, W-15)
            y = rng.randint(140, H-25)
            r = rng.uniform(1.5, 5)
            svg += f'<circle cx="{x:.0f}" cy="{y:.0f}" r="{r:.1f}" fill="{ac}" opacity="{rng.uniform(0.1,0.35):.2f}"/>\n'

    # ── COST INDICATOR (bottom left) ──
    cost = card["cost"]
    svg += f'<rect x="14" y="{H-30}" width="24" height="18" fill="none" stroke="{ac}" stroke-width="1.5" rx="4" opacity="0.6"/>\n'
    svg += f'<text x="26" y="{H-16}" text-anchor="middle" fill="{ac}" font-size="13" font-weight="bold" opacity="0.8">{cost}</text>\n'

    # ── BOTTOM ACCENT ──
    svg += f'<rect x="24" y="{H-8}" width="{W-48}" height="2" fill="{ac}" opacity="0.25" rx="1"/>\n'

    svg += svg_end()

    path = os.path.join(CARD_DIR, f"{cid}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(svg)
    card["artPath"] = f"res://Assets/Cards/{cid}.svg"

# Save updated cards.json
with open("Data/cards.json", "w", encoding="utf-8") as f:
    json.dump(cards_data, f, ensure_ascii=False, indent=4)

print(f"Generated {len(cards_data['cards'])} unique card SVGs")
print("Updated cards.json art paths")
