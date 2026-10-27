import math

from endstone_multivoice.sync.state_diff import state_diff

# below this Y with blocks overhead counts as a cave
CAVE_MAX_Y = 60


def held_and_worn(inv) -> list[str]:
    """Item type ids the voice server matches against Voice:Items."""
    stacks = [inv.item_in_main_hand, inv.item_in_off_hand, inv.helmet, inv.chestplate, inv.leggings, inv.boots]
    return sorted({s.type.id for s in stacks if s is not None})


class PositionSync:
    """Sends the server only the player states that changed."""

    def __init__(self, server, send, muted: set[str]) -> None:
        self.server = server
        self.send = send
        self.muted = muted
        self.sent: dict[str, dict] = {}  # xuid -> state the server already has

    def reset(self) -> None:
        self.sent = {}  # the server clears players on connect, so send everyone again

    # main thread: game API is only touched here
    def tick(self) -> None:
        current = {}
        for p in self.server.online_players:
            loc = p.location
            dim = p.dimension
            current[p.xuid] = {
                "xuid": p.xuid,
                "name": p.name,
                "x": loc.x, "y": loc.y, "z": loc.z,
                "yaw": loc.yaw, "pitch": loc.pitch,
                "dimensionId": dim.name,
                "inWater": p.is_in_water,
                "inCave": loc.y < CAVE_MAX_Y and dim.get_highest_block_y_at(math.floor(loc.x), math.floor(loc.z)) > loc.y,
                "muted": p.name in self.muted,
                "items": held_and_worn(p.inventory),
                "admin": p.is_op,  # the server checks this before /voiceban and site bans
            }
        changed, left = state_diff(self.sent, current)
        if (changed or left) and self.send("Players", changed, left):
            self.sent = current
