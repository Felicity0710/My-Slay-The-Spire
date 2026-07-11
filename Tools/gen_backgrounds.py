"""Generate themed battle background SVGs — 5 per act, 15 total."""
import os, random
random.seed(42)

BG_DIR = "Assets/Backgrounds"
os.makedirs(BG_DIR, exist_ok=True)
W, H = 1920, 1080

# Act 1: Cultist Outpost — deep crimson, stone, torch, ritual
ACT1_COLORS = [
    ("#1a0808", "#2a1010", "#3a1818", "#cc3333"),  # dark red stone
    ("#0d0808", "#1f1015", "#2d1820", "#bb4444"),  # torch-lit corridor
    ("#100808", "#201212", "#381818", "#aa3333"),   # blood-stained floor
    ("#0a080c", "#1a1018", "#2a1828", "#cc3355"),   # ritual chamber
    ("#080a0a", "#151218", "#221a28", "#bb4455"),   # underground crypt
]
# Act 2: Shadow Bazaar — deep purple/blue, market, coins, shadows
ACT2_COLORS = [
    ("#080812", "#101028", "#181838", "#7755aa"),   # shadow market
    ("#0a0a14", "#12122a", "#1a1a3a", "#8866bb"),   # thieves' den
    ("#080a14", "#101830", "#182040", "#6655aa"),    # underground canal
    ("#0a0814", "#141228", "#1c1838", "#9966cc"),    # smuggler's warehouse
    ("#081012", "#101828", "#1a2038", "#7755bb"),    # lamp-lit alley
]
# Act 3: Ancient Sanctum — deep gold/amber, temple, corruption
ACT3_COLORS = [
    ("#0a0804", "#181208", "#2a1a0c", "#cc9944"),   # ruined temple
    ("#080804", "#141008", "#241808", "#ddaa55"),    # sanctum entrance
    ("#0a0a04", "#181410", "#2a2018", "#bb8833"),    # corrupted hall
    ("#080604", "#140e08", "#201810", "#ddaa44"),    # ancient altar
    ("#0a0808", "#1a1210", "#2c1c18", "#cc8833"),    # heart chamber
]

def hex_to_rgb(h):
    return (int(h[1:3],16), int(h[3:5],16), int(h[5:7],16))

def bg_svg(bg, mid, fg, accent, elements):
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}">\n'
    svg += f'<defs>\n'
    svg += f'<radialGradient id="vignette" cx="50%" cy="50%" r="70%">'
    svg += f'<stop offset="0%" stop-color="{mid}" stop-opacity="0.3"/>'
    svg += f'<stop offset="100%" stop-color="{bg}" stop-opacity="1"/></radialGradient>\n'
    svg += f'<linearGradient id="floor" x1="0" y1="0" x2="0" y2="1">'
    svg += f'<stop offset="0%" stop-color="{bg}"/><stop offset="100%" stop-color="{fg}"/></linearGradient>\n'
    svg += f'</defs>\n'

    # Sky/upper area
    svg += f'<rect width="{W}" height="{H*2//3}" fill="{bg}"/>\n'
    # Floor gradient
    svg += f'<rect y="{H*2//3}" width="{W}" height="{H//3}" fill="url(#floor)"/>\n'
    # Vignette overlay
    svg += f'<rect width="{W}" height="{H}" fill="url(#vignette)"/>\n'
    # Floor line
    svg += f'<line x1="0" y1="{H*2//3}" x2="{W}" y2="{H*2//3}" stroke="{accent}" stroke-width="2" opacity="0.3"/>\n'

    for el in elements:
        svg += f'  {el}\n'

    svg += '</svg>'
    return svg

def gen_act1():
    """Act 1: cultist outpost elements — stone arches, torches, ritual circles"""
    bgs = []
    for i, (bg, mid, fg, accent) in enumerate(ACT1_COLORS):
        seed = i * 100
        rng = random.Random(seed)
        elements = []
        # Stone arch pillars (left and right)
        for side in [120, W-180]:
            elements.append(f'<rect x="{side-40}" y="180" width="80" height="{H-180}" fill="{mid}" stroke="{accent}" stroke-width="1.5" opacity="0.4"/>')
            elements.append(f'<rect x="{side-50}" y="160" width="100" height="30" fill="{fg}" stroke="{accent}" stroke-width="1" opacity="0.5" rx="3"/>')
        # Torches on walls
        for tx in [160, W-220]:
            elements.append(f'<rect x="{tx-3}" y="220" width="6" height="40" fill="{accent}" opacity="0.6"/>')
            elements.append(f'<circle cx="{tx}" cy="215" r="10" fill="{accent}" opacity="0.15"/>')
            elements.append(f'<circle cx="{tx}" cy="215" r="5" fill="{accent}" opacity="0.3"/>')
        # Stone floor tiles
        for j in range(8):
            x = 200 + j * 200
            y = H*2//3 + 30 + (j%3)*50
            w = rng.randint(140, 220)
            elements.append(f'<rect x="{x}" y="{y}" width="{w}" height="80" fill="none" stroke="{accent}" stroke-width="0.5" opacity="0.15" rx="2"/>')
        # Ritual circle (center floor)
        elements.append(f'<circle cx="{W//2}" cy="{H*3//4}" r="80" fill="none" stroke="{accent}" stroke-width="1.5" opacity="0.2"/>')
        elements.append(f'<circle cx="{W//2}" cy="{H*3//4}" r="50" fill="none" stroke="{accent}" stroke-width="0.8" opacity="0.15"/>')
        elements.append(f'<circle cx="{W//2}" cy="{H*3//4}" r="20" fill="{accent}" opacity="0.08"/>')
        # Stars (pentagram points in ritual circle)
        for a in [0, 72, 144, 216, 288]:
            import math
            rad = math.radians(a)
            px = W//2 + 80*math.cos(rad)
            py = H*3//4 + 80*math.sin(rad)
            elements.append(f'<circle cx="{px:.0f}" cy="{py:.0f}" r="4" fill="{accent}" opacity="0.3"/>')
        # Chains hanging from ceiling
        for cx in [300, 600, 900, 1200, 1500]:
            elements.append(f'<line x1="{cx}" y1="0" x2="{cx+rng.randint(-20,20)}" y2="{120+rng.randint(0,40)}" stroke="{accent}" stroke-width="1" opacity="0.15"/>')
        bgs.append(bg_svg(bg, mid, fg, accent, elements))
    return bgs

def gen_act2():
    """Act 2: shadow bazaar — market stalls, coins, shadows, crates"""
    bgs = []
    for i, (bg, mid, fg, accent) in enumerate(ACT2_COLORS):
        seed = i * 200
        rng = random.Random(seed)
        elements = []
        # Market stall canopies
        for j in range(4):
            sx = 150 + j * 420
            elements.append(f'<path d="M{sx-80} 250 Q{sx} 200 {sx+80} 250" fill="{mid}" stroke="{accent}" stroke-width="1.5" opacity="0.3"/>')
            elements.append(f'<rect x="{sx-60}" y="250" width="120" height="120" fill="{fg}" stroke="{accent}" stroke-width="0.8" opacity="0.2" rx="2"/>')
            elements.append(f'<line x1="{sx}" y1="250" x2="{sx}" y2="370" stroke="{accent}" stroke-width="0.5" opacity="0.15"/>')
        # Floating coins
        for _ in range(12):
            cx = rng.randint(100, W-100)
            cy = rng.randint(H//3, H-100)
            r = rng.randint(8, 16)
            elements.append(f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="none" stroke="{accent}" stroke-width="1.5" opacity="0.2"/>')
            elements.append(f'<text x="{cx}" y="{cy+5}" text-anchor="middle" fill="{accent}" font-size="{r}" opacity="0.15">$</text>')
        # Lanterns
        for lx in [200, 600, 1000, 1400, 1700]:
            elements.append(f'<line x1="{lx}" y1="0" x2="{lx}" y2="80" stroke="{accent}" stroke-width="1" opacity="0.15"/>')
            elements.append(f'<rect x="{lx-12}" y="80" width="24" height="35" fill="{mid}" stroke="{accent}" stroke-width="1" opacity="0.3" rx="4"/>')
            elements.append(f'<circle cx="{lx}" cy="95" r="6" fill="{accent}" opacity="0.15"/>')
        # Shadow figures in background
        for _ in range(3):
            sx = rng.randint(100, W-100)
            sy = H*2//3 + rng.randint(20, 100)
            elements.append(f'<ellipse cx="{sx}" cy="{sy}" rx="25" ry="40" fill="{fg}" opacity="0.3"/>')
            elements.append(f'<circle cx="{sx}" cy="{sy-30}" r="14" fill="{fg}" opacity="0.3"/>')
        # Crates/barrels on ground
        for _ in range(5):
            bx = rng.randint(100, W-100)
            by = H*2//3 + rng.randint(80, 200)
            bw = rng.randint(30, 60)
            bh = rng.randint(30, 50)
            elements.append(f'<rect x="{bx}" y="{by}" width="{bw}" height="{bh}" fill="{fg}" stroke="{accent}" stroke-width="0.8" opacity="0.25" rx="3"/>')
        bgs.append(bg_svg(bg, mid, fg, accent, elements))
    return bgs

def gen_act3():
    """Act 3: ancient sanctum — temple columns, cracks, corruption, stained glass"""
    bgs = []
    for i, (bg, mid, fg, accent) in enumerate(ACT3_COLORS):
        seed = i * 300
        rng = random.Random(seed)
        elements = []
        # Massive columns
        for col_x in [200, 500, 800, 1100, 1400, 1700]:
            w = rng.randint(30, 60)
            elements.append(f'<rect x="{col_x}" y="100" width="{w}" height="{H-100}" fill="{mid}" stroke="{accent}" stroke-width="1" opacity="0.35" rx="3"/>')
            elements.append(f'<rect x="{col_x-5}" y="80" width="{w+10}" height="25" fill="{fg}" stroke="{accent}" stroke-width="1" opacity="0.4" rx="2"/>')
            # Column cracks
            if rng.random() < 0.5:
                elements.append(f'<line x1="{col_x+w//2}" y1="{140+rng.randint(0,100)}" x2="{col_x+w//2+rng.randint(-5,5)}" y2="{180+rng.randint(0,200)}" stroke="{accent}" stroke-width="1" opacity="0.2"/>')
        # Stained glass window (center top)
        elements.append(f'<circle cx="{W//2}" cy="200" r="90" fill="none" stroke="{accent}" stroke-width="2" opacity="0.3"/>')
        elements.append(f'<circle cx="{W//2}" cy="200" r="70" fill="none" stroke="{accent}" stroke-width="1" opacity="0.2"/>')
        for a in [0, 60, 120, 180, 240, 300]:
            import math
            rad = math.radians(a)
            x1 = W//2 + 35*math.cos(rad)
            y1 = 200 + 35*math.sin(rad)
            x2 = W//2 + 85*math.cos(rad)
            y2 = 200 + 85*math.sin(rad)
            elements.append(f'<line x1="{x1:.0f}" y1="{y1:.0f}" x2="{x2:.0f}" y2="{y2:.0f}" stroke="{accent}" stroke-width="0.8" opacity="0.2"/>')
        # Corruption veins crawling up walls
        for _ in range(4):
            cx = rng.randint(50, W-50)
            path = f'M{cx} {H} '
            for _ in range(5):
                cx += rng.randint(-40, 40)
                path += f'L{cx} {rng.randint(H//2, H-50)} '
            elements.append(f'<path d="{path}" fill="none" stroke="{accent}" stroke-width="2" opacity="0.12"/>')
        # Floating dust/embers
        for _ in range(15):
            dx = rng.randint(50, W-50)
            dy = rng.randint(50, H-50)
            dr = rng.uniform(1, 4)
            elements.append(f'<circle cx="{dx}" cy="{dy}" r="{dr:.1f}" fill="{accent}" opacity="0.15"/>')
        # Temple floor pattern
        for j in range(12):
            tx = 60 + j * 150
            ty = H*2//3 + 40 + (j%2) * 60
            elements.append(f'<rect x="{tx}" y="{ty}" width="140" height="50" fill="none" stroke="{accent}" stroke-width="0.6" opacity="0.12" rx="2"/>')
        bgs.append(bg_svg(bg, mid, fg, accent, elements))
    return bgs

print("Generating battle backgrounds...")
act1 = gen_act1()
act2 = gen_act2()
act3 = gen_act3()

for i, svg in enumerate(act1):
    with open(os.path.join(BG_DIR, f"battle_act1_{i+1}.svg"), "w", encoding="utf-8") as f:
        f.write(svg)
    print(f"  battle_act1_{i+1}.svg")

for i, svg in enumerate(act2):
    with open(os.path.join(BG_DIR, f"battle_act2_{i+1}.svg"), "w", encoding="utf-8") as f:
        f.write(svg)
    print(f"  battle_act2_{i+1}.svg")

for i, svg in enumerate(act3):
    with open(os.path.join(BG_DIR, f"battle_act3_{i+1}.svg"), "w", encoding="utf-8") as f:
        f.write(svg)
    print(f"  battle_act3_{i+1}.svg")

print(f"Done! Generated {len(act1)+len(act2)+len(act3)} backgrounds in {BG_DIR}/")
