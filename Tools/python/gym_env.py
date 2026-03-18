from __future__ import annotations

from typing import Any, Dict, Optional, Tuple

from slay_env import SlayHsEnv, StepResult

try:
    import gymnasium as gym
    from gymnasium import spaces
except ImportError as exc:  # pragma: no cover - import guard
    raise ImportError(
        "gymnasium is not installed. Install it with your Python interpreter before using GymSlayHsEnv."
    ) from exc


class GymSlayHsEnv(gym.Env):
    metadata = {"render_modes": []}

    def __init__(self, battle_test: bool = False) -> None:
        super().__init__()
        self.core_env = SlayHsEnv(battle_test=battle_test)
        self.observation_space = spaces.Dict(
            {
                "stateVersion": spaces.Box(low=0, high=2**31 - 1, shape=(), dtype=int),
            }
        )
        self.action_space = spaces.Dict(
            {
                "kind": spaces.Text(max_length=64),
                "handIndex": spaces.Box(low=-1, high=100, shape=(), dtype=int),
                "targetEnemyIndex": spaces.Box(low=-1, high=100, shape=(), dtype=int),
                "column": spaces.Box(low=-1, high=100, shape=(), dtype=int),
                "optionIndex": spaces.Box(low=-1, high=100, shape=(), dtype=int),
            }
        )

    def reset(
        self,
        *,
        seed: Optional[int] = None,
        options: Optional[Dict[str, Any]] = None,
    ) -> Tuple[Dict[str, Any], Dict[str, Any]]:
        super().reset(seed=seed)
        return self.core_env.reset()

    def step(self, action: Dict[str, Any]) -> Tuple[Dict[str, Any], float, bool, bool, Dict[str, Any]]:
        result: StepResult = self.core_env.step(action)
        return result.observation, result.reward, result.terminated, result.truncated, result.info

    def sample_action(self) -> Optional[Dict[str, Any]]:
        return self.core_env.sample_legal_action()
