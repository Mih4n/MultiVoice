from endstone.command import Command, CommandExecutor, CommandSender

from endstone_multivoice.commands import player_only


class BanCommand(CommandExecutor):
    """/voiceban <player> [minutes]; the server checks that the sender is an op and answers through Tell."""

    def __init__(self, send) -> None:
        CommandExecutor.__init__(self)
        self.send = send

    def on_command(self, sender: CommandSender, command: Command, args: list[str]) -> bool:
        minutes = int(args[1]) if len(args) > 1 else None
        if player_only(sender, command.name) and not self.send("Ban", sender.xuid, args[0], minutes):
            sender.send_error_message("Голосовой сервер недоступен")
        return True


class UnbanCommand(CommandExecutor):
    """/voiceunban <player>"""

    def __init__(self, send) -> None:
        CommandExecutor.__init__(self)
        self.send = send

    def on_command(self, sender: CommandSender, command: Command, args: list[str]) -> bool:
        if player_only(sender, command.name) and not self.send("Unban", sender.xuid, args[0]):
            sender.send_error_message("Голосовой сервер недоступен")
        return True
