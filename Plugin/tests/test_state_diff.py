import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src" / "endstone_multivoice" / "sync"))
from state_diff import state_diff  # noqa: E402

a = {"xuid": "1", "x": 0}
b = {"xuid": "2", "x": 5}

assert state_diff({}, {"1": a}) == ([a], []), "new player is sent"
assert state_diff({"1": a}, {"1": dict(a)}) == ([], []), "unchanged player is not sent"
moved = {**a, "x": 1}
assert state_diff({"1": a, "2": b}, {"1": moved, "2": b}) == ([moved], []), "only the mover is sent"
assert state_diff({"1": a, "2": b}, {"2": b}) == ([], ["1"]), "leaver is reported"
print("state_diff OK")
