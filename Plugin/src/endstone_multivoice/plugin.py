from endstone.command import Command, CommandSender
from endstone.plugin import Plugin, ServicePriority

from endstone_multivoice.hub import Hub
from endstone_multivoice.login import LoginForms
from endstone_multivoice.mute import toggle_mute
from endstone_multivoice.positions import PositionSync
from endstone_multivoice.voice_state import VoiceStates


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
                       handlers={"Ask": lambda args: self.login.receive(args),
                                 "VoiceStates": lambda args: self.voice_states.receive(args)},
                       on_open=self.reconnected)
        self.login = LoginForms(self.server, self.hub.send)
        self.positions = PositionSync(self.server, self.hub.send, self.muted)
        self.voice_states = VoiceStates(self.server)
        self.server.service_manager.register("multivoice", self.voice_states, self, ServicePriority.NORMAL)
        self.hub.start()
        self.server.scheduler.run_task(self, self.tick, delay=0, period=int(self.config["period_ticks"]))

    def on_disable(self) -> None:
        self.hub.stop()
        self.server.service_manager.unregister_all(self)

    def reconnected(self) -> None:
        self.positions.reset()
        self.voice_states.reset()

    def on_command(self, sender: CommandSender, command: Command, args: list[str]) -> bool:
        if command.name == "mute":
            toggle_mute(self.muted, sender)
        return True

    def tick(self) -> None:
        self.login.tick()
        self.positions.tick()
        self.voice_states.tick()
