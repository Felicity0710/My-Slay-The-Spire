from __future__ import annotations

from typing import Any, Dict, List

MAX_ENEMIES = 3
MAX_HAND = 10


def encode_battle_observation(snapshot: Dict[str, Any]) -> List[float]:
    battle = snapshot.get("battle") or {}
    run = snapshot.get("run") or {}
    player = battle.get("player") or {}

    vector: List[float] = []
    vector.extend(
        [
            _norm(run.get("playerHp", 0), 100.0),
            _norm(run.get("maxHp", 0), 100.0),
            _norm(run.get("floor", 0), 100.0),
            _norm(battle.get("turn", 0), 50.0),
            _norm(battle.get("energy", 0), 10.0),
            _norm(player.get("block", 0), 100.0),
            _norm(player.get("strength", 0), 20.0),
            _norm(player.get("vulnerable", 0), 10.0),
            _norm(battle.get("drawPileCount", 0), 100.0),
            _norm(battle.get("discardPileCount", 0), 100.0),
        ]
    )

    enemies = battle.get("enemies") or []
    for enemy_index in range(MAX_ENEMIES):
        enemy = enemies[enemy_index] if enemy_index < len(enemies) else {}
        vector.extend(encode_enemy_features(enemy))

    hand = battle.get("hand") or []
    for hand_index in range(MAX_HAND):
        card = hand[hand_index] if hand_index < len(hand) else {}
        vector.extend(encode_card_features(card))

    return vector


def encode_enemy_features(enemy: Dict[str, Any]) -> List[float]:
    if not enemy:
        return [0.0] * 9

    intent_type = str(enemy.get("intentType", "")).lower()
    return [
        1.0 if enemy.get("isAlive") else 0.0,
        _norm(enemy.get("hp", 0), 200.0),
        _norm(enemy.get("maxHp", 0), 200.0),
        _norm(enemy.get("block", 0), 100.0),
        _norm(enemy.get("strength", 0), 20.0),
        _norm(enemy.get("vulnerable", 0), 10.0),
        1.0 if "attack" in intent_type else 0.0,
        1.0 if "defend" in intent_type else 0.0,
        _norm(enemy.get("intentValue", 0), 50.0),
    ]


def encode_card_features(card: Dict[str, Any]) -> List[float]:
    if not card:
        return [0.0] * 10

    description = str(card.get("description", ""))
    damage = _extract_best_number(description)
    return [
        1.0,
        _norm(card.get("cost", 0), 10.0),
        1.0 if card.get("requiresEnemyTarget") else 0.0,
        1.0 if card.get("isPlayable") else 0.0,
        _norm(damage, 50.0),
        1.0 if _has_text(description, "block", "格挡") else 0.0,
        1.0 if _has_text(description, "vulnerable", "易伤") else 0.0,
        1.0 if _has_text(description, "draw", "抽") else 0.0,
        1.0 if _has_text(description, "strength", "力量") else 0.0,
        1.0 if _has_text(description, "energy", "能量") else 0.0,
    ]


def hash_battle_observation(encoded: List[float]) -> str:
    quantized = [str(int(value * 20.0)) for value in encoded]
    return "|".join(quantized)


def _norm(value: Any, scale: float) -> float:
    try:
        numeric = float(value)
    except (TypeError, ValueError):
        numeric = 0.0
    return max(0.0, min(numeric / scale, 1.0))


def _extract_best_number(text: str) -> int:
    best = 0
    current: List[str] = []
    for ch in text:
        if ch.isdigit():
            current.append(ch)
            continue

        if current:
            best = max(best, int("".join(current)))
            current = []

    if current:
        best = max(best, int("".join(current)))
    return best


def _has_text(text: str, *keywords: str) -> bool:
    lowered = text.lower()
    return any(keyword.lower() in lowered for keyword in keywords)
