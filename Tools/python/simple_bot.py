from __future__ import annotations

import argparse
from typing import Any, Dict, Optional

from game_bridge import GameBridgeClient


def choose_action(snapshot: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    scene_type = snapshot.get("sceneType")
    if scene_type == "main_menu":
        return {"kind": "start_new_run"}
    if scene_type == "map":
        return choose_map_action(snapshot.get("map") or {})
    if scene_type == "battle":
        return choose_battle_action(snapshot.get("battle") or {})
    if scene_type == "reward":
        return choose_reward_action(snapshot.get("reward") or {})
    if scene_type == "event":
        return {"kind": "choose_event_option", "optionIndex": 0}
    return choose_fallback_action(snapshot)


def choose_map_action(map_state: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    rows = map_state.get("rows") or []
    current_row = map_state.get("currentRow")
    for row in rows:
        if row.get("rowIndex") != current_row:
            continue

        candidates = [node for node in row.get("nodes", []) if node.get("canSelect")]
        if not candidates:
            return None

        priority = {
            "NormalBattle": 0,
            "EliteBattle": 1,
            "Event": 2,
            "Rest": 3,
            "Shop": 4,
        }
        node = sorted(candidates, key=lambda item: (priority.get(item.get("nodeType"), 99), item.get("column", 99)))[0]
        return {"kind": "choose_map_node", "column": node.get("column")}

    return None


def choose_battle_action(battle: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    if battle.get("battleEnded"):
        return None

    hand = [card for card in battle.get("hand", []) if card.get("isPlayable")]
    enemies = [enemy for enemy in battle.get("enemies", []) if enemy.get("isAlive")]
    if not hand:
        return {"kind": "end_turn"}

    lethal = try_find_lethal(hand, enemies)
    if lethal is not None:
        return lethal

    debuff = next((card for card in hand if has_any(card, "Vulnerable", "易伤")), None)
    if debuff is not None and enemies:
        target = sorted(enemies, key=lambda enemy: enemy.get("hp", 0) + enemy.get("block", 0), reverse=True)[0]
        return {"kind": "play_card", "handIndex": debuff.get("handIndex"), "targetEnemyIndex": target.get("enemyIndex")}

    attack = next((card for card in sorted(hand, key=estimate_damage, reverse=True) if estimate_damage(card) > 0), None)
    if attack is not None:
        if attack.get("requiresEnemyTarget") and enemies:
            target = sorted(enemies, key=lambda enemy: enemy.get("hp", 0) + enemy.get("block", 0))[0]
            return {"kind": "play_card", "handIndex": attack.get("handIndex"), "targetEnemyIndex": target.get("enemyIndex")}
        return {"kind": "play_card", "handIndex": attack.get("handIndex")}

    defense = next((card for card in hand if has_any(card, "Block", "格挡")), None)
    if defense is not None:
        return {"kind": "play_card", "handIndex": defense.get("handIndex")}

    return {"kind": "end_turn"}


def try_find_lethal(hand: list[Dict[str, Any]], enemies: list[Dict[str, Any]]) -> Optional[Dict[str, Any]]:
    for enemy in sorted(enemies, key=lambda item: item.get("hp", 0) + item.get("block", 0)):
        effective_hp = enemy.get("hp", 0) + enemy.get("block", 0)
        for card in sorted(hand, key=estimate_damage, reverse=True):
            if not card.get("requiresEnemyTarget"):
                continue
            if estimate_damage(card) >= effective_hp:
                return {"kind": "play_card", "handIndex": card.get("handIndex"), "targetEnemyIndex": enemy.get("enemyIndex")}
    return None


def choose_reward_action(reward: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    if reward.get("mode") == "card_pack":
        options = reward.get("cardOptions") or []
        if not options:
            return {"kind": "skip_reward"}

        best = sorted(options, key=score_reward_option, reverse=True)[0]
        return {"kind": "choose_reward_card", "optionIndex": best.get("optionIndex")}

    reward_types = reward.get("rewardTypes") or []
    for reward_type in ("relic", "card_pack", "potion", "random"):
        if reward_type in reward_types:
            return {"kind": "choose_reward_type", "rewardType": reward_type}

    return {"kind": "skip_reward"}


def choose_fallback_action(snapshot: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    legal_actions = snapshot.get("legalActions") or []
    if not legal_actions:
        return None

    first = legal_actions[0]
    action = {"kind": first.get("kind")}
    parameters = first.get("parameters") or {}

    for key in ("column", "handIndex", "cardId", "rewardType", "optionIndex"):
        if key in parameters:
            action[key] = parameters[key]

    target_enemy_indices = parameters.get("targetEnemyIndices") or []
    if target_enemy_indices:
        action["targetEnemyIndex"] = target_enemy_indices[0]

    return action


def score_reward_option(option: Dict[str, Any]) -> int:
    text = f"{option.get('name', '')} {option.get('description', '')}"
    score = 0
    if has_text(text, "Strength", "力量"):
        score += 40
    if has_text(text, "Energy", "能量"):
        score += 35
    if has_text(text, "Vulnerable", "易伤"):
        score += 20
    score += sum(1 for ch in text if ch.isdigit())
    return score


def estimate_damage(card: Dict[str, Any]) -> int:
    text = card.get("description", "")
    best = 0
    current = []
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


def has_any(card: Dict[str, Any], *keywords: str) -> bool:
    text = card.get("description", "")
    return has_text(text, *keywords)


def has_text(text: str, *keywords: str) -> bool:
    return any(keyword.lower() in text.lower() for keyword in keywords)


def main() -> int:
    parser = argparse.ArgumentParser(description="Simple rule-based bot for Slay the HS.")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=47077)
    parser.add_argument("--steps", type=int, default=500)
    args = parser.parse_args()

    client = GameBridgeClient(host=args.host, port=args.port)
    result = client.get_snapshot()
    snapshot = result.get("snapshot")
    if not snapshot:
        raise RuntimeError("The game bridge did not return a snapshot.")

    for step in range(args.steps):
        log_snapshot(step, snapshot)
        action = choose_action(snapshot)
        if action is None:
            print(f"bot stop: no action for scene {snapshot.get('sceneType')}")
            log_legal_actions(snapshot)
            return 0

        print(f"bot action: {format_action(action)}")
        response = client.execute_action(
            action["kind"],
            expected_state_version=snapshot.get("stateVersion"),
            **{k: v for k, v in action.items() if k != "kind"},
        )
        if not response.get("ok"):
            if "State version mismatch" in str(response.get("message")) and response.get("snapshot"):
                print("bot info: version mismatch, refresh snapshot and continue")
                snapshot = response["snapshot"]
                continue
            log_legal_actions(snapshot)
            raise RuntimeError(f"Action failed: {response.get('message')}")

        snapshot = response.get("snapshot")
        if not snapshot:
            raise RuntimeError("Missing snapshot after action.")

    print("bot stop: reached step limit")
    return 0


def log_snapshot(step: int, snapshot: Dict[str, Any]) -> None:
    run = snapshot.get("run") or {}
    legal_actions = snapshot.get("legalActions") or []
    print(
        "bot snapshot:",
        f"step={step}",
        f"scene={snapshot.get('sceneType')}",
        f"version={snapshot.get('stateVersion')}",
        f"floor={run.get('floor')}",
        f"hp={run.get('playerHp')}/{run.get('maxHp')}",
        f"wins={run.get('battlesWon')}",
        f"legal={len(legal_actions)}",
    )

    scene_type = snapshot.get("sceneType")
    if scene_type == "battle":
        battle = snapshot.get("battle") or {}
        alive = sum(1 for enemy in battle.get("enemies", []) if enemy.get("isAlive"))
        print(
            "bot battle:",
            f"turn={battle.get('turn')}",
            f"energy={battle.get('energy')}",
            f"hand={len(battle.get('hand', []))}",
            f"enemiesAlive={alive}",
        )
    elif scene_type == "reward":
        reward = snapshot.get("reward") or {}
        print("bot reward:", f"mode={reward.get('mode')}", f"types={reward.get('rewardTypes')}")
    elif scene_type == "event":
        event_state = snapshot.get("event") or {}
        print("bot event:", f"id={event_state.get('eventId')}", f"title={event_state.get('title')}")
    elif scene_type == "map":
        map_state = snapshot.get("map") or {}
        print("bot map:", f"row={map_state.get('currentRow')}", f"column={map_state.get('currentColumn')}")


def log_legal_actions(snapshot: Dict[str, Any]) -> None:
    legal_actions = snapshot.get("legalActions") or []
    if not legal_actions:
        print("bot legal: none")
        return

    for action in legal_actions[:8]:
        print(f"bot legal: {action.get('kind')} ({action.get('label')})")


def format_action(action: Dict[str, Any]) -> str:
    parts = [str(action.get("kind"))]
    for key in ("handIndex", "cardId", "targetEnemyIndex", "column", "rewardType", "optionIndex", "eventOption"):
        if key in action and action[key] is not None:
            parts.append(f"{key}={action[key]}")
    return " ".join(parts)


if __name__ == "__main__":
    raise SystemExit(main())
