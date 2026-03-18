from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Dict, Optional, Tuple

from game_bridge import GameBridgeClient


@dataclass
class StepResult:
    observation: Dict[str, Any]
    reward: float
    terminated: bool
    truncated: bool
    info: Dict[str, Any]


class SlayHsEnv:
    """
    A lightweight gym-style wrapper around the in-game TCP bridge.

    It intentionally does not depend on gymnasium so it stays zero-dependency.
    """

    def __init__(self, client: Optional[GameBridgeClient] = None, battle_test: bool = False) -> None:
        self.client = client or GameBridgeClient()
        self.battle_test = battle_test
        self._last_snapshot: Optional[Dict[str, Any]] = None

    def reset(self) -> Tuple[Dict[str, Any], Dict[str, Any]]:
        result = self.client.execute_action("start_battle_test_run" if self.battle_test else "start_new_run")
        self._ensure_ok(result)
        snapshot = self._snapshot_from_result(result)
        self._last_snapshot = snapshot
        return snapshot, self._build_info(snapshot)

    def step(self, action: Dict[str, Any]) -> StepResult:
        if not action.get("kind"):
            raise ValueError("Action must include a non-empty 'kind'.")

        expected_state_version = None
        if self._last_snapshot is not None:
            expected_state_version = self._last_snapshot.get("stateVersion")

        action_payload = dict(action)
        kind = action_payload.pop("kind")
        result = self.client.execute_action(kind, expected_state_version=expected_state_version, **action_payload)
        self._ensure_ok(result)

        snapshot = self._snapshot_from_result(result)
        previous = self._last_snapshot
        self._last_snapshot = snapshot

        reward = self._compute_reward(previous, snapshot)
        terminated = self._is_terminal(snapshot)
        info = self._build_info(snapshot)
        return StepResult(
            observation=snapshot,
            reward=reward,
            terminated=terminated,
            truncated=False,
            info=info,
        )

    def sample_legal_action(self) -> Optional[Dict[str, Any]]:
        snapshot = self._last_snapshot or self.client.get_snapshot().get("snapshot")
        if not snapshot:
            return None

        legal_actions = snapshot.get("legalActions", [])
        if not legal_actions:
            return None

        action = legal_actions[0]
        payload = {"kind": action["kind"]}
        parameters = action.get("parameters") or {}

        if payload["kind"] == "play_card":
            payload["handIndex"] = parameters.get("handIndex")
            target_indices = parameters.get("targetEnemyIndices") or []
            if target_indices:
                payload["targetEnemyIndex"] = target_indices[0]
        elif payload["kind"] == "choose_map_node":
            payload["column"] = parameters.get("column")
        elif payload["kind"] == "choose_reward_type":
            payload["rewardType"] = parameters.get("rewardType")
        elif payload["kind"] == "choose_reward_card":
            payload["optionIndex"] = parameters.get("optionIndex")
        elif payload["kind"] == "choose_event_option":
            payload["optionIndex"] = parameters.get("optionIndex")

        return payload

    def _ensure_ok(self, result: Dict[str, Any]) -> None:
        if result.get("ok"):
            return
        raise RuntimeError(result.get("message", "Unknown bridge error."))

    def _snapshot_from_result(self, result: Dict[str, Any]) -> Dict[str, Any]:
        snapshot = result.get("snapshot")
        if not snapshot:
            raise RuntimeError("Bridge response did not include a snapshot.")
        return snapshot

    def _compute_reward(self, previous: Optional[Dict[str, Any]], current: Dict[str, Any]) -> float:
        if previous is None:
            return 0.0

        reward = 0.0
        previous_run = previous.get("run") or {}
        current_run = current.get("run") or {}
        reward += float(current_run.get("battlesWon", 0) - previous_run.get("battlesWon", 0)) * 10.0
        reward += float(current_run.get("floor", 0) - previous_run.get("floor", 0)) * 1.0
        reward += float(current_run.get("playerHp", 0) - previous_run.get("playerHp", 0)) * 0.1

        previous_battle = previous.get("battle") or {}
        current_battle = current.get("battle") or {}
        prev_alive = sum(1 for enemy in previous_battle.get("enemies", []) if enemy.get("isAlive"))
        curr_alive = sum(1 for enemy in current_battle.get("enemies", []) if enemy.get("isAlive"))
        reward += float(prev_alive - curr_alive) * 3.0
        return reward

    def _is_terminal(self, snapshot: Dict[str, Any]) -> bool:
        scene_type = snapshot.get("sceneType")
        run = snapshot.get("run") or {}
        if run.get("playerHp", 1) <= 0:
            return True
        return scene_type == "main_menu" and self._last_snapshot is not None

    def _build_info(self, snapshot: Dict[str, Any]) -> Dict[str, Any]:
        return {
            "scene_type": snapshot.get("sceneType"),
            "state_version": snapshot.get("stateVersion"),
            "legal_actions": snapshot.get("legalActions", []),
        }
