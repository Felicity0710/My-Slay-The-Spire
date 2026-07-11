"""Generate SVG card art for all new cards, plus enemy/event/relic icon SVGs."""
import os, json, random, math

random.seed(42)

CARD_DIR = "Assets/Cards"
ICON_DIR = "Assets/Icons"
os.makedirs(CARD_DIR, exist_ok=True)
os.makedirs(ICON_DIR, exist_ok=True)

# Read cards to know which ones exist
with open("Data/cards.json", "r", encoding="utf-8") as f:
    cards_data = json.load(f)

# ============================================================
# Card archetype color palettes
# ============================================================
IRON_VANGUARD = {"bg": "#1a0a0a", "fg": "#cc3333", "accent": "#ff6644", "shape": "angular"}
PHANTOM_DANCER = {"bg": "#0a0a1a", "fg": "#8855cc", "accent": "#bb77ee", "shape": "curved"}
STORM_MAGE = {"bg": "#0a0a1a", "fg": "#4488dd", "accent": "#66bbff", "shape": "circular"}
GENERIC = {"bg": "#0a0a0a", "fg": "#888888", "accent": "#aaaaaa", "shape": "simple"}

# Which archetype each card belongs to
def card_archetype(card_id):
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
    if card_id in iron: return IRON_VANGUARD
    if card_id in phantom: return PHANTOM_DANCER
    if card_id in storm: return STORM_MAGE
    return GENERIC

# ============================================================
# SVG helpers
# ============================================================
def svg_card(card_id, palette):
    """Generate a 200x280 SVG card illustration."""
    bg, fg, accent = palette["bg"], palette["fg"], palette["accent"]
    w, h = 200, 280
    cx, cy = w/2, h/2

    svg = f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}">\n'

    # Background
    svg += f'  <rect width="{w}" height="{h}" fill="{bg}" rx="8"/>\n'

    # Decorative border
    svg += f'  <rect x="4" y="4" width="{w-8}" height="{h-8}" fill="none" stroke="{fg}" stroke-width="1.5" rx="6" opacity="0.5"/>\n'
    svg += f'  <rect x="8" y="8" width="{w-16}" height="{h-16}" fill="none" stroke="{accent}" stroke-width="0.5" rx="4" opacity="0.3"/>\n'

    # Card-type icon (top area)
    if palette == IRON_VANGUARD:
        # Sword icon
        svg += f'  <rect x="{cx-3}" y="30" width="6" height="80" fill="{accent}" rx="1" opacity="0.8"/>\n'
        svg += f'  <rect x="{cx-15}" y="25" width="30" height="8" fill="{accent}" rx="2" opacity="0.8"/>\n'
        svg += f'  <rect x="{cx-18}" y="105" width="36" height="5" fill="{fg}" rx="1" opacity="0.5"/>\n'
    elif palette == PHANTOM_DANCER:
        # Dagger icon
        svg += f'  <polygon points="{cx-2},30 {cx+2},30 {cx},90" fill="{accent}" opacity="0.8"/>\n'
        svg += f'  <ellipse cx="{cx}" cy="95" rx="12" ry="4" fill="{fg}" opacity="0.5"/>\n'
        svg += f'  <circle cx="{cx}" cy="50" r="20" fill="none" stroke="{accent}" stroke-width="1" opacity="0.3"/>\n'
    elif palette == STORM_MAGE:
        # Orb/spark icon
        svg += f'  <circle cx="{cx}" cy="55" r="25" fill="{accent}" opacity="0.15"/>\n'
        svg += f'  <circle cx="{cx}" cy="55" r="12" fill="{accent}" opacity="0.4"/>\n'
        svg += f'  <circle cx="{cx}" cy="55" r="5" fill="{accent}" opacity="0.8"/>\n'
        svg += f'  <line x1="{cx-35}" y1="55" x2="{cx-28}" y2="55" stroke="{accent}" stroke-width="1.5" opacity="0.5"/>\n'
        svg += f'  <line x1="{cx+28}" y1="55" x2="{cx+35}" y2="55" stroke="{accent}" stroke-width="1.5" opacity="0.5"/>\n'
    else:
        svg += f'  <circle cx="{cx}" cy="55" r="18" fill="{accent}" opacity="0.3"/>\n'

    # Decorative pattern (varies by card id hash for uniqueness)
    seed = sum(ord(c) for c in card_id)
    rng = random.Random(seed)

    for _ in range(6):
        x = rng.randint(20, w-20)
        y = rng.randint(120, h-20)
        size = rng.randint(3, 10)
        op = rng.uniform(0.1, 0.3)
        if rng.random() < 0.5:
            svg += f'  <circle cx="{x}" cy="{y}" r="{size}" fill="{accent}" opacity="{op:.2f}"/>\n'
        else:
            x2 = x + rng.randint(-8, 8)
            y2 = y + rng.randint(-8, 8)
            svg += f'  <line x1="{x}" y1="{y}" x2="{x2}" y2="{y2}" stroke="{fg}" stroke-width="0.8" opacity="{op:.2f}"/>\n'

    # Bottom accent line
    svg += f'  <rect x="20" y="{h-15}" width="{w-40}" height="2" fill="{accent}" opacity="0.4" rx="1"/>\n'

    svg += '</svg>'
    return svg

# ============================================================
# Generate card art
# ============================================================
new_cards = [c for c in cards_data["cards"] if c["id"] not in
    {"strike","defend","heavy_slash","bash","shrug","quick_slash","whirlwind","battle_focus",
     "adrenaline_rush","second_wind","rending_wave","twin_strike","shield_bash","iron_wall",
     "rend_armor","blood_pact","tactical_step","reaper_touch","fortify","meteor_shower",
     "glass_cannon","war_cry","berserker_form","overclock","crushing_blow","chain_lightning",
     "meditate","fortress_stance","spark_loop","hand_overflow","mana_turbine","infinite_fireball",
     "ember_wheel","arcane_recycle","grave_whisper","bone_shrapnel","death_chorus","soul_siphon",
     "phoenix_cycle","arcane_barrage","triage"}]

print(f"Generating {len(new_cards)} new card SVGs...")
for card in new_cards:
    cid = card["id"]
    palette = card_archetype(cid)
    art = svg_card(cid, palette)
    path = os.path.join(CARD_DIR, f"{cid}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(art)
    print(f"  {cid}.svg")

# Also update cards.json to reference the new SVG art paths
for card in cards_data["cards"]:
    cid = card["id"]
    svg_path = f"res://Assets/Cards/{cid}.svg"
    png_path = f"res://Assets/Cards/{cid}.png"
    # Only update if no artPath set or if it points to placeholder
    if "artPath" not in card or "placeholder" in card.get("artPath", ""):
        card["artPath"] = svg_path

with open("Data/cards.json", "w", encoding="utf-8") as f:
    json.dump(cards_data, f, ensure_ascii=False, indent=4)
print("Updated cards.json art paths")

# ============================================================
# Generate enemy SVGs (one per visual group)
# ============================================================
enemy_svgs = {
    "cultist": ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 160">'
        '<rect width="120" height="160" fill="#1a1018" rx="8"/>'
        '<circle cx="60" cy="45" r="22" fill="#4a2030" stroke="#7a3a4a" stroke-width="2"/>'
        '<circle cx="52" cy="40" r="4" fill="#c04040"/>'
        '<circle cx="68" cy="40" r="4" fill="#c04040"/>'
        '<path d="M54 52 Q60 58 66 52" fill="none" stroke="#7a3a4a" stroke-width="2"/>'
        '<rect x="45" y="70" width="30" height="50" fill="#2a1520" rx="4"/>'
        '<line x1="60" y1="70" x2="60" y2="120" stroke="#4a2030" stroke-width="1"/>'
        '<rect x="35" y="75" width="10" height="30" fill="#3a1a28" rx="3"/>'
        '<rect x="75" y="75" width="10" height="30" fill="#3a1a28" rx="3"/>'
        '</svg>'),
    "elite": ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 160">'
        '<rect width="120" height="160" fill="#1a1020" rx="8"/>'
        '<circle cx="60" cy="40" r="26" fill="#5a2060" stroke="#c080c0" stroke-width="2.5"/>'
        '<circle cx="50" cy="34" r="5" fill="#ff80ff"/>'
        '<circle cx="70" cy="34" r="5" fill="#ff80ff"/>'
        '<polygon points="60,52 54,62 66,62" fill="#8040a0"/>'
        '<rect x="44" y="70" width="32" height="55" fill="#302040" rx="5"/>'
        '<rect x="36" y="80" width="12" height="35" fill="#402060" rx="3"/>'
        '<rect x="72" y="80" width="12" height="35" fill="#402060" rx="3"/>'
        '</svg>'),
    "boss": ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 160">'
        '<rect width="120" height="160" fill="#100a10" rx="8"/>'
        '<circle cx="60" cy="35" r="30" fill="#3a1040" stroke="#f0a0f0" stroke-width="3"/>'
        '<circle cx="48" cy="28" r="5" fill="#ff4444"/>'
        '<circle cx="72" cy="28" r="5" fill="#ff4444"/>'
        '<polygon points="48,52 72,52 66,68 54,68" fill="#601060"/>'
        '<rect x="42" y="68" width="36" height="60" fill="#201030" rx="6"/>'
        '<rect x="32" y="78" width="14" height="38" fill="#301840" rx="4"/>'
        '<rect x="74" y="78" width="14" height="38" fill="#301840" rx="4"/>'
        '<circle cx="60" cy="95" r="10" fill="#801060" opacity="0.6"/>'
        '</svg>'),
}

for name, svg_content in enemy_svgs.items():
    path = os.path.join(ICON_DIR, f"enemy_{name}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(svg_content)
    print(f"  enemy_{name}.svg")

# ============================================================
# Generate relic category SVGs
# ============================================================
relic_icons = {
    "relic_generic": '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64"><rect width="64" height="64" fill="#1a1a2e" rx="8"/><circle cx="32" cy="32" r="18" fill="#4a3a20" stroke="#c0a060" stroke-width="2"/><circle cx="32" cy="32" r="8" fill="#806030" opacity="0.6"/></svg>',
    "card_back": '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 280"><rect width="200" height="280" fill="#0a0a18" rx="8"/><rect x="4" y="4" width="192" height="272" fill="none" stroke="#887744" stroke-width="2" rx="6"/><rect x="10" y="10" width="180" height="260" fill="none" stroke="#665533" stroke-width="0.5" rx="4"/><circle cx="100" cy="140" r="50" fill="none" stroke="#887744" stroke-width="1" opacity="0.4"/><circle cx="100" cy="140" r="30" fill="none" stroke="#887744" stroke-width="1.5" opacity="0.6"/><text x="100" y="145" text-anchor="middle" fill="#887744" font-size="28" font-family="serif" opacity="0.8">SHS</text></svg>',
}

for name, svg_content in relic_icons.items():
    path = os.path.join(ICON_DIR, f"{name}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(svg_content)
    print(f"  {name}.svg")

print("\nDone! Generated all SVG assets.")
