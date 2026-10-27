from endstone.command import Command, CommandExecutor, CommandSender

from endstone_multivoice.commands import player_only


class MuteCommand(CommandExecutor):
    """/mute toggles the player's own microphone; the flag goes out with positions."""

    def __init__(self, muted: set[str]) -> None:
        CommandExecutor.__init__(self)
        self.muted = muted

    def on_command(self, sender: CommandSender, command: Command, args: list[str]) -> bool:
        if not player_only(sender, command.name):
            return True
        if sender.name in self.muted:
            self.muted.discard(sender.name)
            sender.send_message("Микрофон включён")
        else:
            self.muted.add(sender.name)
            sender.send_message("Микрофон выключен")
        return True
