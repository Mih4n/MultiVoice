def state_diff(sent: dict[str, dict], current: dict[str, dict]) -> tuple[list[dict], list[str]]:
    """Players whose state differs from what the server has, and xuids that left."""
    changed = [state for xuid, state in current.items() if sent.get(xuid) != state]
    left = [xuid for xuid in sent if xuid not in current]
    return changed, left
