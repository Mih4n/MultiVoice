from endstone.command import Command, CommandExecutor, CommandSender

from endstone_multivoice.commands import player_only


class RoomCommand(CommandExecutor):
    """/room create | join <code> | leave; the voice server owns rooms and answers through Tell."""

    def __init__(self, send) -> None:
        CommandExecutor.__init__(self)
        self.send = send

    def on_command(self, sender: CommandSender, command: Command, args: list[str]) -> bool:
        if player_only(sender, command.name) and \
                not self.send("Room", sender.xuid, args[0] if args else "", args[1] if len(args) > 1 else None):
            sender.send_error_message("Голосовой сервер недоступен")
        return True
