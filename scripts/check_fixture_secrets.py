"""Check synthetic fixture inputs and reviewed snapshots without printing values.

This is a bounded repository hygiene check, not the product secret detector.
Uses only Python's standard library; JSON is decoded to catch escaped literals.
"""

import argparse
import json
from pathlib import Path
import re
import sys


PATTERNS = (
    ("private-key", re.compile(r"-----BEGIN [A-Z0-9 ]*PRIVATE KEY-----")),
    ("github-token", re.compile(r"\b(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,})\b")),
    ("aws-access-key", re.compile(r"\b(?:AKIA|ASIA)[A-Z0-9]{16}\b")),
    ("slack-token", re.compile(r"\bxox[baprs]-[A-Za-z0-9-]{16,}\b")),
    ("jwt", re.compile(r"\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b")),
    ("authorization-literal", re.compile(r"\b(?:Bearer|Basic)\s+[A-Za-z0-9+/=._~%-]{8,}", re.IGNORECASE)),
    ("password-assignment", re.compile(r"\b(?:password|pwd)\s*=\s*[^;\s\"']+", re.IGNORECASE)),
)
SENSITIVE_KEYS = frozenset({
    "authorization", "proxyauthorization", "cookie", "setcookie", "password",
    "passwd", "pwd", "secret", "clientsecret", "apikey", "accesskey",
    "accesskeyid", "secretaccesskey", "connectionstring",
})
SENSITIVE_SUFFIXES = ("password", "token", "apikey", "clientsecret", "connectionstring")
FIXTURES = Path("tests/fixtures")
SNAPSHOTS = Path("tests/ScaleProof.ContractTests/Snapshots")


def text_rules(text):
    return {rule for rule, pattern in PATTERNS if pattern.search(text)}


def json_rules(value):
    rules = set()
    if isinstance(value, dict):
        for key, child in value.items():
            rules.update(text_rules(key))
            normalized = re.sub(r"[^a-z0-9]", "", key.casefold())
            sensitive = normalized in SENSITIVE_KEYS or normalized.endswith(SENSITIVE_SUFFIXES)
            reference = isinstance(child, dict) and isinstance(child.get("secretRef"), str)
            if sensitive and not reference and child is not None and child != "":
                rules.add("credential-field")
            rules.update(json_rules(child))
    elif isinstance(value, list):
        for child in value:
            rules.update(json_rules(child))
    elif isinstance(value, str):
        rules.update(text_rules(value))
    return rules


def scan_file(path):
    if path.is_symlink():
        return {"symlink"}
    try:
        text = path.read_text(encoding="utf-8-sig")
    except (OSError, UnicodeError):
        return {"unreadable-file"}
    rules = text_rules(text)
    try:
        data = json.loads(text)
    except (ValueError, RecursionError):
        # Verify's initial schema inventory is formatted text, not JSON.
        if path.suffix == ".json":
            rules.add("invalid-json")
    else:
        rules.update(json_rules(data))
    return rules


def scan_repository(root):
    paths = []
    for directory, pattern in ((FIXTURES, "*.json"), (SNAPSHOTS, "*.verified.txt")):
        scope = root / directory
        if not scope.is_dir() or scope.is_symlink():
            return [], [(directory, "missing-or-symlinked-scope")]
        found = sorted(scope.rglob(pattern))
        if not found:
            return [], [(directory, "empty-scope")]
        # Do not follow symlinked directories or silently skip their contents.
        symlinks = [path for path in scope.rglob("*") if path.is_symlink()]
        if symlinks:
            return [], [(path.relative_to(root), "symlink") for path in symlinks]
        paths.extend(found)
    findings = [(path.relative_to(root), rule) for path in paths for rule in sorted(scan_file(path))]
    return paths, findings


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    root = parser.parse_args(argv).root.resolve()
    paths, findings = scan_repository(root)
    if findings:
        for path, rule in findings:
            # repr escapes control characters; matched input is never printed.
            print(f"{str(path)!r}: {rule}", file=sys.stderr)
        print(f"Fixture hygiene failed ({len(findings)} findings).", file=sys.stderr)
        return 1
    print(f"Fixture hygiene passed ({len(paths)} fixture/snapshot files).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
