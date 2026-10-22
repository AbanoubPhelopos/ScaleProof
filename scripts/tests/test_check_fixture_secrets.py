"""Synthetic positive/negative controls for the CI hygiene gate (no real secrets)."""

from contextlib import redirect_stderr, redirect_stdout
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / "check_fixture_secrets.py"
SPEC = importlib.util.spec_from_file_location("check_fixture_secrets", SCRIPT)
scanner = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(scanner)


class FixtureSecretTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        self.fixture = self.root / scanner.FIXTURES / "synthetic.json"
        self.snapshot = self.root / scanner.SNAPSHOTS / "synthetic.verified.txt"
        self.fixture.parent.mkdir(parents=True)
        self.snapshot.parent.mkdir(parents=True)
        self.fixture.write_text('{}', encoding="utf-8")
        self.snapshot.write_text('{}', encoding="utf-8")

    def test_references_templates_hashes_and_contract_values_are_allowed(self):
        data = {
            "Authorization": {"secretRef": "sample-token", "format": "Bearer {value}"},
            "hash": "sha256:" + "a" * 64,
            "key": "synthetic/artifact.json",
            "generatedAt": "2026-01-01T00:00:00Z",
            "precision": "12345678901234567890.12345678901234567890",
        }
        self.fixture.write_text(json.dumps(data), encoding="utf-8")
        paths, findings = scanner.scan_repository(self.root)
        self.assertEqual(2, len(paths))
        self.assertEqual([], findings)

    def test_common_secret_patterns_are_rejected(self):
        cases = {
            "private-key": "-----BEGIN " + "PRIVATE KEY-----",
            "github-token": "ghp_" + "A" * 36,
            "aws-access-key": "AKIA" + "A" * 16,
            "slack-token": "xoxb-" + "1" * 20,
            "jwt": "eyJ" + "A" * 12 + "." + "B" * 20 + "." + "C" * 20,
            "authorization-literal": "Bearer " + "A" * 16,
            "password-assignment": "Server=synthetic;Password=" + "example" + ";",
        }
        for rule, value in cases.items():
            with self.subTest(rule=rule):
                self.fixture.write_text(json.dumps({"payload": value}), encoding="utf-8")
                self.assertIn(rule, scanner.scan_file(self.fixture))

    def test_short_credentials_are_rejected_by_field_name(self):
        for name in ("Authorization", "Cookie", "api_key", "clientSecret", "accessToken", "password"):
            with self.subTest(name=name):
                self.fixture.write_text(json.dumps({name: "synthetic"}), encoding="utf-8")
                self.assertIn("credential-field", scanner.scan_file(self.fixture))

    def test_reference_objects_do_not_hide_nested_token_fields(self):
        self.fixture.write_text(json.dumps({"Authorization": {"secretRef": "sample-token", "token": "synthetic"}}), encoding="utf-8")
        self.assertIn("credential-field", scanner.scan_file(self.fixture))

    def test_escaped_json_credentials_are_decoded(self):
        # Raw text cannot match this synthetic token until JSON escapes are decoded.
        value = "ghp_" + "A" * 36
        escaped = "".join("\\u%04x" % ord(char) for char in value)
        self.fixture.write_text('{"payload":"' + escaped + '"}', encoding="utf-8")
        self.assertIn("github-token", scanner.scan_file(self.fixture))
        self.fixture.write_text('{"\\u0070assword":"synthetic"}', encoding="utf-8")
        self.assertIn("credential-field", scanner.scan_file(self.fixture))

    def test_reviewed_json_and_formatted_text_snapshots_are_scanned(self):
        for text in (json.dumps({"password": "synthetic"}), "Inventory: " + "ghp_" + "A" * 36):
            with self.subTest(json_format=text.startswith("{")):
                self.snapshot.write_text(text, encoding="utf-8-sig")
                self.assertTrue(scanner.scan_file(self.snapshot))

    def test_non_json_inventory_snapshot_is_allowed(self):
        self.snapshot.write_text('[{ Id: synthetic, Required: [name] }]', encoding="utf-8-sig")
        self.assertEqual(set(), scanner.scan_file(self.snapshot))

    def test_malformed_fixture_fails_without_printing_its_contents(self):
        canary = "malformed-private-input"
        self.fixture.write_text('{"password": "' + canary, encoding="utf-8")
        self.assertIn("invalid-json", scanner.scan_file(self.fixture))
        result, output = self.run_cli()
        self.assertEqual(1, result)
        self.assertNotIn(canary, output)

    def test_cli_failure_does_not_echo_matched_values(self):
        canary = "ghp_" + "A" * 36
        self.fixture.write_text(json.dumps({"payload": canary}), encoding="utf-8")
        result, output = self.run_cli()
        self.assertEqual(1, result)
        self.assertIn("github-token", output)
        self.assertNotIn(canary, output)

    def test_cli_success_reports_scanned_count(self):
        result, output = self.run_cli()
        self.assertEqual(0, result)
        self.assertIn("2 fixture/snapshot files", output)

    def test_empty_scope_fails(self):
        self.snapshot.unlink()
        self.assertEqual(1, self.run_cli()[0])

    def test_missing_scope_fails(self):
        self.snapshot.unlink()
        self.snapshot.parent.rmdir()
        self.assertEqual(1, self.run_cli()[0])

    def test_symlinked_fixture_fails(self):
        target = self.root / "outside.json"
        target.write_text('{}', encoding="utf-8")
        link = self.fixture.parent / "linked.json"
        link.symlink_to(target)
        self.assertEqual(1, self.run_cli()[0])

    def test_invalid_utf8_fails_without_echoing_bytes(self):
        self.fixture.write_bytes(bytes([255, 254]))
        self.assertIn("unreadable-file", scanner.scan_file(self.fixture))
        self.assertEqual(1, self.run_cli()[0])

    def test_symlinked_subdirectory_is_not_silently_skipped(self):
        target = self.root / "outside"
        target.mkdir()
        (self.fixture.parent / "linked-directory").symlink_to(target, target_is_directory=True)
        self.assertEqual(1, self.run_cli()[0])

    def run_cli(self):
        output = io.StringIO()
        with redirect_stdout(output), redirect_stderr(output):
            result = scanner.main(["--root", str(self.root)])
        return result, output.getvalue()


if __name__ == "__main__":
    unittest.main()
