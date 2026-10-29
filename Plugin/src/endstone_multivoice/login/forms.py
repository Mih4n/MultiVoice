import queue
import time
from dataclasses import dataclass

from endstone.form import ActionForm

# the form keeps reappearing until the player answers or this runs out
ASK_TIMEOUT = 60
RETRY_SECONDS = 1


@dataclass
class PendingAsk:
    deadline: float
    form: ActionForm | None = None
    shown: bool = False
    retry_at: float = 0


class Placeholders(dict):
    def __missing__(self, key: str) -> str:
        return "{" + key + "}"  # a typo in the config shows up as is instead of breaking the form


class LoginForms:
    """Asks players in game which code the site shows."""

    def __init__(self, server, send, config: dict | None = None, clock=time.monotonic) -> None:
        self.server = server
        self.send = send
        self.config = config or {}  # [login_form]; a missing key falls back to the defaults below
        self.clock = clock
        self.incoming: queue.Queue[list] = queue.Queue()  # hub thread -> main thread
        self.pending: dict[str, PendingAsk] = {}  # player name -> unanswered form

    # hub thread
    def receive(self, args: list) -> None:
        self.incoming.put(args)

    # main thread
    def tick(self) -> None:
        while not self.incoming.empty():
            name, codes = self.incoming.get_nowait()
            self.ask(name, codes)

        now = self.clock()
        for name, entry in list(self.pending.items()):
            player = self.server.get_player(name)
            if player is None or now > entry.deadline:
                del self.pending[name]
            elif not entry.shown and now >= entry.retry_at:
                entry.shown = True
                entry.retry_at = now + RETRY_SECONDS
                player.send_form(entry.form)

    def ask(self, name: str, codes: list[int]) -> None:
        entry = PendingAsk(deadline=self.clock() + ASK_TIMEOUT)
        # a busy client (pause menu while the player is on the site, chat open) closes the form at once,
        # so a closed form is shown again; "Это не я" is the way to refuse
        text = lambda key, default, **values: self.config.get(key, default).format_map(Placeholders(player=name, **values))
        entry.form = ActionForm(title=text("title", "MultiVoice"), on_close=lambda player: setattr(entry, "shown", False))
        if label := text("label", ""):
            entry.form.add_label(label)
        for code in codes:
            entry.form.add_button(text("code_button", "{code}", code=code),
                                  on_click=lambda player, c=code: self.answer(player, entry, c))
        entry.form.add_button(text("deny_button", "Это не я"), on_click=lambda player: self.answer(player, entry, 0))
        self.pending[name] = entry

    def answer(self, player, entry: PendingAsk, code: int) -> None:
        if self.pending.get(player.name) is entry:
            del self.pending[player.name]
        self.send("Confirm", player.xuid, code)
