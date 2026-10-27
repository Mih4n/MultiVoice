from endstone import Player
from endstone.command import CommandSender


def player_only(sender: CommandSender, command: str) -> bool:
    if isinstance(sender, Player):
        return True
    sender.send_error_message(f"Only players can use /{command}")
    return False
