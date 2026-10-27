from endstone.plugin import Plugin, ServicePriority

from endstone_multivoice.commands.ban import BanCommand, UnbanCommand
from endstone_multivoice.commands.mute import MuteCommand
from endstone_multivoice.commands.replies import Replies
from endstone_multivoice.commands.room import RoomCommand
from endstone_multivoice.connection.hub import Hub
from endstone_multivoice.login.forms import LoginForms
from endstone_multivoice.sync.positions import PositionSync
from endstone_multivoice.voice.states import VoiceStates


class MultiVoicePlugin(Plugin):
    api_version = "0.11"

    commands = {
        "mute": {
            "description": "Toggle your proximity voice microphone.",
            "usages": ["/mute"],
            "permissions": ["multivoice.command.mute"],
        },
        "room": {
            "description": "Create, join or leave a voice room.",
            "usages": ["/room (create|leave)<action: RoomAction>", "/room (join)<action: RoomJoinAction> <code: str>"],
            "permissions": ["multivoice.command.room"],
        },
        "voiceban": {
            "description": "Forbid a player to speak in voice chat, for some minutes or until /voiceunban.",
            "usages": ["/voiceban <player: player> [minutes: int]"],
            "permissions": ["multivoice.command.ban"],
        },
        "voiceunban": {
            "description": "Let a banned player speak in voice chat again.",
            "usages": ["/voiceunban <player: player>"],
            "permissions": ["multivoice.command.ban"],
        },
    }

    permissions = {
        "multivoice.command.mute": {
            "description": "Allow players to mute themselves in voice chat.",
            "default": True,
        },
        "multivoice.command.room": {
            "description": "Allow players to use voice rooms.",
            "default": True,
        },
        "multivoice.command.ban": {
            "description": "Allow banning players from speaking in voice chat.",
            "default": "op",
        },
    }

    def on_enable(self) -> None:
        self.save_default_config()
        self.muted: set[str] = set()
        self.hub = Hub(self.config["url"], self.config["secret"], self.logger,
                       handlers={"Ask": lambda args: self.login.receive(args),
                                 "VoiceStates": lambda args: self.voice_states.receive(args),
                                 "Tell": lambda args: self.replies.receive(args)},
                       on_open=self.reconnected)
        self.login = LoginForms(self.server, self.hub.send)
        self.positions = PositionSync(self.server, self.hub.send, self.muted)
        self.voice_states = VoiceStates(self.server)
        self.replies = Replies(self.server)
        self.get_command("mute").executor = MuteCommand(self.muted)
        self.get_command("room").executor = RoomCommand(self.hub.send)
        self.get_command("voiceban").executor = BanCommand(self.hub.send)
        self.get_command("voiceunban").executor = UnbanCommand(self.hub.send)
        self.server.service_manager.register("multivoice", self.voice_states, self, ServicePriority.NORMAL)
        self.hub.start()
        self.server.scheduler.run_task(self, self.tick, delay=0, period=int(self.config["period_ticks"]))

    def on_disable(self) -> None:
        self.hub.stop()
        self.server.service_manager.unregister_all(self)

    def reconnected(self) -> None:
        self.positions.reset()
        self.voice_states.reset()

    def tick(self) -> None:
        self.login.tick()
        self.positions.tick()
        self.voice_states.tick()
        self.replies.tick()
