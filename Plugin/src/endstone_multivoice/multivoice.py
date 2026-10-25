import math
import queue
import threading
import time

from endstone import Player
from endstone.command import Command, CommandSender
from endstone.form import ActionForm
from endstone.plugin import Plugin
from signalrcore.hub_connection_builder import HubConnectionBuilder

from endstone_multivoice.state_diff import state_diff

# below this Y with blocks overhead counts as a cave
CAVE_MAX_Y = 60


class MultiVoicePlugin(Plugin):
    api_version = "0.11"

    commands = {
        "mute": {
            "description": "Toggle your proximity voice microphone.",
            "usages": ["/mute"],
            "permissions": ["multivoice.command.mute"],
        }
    }

    permissions = {
        "multivoice.command.mute": {
            "description": "Allow players to mute themselves in voice chat.",
            "default": True,
        }
    }

    def on_enable(self) -> None:
        self.save_default_config()
        self.muted: set[str] = set()
        self.asks: queue.Queue[list] = queue.Queue()  # hub thread -> main thread
        self.hub = None
        self.sent: dict[str, dict] = {}  # xuid -> state the server already has
        self.connected = False
        self.running = True
        threading.Thread(target=self.connect_loop, daemon=True).start()
        self.server.scheduler.run_task(self, self.tick, delay=0, period=int(self.config["period_ticks"]))

    def on_disable(self) -> None:
        self.running = False
        if self.hub is not None:
            self.hub.stop()

    def on_command(self, sender: CommandSender, command: Command, args: list[str]) -> bool:
        if command.name == "mute":
            if not isinstance(sender, Player):
                sender.send_error_message("Only players can use /mute")
                return True
            if sender.name in self.muted:
                self.muted.discard(sender.name)
                sender.send_message("Микрофон включён")
            else:
                self.muted.add(sender.name)
                sender.send_message("Микрофон выключен")
        return True

    # main thread: game API is only touched here
    def tick(self) -> None:
        while not self.asks.empty():
            name, codes = self.asks.get_nowait()
            self.ask(name, codes)

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
            }
        changed, left = state_diff(self.sent, current)
        if (changed or left) and self.send("Players", changed, left):
            self.sent = current

    def ask(self, name: str, codes: list[int]) -> None:
        player = self.server.get_player(name)
        if player is None:
            return
        form = ActionForm(title="MultiVoice", content="Какой код показан на сайте?")
        for code in codes:
            form.add_button(str(code), on_click=lambda clicked, c=code: self.send("Confirm", clicked.xuid, c))
        form.add_button("Это не я", on_click=lambda clicked: self.send("Confirm", clicked.xuid, 0))
        player.send_form(form)

    def send(self, method: str, *args) -> bool:
        # dropped while disconnected; the full state is resent after reconnecting
        if not self.connected:
            return False
        self.hub.send(method, list(args))
        return True

    def on_open(self) -> None:
        self.sent = {}  # the server clears players on connect, so send everyone again
        self.connected = True
        self.logger.info("Connected to MultiVoice server")

    def on_close(self) -> None:
        self.connected = False

    # background thread: keeps one SignalR connection to MinecraftHub, reconnects forever
    def connect_loop(self) -> None:
        while self.running:
            if not self.connected:
                if self.hub is not None:
                    self.hub.stop()
                self.hub = HubConnectionBuilder() \
                    .with_url(self.config["url"], options={"headers": {"X-Secret": self.config["secret"]}}) \
                    .build()
                self.hub.on_open(self.on_open)
                self.hub.on_close(self.on_close)
                self.hub.on("Ask", self.asks.put)
                try:
                    self.hub.start()
                except Exception as e:
                    self.logger.warning(f"MultiVoice connection failed: {e}")
            time.sleep(3)
