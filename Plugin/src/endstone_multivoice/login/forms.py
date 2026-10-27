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


class LoginForms:
    """Asks players in game which code the site shows."""

    def __init__(self, server, send, clock=time.monotonic) -> None:
        self.server = server
        self.send = send
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
        entry.form = ActionForm(title="MultiVoice", content="Какой код показан на сайте?",
                                on_close=lambda player: setattr(entry, "shown", False))
        for code in codes:
            entry.form.add_button(str(code), on_click=lambda player, c=code: self.answer(player, entry, c))
        entry.form.add_button("Это не я", on_click=lambda player: self.answer(player, entry, 0))
        self.pending[name] = entry

    def answer(self, player, entry: PendingAsk, code: int) -> None:
        if self.pending.get(player.name) is entry:
            del self.pending[player.name]
        self.send("Confirm", player.xuid, code)
