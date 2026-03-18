from __future__ import annotations

from typing import Any, Dict, Optional


def compute_battle_only_reward(previous: Optional[Dict[str, Any]], current: Dict[str, Any]) -> float:
    if previous is None:
        return 0.0

    reward = 0.0
    prev_run = previous.get("run") or {}
    curr_run = current.get("run") or {}
    prev_battle = previous.get("battle") or {}
    curr_battle = current.get("battle") or {}

    reward += float(curr_run.get("battlesWon", 0) - prev_run.get("battlesWon", 0)) * 25.0
    reward += float(curr_run.get("floor", 0) - prev_run.get("floor", 0)) * 3.0
    reward += float(curr_run.get("playerHp", 0) - prev_run.get("playerHp", 0)) * 0.2

    prev_alive = sum(1 for enemy in prev_battle.get("enemies", []) if enemy.get("isAlive"))
    curr_alive = sum(1 for enemy in curr_battle.get("enemies", []) if enemy.get("isAlive"))
    reward += float(prev_alive - curr_alive) * 5.0

    if curr_run.get("playerHp", 1) <= 0:
        reward -= 30.0

    return reward
