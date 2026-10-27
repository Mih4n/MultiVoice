import queue


class Replies:
    """Tell from the voice server: answers to /room and /voiceban, delivered on the main thread."""

    def __init__(self, server) -> None:
        self.server = server
        self.incoming: queue.Queue[list] = queue.Queue()  # hub thread -> main thread

    # hub thread
    def receive(self, args: list) -> None:
        self.incoming.put(args)

    # main thread
    def tick(self) -> None:
        while not self.incoming.empty():
            xuid, message = self.incoming.get_nowait()
            player = next((p for p in self.server.online_players if p.xuid == xuid), None)
            if player is not None:
                player.send_message(message)
