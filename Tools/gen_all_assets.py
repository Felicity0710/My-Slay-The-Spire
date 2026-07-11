"""Generate ALL game art assets: 77 card SVGs, relic/potion icons, character portraits."""
import os, json, random, math

random.seed(42)
CARD_DIR = "Assets/Cards"
ICON_DIR = "Assets/Icons"
PORTRAIT_DIR = "Assets/Portraits"
for d in [CARD_DIR, ICON_DIR, PORTRAIT_DIR]:
    os.makedirs(d, exist_ok=True)

with open("Data/cards.json", "r", encoding="utf-8") as f:
    cards_data = json.load(f)

# ============================================================
# Color palettes
# ============================================================
R = {"bg": "#1a0808", "fg": "#cc3333", "ac": "#ff5544", "name": "Iron Vanguard"}
P = {"bg": "#08081a", "fg": "#8855cc", "ac": "#bb77ee", "name": "Phantom Dancer"}
B = {"bg": "#080a1a", "fg": "#4488dd", "ac": "#66bbff", "name": "Storm Mage"}
G = {"bg": "#0a0a0a", "fg": "#888888", "ac": "#aaaaaa", "name": "Neutral"}

def get_palette(cid):
    iron = {"heavy_slash","bash","twin_strike","crushing_blow","sword_boomerang","immolate",
            "hemoplague","executioner","demon_form","offering","body_slam","reckless_charge",
            "juggernaut","blood_for_blood","iron_skin","battle_focus","blood_pact",
            "berserker_form","death_chorus","fortress_stance","rend_armor","glass_cannon",
            "reaper_touch","war_cry","shield_bash","iron_wall","second_wind","fortify","rending_wave"}
    phantom = {"quick_slash","bone_shrapnel","chain_lightning","dagger_spray","acrobatics",
               "shadow_cloak","skewer","mortal_strike","calculated_gamble","thousand_cuts",
               "grand_finale","flash_knives","deadly_poison","evasion","backflip","venom_strike",
               "triage","tactical_step","adrenaline_rush","hand_overflow","grave_whisper","ember_wheel"}
    storm = {"arcane_barrage","meteor_shower","infinite_fireball","whirlwind","firestorm",
             "thunder_strike","static_discharge","frost_bolt","apocalypse","spark_loop",
             "arcane_recycle","mana_turbine","overclock","mana_storm","echo_form","time_warp",
             "crystal_shield","arcane_surge","lightning_cascade","energy_shield",
             "phoenix_cycle","meditate"}
    if cid == "strike": return R
    if cid == "defend": return R
    if cid in iron: return R
    if cid in phantom: return P
    if cid in storm: return B
    return G

# ============================================================
# Card art generator — unified style for all 77 cards
# ============================================================
def card_svg(cid, pal):
    bg, fg, ac = pal["bg"], pal["fg"], pal["ac"]
    w, h = 200, 280
    cx, cy = w/2, h/2
    seed = sum(ord(c)*i for i,c in enumerate(cid))
    rng = random.Random(seed)

    svg = f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}">\n'
    svg += f'<defs><filter id="glow"><feGaussianBlur stdDeviation="3" result="blur"/><feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge></filter></defs>\n'
    # Background with subtle gradient effect (layered rects)
    svg += f'<rect width="{w}" height="{h}" fill="{bg}" rx="10"/>\n'
    svg += f'<rect x="2" y="2" width="{w-4}" height="{h-4}" fill="none" stroke="{fg}" stroke-width="1.5" rx="8" opacity="0.3"/>\n'
    svg += f'<rect x="6" y="6" width="{w-12}" height="{h-12}" fill="none" stroke="{ac}" stroke-width="0.5" rx="6" opacity="0.15"/>\n'

    # Central icon based on archetype
    if pal == R:
        # Sword + shield motif
        svg += f'<rect x="{cx-2}" y="22" width="4" height="85" fill="{ac}" rx="1" opacity="0.7" filter="url(#glow)"/>\n'
        svg += f'<rect x="{cx-16}" y="18" width="32" height="8" fill="{ac}" rx="2" opacity="0.6"/>\n'
        svg += f'<path d="M{cx-12} 100 L{cx} 115 L{cx+12} 100" fill="none" stroke="{ac}" stroke-width="2" opacity="0.5"/>\n'
        svg += f'<circle cx="{cx}" cy="50" r="30" fill="none" stroke="{fg}" stroke-width="1" opacity="0.15"/>\n'
    elif pal == P:
        # Twin daggers motif
        svg += f'<line x1="{cx-10}" y1="25" x2="{cx-2}" y2="90" stroke="{ac}" stroke-width="2.5" opacity="0.7" filter="url(#glow)"/>\n'
        svg += f'<line x1="{cx+10}" y1="25" x2="{cx+2}" y2="90" stroke="{ac}" stroke-width="2.5" opacity="0.7" filter="url(#glow)"/>\n'
        svg += f'<ellipse cx="{cx}" cy="92" rx="18" ry="5" fill="none" stroke="{fg}" stroke-width="1.5" opacity="0.4"/>\n'
        svg += f'<circle cx="{cx}" cy="55" r="22" fill="none" stroke="{fg}" stroke-width="0.8" opacity="0.15"/>\n'
    elif pal == B:
        # Orb + lightning motif
        svg += f'<circle cx="{cx}" cy="55" r="28" fill="{ac}" opacity="0.08"/>\n'
        svg += f'<circle cx="{cx}" cy="55" r="14" fill="{ac}" opacity="0.25"/>\n'
        svg += f'<circle cx="{cx}" cy="55" r="5" fill="{ac}" opacity="0.7" filter="url(#glow)"/>\n'
        for angle in [0, 72, 144, 216, 288]:
            rad = math.radians(angle)
            x1 = cx + 18*math.cos(rad)
            y1 = 55 + 18*math.sin(rad)
            x2 = cx + 35*math.cos(rad)
            y2 = 55 + 35*math.sin(rad)
            svg += f'<line x1="{x1:.0f}" y1="{y1:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="{ac}" stroke-width="1" opacity="0.4"/>\n'
    else:
        svg += f'<circle cx="{cx}" cy="55" r="20" fill="{ac}" opacity="0.15"/>\n'
        svg += f'<circle cx="{cx}" cy="55" r="8" fill="{ac}" opacity="0.35"/>\n'

    # Floating particles unique per card
    for _ in range(8):
        x = rng.randint(15, w-15)
        y = rng.randint(110, h-20)
        r = rng.uniform(1.5, 5)
        op = rng.uniform(0.1, 0.4)
        if rng.random() < 0.4:
            svg += f'<circle cx="{x:.0f}" cy="{y:.0f}" r="{r:.1f}" fill="{ac}" opacity="{op:.2f}"/>\n'
        elif rng.random() < 0.4:
            svg += f'<rect x="{x:.0f}" y="{y:.0f}" width="{r*2:.0f}" height="{r*2:.0f}" fill="none" stroke="{fg}" stroke-width="0.8" opacity="{op:.2f}" transform="rotate({rng.randint(0,45)} {x:.0f} {y:.0f})"/>\n'
        else:
            x2 = x + rng.randint(-10, 10)
            y2 = y + rng.randint(-10, 10)
            svg += f'<line x1="{x:.0f}" y1="{y:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="{ac}" stroke-width="0.6" opacity="{op:.2f}"/>\n'

    # Bottom accent bar
    svg += f'<rect x="25" y="{h-12}" width="{w-50}" height="2" fill="{ac}" opacity="0.3" rx="1"/>\n'
    svg += '</svg>'
    return svg

# ============================================================
# Generate ALL 77 card SVGs
# ============================================================
print("Generating card art for all 77 cards...")
for card in cards_data["cards"]:
    cid = card["id"]
    pal = get_palette(cid)
    art = card_svg(cid, pal)
    path = os.path.join(CARD_DIR, f"{cid}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(art)
    card["artPath"] = f"res://Assets/Cards/{cid}.svg"

with open("Data/cards.json", "w", encoding="utf-8") as f:
    json.dump(cards_data, f, ensure_ascii=False, indent=4)
print(f"  Generated {len(cards_data['cards'])} card SVGs, updated cards.json")

# ============================================================
# Relic icons — each with a unique design matching its description
# ============================================================
with open("Data/relics.json", "r", encoding="utf-8") as f:
    relics_data = json.load(f)["relics"]

relic_designs = {
    "lantern": ('<circle cx="32" cy="28" r="12" fill="#ffe080" opacity="0.6"/>'
                '<rect x="29" y="38" width="6" height="14" fill="#c0a040" rx="2"/>'
                '<rect x="25" y="50" width="14" height="4" fill="#806020" rx="1"/>'),
    "anchor": ('<circle cx="32" cy="32" r="6" fill="none" stroke="#80a0c0" stroke-width="3"/>'
               '<line x1="32" y1="38" x2="32" y2="56" stroke="#80a0c0" stroke-width="3"/>'
               '<line x1="20" y1="42" x2="44" y2="42" stroke="#80a0c0" stroke-width="2.5"/>'
               '<path d="M22 56 Q32 60 42 56" fill="none" stroke="#80a0c0" stroke-width="2.5"/>'),
    "whetstone": ('<rect x="16" y="20" width="32" height="10" fill="#8090a0" rx="2"/>'
                  '<polygon points="18,30 46,30 40,50 24,50" fill="#607080"/>'
                  '<line x1="32" y1="32" x2="32" y2="46" stroke="#a0b0c0" stroke-width="1"/>'),
    "charm": ('<circle cx="32" cy="32" r="14" fill="none" stroke="#80e080" stroke-width="2.5"/>'
              '<circle cx="32" cy="32" r="6" fill="#80e080" opacity="0.6"/>'
              '<line x1="22" y1="22" x2="12" y2="12" stroke="#80e080" stroke-width="1.5"/>'),
    "ember_ring": ('<circle cx="32" cy="32" r="16" fill="none" stroke="#ff8040" stroke-width="3"/>'
                   '<circle cx="32" cy="32" r="4" fill="#ff8040"/>'
                   '<circle cx="32" cy="32" r="10" fill="none" stroke="#ff8040" stroke-width="1" opacity="0.4"/>'),
    "iron_shell": ('<path d="M16 20 Q32 10 48 20 L44 50 Q32 56 20 50 Z" fill="#8090a0" stroke="#b0c0d0" stroke-width="2"/>'
                   '<line x1="32" y1="16" x2="32" y2="50" stroke="#b0c0d0" stroke-width="1" opacity="0.5"/>'),
    "blood_vial": ('<rect x="24" y="14" width="16" height="10" fill="#c04040" rx="3"/>'
                   '<rect x="26" y="22" width="12" height="24" fill="#801020" rx="4"/>'
                   '<circle cx="32" cy="34" r="4" fill="#e06060" opacity="0.6"/>'),
}

def default_relic_svg(seed_str):
    rng = random.Random(sum(ord(c) for c in seed_str))
    c1 = f"#{rng.randint(0,255):02x}{rng.randint(0,255):02x}{rng.randint(0,255):02x}"
    c2 = f"#{rng.randint(0,255):02x}{rng.randint(0,255):02x}{rng.randint(0,255):02x}"
    n = rng.randint(3, 6)
    pts = []
    for i in range(n):
        a = 2*math.pi*i/n - math.pi/2
        r = rng.randint(12, 18)
        pts.append(f"{32+r*math.cos(a):.0f},{32+r*math.sin(a):.0f}")
    fill_op = rng.uniform(0.3, 0.6)
    return (f'<circle cx="32" cy="32" r="16" fill="{c1}" opacity="{fill_op:.2f}"/>'
            f'<polygon points="{" ".join(pts)}" fill="none" stroke="{c2}" stroke-width="2"/>')

print("Generating relic icons...")
for relic in relics_data:
    rid = relic["id"]
    inner = relic_designs.get(rid, default_relic_svg(rid+"relic"))
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">'
           f'<rect width="64" height="64" fill="#12121e" rx="10"/>'
           f'<rect x="2" y="2" width="60" height="60" fill="none" stroke="#887744" stroke-width="1.5" rx="8" opacity="0.5"/>'
           f'{inner}</svg>')
    path = os.path.join(ICON_DIR, f"relic_{rid}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(svg)
print(f"  Generated {len(relics_data)} relic icons")

# ============================================================
# Potion icons
# ============================================================
potion_colors = {
    "healing_potion": ("#e04040", "#ff6666"), "greater_healing_potion": ("#cc2020", "#ff4444"),
    "strength_potion": ("#e08030", "#ff9940"), "guard_potion": ("#4080d0", "#6699ee"),
    "swift_potion": ("#40b060", "#66dd88"), "fury_potion": ("#c040c0", "#e066e0"),
    "block_potion": ("#5080c0", "#80a8e0"), "max_hp_potion": ("#e0b030", "#ffcc40"),
    "energy_potion": ("#40b0c0", "#66d0e0"), "vampire_potion": ("#802040", "#c03060"),
}
print("Generating potion icons...")
for pid, (c1, c2) in potion_colors.items():
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64">'
           f'<rect width="64" height="64" fill="#12121e" rx="10"/>'
           f'<rect x="20" y="10" width="24" height="12" fill="{c1}" rx="4"/>'
           f'<rect x="24" y="8" width="16" height="6" fill="{c2}" rx="2"/>'
           f'<rect x="22" y="20" width="20" height="30" fill="{c1}" rx="6" opacity="0.8"/>'
           f'<ellipse cx="32" cy="36" rx="5" ry="6" fill="{c2}" opacity="0.5"/>'
           f'<rect x="26" y="16" width="12" height="2" fill="{c2}" opacity="0.4" rx="1"/>'
           f'</svg>')
    path = os.path.join(ICON_DIR, f"potion_{pid}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(svg)
print(f"  Generated {len(potion_colors)} potion icons")

# ============================================================
# Character portraits
# ============================================================
portraits = {
    "iron_vanguard": (R, ['<rect x="70" y="30" width="60" height="100" fill="#cc3333" rx="8" opacity="0.6"/>',
        '<rect x="50" y="45" width="100" height="15" fill="#ff5544" rx="3" opacity="0.5"/>',
        '<circle cx="100" cy="60" r="30" fill="#1a0808" stroke="#ff5544" stroke-width="3"/>',
        '<circle cx="90" cy="52" r="5" fill="#ff5544"/>',
        '<circle cx="110" cy="52" r="5" fill="#ff5544"/>',
        '<path d="M88 72 L100 82 L112 72" fill="none" stroke="#ff5544" stroke-width="2.5"/>']),
    "phantom_dancer": (P, ['<path d="M60 130 L100 30 L140 130 Z" fill="#8855cc" opacity="0.2"/>',
        '<circle cx="100" cy="55" r="28" fill="#08081a" stroke="#bb77ee" stroke-width="2.5"/>',
        '<ellipse cx="90" cy="48" rx="4" ry="6" fill="#bb77ee"/>',
        '<ellipse cx="110" cy="48" rx="4" ry="6" fill="#bb77ee"/>',
        '<path d="M90 68 Q100 78 110 68" fill="none" stroke="#bb77ee" stroke-width="2"/>',
        '<line x1="100" y1="83" x2="100" y2="130" stroke="#8855cc" stroke-width="3" opacity="0.5"/>']),
    "storm_mage": (B, ['<circle cx="100" cy="60" r="35" fill="#080a1a" stroke="#66bbff" stroke-width="2.5"/>',
        '<circle cx="100" cy="60" r="15" fill="#66bbff" opacity="0.15"/>',
        '<circle cx="100" cy="60" r="6" fill="#66bbff" opacity="0.6"/>',
        '<circle cx="88" cy="50" r="4" fill="#66bbff" opacity="0.8"/>',
        '<circle cx="112" cy="50" r="4" fill="#66bbff" opacity="0.8"/>',
        '<path d="M88 72 Q100 80 112 72" fill="none" stroke="#66bbff" stroke-width="2"/>',
        '<line x1="70" y1="60" x2="88" y2="60" stroke="#66bbff" stroke-width="1.5" opacity="0.5"/>',
        '<line x1="112" y1="60" x2="130" y2="60" stroke="#66bbff" stroke-width="1.5" opacity="0.5"/>']),
}

print("Generating character portraits...")
for char_id, (pal, elements) in portraits.items():
    bg, fg, ac = pal["bg"], pal["fg"], pal["ac"]
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 160">\n'
    svg += f'<rect width="200" height="160" fill="{bg}" rx="12"/>\n'
    svg += f'<rect x="3" y="3" width="194" height="154" fill="none" stroke="{ac}" stroke-width="2" rx="10" opacity="0.4"/>\n'
    for el in elements:
        svg += f'  {el}\n'
    # Floating particles
    rng = random.Random(sum(ord(c) for c in char_id))
    for _ in range(6):
        x = rng.randint(10, 190)
        y = rng.randint(10, 150)
        r = rng.uniform(1.5, 3)
        svg += f'  <circle cx="{x:.0f}" cy="{y:.0f}" r="{r:.1f}" fill="{ac}" opacity="0.3"/>\n'
    svg += '</svg>'
    path = os.path.join(PORTRAIT_DIR, f"{char_id}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(svg)
print(f"  Generated {len(portraits)} character portraits")

# ============================================================
# Transparent enemy SVGs (no background rect)
# ============================================================
enemy_transparent = {
    "cultist_nobg": (
        '<ellipse cx="60" cy="90" rx="25" ry="50" fill="#2a1520" stroke="#4a2030" stroke-width="2"/>'
        '<circle cx="60" cy="42" r="18" fill="#3a1825" stroke="#6a3040" stroke-width="1.5"/>'
        '<circle cx="53" cy="38" r="3.5" fill="#d04040"/>'
        '<circle cx="67" cy="38" r="3.5" fill="#d04040"/>'
        '<path d="M54 48 Q60 53 66 48" fill="none" stroke="#8a4050" stroke-width="1.5"/>'
        '<rect x="52" y="65" width="16" height="30" fill="#201018" rx="3" opacity="0.7"/>'),
    "elite_nobg": (
        '<ellipse cx="60" cy="88" rx="28" ry="52" fill="#302040" stroke="#604080" stroke-width="2.5"/>'
        '<circle cx="60" cy="38" r="22" fill="#402860" stroke="#c080c0" stroke-width="2"/>'
        '<circle cx="50" cy="32" r="4" fill="#ff80ff"/>'
        '<circle cx="70" cy="32" r="4" fill="#ff80ff"/>'
        '<polygon points="60,48 52,56 68,56" fill="#8040a0"/>'
        '<rect x="50" y="66" width="20" height="32" fill="#251838" rx="4"/>'),
    "boss_nobg": (
        '<ellipse cx="60" cy="85" rx="32" ry="55" fill="#1a0a20" stroke="#a060c0" stroke-width="3"/>'
        '<circle cx="60" cy="35" r="26" fill="#3a1040" stroke="#f0a0f0" stroke-width="2.5"/>'
        '<circle cx="48" cy="28" r="4.5" fill="#ff4444"/>'
        '<circle cx="72" cy="28" r="4.5" fill="#ff4444"/>'
        '<polygon points="48,52 72,52 66,68 54,68" fill="#601060"/>'
        '<rect x="48" y="66" width="24" height="36" fill="#1a0a28" rx="5"/>'
        '<circle cx="60" cy="55" r="8" fill="#901070" opacity="0.5"/>'),
}

print("Generating transparent enemy SVGs...")
for name, inner in enemy_transparent.items():
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 160">\n{inner}\n</svg>'
    path = os.path.join(ICON_DIR, f"{name}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(svg)
print(f"  Generated {len(enemy_transparent)} transparent enemy SVGs")

print("\nALL DONE!")
print(f"Total assets: {len(cards_data['cards'])} cards + {len(relics_data)} relics + {len(potion_colors)} potions + {len(portraits)} portraits + {len(enemy_transparent)} enemies")
