from endstone import Player
from endstone.command import CommandSender


def toggle_mute(muted: set[str], sender: CommandSender) -> None:
    if not isinstance(sender, Player):
        sender.send_error_message("Only players can use /mute")
    elif sender.name in muted:
        muted.discard(sender.name)
        sender.send_message("Микрофон включён")
    else:
        muted.add(sender.name)
        sender.send_message("Микрофон выключен")
