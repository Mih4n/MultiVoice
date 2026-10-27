import sys
import types
from pathlib import Path

sys.modules.setdefault("endstone", types.ModuleType("endstone"))
sys.modules["endstone.plugin"] = types.SimpleNamespace(Service=object)
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src" / "endstone_multivoice" / "voice"))
from states import VoiceStates  # noqa: E402


def setup():
    steve = types.SimpleNamespace(xuid="42", name="Steve")
    states = VoiceStates(types.SimpleNamespace(online_players=[steve]))
    seen = []
    states.on_change(lambda player, state: seen.append((player.name, state)))
    return states, steve, seen


def test_changes_apply_on_tick_and_notify_once():
    states, steve, seen = setup()
    states.receive([{"42": "speaking", "7": "mic"}])
    assert states.get_state(steve) == "off"  # nothing changes off the main thread
    states.tick()
    assert states.get_state(steve) == "speaking"
    assert seen == [("Steve", "speaking")]  # "7" is not online, so no callback
    states.receive([{"42": "speaking"}])
    states.tick()
    assert seen == [("Steve", "speaking")]


def test_reset_turns_everyone_off():
    states, steve, seen = setup()
    states.receive([{"42": "mic"}])
    states.reset()
    states.tick()
    assert states.get_state(steve) == "off"
    assert states.states == {}
    assert seen == [("Steve", "mic"), ("Steve", "off")]
