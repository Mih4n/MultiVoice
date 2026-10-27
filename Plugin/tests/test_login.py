import sys
import types
from pathlib import Path


class FakeForm:
    def __init__(self, title="", content="", on_close=None):
        self.on_close = on_close
        self.buttons = []

    def add_button(self, text, on_click=None):
        self.buttons.append((text, on_click))


sys.modules["endstone"] = types.ModuleType("endstone")
sys.modules["endstone.form"] = types.SimpleNamespace(ActionForm=FakeForm)
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src" / "endstone_multivoice" / "login"))
from forms import ASK_TIMEOUT, RETRY_SECONDS, LoginForms  # noqa: E402


class FakePlayer:
    name, xuid = "Steve", "42"

    def __init__(self):
        self.forms = []

    def send_form(self, form):
        self.forms.append(form)


def setup():
    player, sent, now = FakePlayer(), [], [0.0]
    server = types.SimpleNamespace(get_player=lambda name: player if name == player.name else None)
    login = LoginForms(server, lambda *a: sent.append(a), clock=lambda: now[0])
    login.receive(["Steve", [11, 22, 33]])
    return login, player, sent, now


def test_busy_client_gets_form_again_until_answered():
    login, player, sent, now = setup()
    login.tick()
    assert len(player.forms) == 1
    form = player.forms[0]

    form.on_close(player)  # client was busy
    login.tick()
    assert len(player.forms) == 1  # waits before retrying
    now[0] += RETRY_SECONDS
    login.tick()
    assert len(player.forms) == 2

    form.buttons[1][1](player)
    assert sent == [("Confirm", "42", 22)]
    now[0] += RETRY_SECONDS
    login.tick()
    assert len(player.forms) == 2


def test_unanswered_form_expires():
    login, player, sent, now = setup()
    login.tick()
    now[0] += ASK_TIMEOUT + 1
    login.tick()
    assert login.pending == {}
