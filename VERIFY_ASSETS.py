"""Verify the local supplied asset inventory. No network access. Python 3 standard library."""
import csv
import hashlib
from pathlib import Path

root = Path(__file__).resolve().parent
errors = []
with (root / "ASSET_INVENTORY.csv").open(encoding="utf-8", newline="") as stream:
    rows = list(csv.DictReader(stream))
for row in rows:
    path = (root / row["path"]).resolve()
    if not path.is_relative_to(root):
        errors.append("Unsafe path: " + row["path"])
    elif not path.is_file():
        errors.append("Missing: " + row["path"])
    elif path.stat().st_size != int(row["bytes"]) or hashlib.sha256(path.read_bytes()).hexdigest() != row["sha256"]:
        errors.append("Changed: " + row["path"])
print("Files:", len(rows), "Errors:", len(errors))
for error in errors:
    print(error)
raise SystemExit(1 if errors else 0)
