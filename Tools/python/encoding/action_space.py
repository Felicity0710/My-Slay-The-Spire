from __future__ import annotations

from typing import Any, Dict, List, Optional, Tuple

ACTION_END_TURN = 0
ACTION_PLAY_CARD_START = 1
ACTION_PLAY_CARD_COUNT = 30
ACTION_MAP_START = ACTION_PLAY_CARD_START + ACTION_PLAY_CARD_COUNT
ACTION_MAP_COUNT = 5
ACTION_REWARD_TYPE_START = ACTION_MAP_START + ACTION_MAP_COUNT
ACTION_REWARD_TYPE_COUNT = 4
ACTION_REWARD_CARD_START = ACTION_REWARD_TYPE_START + ACTION_REWARD_TYPE_COUNT
ACTION_REWARD_CARD_COUNT = 3
ACTION_SKIP_REWARD = ACTION_REWARD_CARD_START + ACTION_REWARD_CARD_COUNT
ACTION_EVENT_START = ACTION_SKIP_REWARD + 1
ACTION_EVENT_COUNT = 2
ACTION_START_NEW_RUN = ACTION_EVENT_START + ACTION_EVENT_COUNT
ACTION_START_BATTLE_TEST = ACTION_START_NEW_RUN + 1
ACTION_DIM = ACTION_START_BATTLE_TEST + 1

REWARD_TYPE_ORDER = ["relic", "card_pack", "potion", "random"]


def build_action_mask(snapshot: Dict[str, Any]) -> List[int]:
    mask = [0] * ACTION_DIM
    for legal in snapshot.get("legalActions", []):
        action_id = encode_legal_action(legal)
        if action_id is not None:
            mask[action_id] = 1
    return mask


def encode_action(action: Dict[str, Any]) -> Optional[int]:
    kind = action.get("kind")
    if kind == "end_turn":
        return ACTION_END_TURN
    if kind == "play_card":
        hand_index = int(action.get("handIndex", -1))
        target_enemy_index = int(action.get("targetEnemyIndex", -1))
        target_bucket = max(target_enemy_index, 0)
        if hand_index < 0 or hand_index >= 10 or target_bucket >= 3:
            return None
        return ACTION_PLAY_CARD_START + hand_index * 3 + target_bucket
    if kind == "choose_map_node":
        column = int(action.get("column", -1))
        if 0 <= column < ACTION_MAP_COUNT:
            return ACTION_MAP_START + column
        return None
    if kind == "choose_reward_type":
        reward_type = str(action.get("rewardType", "")).lower()
        if reward_type in REWARD_TYPE_ORDER:
            return ACTION_REWARD_TYPE_START + REWARD_TYPE_ORDER.index(reward_type)
        return None
    if kind == "choose_reward_card":
        option_index = int(action.get("optionIndex", -1))
        if 0 <= option_index < ACTION_REWARD_CARD_COUNT:
            return ACTION_REWARD_CARD_START + option_index
        return None
    if kind == "skip_reward":
        return ACTION_SKIP_REWARD
    if kind == "choose_event_option":
        option_index = int(action.get("optionIndex", -1))
        if 0 <= option_index < ACTION_EVENT_COUNT:
            return ACTION_EVENT_START + option_index
        return None
    if kind == "start_new_run":
        return ACTION_START_NEW_RUN
    if kind == "start_battle_test_run":
        return ACTION_START_BATTLE_TEST
    return None


def decode_action(action_id: int, snapshot: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    if action_id == ACTION_END_TURN:
        return {"kind": "end_turn"}
    if ACTION_PLAY_CARD_START <= action_id < ACTION_MAP_START:
        offset = action_id - ACTION_PLAY_CARD_START
        hand_index = offset // 3
        target_enemy_index = offset % 3
        battle = snapshot.get("battle") or {}
        hand = battle.get("hand") or []
        if hand_index >= len(hand):
            return None
        card = hand[hand_index]
        action = {"kind": "play_card", "handIndex": hand_index}
        if card.get("requiresEnemyTarget"):
            action["targetEnemyIndex"] = target_enemy_index
        return action
    if ACTION_MAP_START <= action_id < ACTION_REWARD_TYPE_START:
        return {"kind": "choose_map_node", "column": action_id - ACTION_MAP_START}
    if ACTION_REWARD_TYPE_START <= action_id < ACTION_REWARD_CARD_START:
        return {"kind": "choose_reward_type", "rewardType": REWARD_TYPE_ORDER[action_id - ACTION_REWARD_TYPE_START]}
    if ACTION_REWARD_CARD_START <= action_id < ACTION_SKIP_REWARD:
        return {"kind": "choose_reward_card", "optionIndex": action_id - ACTION_REWARD_CARD_START}
    if action_id == ACTION_SKIP_REWARD:
        return {"kind": "skip_reward"}
    if ACTION_EVENT_START <= action_id < ACTION_START_NEW_RUN:
        return {"kind": "choose_event_option", "optionIndex": action_id - ACTION_EVENT_START}
    if action_id == ACTION_START_NEW_RUN:
        return {"kind": "start_new_run"}
    if action_id == ACTION_START_BATTLE_TEST:
        return {"kind": "start_battle_test_run"}
    return None


def filter_legal_action_ids(snapshot: Dict[str, Any]) -> List[int]:
    return [index for index, allowed in enumerate(build_action_mask(snapshot)) if allowed]


def encode_legal_action(legal_action: Dict[str, Any]) -> Optional[int]:
    kind = legal_action.get("kind")
    parameters = legal_action.get("parameters") or {}
    if kind == "play_card":
        hand_index = parameters.get("handIndex")
        target_indices = parameters.get("targetEnemyIndices") or []
        if hand_index is None:
            return None
        if target_indices:
            return encode_action({"kind": kind, "handIndex": hand_index, "targetEnemyIndex": target_indices[0]})
        return encode_action({"kind": kind, "handIndex": hand_index, "targetEnemyIndex": 0})
    action = {"kind": kind}
    action.update(parameters)
    return encode_action(action)


def choose_first_legal_decoded(snapshot: Dict[str, Any]) -> Optional[Tuple[int, Dict[str, Any]]]:
    for action_id in filter_legal_action_ids(snapshot):
        action = decode_action(action_id, snapshot)
        if action is not None:
            return action_id, action
    return None
