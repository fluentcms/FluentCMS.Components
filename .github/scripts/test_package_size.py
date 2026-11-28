import gzip
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from zipfile import ZipFile


class PackageSizeTests(unittest.TestCase):
    css = "staticwebassets/fluentcms-components.css"
    script = "staticwebassets/FluentCMS.Components.lib.module.js"
    fingerprinted = "staticwebassets/FluentCMS.Components.l343k390qb.lib.module.js"
    assembly = "lib/net10.0/FluentCMS.Components.dll"

    def report(self, files):
        with tempfile.TemporaryDirectory() as folder:
            package = Path(folder) / "fixture.nupkg"
            with ZipFile(package, "w") as archive:
                for name, content in files:
                    archive.writestr(name, content)
            return subprocess.run(
                [sys.executable, Path(__file__).with_name("package_size.py"), package],
                capture_output=True, text=True, check=False,
            )

    def test_compresses_each_shipping_asset(self):
        for script in (self.fingerprinted, self.script):
            with self.subTest(script=script):
                files = [(self.css, b".fl{display:none}" * 100), (self.assembly, bytes(range(256))), (script, b"export {};")]
                result = self.report(files)
                self.assertEqual(result.returncode, 0, result.stderr)
                for name, content in files:
                    size = len(gzip.compress(content, compresslevel=9, mtime=0))
                    self.assertIn(f"| `{name}` | {size} |", result.stdout)

    def test_other_javascript_does_not_count_as_the_initializer(self):
        result = self.report([
            (self.css, b"css"), (self.assembly, b"dll"),
            ("staticwebassets/Other.l343k390qb.lib.module.js", b"other library"),
            ("staticwebassets/FluentCMS.Components.min.lib.module.js", b"not an SDK fingerprint"),
            (self.fingerprinted + ".map", b"source map"),
            ("nested/" + self.script, b"wrong path"),
        ])
        self.assertNotEqual(result.returncode, 0)
        self.assertIn(self.script, result.stderr)

    def test_ambiguous_initializer_fails(self):
        for other in (self.script, self.fingerprinted.replace("l343k390qb", "a123b456c7")):
            with self.subTest(other=other):
                result = self.report([(self.css, b"css"), (self.assembly, b"dll"),
                                      (self.fingerprinted, b"first"), (other, b"second")])
                self.assertNotEqual(result.returncode, 0)
                self.assertIn(self.fingerprinted, result.stderr)
                self.assertIn(other, result.stderr)

    def test_missing_required_asset_fails(self):
        for name in (self.css, self.script, self.assembly):
            with self.subTest(name=name):
                files = {self.css: b"css", self.script: b"js", self.assembly: b"dll"}
                del files[name]
                result = self.report(files.items())
                self.assertNotEqual(result.returncode, 0)
                self.assertIn(name, result.stderr)

    def test_duplicate_archive_entry_fails(self):
        files = [(self.css, b"css"), (self.script, b"js"), (self.assembly, b"dll")]
        for name, _ in files:
            with self.subTest(name=name):
                with self.assertWarnsRegex(UserWarning, "Duplicate name"):
                    result = self.report([*files, (name, b"duplicate")])
                self.assertNotEqual(result.returncode, 0)
                self.assertIn(name, result.stderr)


if __name__ == "__main__":
    unittest.main()
