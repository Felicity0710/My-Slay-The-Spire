import json

with open('Data/Localization/en.json', 'r', encoding='utf-8-sig') as f:
    en = json.load(f)
with open('Data/Localization/zh_hans.json', 'r', encoding='utf-8-sig') as f:
    zh = json.load(f)

# ── Card translations for all 36 new cards ──
cards = {
    'card.sword_boomerang.name': ('Sword Boomerang', '回旋剑'),
    'card.sword_boomerang.description': ('Deal 3 damage three times.', '造成3点伤害，重复3次。'),
    'card.immolate.name': ('Immolate', '献祭之火'),
    'card.immolate.description': ('Deal 10 damage to all enemies. Exhaust.', '对所有敌人造成10点伤害。消耗。'),
    'card.hemoplague.name': ('Hemoplague', '血疫斩'),
    'card.hemoplague.description': ('Deal 10 damage. Heal 3 HP. Apply 1 Vulnerable to yourself.', '造成10点伤害。回复3点生命。对自己施加1层易伤。'),
    'card.executioner.name': ('Executioner', '处刑者'),
    'card.executioner.description': ('Deal 8 damage. Deals double damage if enemy has Vulnerable.', '造成8点伤害。若敌人有易伤，伤害翻倍。'),
    'card.demon_form.name': ('Demon Form', '恶魔形态'),
    'card.demon_form.description': ('Gain 2 Strength. At the start of each turn, gain 1 Strength.', '获得2点力量。每回合开始时获得1点力量。'),
    'card.offering.name': ('Offering', '祭品'),
    'card.offering.description': ('Gain 2 Energy. Draw 3 cards. Apply 1 Vulnerable to yourself. Exhaust.', '获得2点能量。抽3张牌。对自己施加1层易伤。消耗。'),
    'card.body_slam.name': ('Body Slam', '泰山压顶'),
    'card.body_slam.description': ('Deal damage equal to your current Block. Gain 4 Block.', '造成等同于你当前格挡值的伤害。获得4点格挡。'),
    'card.reckless_charge.name': ('Reckless Charge', '鲁莽冲锋'),
    'card.reckless_charge.description': ('Deal 15 damage. Apply 2 Vulnerable to yourself.', '造成15点伤害。对自己施加2层易伤。'),
    'card.juggernaut.name': ('Juggernaut', '主宰'),
    'card.juggernaut.description': ('Gain 8 Block. Whenever you gain Block this turn, deal 2 damage to a random enemy.', '获得8点格挡。本回合每次获得格挡时，对随机敌人造成2点伤害。'),
    'card.blood_for_blood.name': ('Blood for Blood', '以血还血'),
    'card.blood_for_blood.description': ('Deal 11 damage. Heal 3 HP for each enemy killed by this card this combat.', '造成11点伤害。本场战斗中此卡每击杀一名敌人，回复3点生命。'),
    'card.iron_skin.name': ('Iron Skin', '铁皮术'),
    'card.iron_skin.description': ('Gain 20 Block. Exhaust.', '获得20点格挡。消耗。'),
    'card.dagger_spray.name': ('Dagger Spray', '匕首雨'),
    'card.dagger_spray.description': ('Deal 3 damage to all enemies twice.', '对所有敌人造成3点伤害，重复2次。'),
    'card.acrobatics.name': ('Acrobatics', '杂技'),
    'card.acrobatics.description': ('Draw 3 cards. Discard 1 card.', '抽3张牌。丢弃1张牌。'),
    'card.shadow_cloak.name': ('Shadow Cloak', '暗影斗篷'),
    'card.shadow_cloak.description': ('Gain 8 Block. Draw 1 card next turn.', '获得8点格挡。下回合额外抽1张牌。'),
    'card.skewer.name': ('Skewer', '穿刺'),
    'card.skewer.description': ('Deal 6 damage twice.', '造成6点伤害，重复2次。'),
    'card.mortal_strike.name': ('Mortal Strike', '致命一击'),
    'card.mortal_strike.description': ('Deal 10 damage. If enemy has Vulnerable, deal 4 additional damage.', '造成10点伤害。若敌人有易伤，额外造成4点伤害。'),
    'card.calculated_gamble.name': ('Calculated Gamble', '精密赌博'),
    'card.calculated_gamble.description': ('Discard all cards in hand. Draw that many cards. Exhaust.', '弃掉所有手牌，然后抽等量的牌。消耗。'),
    'card.thousand_cuts.name': ('Thousand Cuts', '千刀万剐'),
    'card.thousand_cuts.description': ('Whenever you play an Attack this turn, deal 1 damage to all enemies.', '本回合每打出一张攻击牌，对所有敌人造成1点伤害。'),
    'card.grand_finale.name': ('Grand Finale', '盛大终章'),
    'card.grand_finale.description': ('Can only be played if hand is empty. Deal 30 damage to all enemies.', '仅在手牌为空时可打出。对所有敌人造成30点伤害。'),
    'card.flash_knives.name': ('Flash Knives', '闪刀'),
    'card.flash_knives.description': ('Deal 3 damage. Draw 2 cards.', '造成3点伤害。抽2张牌。'),
    'card.deadly_poison.name': ('Deadly Poison', '致命毒药'),
    'card.deadly_poison.description': ('Apply 3 Vulnerable to an enemy. Draw 1 card.', '施加3层易伤。抽1张牌。'),
    'card.evasion.name': ('Evasion', '闪避'),
    'card.evasion.description': ('Gain 6 Block. Apply 1 Vulnerable to all enemies.', '获得6点格挡。对所有敌人施加1层易伤。'),
    'card.backflip.name': ('Backflip', '后空翻'),
    'card.backflip.description': ('Gain 3 Block. Draw 2 cards.', '获得3点格挡。抽2张牌。'),
    'card.venom_strike.name': ('Venom Strike', '毒液打击'),
    'card.venom_strike.description': ('Deal 5 damage. Apply 2 Vulnerable.', '造成5点伤害。施加2层易伤。'),
    'card.static_discharge.name': ('Static Discharge', '静电释放'),
    'card.static_discharge.description': ('Deal 5 damage. Gain 1 Energy.', '造成5点伤害。获得1点能量。'),
    'card.frost_bolt.name': ('Frost Bolt', '霜冻箭'),
    'card.frost_bolt.description': ('Deal 5 damage. Gain 4 Block.', '造成5点伤害。获得4点格挡。'),
    'card.firestorm.name': ('Firestorm', '烈焰风暴'),
    'card.firestorm.description': ('Deal 18 damage to all enemies.', '对所有敌人造成18点伤害。'),
    'card.thunder_strike.name': ('Thunder Strike', '雷霆一击'),
    'card.thunder_strike.description': ('Deal 7 damage. Gains +2 damage for each card played this turn.', '造成7点伤害。本回合每打出一张牌，伤害+2。'),
    'card.mana_storm.name': ('Mana Storm', '法力风暴'),
    'card.mana_storm.description': ('Gain 1 Energy. Draw 3 cards.', '获得1点能量。抽3张牌。'),
    'card.echo_form.name': ('Echo Form', '回声形态'),
    'card.echo_form.description': ('The first card you play each turn is played twice.', '每回合第一张打出的牌打出两次。'),
    'card.apocalypse.name': ('Apocalypse', '天启'),
    'card.apocalypse.description': ('Deal 22 damage to all enemies. Exhaust.', '对所有敌人造成22点伤害。消耗。'),
    'card.time_warp.name': ('Time Warp', '时间扭曲'),
    'card.time_warp.description': ('Gain 2 Energy this turn. Next turn lose 1 Energy. Exhaust.', '本回合获得2点能量。下回合能量-1。消耗。'),
    'card.crystal_shield.name': ('Crystal Shield', '水晶护盾'),
    'card.crystal_shield.description': ('Gain 8 Block. Next turn, gain 1 Energy.', '获得8点格挡。下回合获得1点能量。'),
    'card.arcane_surge.name': ('Arcane Surge', '奥术涌动'),
    'card.arcane_surge.description': ('Gain 2 Strength. Draw 2 cards. Exhaust.', '获得2点力量。抽2张牌。消耗。'),
    'card.lightning_cascade.name': ('Lightning Cascade', '闪电瀑布'),
    'card.lightning_cascade.description': ('Deal 2 damage to all enemies four times. Draw 1 card.', '对所有敌人造成2点伤害，重复4次。抽1张牌。'),
    'card.energy_shield.name': ('Energy Shield', '能量护盾'),
    'card.energy_shield.description': ('Gain 5 Block. Gain 1 Energy.', '获得5点格挡。获得1点能量。'),
}

for key, (en_text, zh_text) in cards.items():
    en[key] = en_text
    zh[key] = zh_text

# ── Map node labels ──
zh['map.node.normal'] = '普通战斗'
zh['map.node.elite'] = '精英战斗'
zh['map.node.event'] = '事件'
zh['map.node.rest'] = '篝火'
zh['map.node.shop'] = '商店'
zh['map.node.unknown'] = '未知'

# ── UI keys ──
ui = {
    'ui.intro.title': ('Act {0} Intro', '第{0}章'),
    'ui.intro.hp': ('HP {0}/{1}', '生命 {0}/{1}'),
    'ui.intro.continue': ('Continue', '继续前进'),
    'ui.intro.title_act1': ('Act I — The Forsaken Cathedral', '第一章 — 被遗忘的大圣堂'),
    'ui.intro.title_act2': ('Act II — The Shadow Bazaar', '第二章 — 暗影市集'),
    'ui.intro.title_act3': ('Act III — The Ancient Sanctum', '第三章 — 远古圣殿'),
    'ui.intro.act1': (
        'You descend into the depths beneath the forsaken cathedral. '
        'Whispers of a dark cult echo through the stone corridors. '
        'The air is thick with incense and decay.\n\n'
        'The High Priest awaits somewhere below, shielded by his fanatical disciples. '
        'They will not negotiate. They will not surrender.\n\n'
        'Steel yourself, adventurer. The dungeon will not yield its secrets easily.',
        '你踏入被遗忘大圣堂的地下深处。黑暗教团的低语在石廊中回荡，'
        '空气中弥漫着熏香与腐臭。\n\n'
        '大祭司在深处等待，狂热的信徒层层守护。'
        '他们不会谈判，不会投降。\n\n'
        '冒险者，做好准备。这座地牢不会轻易交出它的秘密。'),
    'ui.intro.act2': (
        'Beneath the ruined outpost, a hidden city thrives — '
        'the Shadow Bazaar. Thieves, slavers, and black-market dealers '
        'rule this lawless underworld. Gold talks here, and blood talks louder.\n\n'
        'The Master Thief Rane watches from the shadows, her blade '
        'ready to claim whatever you value most. Trust no one.',
        '废墟哨站之下，一座隐秘的城市蓬勃发展——暗影市集。'
        '盗贼、奴隶贩子和黑市商人统治着这个无法地带。'
        '在这里，金钱能说话，而鲜血说得更大声。\n\n'
        '影贼大师雷恩在暗处注视着一切，她的刀刃随时准备夺走你最珍贵的东西。不要相信任何人。'),
    'ui.intro.act3': (
        'At the bottom of the abyss, the Ancient Sanctum reveals itself. '
        'Once a holy temple, now twisted beyond recognition by the corruption '
        'that seeps from its core.\n\n'
        'Guardians of living stone, dragons warped by dark magic — all stand '
        'between you and the source: the Corrupted Heart.\n\n'
        'This is it. The final battle. Destroy the Heart, or become part of it forever.',
        '深渊之底，远古圣殿显现真容。这里曾是一座神圣的殿堂，'
        '如今却被核心渗出的腐化扭曲得面目全非。\n\n'
        '活石守卫、被黑暗魔法扭曲的巨龙——它们挡在你和源头之间：腐化之心。\n\n'
        '就是这里了。最终之战。消灭腐化之心，或者永远成为它的一部分。'),
    'ui.victory.title': ('Victory', '胜利'),
    'ui.victory.subtitle': ('You have cleared all {0} acts.', '你已通关全部{0}个章节。'),
    'ui.victory.summary': (
        'Floors climbed: {0}    Battles won: {1}    Final HP: {2}/{3}    Gold: {4}',
        '攀登楼层：{0}    战斗胜利：{1}    最终生命：{2}/{3}    金币：{4}'),
    'ui.victory.menu_button': ('Back to Main Menu', '返回主菜单'),
    'ui.achievement.unlocked': ('Achievement Unlocked!', '成就解锁！'),
}
for key, (en_text, zh_text) in ui.items():
    en[key] = en_text
    zh[key] = zh_text

# ── Character presets ──
zh['deck_preset.iron_vanguard.name'] = '铁卫先锋'
zh['deck_preset.iron_vanguard.description'] = '力量型战士。叠加力量，用重击粉碎敌人。以自身为代价换取巨大力量。'
zh['deck_preset.phantom_dancer.name'] = '幻影舞者'
zh['deck_preset.phantom_dancer.description'] = '敏捷刺客。多段连击，叠易伤爆发，过牌弃牌循环，一击致命。'
zh['deck_preset.storm_mage.name'] = '风暴法师'
zh['deck_preset.storm_mage.description'] = '奥术施法者。操控能量，释放毁灭性的多段法术，编织无限连击。'

# ── Event translations ──
events = {
    'event.brewer.title': ('Mysterious Brew', '神秘酿造'),
    'event.brewer.description': ('A bubbling cauldron sits unattended.', '一口冒泡的大锅无人看管。'),
    'event.healer.title': ('Wandering Healer', '流浪治疗师'),
    'event.healer.description': ('A cloaked figure offers you a choice.', '一位披着斗篷的人给你选择。'),
    'event.altar.title': ('Card Altar', '卡牌祭坛'),
    'event.altar.description': ('An ancient altar accepts offerings of cards.', '一座古老的祭坛接受卡牌作为祭品。'),
    'event.chest.title': ('Mysterious Chest', '神秘宝箱'),
    'event.chest.description': ('A heavy iron chest sits in the corner.', '角落里放着一口沉重的铁箱。'),
    'event.forge.title': ('Dark Forge', '黑暗熔炉'),
    'event.forge.description': ('The anvil glows with infernal heat.', '铁砧散发着地狱般的热量。'),
    'event.ritual.title': ('Blood Ritual', '血之仪式'),
    'event.ritual.description': ('A circle of dried blood marks the floor.', '地板上画着一个干涸的血迹圆圈。'),
    'event.gambling.title': ('Gambling Den', '赌场'),
    'event.gambling.description': ('Dice rattle on a velvet table.', '骰子在绒布桌面上响动。'),
    'event.ambush.title': ('Bandit Ambush!', '强盗伏击！'),
    'event.ambush.description': ('Three cutthroats leap from the shadows!', '三个亡命徒从阴影中跳出！'),
    'event.slime_pit.title': ('Slime Pit', '史莱姆坑'),
    'event.slime_pit.description': ('The floor gives way to a nest of plague rats.', '地板塌陷，露出一个瘟疫鼠的巢穴。'),
    'event.cursed_idol.title': ('Cursed Idol', '诅咒神像'),
    'event.cursed_idol.description': ('An obsidian statue oozes dark power.', '一尊黑曜石雕像渗出黑暗力量。'),
}
for key, (en_text, zh_text) in events.items():
    en[key] = en_text
    zh[key] = zh_text

# ── Save ──
with open('Data/Localization/en.json', 'w', encoding='utf-8') as f:
    json.dump(en, f, ensure_ascii=False, indent=4)
with open('Data/Localization/zh_hans.json', 'w', encoding='utf-8') as f:
    json.dump(zh, f, ensure_ascii=False, indent=4)

print(f'en.json: {len(en)} keys')
print(f'zh_hans.json: {len(zh)} keys')
print('Done!')
