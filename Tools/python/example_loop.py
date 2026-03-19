from slay_env import SlayHsEnv


def main() -> int:
    env = SlayHsEnv()
    observation, info = env.reset()
    print("reset:", info["scene_type"], info["state_version"])

    for step in range(50):
        action = env.sample_legal_action()
        if action is None:
            print("no legal action, stop")
            return 0

        result = env.step(action)
        print(
            f"step={step} action={action['kind']} reward={result.reward:.2f} "
            f"scene={result.info['scene_type']} version={result.info['state_version']}"
        )
        if result.terminated or result.truncated:
            print("episode finished")
            return 0

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
