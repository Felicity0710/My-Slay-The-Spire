from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Dict, Optional

from encoding.action_space import build_action_mask, decode_action, filter_legal_action_ids, choose_first_legal_decoded
from encoding.observation import encode_battle_observation, hash_battle_observation
from simple_bot import choose_action as choose_rule_action
from training.nn_policy import TinyPolicyNetwork
from training.ppo_lite_policy import TinyActorCritic


def load_tabular_policy(path: str | Path) -> Dict:
    with Path(path).open("r", encoding="utf-8") as handle:
        return json.load(handle)


def load_nn_policy(path: str | Path) -> TinyPolicyNetwork:
    with Path(path).open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    return TinyPolicyNetwork.from_payload(payload)


def load_ppo_policy(path: str | Path) -> TinyActorCritic:
    with Path(path).open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    return TinyActorCritic.from_payload(payload)


def choose_tabular_action(policy: Dict, snapshot: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    scene_type = snapshot.get("sceneType")
    if scene_type != "battle":
        return choose_rule_action(snapshot)

    key = hash_battle_observation(encode_battle_observation(snapshot))
    action_id = policy["policy_table"].get(key, policy.get("default_action_id"))
    if action_id is None:
        return _fallback_action(snapshot)

    action_id = int(action_id)
    legal_ids = set(filter_legal_action_ids(snapshot))
    if action_id not in legal_ids:
        fallback = next(iter(legal_ids), None)
        if fallback is None:
            return _fallback_action(snapshot)
        action_id = fallback

    action = decode_action(action_id, snapshot)
    return action if action is not None else _fallback_action(snapshot)


def choose_nn_action(network: TinyPolicyNetwork, snapshot: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    scene_type = snapshot.get("sceneType")
    if scene_type != "battle":
        return choose_rule_action(snapshot)

    action_mask = build_action_mask(snapshot)
    action_id = network.predict_action(encode_battle_observation(snapshot), action_mask)
    if action_id is None:
        return _fallback_action(snapshot)

    action = decode_action(action_id, snapshot)
    return action if action is not None else _fallback_action(snapshot)


def choose_ppo_action(network: TinyActorCritic, snapshot: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    scene_type = snapshot.get("sceneType")
    if scene_type != "battle":
        return choose_rule_action(snapshot)

    action_mask = build_action_mask(snapshot)
    action_id = network.predict_action(encode_battle_observation(snapshot), action_mask)
    if action_id is None:
        return _fallback_action(snapshot)

    action = decode_action(action_id, snapshot)
    return action if action is not None else _fallback_action(snapshot)


def choose_policy_action(kind: str, snapshot: Dict[str, Any], *, tabular_policy: Dict | None = None, nn_policy: TinyPolicyNetwork | None = None, ppo_policy: TinyActorCritic | None = None) -> Optional[Dict[str, Any]]:
    normalized = kind.strip().lower()
    if normalized == "rule":
        return choose_rule_action(snapshot)
    if normalized == "tabular":
        if tabular_policy is None:
            raise ValueError("tabular_policy is required for tabular strategy")
        return choose_tabular_action(tabular_policy, snapshot)
    if normalized == "nn":
        if nn_policy is None:
            raise ValueError("nn_policy is required for nn strategy")
        return choose_nn_action(nn_policy, snapshot)
    if normalized == "ppo":
        if ppo_policy is None:
            raise ValueError("ppo_policy is required for ppo strategy")
        return choose_ppo_action(ppo_policy, snapshot)
    raise ValueError(f"Unsupported policy kind '{kind}'.")


def _fallback_action(snapshot: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    fallback = choose_first_legal_decoded(snapshot)
    return fallback[1] if fallback is not None else choose_rule_action(snapshot)
