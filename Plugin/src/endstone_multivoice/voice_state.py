import queue

from endstone.plugin import Service

OFF = "off"


class VoiceStates(Service):
    """Service for other plugins: each player's voice state, one of off, listening, mic, speaking."""

    def __init__(self, server) -> None:
        Service.__init__(self)
        self.server = server
        self.states: dict[str, str] = {}  # xuid -> state other than off
        self.callbacks = []
        self.incoming: queue.Queue[dict | None] = queue.Queue()  # hub thread -> main thread

    def get_state(self, player) -> str:
        return self.states.get(player.xuid, OFF)

    def on_change(self, callback) -> None:
        """callback(player, state) runs on the main thread."""
        self.callbacks.append(callback)

    # hub thread
    def receive(self, args: list) -> None:
        self.incoming.put(args[0])

    def reset(self) -> None:
        self.incoming.put(None)  # reconnected: the server pushes every state again, so forget ours

    # main thread
    def tick(self) -> None:
        while not self.incoming.empty():
            changes = self.incoming.get_nowait()
            if changes is None:
                changes = dict.fromkeys(self.states, OFF)
            for xuid, state in changes.items():
                self.set(xuid, state)

    def set(self, xuid: str, state: str) -> None:
        if self.states.get(xuid, OFF) == state:
            return
        if state == OFF:
            del self.states[xuid]
        else:
            self.states[xuid] = state
        player = next((p for p in self.server.online_players if p.xuid == xuid), None)
        if player is not None:  # a player who left has nobody to report to
            for callback in self.callbacks:
                callback(player, state)
