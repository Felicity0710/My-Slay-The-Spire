import os

logs = {
    '05-26.md': '# Dev Log - 5/26\n\n## Enemy Balance Pass\n\nRebalanced all 29 enemy HPs and attack values referencing Slay the Spire:\n- Act 1 normals: 30-60 HP / 5-12 damage\n- Act 2 normals: 45-80 HP / 8-13 damage\n- Act 3 normals: 55-120 HP / 9-18 damage\n- Bosses: 170 / 235 / 330 HP\n\n## EnemyEncounterBuilder Rewrite\n\nRoot cause: Builder added ALL minFloor-qualified enemies to one fight (5 enemies at floor 6).\nFix: Random subset selection, 1-3 enemies per encounter. Boss selected by act index.',

    '05-27.md': '# Dev Log - 5/27\n\n## Distinct Enemy AI\n\nRewrote IntentResolver with unique AI for all 29 archetypes:\n- Cultist: 60% attack / 40% buff (tutorial)\n- Acolyte: frequent team buffs, very low HP (priority target)\n- Ancient Automaton: 2-turn charge -> Hyper Beam (telegraphed)\n- Corrupted Heart: 5-turn P1 -> 3-turn P2 (ultimate boss)\n\nEach enemy has clear telegraphing and distinct tactical role.',

    '05-28.md': '# Dev Log - 5/28\n\n## Achievement System\n\nExpanded AchievementCatalog from 12 to 24 achievements with categories:\n- Milestone, Combat, Collection, Playstyle, Character-specific, Challenge\n\n## Potion System Rewrite\n\nExpanded PotionData from 5 to 10 types. Added NameZh/DescriptionZh fields.\nAdded DisplayName/DisplayDescription properties for direct bilingual support.',

    '05-29.md': '# Dev Log - 5/29\n\n## Event System Expansion\n\nExpanded EventScene from 2 to 12 random events:\n- Positive: Shrine, Brewer, Healer, Altar, Chest\n- Risky: Dealer, Forge, Ritual, Gambling\n- Combat: Ambush, Slime Pit, Cursed Idol\n\nGameState.BeginRandomEvent() selects from all 12 events.',

    '05-30.md': '# Dev Log - 5/30\n\n## Act Intro UI\n\nRedesigned IntroEventScene with dark fantasy theme:\n- Act-specific story text (Cathedral / Bazaar / Sanctum)\n- Gold ornamental borders, HP bar, styled Continue button\n- Programmatic UI replacing TSCN node conflicts',

    '05-31.md': '# Dev Log - 5/31\n\n## Bestiary & Build Fixes\n\nCombatVisualCatalog expanded to 29 enemies with unique portraits and traits.\n\nBuild fixes:\n- TryAddRandomPotion signature fix\n- PendingEncounterType setter -> BeginEncounter()\n- Test file BuildEncounter calls updated\n- Vampire Philter simplified',

    '06-01.md': '# Dev Log - 6/1\n\n## Bilingual Localization\n\nExpanded en.json and zh_hans.json from ~550 to 705 keys:\n- 36 new cards, 28 new enemies, 12 events, 3 act stories\n- All 63 relics and 10 potions\n- Map node labels, UI buttons, battle log messages\n\nPython generation script (Tools/gen_locale.py) for maintainability.',

    '06-02.md': '# Dev Log - 6/2\n\n## Language Button & Mixed Language Fix\n\nSettingsScene: moved critical signal connections outside try-catch.\nLanguageButtonText: shows current language (中文/English), not target.\nLocalizationService.Get(): removed English fallback in Chinese mode - callers provide proper fallbacks.',

    '06-03.md': '# Dev Log - 6/3\n\n## Procedural Audio System\n\nCreated AudioManager autoload with AudioStreamGenerator synthesis:\n- BGM: 4-layer music (Bass + Chord Pad + Arpeggio + Melody)\n- D minor pentatonic / C phrygian scales\n- 24 SFX types with distinct frequencies and durations\n- 10 scene-specific BGM presets\n- Zero external file dependencies',

    '06-04.md': '# Dev Log - 6/4\n\n## BGM Quality & Localization Audit\n\nRewrote music engine with proper musical scales and multi-layer synthesis.\n\nAudited all 344 localization keys referenced in C# code. Found and filled 64 missing keys including battle log messages (curious, exhaust, replay, retain), map nodes (boss, intro), card kinds (attack, skill), shop UI, rest UI, and common buttons.',

    '06-05.md': '# Dev Log - 6/5\n\n## JSON Encoding Fix\n\nRoot cause of localization failures: Python json.dump outputs UTF-8 without BOM, but Godot FileAccess needs BOM for encoding detection.\n\nFixed: Re-saved all locale files with UTF-8 BOM + CRLF + ASCII-safe escaping.\nAdded ProjectSettings.GlobalizePath fallback in LoadLanguage().',

    '06-06.md': '# Dev Log - 6/6\n\n## Settings & Rest Bilingual\n\nReplaced all LocalizationService.Get() calls in NodeSettingsOverlay and SettingsScene with direct language checks (bool zh = CurrentLanguage == ZhHans).\nRestScene: all 15 text elements converted to inline bilingual.',

    '06-07.md': '# Dev Log - 6/7\n\n## Potions & Relics Bilingual\n\nPotionData: added DisplayName/DisplayDescription with built-in Chinese.\nUpdated 7 display points across BattleScene, MapScene, ShopScene, etc.\n\nRelicData: added ZhText dictionary with 63 relic Chinese translations.\nLocalizedName/LocalizedDescription check CurrentLanguage directly.',

    '06-08.md': '# Dev Log - 6/8\n\n## Shop & Bestiary Bilingual\n\nShopScene: all text (title, section labels, card types, prices, buttons) converted to inline bilingual.\n\nCombatVisualCatalog: added TraitFallbacksZh with 29 Chinese trait descriptions.\nGetEnemyTraitSummary returns Chinese when in ZhHans mode.',

    '06-09.md': '# Dev Log - 6/9\n\n## Event Effects & Text\n\nAll 12 event option labels converted to bilingual using Zh helper property.\n\nFixed event effects:\n- BrewerDrink: now adds upgraded Battle Focus card (representing +2 Strength)\n- RitualPerform: 18 HP -> 3 upgraded Battle Focus cards\n- Verified all other event actions work via GameState methods',

    '06-10.md': '# Dev Log - 6/10\n\n## Card Tooltips & Highlighting\n\nBattleScene.ShowKeywordTooltip: all 8 keyword descriptions in Chinese.\nCardView.UpdateKeywordChip: Retain/Exhaust/Curious/Replay in Chinese.\nCardData.CostLabel: direct language check.\nHighlightCardDescription: fixed Chinese keywords from wrong chars to correct ones.',

    '06-11.md': '# Dev Log - 6/11\n\n## Achievement Popup System\n\nCreated AchievementPopup.cs with slide-in animation and auto-dismiss.\nCreated AchievementState.cs with persistent unlock tracking (achievements.json).\n\nVictoryScene and DefeatScene evaluate achievements at end of run.\nGameState tracks EliteKills, EventsResolved, MerchantRobbed for triggers.',

    '06-12.md': '# Dev Log - 6/12\n\n## SVG Card Art Generation\n\nPython script generates SVG card art for all 77 cards:\n- Iron Vanguard (red): sword/shield geometry, angular lines\n- Phantom Dancer (purple): dagger arcs, circular accents\n- Storm Mage (blue): orb/lightning patterns, scattered particles\n\nGenerated 63 relic-specific icons and 10 potion icons.',

    '06-13.md': '# Dev Log - 6/13\n\n## Unique Card Art Redesign\n\nUser feedback: art too repetitive. Complete redesign with per-card uniqueness:\n- Top banner: ATTACK (red) or SKILL (blue)\n- Central icon based on primary effect (sword/shield/crack/card/fist/spark/cross/arrow)\n- Secondary effect mini-icons (up to 3)\n- Cost badge and accent line\n- Unique seed-based decoration per card',

    '06-14.md': '# Dev Log - 6/14\n\n## Card Art Display Fix\n\nFixed card art clipping in battle/compendium/shop:\n- CardView: height 250->280, art min height 96->110, margins tightened\n- RewardCardOptionView TSCN: stretch_mode 6->5 (KeepAspectCovered->Centered)\n- Art texture sizing unified across all card display contexts',

    '06-15.md': '# Dev Log - 6/15\n\n## Character & Enemy Art\n\nCreated 29 unique enemy SVGs with transparent backgrounds:\n- Each enemy type has distinct visual design matching its description\n- Bosses: High Priest (crown+staff), Master Thief (mask+cape+knives), Corrupted Heart (pulsating organ+vessels+tentacles)\n- Merchant: hat + wares\n\nCreated 3 player battle portraits:\n- Iron Vanguard: heavy armor + greatsword\n- Phantom Dancer: hood + twin daggers\n- Storm Mage: robes + staff + floating orb',

    '06-16.md': '# Dev Log - 6/16\n\n## Transparent Character Backgrounds\n\nEliminated all backgrounds from enemies and player:\n- EnemyCardView: Theme=null, PortraitBg transparent in CacheNodes()\n- Button states: all StyleBoxEmpty overrides\n- Selected state: no border/shadow (hover-only glow)\n- PlayerCardView.Configure(): PortraitBg + PortraitGlow -> Color(0,0,0,0)\n- PlayerPanel: StyleBoxEmpty override\n- EnemyCardView TSCN: PortraitBg min height 78->140px',

    '06-17.md': '# Dev Log - 6/17\n\n## Enemy Sizing\n\nDramatically increased enemy display size:\n- Single enemy base scale: 2.50x (~0.95x player visually)\n- Boss bonus: +0.70~0.80 (Corrupted Heart 3.30x, towering over player)\n- Elite bonus: +0.25~0.50\n- Merchant bonus: +0.50\n- Special monsters: +0.20~0.35 (Ancient Automaton, Brute, etc.)',

    '06-18.md': '# Dev Log - 6/18\n\n## Relic Effects - Combat (Part 1)\n\nImplemented 27 missing combat relic effects:\n\nTurn Start: arcane_battery, philosopher_stone, cursed_key, coffee_dripper, dawn_totem, bottled_water, warlord_crown, twisted_funnel, bandolier, astrolabe\n\nCost Reduction: dusty_scroll, spellbook, overclock_core, shadow_step\n\nOn Card Played: echo_coin, shuriken, storm_feather, bird_faced_urn, cracked_orb, cracked_core, pen_nib, wrist_blade, glass_meteor, hourglass, frozen_lens, poison_vial, blood_chalice, necronomicon, twin_blade_badge\n\nCard play tracking fields added: _cardsPlayedThisTurn, _cardsPlayedThisTurnAttacks, _totalCardsPlayed, etc.',

    '06-19.md': '# Dev Log - 6/19\n\n## Relic Effects - Combat (Part 2) & Non-Combat\n\nDamage/Exhaust/End-of-Turn: tungsten_rod, prayer_beads, thorn_mail, self_repair_kit, ember_chisel, dead_branch, void_hourglass, warding_bell, orichalcum, ice_cream, pocket_watch\n\nCombat Start: jade_cicada (+6 block when HP<50%), philosopher_stone (enemies +1 str)\n\nPost-Battle: cinder_tea, black_blood, lucky_coin (in GameState.ResolveBattleVictory)\n\nNon-Combat: preserved_meat (rest +20%), dream_catcher (rest auto-upgrade), membership_card (shop -20%), soul_compass (elite +1 relic), mango (+15 max HP on pickup), molten_egg (auto-upgrade attacks), toxic_egg (auto-upgrade skills)\n\nTotal: all 63 relics implemented.',

    '06-20.md': '# Dev Log - 6/20\n\n## Battle Backgrounds\n\nCreated procedural battle backgrounds using Control._Draw():\n- 15 backgrounds (3 acts x 5 variants)\n- Act 1: deep red, stone pillars, torches, ritual circles\n- Act 2: dark purple, market stalls, lanterns, coin arcs\n- Act 3: dark gold, columns, stained glass, dust motes\n\nOld ColorRect backgrounds set to Visible=false.\nBattleBgControl anchors to FullRect, inserted at arena child index 0.\nElement opacity tuned through multiple iterations.\n\n## Gold Display Fix\n\nui.map.gold format string was missing {0} placeholder - gold amount never rendered.',

    '06-21.md': '# Dev Log - 6/21\n\n## Monthly Summary\n\n### Content Created\n- 77 cards (41 original + 36 new)\n- 29 enemy archetypes with unique AI and SVG art\n- 63 relics with full effect implementation\n- 10 potions with bilingual support\n- 12 random events\n- 24 achievements with popup system\n- 705 bilingual localization keys\n- Procedural BGM (10 scenes) + 24 SFX\n- 77 card SVGs + 29 enemy + 3 character + 63 relic + 10 potion + 15 background SVGs\n\n### Key Bugs Fixed\n- JSON comment objects crashing parser\n- EncounterBuilder spawning too many enemies\n- Language toggle not working\n- Mixed Chinese/English display throughout UI\n- Card art clipping in battle/compendium/shop\n- Character/enemy backgrounds not transparent\n- Gold count not displaying\n- Settings/resolution/volume/FPS counter\n\n### Code Changes\n~30 files modified across all layers (Data, Systems, Scenes, UI, Autoload).',
}

for fname, content in logs.items():
    path = os.path.join('Docs/DevLogs', fname)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

print(f'Written {len(logs)} dev logs')
