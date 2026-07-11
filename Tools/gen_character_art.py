"""Generate unique transparent-background SVGs for all enemies and player characters."""
import os, json, math

ICON_DIR = "Assets/Icons"
PORTRAIT_DIR = "Assets/Portraits"
os.makedirs(ICON_DIR, exist_ok=True)
os.makedirs(PORTRAIT_DIR, exist_ok=True)

W, H = 120, 160  # enemy dimensions
PW, PH = 120, 160  # player character dimensions

def svg_file(name, inner, w=W, h=H):
    """Wrap inner SVG elements in a transparent-background SVG."""
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {w} {h}">\n{inner}\n</svg>'

def save(name, inner, w=W, h=H):
    path = os.path.join(ICON_DIR, f"{name}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(svg_file(name, inner, w, h))

def save_portrait(name, inner):
    path = os.path.join(PORTRAIT_DIR, f"{name}.svg")
    with open(path, "w", encoding="utf-8") as f:
        f.write(svg_file(name, inner, PW, PH))

# ============================================================
# PLAYER CHARACTERS — transparent, class-distinct
# ============================================================
print("=== Player Characters ===")

# Iron Vanguard: Heavy armored warrior with greatsword
save_portrait("iron_vanguard_battle",
    # Body — broad armored torso
    '<ellipse cx="60" cy="95" rx="28" ry="42" fill="#1a1015" stroke="#cc3333" stroke-width="2.5"/>'
    # Pauldrons
    '<rect x="25" y="62" width="22" height="14" fill="#2a1520" stroke="#ff5544" stroke-width="1.5" rx="3"/>'
    '<rect x="73" y="62" width="22" height="14" fill="#2a1520" stroke="#ff5544" stroke-width="1.5" rx="3"/>'
    # Head — helmet
    '<rect x="40" y="28" width="40" height="35" fill="#1a1015" stroke="#cc3333" stroke-width="2" rx="6"/>'
    '<rect x="44" y="32" width="32" height="6" fill="#cc3333" opacity="0.6" rx="2"/>'
    '<rect x="50" y="20" width="20" height="12" fill="#2a1520" stroke="#cc3333" stroke-width="1.5" rx="2"/>'
    # Eyes — glowing red
    '<circle cx="51" cy="42" r="3" fill="#ff5544"/>'
    '<circle cx="69" cy="42" r="3" fill="#ff5544"/>'
    # Greatsword — held vertically
    '<rect x="85" y="18" width="5" height="90" fill="#cc3333" rx="2"/>'
    '<rect x="78" y="14" width="19" height="8" fill="#ff5544" rx="2"/>'
    '<rect x="82" y="100" width="11" height="5" fill="#cc3333" rx="1"/>'
    # Legs
    '<rect x="40" y="120" width="18" height="30" fill="#1a1015" stroke="#cc3333" stroke-width="1.5" rx="3"/>'
    '<rect x="62" y="120" width="18" height="30" fill="#1a1015" stroke="#cc3333" stroke-width="1.5" rx="3"/>'
)
print("  iron_vanguard_battle.svg")

# Phantom Dancer: Hooded agile assassin with twin daggers
save_portrait("phantom_dancer_battle",
    # Body — slim agile frame
    '<ellipse cx="60" cy="95" rx="20" ry="38" fill="#0a0a18" stroke="#8855cc" stroke-width="2"/>'
    # Hood/cloak — flowing
    '<path d="M38 30 Q60 18 82 30 L78 75 Q60 85 42 75 Z" fill="#0d0d20" stroke="#8855cc" stroke-width="1.5"/>'
    '<ellipse cx="60" cy="45" rx="16" ry="18" fill="#0a0a18" stroke="#bb77ee" stroke-width="1.5"/>'
    # Eyes — glowing purple
    '<ellipse cx="52" cy="42" rx="3" ry="2" fill="#bb77ee" opacity="0.9"/>'
    '<ellipse cx="68" cy="42" rx="3" ry="2" fill="#bb77ee" opacity="0.9"/>'
    # Mask — lower face
    '<path d="M48 50 Q60 56 72 50" fill="none" stroke="#bb77ee" stroke-width="1.5" opacity="0.6"/>'
    # Twin daggers — crossed behind
    '<line x1="38" y1="25" x2="50" y2="85" stroke="#bb77ee" stroke-width="2.5" opacity="0.8"/>'
    '<line x1="82" y1="25" x2="70" y2="85" stroke="#bb77ee" stroke-width="2.5" opacity="0.8"/>'
    # Legs
    '<rect x="46" y="118" width="12" height="30" fill="#0a0a18" stroke="#8855cc" stroke-width="1" rx="2"/>'
    '<rect x="62" y="118" width="12" height="30" fill="#0a0a18" stroke="#8855cc" stroke-width="1" rx="2"/>'
)
print("  phantom_dancer_battle.svg")

# Storm Mage: Robed spellcaster with floating orb
save_portrait("storm_mage_battle",
    # Body — flowing robes
    '<path d="M35 50 Q30 100 38 140 L82 140 Q90 100 85 50 Z" fill="#080a18" stroke="#4488dd" stroke-width="2"/>'
    # Hood
    '<path d="M38 30 Q60 15 82 30 L78 55 Q60 50 42 55 Z" fill="#080a18" stroke="#4488dd" stroke-width="1.5"/>'
    '<ellipse cx="60" cy="42" rx="14" ry="16" fill="#0a0c20" stroke="#66bbff" stroke-width="1.5"/>'
    # Eyes — glowing blue
    '<circle cx="52" cy="38" r="3" fill="#66bbff" opacity="0.9"/>'
    '<circle cx="68" cy="38" r="3" fill="#66bbff" opacity="0.9"/>'
    # Floating orb — above hand
    '<circle cx="80" cy="72" r="12" fill="none" stroke="#66bbff" stroke-width="2"/>'
    '<circle cx="80" cy="72" r="5" fill="#66bbff" opacity="0.6"/>'
    '<circle cx="80" cy="72" r="2" fill="#ffffff" opacity="0.8"/>'
    # Orb trails
    '<path d="M68 68 Q74 70 68 76" fill="none" stroke="#66bbff" stroke-width="1" opacity="0.4"/>'
    '<path d="M92 68 Q86 70 92 76" fill="none" stroke="#66bbff" stroke-width="1" opacity="0.4"/>'
    # Arm holding orb
    '<line x1="65" y1="70" x2="75" y2="72" stroke="#0a0c20" stroke-width="6"/>'
    # Legs
    '<rect x="44" y="125" width="14" height="30" fill="#080a18" stroke="#4488dd" stroke-width="1" rx="2"/>'
    '<rect x="62" y="125" width="14" height="30" fill="#080a18" stroke="#4488dd" stroke-width="1" rx="2"/>'
    # Staff held in other hand
    '<line x1="35" y1="25" x2="38" y2="135" stroke="#4488dd" stroke-width="3" opacity="0.6"/>'
    '<circle cx="35" cy="25" r="4" fill="#66bbff" opacity="0.5"/>'
)
print("  storm_mage_battle.svg")

# ============================================================
# UNIQUE ENEMIES — 29 distinct types with transparent backgrounds
# ============================================================
print("\n=== Enemies ===")

# ---- Act 1: Cultist Outpost ----

save("enemy_cultist",
    # Basic hooded cultist with dagger
    '<ellipse cx="60" cy="100" rx="20" ry="38" fill="#1a1218" stroke="#3a2830" stroke-width="2"/>'
    '<ellipse cx="60" cy="45" rx="16" ry="18" fill="#1a1218" stroke="#4a3038" stroke-width="1.5"/>'
    '<circle cx="52" cy="40" r="3" fill="#dd4444"/>'
    '<circle cx="68" cy="40" r="3" fill="#dd4444"/>'
    '<path d="M54 50 L60 55 L66 50" fill="none" stroke="#4a3038" stroke-width="1.5"/>'
    '<rect x="55" y="68" width="6" height="30" fill="#4a3038" rx="1"/>'
    '<line x1="55" y1="70" x2="42" y2="78" stroke="#4a3038" stroke-width="1"/>'
    '<line x1="61" y1="70" x2="68" y2="85" stroke="#3a2830" stroke-width="1"/>'
)
print("  enemy_cultist.svg")

save("enemy_cultist_scout",
    # Lean scout, crouching pose, short blade
    '<ellipse cx="55" cy="100" rx="16" ry="35" fill="#0f1a1a" stroke="#2a5040" stroke-width="2"/>'
    '<ellipse cx="55" cy="42" rx="14" ry="16" fill="#0f1a1a" stroke="#2a5040" stroke-width="1.5"/>'
    '<circle cx="48" cy="38" r="2.5" fill="#44dd88"/>'
    '<circle cx="62" cy="38" r="2.5" fill="#44dd88"/>'
    # Short blade
    '<rect x="72" y="55" width="3" height="22" fill="#80c0a0" rx="1" transform="rotate(20 72 55)"/>'
    '<rect x="68" y="50" width="10" height="4" fill="#608070" rx="1" transform="rotate(20 72 55)"/>'
    '<rect x="42" y="120" width="10" height="25" fill="#0f1a1a" stroke="#2a5040" stroke-width="1" rx="2"/>'
    '<rect x="58" y="122" width="10" height="25" fill="#0f1a1a" stroke="#2a5040" stroke-width="1" rx="2"/>'
)
print("  enemy_cultist_scout.svg")

save("enemy_cultist_guard",
    # Heavy guard with shield
    '<ellipse cx="60" cy="98" rx="24" ry="40" fill="#0f1525" stroke="#2a4060" stroke-width="2.5"/>'
    '<rect x="46" y="55" width="28" height="20" fill="#152030" stroke="#3a5580" stroke-width="1.5" rx="3"/>'
    '<ellipse cx="60" cy="42" rx="17" ry="18" fill="#0f1525" stroke="#3a5580" stroke-width="2"/>'
    '<circle cx="52" cy="38" r="3" fill="#6699dd"/>'
    '<circle cx="68" cy="38" r="3" fill="#6699dd"/>'
    '<rect x="54" y="48" width="12" height="3" fill="#3a5580" rx="1"/>'
    # Tower shield
    '<rect x="30" y="58" width="18" height="45" fill="#1a2a40" stroke="#6699dd" stroke-width="2" rx="4"/>'
    '<line x1="35" y1="68" x2="43" y2="68" stroke="#6699dd" stroke-width="1" opacity="0.4"/>'
    '<line x1="35" y1="78" x2="43" y2="78" stroke="#6699dd" stroke-width="1" opacity="0.4"/>'
    '<line x1="35" y1="88" x2="43" y2="88" stroke="#6699dd" stroke-width="1" opacity="0.4"/>'
    '<rect x="40" y="122" width="14" height="25" fill="#0f1525" stroke="#2a4060" stroke-width="1" rx="2"/>'
    '<rect x="62" y="122" width="14" height="25" fill="#0f1525" stroke="#2a4060" stroke-width="1" rx="2"/>'
)
print("  enemy_cultist_guard.svg")

save("enemy_cultist_shaman",
    # Shaman with staff and glowing hands
    '<ellipse cx="60" cy="98" rx="18" ry="36" fill="#150a20" stroke="#402060" stroke-width="2"/>'
    '<ellipse cx="60" cy="40" rx="15" ry="16" fill="#150a20" stroke="#554080" stroke-width="1.5"/>'
    '<circle cx="52" cy="36" r="3" fill="#bb88ee"/>'
    '<circle cx="68" cy="36" r="3" fill="#bb88ee"/>'
    '<path d="M52 46 Q60 52 68 46" fill="none" stroke="#554080" stroke-width="1.5"/>'
    # Staff
    '<line x1="38" y1="18" x2="42" y2="138" stroke="#6644aa" stroke-width="3.5"/>'
    '<circle cx="38" cy="18" r="6" fill="#8855cc" opacity="0.6"/>'
    '<circle cx="38" cy="18" r="3" fill="#cc88ff" opacity="0.8"/>'
    # Glowing hands
    '<circle cx="70" cy="72" r="5" fill="#8855cc" opacity="0.4"/>'
    '<circle cx="70" cy="72" r="3" fill="#cc88ff" opacity="0.6"/>'
    '<rect x="44" y="120" width="12" height="25" fill="#150a20" stroke="#402060" stroke-width="1" rx="2"/>'
    '<rect x="62" y="120" width="12" height="25" fill="#150a20" stroke="#402060" stroke-width="1" rx="2"/>'
)
print("  enemy_cultist_shaman.svg")

save("enemy_cultist_acolyte",
    # Tiny acolyte, praying pose, no weapon
    '<ellipse cx="60" cy="105" rx="14" ry="30" fill="#1a0815" stroke="#3a1830" stroke-width="1.5"/>'
    '<ellipse cx="60" cy="48" rx="13" ry="15" fill="#1a0815" stroke="#3a1830" stroke-width="1.5"/>'
    '<circle cx="53" cy="44" r="2.5" fill="#ee88aa"/>'
    '<circle cx="67" cy="44" r="2.5" fill="#ee88aa"/>'
    '<path d="M55 52 L60 56 L65 52" fill="none" stroke="#3a1830" stroke-width="1"/>'
    # Praying hands
    '<rect x="58" y="66" width="4" height="16" fill="#3a1830" rx="2"/>'
    # Kneeling legs (small)
    '<rect x="48" y="125" width="10" height="20" fill="#1a0815" stroke="#3a1830" stroke-width="1" rx="2"/>'
    '<rect x="62" y="125" width="10" height="20" fill="#1a0815" stroke="#3a1830" stroke-width="1" rx="2"/>'
)
print("  enemy_cultist_acolyte.svg")

save("enemy_cultist_brute",
    # Large brute with great axe
    '<ellipse cx="60" cy="92" rx="28" ry="44" fill="#1a0e0a" stroke="#4a2818" stroke-width="2.5"/>'
    '<rect x="46" y="54" width="28" height="16" fill="#2a1510" stroke="#5a3528" stroke-width="1.5" rx="3"/>'
    '<ellipse cx="60" cy="38" rx="18" ry="19" fill="#1a0e0a" stroke="#5a3528" stroke-width="2"/>'
    '<circle cx="51" cy="34" r="3" fill="#ee6633"/>'
    '<circle cx="69" cy="34" r="3" fill="#ee6633"/>'
    '<rect x="54" y="44" width="12" height="3" fill="#5a3528" rx="1"/>'
    # Great axe
    '<rect x="82" y="20" width="5" height="85" fill="#4a2818" rx="2"/>'
    '<path d="M74 20 L95 12 L90 36 Z" fill="#6a4030" stroke="#5a3528" stroke-width="1"/>'
    '<rect x="40" y="118" width="16" height="28" fill="#1a0e0a" stroke="#4a2818" stroke-width="1.5" rx="3"/>'
    '<rect x="64" y="118" width="16" height="28" fill="#1a0e0a" stroke="#4a2818" stroke-width="1.5" rx="3"/>'
)
print("  enemy_cultist_brute.svg")

# ---- Act 2: Shadow Bazaar ----

save("enemy_thief_cutpurse",
    # Small thief holding coin purse
    '<ellipse cx="58" cy="100" rx="17" ry="35" fill="#0a180a" stroke="#2a4a20" stroke-width="1.8"/>'
    '<ellipse cx="58" cy="44" rx="14" ry="16" fill="#0a180a" stroke="#2a4a20" stroke-width="1.5"/>'
    '<circle cx="51" cy="40" r="2.5" fill="#88dd66"/>'
    '<circle cx="65" cy="40" r="2.5" fill="#88dd66"/>'
    '<path d="M52 48 Q58 52 64 48" fill="none" stroke="#2a4a20" stroke-width="1.2"/>'
    # Coin purse
    '<circle cx="50" cy="72" r="8" fill="#3a5a28" stroke="#88dd66" stroke-width="1"/>'
    '<text x="50" y="75" text-anchor="middle" fill="#ffdd44" font-size="8">G</text>'
    '<rect x="46" y="120" width="10" height="25" fill="#0a180a" stroke="#2a4a20" stroke-width="1" rx="2"/>'
    '<rect x="60" y="120" width="10" height="25" fill="#0a180a" stroke="#2a4a20" stroke-width="1" rx="2"/>'
)
print("  enemy_thief_cutpurse.svg")

save("enemy_thief_assassin",
    # Dark cloaked assassin, twin daggers
    '<ellipse cx="60" cy="96" rx="18" ry="38" fill="#080812" stroke="#282048" stroke-width="2"/>'
    '<ellipse cx="60" cy="42" rx="15" ry="17" fill="#080812" stroke="#383060" stroke-width="1.5"/>'
    '<circle cx="52" cy="38" r="3" fill="#cc88ff"/>'
    '<circle cx="68" cy="38" r="3" fill="#cc88ff"/>'
    '<path d="M52 48 L60 54 L68 48" fill="none" stroke="#383060" stroke-width="1.5"/>'
    # Twin daggers
    '<line x1="48" y1="28" x2="55" y2="72" stroke="#aa77dd" stroke-width="2.5" opacity="0.8"/>'
    '<line x1="72" y1="28" x2="65" y2="72" stroke="#aa77dd" stroke-width="2.5" opacity="0.8"/>'
    '<rect x="48" y="118" width="12" height="26" fill="#080812" stroke="#282048" stroke-width="1" rx="2"/>'
    '<rect x="60" y="118" width="12" height="26" fill="#080812" stroke="#282048" stroke-width="1" rx="2"/>'
)
print("  enemy_thief_assassin.svg")

save("enemy_thief_fencer",
    # Elegant fencer with rapier
    '<ellipse cx="60" cy="98" rx="18" ry="38" fill="#0a1218" stroke="#284060" stroke-width="2"/>'
    '<ellipse cx="60" cy="42" rx="15" ry="17" fill="#0a1218" stroke="#385880" stroke-width="1.5"/>'
    '<circle cx="52" cy="38" r="2.5" fill="#66bbee"/>'
    '<circle cx="68" cy="38" r="2.5" fill="#66bbee"/>'
    '<path d="M52 48 Q60 54 68 48" fill="none" stroke="#385880" stroke-width="1.5"/>'
    # Rapier — long thin blade
    '<line x1="82" y1="35" x2="42" y2="50" stroke="#aaccee" stroke-width="1.5"/>'
    '<circle cx="82" cy="35" r="3" fill="#66bbee"/>'
    # Guard stance
    '<rect x="42" y="65" width="12" height="30" fill="#0a1218" stroke="#284060" stroke-width="1" rx="2"/>'
    '<rect x="46" y="120" width="12" height="25" fill="#0a1218" stroke="#284060" stroke-width="1" rx="2"/>'
    '<rect x="62" y="120" width="12" height="25" fill="#0a1218" stroke="#284060" stroke-width="1" rx="2"/>'
)
print("  enemy_thief_fencer.svg")

save("enemy_slaver_red",
    # Red-armored slaver with whip
    '<ellipse cx="60" cy="96" rx="22" ry="40" fill="#180a0a" stroke="#5a2020" stroke-width="2.5"/>'
    '<ellipse cx="60" cy="40" rx="17" ry="18" fill="#180a0a" stroke="#5a2020" stroke-width="2"/>'
    '<circle cx="51" cy="36" r="3" fill="#ff4444"/>'
    '<circle cx="69" cy="36" r="3" fill="#ff4444"/>'
    '<rect x="53" y="46" width="14" height="3" fill="#5a2020" rx="1"/>'
    # Whip
    '<path d="M78 55 Q95 45 85 80 Q75 70 80 60" fill="none" stroke="#dd6644" stroke-width="2"/>'
    '<rect x="76" y="52" width="6" height="12" fill="#6a3030" rx="1"/>'
    '<rect x="40" y="118" width="14" height="28" fill="#180a0a" stroke="#5a2020" stroke-width="1.5" rx="3"/>'
    '<rect x="62" y="118" width="14" height="28" fill="#180a0a" stroke="#5a2020" stroke-width="1.5" rx="3"/>'
)
print("  enemy_slaver_red.svg")

save("enemy_slaver_blue",
    # Blue-armored slaver with chain
    '<ellipse cx="60" cy="96" rx="22" ry="40" fill="#0a0a18" stroke="#203060" stroke-width="2.5"/>'
    '<ellipse cx="60" cy="40" rx="17" ry="18" fill="#0a0a18" stroke="#203060" stroke-width="2"/>'
    '<circle cx="51" cy="36" r="3" fill="#4466ff"/>'
    '<circle cx="69" cy="36" r="3" fill="#4466ff"/>'
    '<rect x="53" y="46" width="14" height="3" fill="#203060" rx="1"/>'
    # Chain
    '<path d="M78 50 Q90 40 88 65 Q86 75 82 68" fill="none" stroke="#6688cc" stroke-width="2.5"/>'
    '<circle cx="80" cy="50" r="3" fill="none" stroke="#6688cc" stroke-width="1.5"/>'
    '<circle cx="86" cy="58" r="3" fill="none" stroke="#6688cc" stroke-width="1.5"/>'
    '<rect x="40" y="118" width="14" height="28" fill="#0a0a18" stroke="#203060" stroke-width="1.5" rx="3"/>'
    '<rect x="62" y="118" width="14" height="28" fill="#0a0a18" stroke="#203060" stroke-width="1.5" rx="3"/>'
)
print("  enemy_slaver_blue.svg")

save("enemy_plague_rat",
    # Rat-like creature, toxic green
    '<ellipse cx="55" cy="105" rx="20" ry="32" fill="#181808" stroke="#4a4a10" stroke-width="2"/>'
    '<ellipse cx="48" cy="52" rx="16" ry="14" fill="#181808" stroke="#4a4a10" stroke-width="1.5"/>'
    '<circle cx="40" cy="48" r="3" fill="#ddff44"/>'
    '<circle cx="50" cy="46" r="3" fill="#ddff44"/>'
    # Snout
    '<ellipse cx="34" cy="54" rx="8" ry="5" fill="#181808" stroke="#4a4a10" stroke-width="1"/>'
    '<circle cx="32" cy="53" r="2" fill="#ff88aa"/>'
    # Ears
    '<circle cx="42" cy="38" r="6" fill="none" stroke="#4a4a10" stroke-width="1.5"/>'
    '<circle cx="56" cy="38" r="6" fill="none" stroke="#4a4a10" stroke-width="1.5"/>'
    # Tail
    '<path d="M75 105 Q95 110 90 130 Q85 140 88 145" fill="none" stroke="#6a6a20" stroke-width="2"/>'
    # Toxic glow
    '<circle cx="60" cy="90" r="6" fill="#88ff44" opacity="0.2"/>'
    '<rect x="40" y="125" width="10" height="20" fill="#181808" stroke="#4a4a10" stroke-width="1" rx="2"/>'
    '<rect x="58" y="125" width="10" height="20" fill="#181808" stroke="#4a4a10" stroke-width="1" rx="2"/>'
)
print("  enemy_plague_rat.svg")

save("enemy_mercenary",
    # Heavily armored veteran
    '<ellipse cx="60" cy="95" rx="24" ry="42" fill="#141418" stroke="#505560" stroke-width="2.5"/>'
    '<ellipse cx="60" cy="40" rx="18" ry="19" fill="#141418" stroke="#606878" stroke-width="2"/>'
    '<circle cx="51" cy="36" r="3" fill="#ccd0d8"/>'
    '<circle cx="69" cy="36" r="3" fill="#ccd0d8"/>'
    '<rect x="53" y="46" width="14" height="3" fill="#606878" rx="1"/>'
    # Shoulder armor
    '<rect x="30" y="58" width="20" height="12" fill="#202028" stroke="#707880" stroke-width="1.5" rx="3"/>'
    '<rect x="70" y="58" width="20" height="12" fill="#202028" stroke="#707880" stroke-width="1.5" rx="3"/>'
    # Sword
    '<rect x="82" y="30" width="4" height="50" fill="#8890a0" rx="1"/>'
    '<rect x="76" y="26" width="16" height="6" fill="#707880" rx="1.5"/>'
    '<rect x="40" y="118" width="14" height="28" fill="#141418" stroke="#505560" stroke-width="1.5" rx="3"/>'
    '<rect x="62" y="118" width="14" height="28" fill="#141418" stroke="#505560" stroke-width="1.5" rx="3"/>'
)
print("  enemy_mercenary.svg")

# ---- Act 3: Ancient Sanctum ----

save("enemy_corrupted_guardian",
    # Stone guardian, cracked, purple corruption
    '<ellipse cx="60" cy="95" rx="26" ry="44" fill="#1a1520" stroke="#4a3060" stroke-width="2.5"/>'
    '<ellipse cx="60" cy="38" rx="19" ry="20" fill="#1a1520" stroke="#4a3060" stroke-width="2"/>'
    '<circle cx="51" cy="34" r="3" fill="#cc88ff"/>'
    '<circle cx="69" cy="34" r="3" fill="#cc88ff"/>'
    '<rect x="54" y="46" width="12" height="3" fill="#4a3060" rx="1"/>'
    # Stone cracks
    '<line x1="48" y1="60" x2="55" y2="72" stroke="#6a4080" stroke-width="1" opacity="0.6"/>'
    '<line x1="55" y1="72" x2="52" y2="82" stroke="#6a4080" stroke-width="1" opacity="0.6"/>'
    '<line x1="68" y1="65" x2="72" y2="78" stroke="#6a4080" stroke-width="1" opacity="0.5"/>'
    # Shield arm
    '<rect x="30" y="62" width="22" height="40" fill="#1a1520" stroke="#4a3060" stroke-width="1.5" rx="4"/>'
    '<rect x="40" y="120" width="14" height="26" fill="#1a1520" stroke="#4a3060" stroke-width="1.5" rx="3"/>'
    '<rect x="62" y="120" width="14" height="26" fill="#1a1520" stroke="#4a3060" stroke-width="1.5" rx="3"/>'
)
print("  enemy_corrupted_guardian.svg")

save("enemy_dark_oracle",
    # Floating hooded figure, dark energy
    '<ellipse cx="60" cy="85" rx="20" ry="30" fill="#0a0a10" stroke="#202040" stroke-width="2"/>'
    '<ellipse cx="60" cy="38" rx="17" ry="18" fill="#0a0a10" stroke="#303060" stroke-width="1.5"/>'
    '<circle cx="50" cy="34" r="3.5" fill="#ffffff" opacity="0.6"/>'
    '<circle cx="70" cy="34" r="3.5" fill="#ffffff" opacity="0.6"/>'
    '<path d="M50 44 Q60 52 70 44" fill="none" stroke="#303060" stroke-width="2"/>'
    # Floating — no legs, dark mist below
    '<ellipse cx="60" cy="110" rx="25" ry="15" fill="#151528" opacity="0.5"/>'
    '<ellipse cx="55" cy="112" rx="10" ry="8" fill="#202040" opacity="0.4"/>'
    '<ellipse cx="68" cy="108" rx="12" ry="6" fill="#181838" opacity="0.3"/>'
    # Dark energy in hands
    '<circle cx="42" cy="68" r="6" fill="#303060" opacity="0.4"/>'
    '<circle cx="42" cy="68" r="3" fill="#6060cc" opacity="0.5"/>'
    '<circle cx="78" cy="68" r="6" fill="#303060" opacity="0.4"/>'
    '<circle cx="78" cy="68" r="3" fill="#6060cc" opacity="0.5"/>'
)
print("  enemy_dark_oracle.svg")

save("enemy_void_beast",
    # Amorphous shadow creature with tendrils
    '<ellipse cx="60" cy="90" rx="26" ry="40" fill="#080410" stroke="#301060" stroke-width="2"/>'
    '<ellipse cx="60" cy="42" rx="16" ry="15" fill="#080410" stroke="#402080" stroke-width="1.5"/>'
    '<circle cx="52" cy="38" r="3" fill="#9944ff"/>'
    '<circle cx="68" cy="38" r="3" fill="#9944ff"/>'
    '<ellipse cx="60" cy="50" rx="10" ry="6" fill="#301060" opacity="0.5"/>'
    # Tendrils
    '<path d="M40 80 Q20 85 15 110 Q12 120 18 125" fill="none" stroke="#402080" stroke-width="3"/>'
    '<path d="M80 80 Q100 82 105 105 Q108 115 102 120" fill="none" stroke="#402080" stroke-width="3"/>'
    '<path d="M50 110 Q40 130 35 140" fill="none" stroke="#301060" stroke-width="2.5"/>'
    '<path d="M70 110 Q80 128 85 138" fill="none" stroke="#301060" stroke-width="2.5"/>'
    # Core glow
    '<circle cx="60" cy="72" r="5" fill="#8040ee" opacity="0.3"/>'
)
print("  enemy_void_beast.svg")

save("enemy_ancient_automaton",
    # Mechanical construct, angular
    '<rect x="35" y="55" width="50" height="55" fill="#181820" stroke="#606880" stroke-width="2" rx="4"/>'
    '<rect x="30" y="40" width="60" height="18" fill="#202028" stroke="#707890" stroke-width="1.5" rx="3"/>'
    '<rect x="45" y="20" width="30" height="22" fill="#202028" stroke="#707890" stroke-width="2" rx="4"/>'
    '<circle cx="52" cy="30" r="3" fill="#ff6644" opacity="0.8"/>'
    '<circle cx="68" cy="30" r="3" fill="#ff6644" opacity="0.8"/>'
    '<rect x="54" y="36" width="12" height="3" fill="#707890" rx="1" opacity="0.6"/>'
    # Mechanical joints
    '<circle cx="40" cy="42" r="4" fill="none" stroke="#8898a8" stroke-width="1.5"/>'
    '<circle cx="80" cy="42" r="4" fill="none" stroke="#8898a8" stroke-width="1.5"/>'
    # Arms
    '<rect x="22" y="60" width="14" height="35" fill="#181820" stroke="#606880" stroke-width="1.5" rx="2"/>'
    '<rect x="84" y="60" width="14" height="35" fill="#181820" stroke="#606880" stroke-width="1.5" rx="2"/>'
    # Legs
    '<rect x="42" y="108" width="14" height="30" fill="#181820" stroke="#606880" stroke-width="1.5" rx="2"/>'
    '<rect x="64" y="108" width="14" height="30" fill="#181820" stroke="#606880" stroke-width="1.5" rx="2"/>'
)
print("  enemy_ancient_automaton.svg")

save("enemy_temple_knight",
    # Corrupted holy knight, gold/black armor
    '<ellipse cx="60" cy="95" rx="22" ry="40" fill="#181008" stroke="#886622" stroke-width="2.5"/>'
    '<rect x="42" y="52" width="36" height="18" fill="#201510" stroke="#886622" stroke-width="1.5" rx="3"/>'
    '<ellipse cx="60" cy="38" rx="16" ry="17" fill="#181008" stroke="#886622" stroke-width="2"/>'
    '<circle cx="52" cy="34" r="3" fill="#ffcc44"/>'
    '<circle cx="68" cy="34" r="3" fill="#ffcc44"/>'
    '<rect x="54" y="44" width="12" height="3" fill="#886622" rx="1"/>'
    # Holy symbol on chest
    '<line x1="60" y1="56" x2="60" y2="68" stroke="#ffcc44" stroke-width="2.5"/>'
    '<line x1="52" y1="62" x2="68" y2="62" stroke="#ffcc44" stroke-width="2.5"/>'
    # Cape
    '<path d="M42 60 Q35 85 38 130" fill="none" stroke="#664418" stroke-width="4" opacity="0.5"/>'
    '<rect x="40" y="118" width="14" height="28" fill="#181008" stroke="#886622" stroke-width="1.5" rx="3"/>'
    '<rect x="62" y="118" width="14" height="28" fill="#181008" stroke="#886622" stroke-width="1.5" rx="3"/>'
)
print("  enemy_temple_knight.svg")

save("enemy_flame_wraith",
    # Translucent fire spirit
    '<ellipse cx="60" cy="95" rx="18" ry="38" fill="#201008" stroke="#ee6622" stroke-width="1.5" opacity="0.7"/>'
    '<ellipse cx="60" cy="42" rx="14" ry="16" fill="#201008" stroke="#ee6622" stroke-width="1.5" opacity="0.7"/>'
    '<circle cx="52" cy="38" r="3" fill="#ffaa44"/>'
    '<circle cx="68" cy="38" r="3" fill="#ffaa44"/>'
    '<ellipse cx="60" cy="50" rx="8" ry="5" fill="#ee6622" opacity="0.3"/>'
    # Flames
    '<path d="M42 60 Q35 52 44 44 Q38 38 48 30" fill="#ee6622" opacity="0.3"/>'
    '<path d="M78 60 Q85 52 76 44 Q82 38 72 30" fill="#ee6622" opacity="0.3"/>'
    '<path d="M60 20 Q62 14 58 10 Q64 6 60 2" fill="#ff8844" opacity="0.25"/>'
    # Floating — no legs, flame wisps below
    '<ellipse cx="60" cy="125" rx="16" ry="8" fill="#ee6622" opacity="0.15"/>'
    '<ellipse cx="55" cy="130" rx="8" ry="5" fill="#ff8844" opacity="0.12"/>'
    '<ellipse cx="66" cy="132" rx="6" ry="4" fill="#ee6622" opacity="0.1"/>'
)
print("  enemy_flame_wraith.svg")

# ---- Elites ----

save("enemy_elite_sentinel",
    # Heavily armored elite with spear
    '<ellipse cx="60" cy="92" rx="24" ry="42" fill="#100a14" stroke="#602860" stroke-width="3"/>'
    '<ellipse cx="60" cy="36" rx="18" ry="19" fill="#100a14" stroke="#8040a0" stroke-width="2.5"/>'
    '<circle cx="51" cy="32" r="3.5" fill="#ee88ff"/>'
    '<circle cx="69" cy="32" r="3.5" fill="#ee88ff"/>'
    '<rect x="52" y="44" width="16" height="3" fill="#8040a0" rx="1"/>'
    # Crested helm
    '<rect x="48" y="16" width="24" height="8" fill="#602860" stroke="#8040a0" stroke-width="1.5" rx="2"/>'
    '<line x1="60" y1="12" x2="60" y2="20" stroke="#ee88ff" stroke-width="2"/>'
    # Spear
    '<line x1="88" y1="18" x2="38" y2="50" stroke="#ee88ff" stroke-width="2.5" opacity="0.8"/>'
    '<polygon points="88,18 94,14 90,22" fill="#ee88ff" opacity="0.7"/>'
    '<rect x="40" y="118" width="16" height="28" fill="#100a14" stroke="#602860" stroke-width="1.5" rx="3"/>'
    '<rect x="64" y="118" width="16" height="28" fill="#100a14" stroke="#602860" stroke-width="1.5" rx="3"/>'
)
print("  enemy_elite_sentinel.svg")

save("enemy_elite_inquisitor",
    # Red-robed inquisitor with book and fire
    '<ellipse cx="60" cy="96" rx="22" ry="40" fill="#180a0a" stroke="#882020" stroke-width="2.5"/>'
    '<ellipse cx="60" cy="38" rx="17" ry="18" fill="#180a0a" stroke="#a03030" stroke-width="2"/>'
    '<circle cx="51" cy="34" r="3.5" fill="#ff6644"/>'
    '<circle cx="69" cy="34" r="3.5" fill="#ff6644"/>'
    '<rect x="53" y="46" width="14" height="3" fill="#a03030" rx="1"/>'
    # Book in left hand
    '<rect x="35" y="62" width="18" height="14" fill="#401010" stroke="#a03030" stroke-width="1.5" rx="2"/>'
    '<line x1="44" y1="66" x2="44" y2="72" stroke="#ff6644" stroke-width="0.5" opacity="0.5"/>'
    # Fire in right hand
    '<circle cx="82" cy="68" r="8" fill="#ff6644" opacity="0.3"/>'
    '<circle cx="82" cy="68" r="4" fill="#ffaa44" opacity="0.5"/>'
    '<rect x="42" y="120" width="14" height="26" fill="#180a0a" stroke="#882020" stroke-width="1.5" rx="3"/>'
    '<rect x="64" y="120" width="14" height="26" fill="#180a0a" stroke="#882020" stroke-width="1.5" rx="3"/>'
)
print("  enemy_elite_inquisitor.svg")

save("enemy_elite_assassin_guild",
    # Shadowy figure with multiple blades
    '<ellipse cx="60" cy="94" rx="20" ry="38" fill="#060810" stroke="#303860" stroke-width="2.5"/>'
    '<ellipse cx="60" cy="40" rx="16" ry="17" fill="#060810" stroke="#404880" stroke-width="2"/>'
    '<circle cx="52" cy="36" r="3" fill="#aa88ff"/>'
    '<circle cx="68" cy="36" r="3" fill="#aa88ff"/>'
    '<path d="M52 46 Q60 52 68 46" fill="none" stroke="#404880" stroke-width="1.5"/>'
    # Multiple blades
    '<line x1="46" y1="22" x2="54" y2="70" stroke="#aa88ff" stroke-width="2"/>'
    '<line x1="74" y1="22" x2="66" y2="70" stroke="#aa88ff" stroke-width="2"/>'
    '<line x1="60" y1="85" x2="60" y2="110" stroke="#aa88ff" stroke-width="2"/>'
    # Blades tips
    '<circle cx="46" cy="22" r="2.5" fill="#ddbbff"/>'
    '<circle cx="74" cy="22" r="2.5" fill="#ddbbff"/>'
    '<rect x="48" y="118" width="12" height="26" fill="#060810" stroke="#303860" stroke-width="1" rx="2"/>'
    '<rect x="60" y="118" width="12" height="26" fill="#060810" stroke="#303860" stroke-width="1" rx="2"/>'
)
print("  enemy_elite_assassin_guild.svg")

save("enemy_elite_slaver_boss",
    # Large slaver with whip and chains
    '<ellipse cx="60" cy="88" rx="26" ry="46" fill="#180a08" stroke="#5a3020" stroke-width="3"/>'
    '<ellipse cx="60" cy="34" rx="18" ry="19" fill="#180a08" stroke="#6a4030" stroke-width="2.5"/>'
    '<circle cx="50" cy="30" r="3.5" fill="#ff6622"/>'
    '<circle cx="70" cy="30" r="3.5" fill="#ff6622"/>'
    '<rect x="53" y="42" width="14" height="3" fill="#6a4030" rx="1"/>'
    # Whip
    '<path d="M82 50 Q100 40 95 75 Q90 95 85 85" fill="none" stroke="#dd7744" stroke-width="2.5"/>'
    '<path d="M82 50 Q100 65 92 90 Q88 100 84 95" fill="none" stroke="#cc6633" stroke-width="2"/>'
    # Chains on belt
    '<ellipse cx="55" cy="78" rx="8" ry="3" fill="none" stroke="#888888" stroke-width="1.5"/>'
    '<ellipse cx="65" cy="78" rx="8" ry="3" fill="none" stroke="#888888" stroke-width="1.5"/>'
    '<rect x="38" y="118" width="16" height="28" fill="#180a08" stroke="#5a3020" stroke-width="1.5" rx="3"/>'
    '<rect x="64" y="118" width="16" height="28" fill="#180a08" stroke="#5a3020" stroke-width="1.5" rx="3"/>'
)
print("  enemy_elite_slaver_boss.svg")

save("enemy_elite_corrupted_golem",
    # Huge stone golem, massive presence
    '<rect x="30" y="30" width="60" height="80" fill="#1a1520" stroke="#5a4070" stroke-width="3" rx="6"/>'
    '<rect x="24" y="24" width="72" height="16" fill="#221828" stroke="#6a5080" stroke-width="2" rx="4"/>'
    '<circle cx="48" cy="32" r="4" fill="#cc44ff" opacity="0.8"/>'
    '<circle cx="72" cy="32" r="4" fill="#cc44ff" opacity="0.8"/>'
    '<rect x="52" y="38" width="16" height="3" fill="#6a5080" rx="1"/>'
    # Giant arms
    '<rect x="14" y="45" width="18" height="50" fill="#1a1520" stroke="#5a4070" stroke-width="2" rx="4"/>'
    '<rect x="88" y="45" width="18" height="50" fill="#1a1520" stroke="#5a4070" stroke-width="2" rx="4"/>'
    # Cracks
    '<line x1="42" y1="65" x2="50" y2="80" stroke="#7a5a90" stroke-width="1.5" opacity="0.5"/>'
    '<line x1="70" y1="55" x2="78" y2="68" stroke="#7a5a90" stroke-width="1.5" opacity="0.5"/>'
    '<line x1="50" y1="80" x2="58" y2="90" stroke="#7a5a90" stroke-width="1" opacity="0.3"/>'
    '<rect x="44" y="108" width="14" height="30" fill="#1a1520" stroke="#5a4070" stroke-width="2" rx="3"/>'
    '<rect x="62" y="108" width="14" height="30" fill="#1a1520" stroke="#5a4070" stroke-width="2" rx="3"/>'
)
print("  enemy_elite_corrupted_golem.svg")

save("enemy_elite_ancient_dragon",
    # Young dragon, wings, claw
    '<ellipse cx="60" cy="90" rx="24" ry="36" fill="#1a0a0a" stroke="#8a3030" stroke-width="2.5"/>'
    '<ellipse cx="60" cy="42" rx="17" ry="16" fill="#1a0a0a" stroke="#8a3030" stroke-width="2"/>'
    '<circle cx="52" cy="38" r="3" fill="#ff6644"/>'
    '<circle cx="68" cy="38" r="3" fill="#ff6644"/>'
    # Snout
    '<ellipse cx="60" cy="52" rx="10" ry="5" fill="#1a0a0a" stroke="#8a3030" stroke-width="1"/>'
    '<circle cx="56" cy="51" r="1.5" fill="#ff6644"/>'
    '<circle cx="64" cy="51" r="1.5" fill="#ff6644"/>'
    # Wings
    '<path d="M40 65 Q20 40 30 20 Q35 25 38 65" fill="#2a1010" stroke="#8a3030" stroke-width="1.5" opacity="0.6"/>'
    '<path d="M80 65 Q100 40 90 20 Q85 25 82 65" fill="#2a1010" stroke="#8a3030" stroke-width="1.5" opacity="0.6"/>'
    # Claw
    '<path d="M75 80 Q90 85 92 95" fill="none" stroke="#cc6644" stroke-width="3"/>'
    '<path d="M92 95 L98 92 M92 95 L96 100 M92 95 L88 100" stroke="#cc6644" stroke-width="1.5"/>'
    '<rect x="48" y="118" width="14" height="25" fill="#1a0a0a" stroke="#8a3030" stroke-width="1.5" rx="3"/>'
    '<rect x="60" y="118" width="14" height="25" fill="#1a0a0a" stroke="#8a3030" stroke-width="1.5" rx="3"/>'
)
print("  enemy_elite_ancient_dragon.svg")

# ---- Special ----

save("enemy_merchant",
    # Friendly merchant with wares, surprised fighting pose
    '<ellipse cx="60" cy="98" rx="20" ry="36" fill="#1a1810" stroke="#4a4020" stroke-width="2"/>'
    '<ellipse cx="60" cy="42" rx="16" ry="17" fill="#1a1810" stroke="#5a5030" stroke-width="1.5"/>'
    '<circle cx="53" cy="38" r="3" fill="#886633"/>'
    '<circle cx="67" cy="38" r="3" fill="#886633"/>'
    '<path d="M54 48 Q60 53 66 48" fill="none" stroke="#5a5030" stroke-width="1.5"/>'
    # Hat
    '<rect x="44" y="22" width="32" height="6" fill="#3a3020" stroke="#6a5a30" stroke-width="1" rx="2"/>'
    '<rect x="48" y="14" width="24" height="10" fill="#3a3020" stroke="#6a5a30" stroke-width="1" rx="2"/>'
    # Wares — holding items
    '<rect x="32" y="62" width="12" height="16" fill="#4a4020" stroke="#6a5a30" stroke-width="1" rx="2"/>'
    '<circle cx="38" cy="58" r="4" fill="#887744" opacity="0.5"/>'
    '<rect x="44" y="120" width="12" height="25" fill="#1a1810" stroke="#4a4020" stroke-width="1" rx="2"/>'
    '<rect x="62" y="120" width="12" height="25" fill="#1a1810" stroke="#4a4020" stroke-width="1" rx="2"/>'
)
print("  enemy_merchant.svg")

save("enemy_boss_high_priest",
    # Tall ceremonial priest, crown, staff, flowing robes
    '<ellipse cx="60" cy="96" rx="22" ry="44" fill="#10081a" stroke="#6a30a0" stroke-width="3"/>'
    '<ellipse cx="60" cy="34" rx="18" ry="19" fill="#10081a" stroke="#8a40c0" stroke-width="2.5"/>'
    '<circle cx="50" cy="30" r="3.5" fill="#ff88ff"/>'
    '<circle cx="70" cy="30" r="3.5" fill="#ff88ff"/>'
    '<rect x="53" y="42" width="14" height="3" fill="#8a40c0" rx="1"/>'
    # Ceremonial crown
    '<polygon points="45,18 52,6 60,16 68,6 75,18" fill="#6a30a0" stroke="#cc88ff" stroke-width="1.5"/>'
    '<circle cx="60" cy="10" r="3" fill="#ff88ff" opacity="0.8"/>'
    # Staff
    '<line x1="32" y1="10" x2="36" y2="140" stroke="#8a40c0" stroke-width="4"/>'
    '<circle cx="32" cy="10" r="8" fill="none" stroke="#cc88ff" stroke-width="2.5"/>'
    '<circle cx="32" cy="10" r="4" fill="#cc88ff" opacity="0.6"/>'
    # Robe detail
    '<line x1="48" y1="60" x2="46" y2="130" stroke="#8a40c0" stroke-width="1.5" opacity="0.4"/>'
    '<line x1="72" y1="60" x2="74" y2="130" stroke="#8a40c0" stroke-width="1.5" opacity="0.4"/>'
    '<rect x="42" y="122" width="14" height="25" fill="#10081a" stroke="#6a30a0" stroke-width="1.5" rx="3"/>'
    '<rect x="62" y="122" width="14" height="25" fill="#10081a" stroke="#6a30a0" stroke-width="1.5" rx="3"/>'
)
print("  enemy_boss_high_priest.svg")

save("enemy_boss_master_thief",
    # Masked agile master thief, many throwing knives
    '<ellipse cx="60" cy="96" rx="19" ry="38" fill="#060610" stroke="#404060" stroke-width="2.5"/>'
    '<ellipse cx="60" cy="40" rx="15" ry="17" fill="#060610" stroke="#505080" stroke-width="2"/>'
    '<circle cx="52" cy="36" r="3" fill="#ccccff"/>'
    '<circle cx="68" cy="36" r="3" fill="#ccccff"/>'
    '<path d="M52 46 Q60 52 68 46" fill="none" stroke="#505080" stroke-width="1.5"/>'
    # Mask
    '<rect x="46" y="34" width="28" height="5" fill="#202040" stroke="#505080" stroke-width="1" rx="2"/>'
    # Cloak
    '<path d="M42 55 Q35 90 38 130" fill="none" stroke="#404060" stroke-width="4" opacity="0.5"/>'
    '<path d="M78 55 Q85 90 82 130" fill="none" stroke="#404060" stroke-width="4" opacity="0.5"/>'
    # Throwing knives (fan pattern)
    '<line x1="78" y1="45" x2="100" y2="30" stroke="#aa88dd" stroke-width="2"/>'
    '<line x1="80" y1="50" x2="105" y2="50" stroke="#aa88dd" stroke-width="2"/>'
    '<line x1="78" y1="55" x2="100" y2="70" stroke="#aa88dd" stroke-width="2"/>'
    '<rect x="48" y="118" width="12" height="26" fill="#060610" stroke="#404060" stroke-width="1" rx="2"/>'
    '<rect x="60" y="118" width="12" height="26" fill="#060610" stroke="#404060" stroke-width="1" rx="2"/>'
)
print("  enemy_boss_master_thief.svg")

save("enemy_boss_corrupted_heart",
    # Pulsating heart with dark energy, the ultimate boss
    '<ellipse cx="60" cy="75" rx="32" ry="38" fill="#1a0010" stroke="#880040" stroke-width="3"/>'
    '<ellipse cx="60" cy="75" rx="20" ry="24" fill="#2a0020" stroke="#aa0050" stroke-width="2"/>'
    '<ellipse cx="60" cy="75" rx="10" ry="12" fill="#3a0030" stroke="#cc0060" stroke-width="1.5"/>'
    '<circle cx="60" cy="75" r="5" fill="#ff0066" opacity="0.5"/>'
    '<circle cx="60" cy="75" r="2.5" fill="#ff4488" opacity="0.7"/>'
    # Aorta/vessels at top
    '<path d="M48 38 Q45 22 38 14" fill="none" stroke="#880040" stroke-width="6"/>'
    '<path d="M72 38 Q75 22 82 14" fill="none" stroke="#880040" stroke-width="6"/>'
    '<path d="M54 40 Q52 28 50 18" fill="none" stroke="#aa0050" stroke-width="4"/>'
    '<path d="M66 40 Q68 28 70 18" fill="none" stroke="#aa0050" stroke-width="4"/>'
    # Dark energy aura
    '<ellipse cx="60" cy="75" rx="38" ry="44" fill="none" stroke="#ff0066" stroke-width="1.5" opacity="0.3"/>'
    '<ellipse cx="60" cy="75" rx="44" ry="50" fill="none" stroke="#ff0066" stroke-width="1" opacity="0.15"/>'
    # Tendrils below
    '<path d="M40 110 Q35 135 30 150" fill="none" stroke="#880040" stroke-width="3"/>'
    '<path d="M55 112 Q52 140 50 155" fill="none" stroke="#880040" stroke-width="3"/>'
    '<path d="M65 112 Q68 140 70 155" fill="none" stroke="#880040" stroke-width="3"/>'
    '<path d="M80 110 Q85 135 90 150" fill="none" stroke="#880040" stroke-width="3"/>'
    # Evil eyes on heart
    '<ellipse cx="50" cy="68" rx="4" ry="3" fill="#ff0066" opacity="0.6"/>'
    '<ellipse cx="70" cy="68" rx="4" ry="3" fill="#ff0066" opacity="0.6"/>'
)
print("  enemy_boss_corrupted_heart.svg")

print("\nDONE! Generated all unique enemy and character SVGs.")
