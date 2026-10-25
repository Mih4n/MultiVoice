from endstone.command import Command, CommandSender
from endstone.plugin import Plugin

from endstone_multivoice.hub import Hub
from endstone_multivoice.login import LoginForms
from endstone_multivoice.mute import toggle_mute
from endstone_multivoice.positions import PositionSync


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
        self.hub = Hub(self.config["url"], self.config["secret"], self.logger,
                       handlers={"Ask": lambda args: self.login.receive(args)},
                       on_open=lambda: self.positions.reset())
        self.login = LoginForms(self.server, self.hub.send)
        self.positions = PositionSync(self.server, self.hub.send, self.muted)
        self.hub.start()
        self.server.scheduler.run_task(self, self.tick, delay=0, period=int(self.config["period_ticks"]))

    def on_disable(self) -> None:
        self.hub.stop()

    def on_command(self, sender: CommandSender, command: Command, args: list[str]) -> bool:
        if command.name == "mute":
            toggle_mute(self.muted, sender)
        return True

    def tick(self) -> None:
        self.login.tick()
        self.positions.tick()
