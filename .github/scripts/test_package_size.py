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
    assembly = "lib/net10.0/FluentCMS.Components.dll"

    def report(self, files):
        with tempfile.TemporaryDirectory() as folder:
            package = Path(folder) / "fixture.nupkg"
            with ZipFile(package, "w") as archive:
                for name, content in files.items():
                    archive.writestr(name, content)
            return subprocess.run(
                [sys.executable, Path(__file__).with_name("package_size.py"), package],
                capture_output=True, text=True, check=False,
            )

    def test_compresses_each_shipping_asset(self):
        files = {self.css: b".fl{display:none}" * 100, self.assembly: bytes(range(256)), self.script: b"export {};"}
        result = self.report(files)
        self.assertEqual(result.returncode, 0, result.stderr)
        for name, content in files.items():
            size = len(gzip.compress(content, compresslevel=9, mtime=0))
            self.assertIn(f"| `{name}` | {size} |", result.stdout)

    def test_reports_absent_initializer_even_if_other_javascript_ships(self):
        result = self.report({self.css: b"css", self.assembly: b"dll", "staticwebassets/other.js": b"js"})
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn(f"| `{self.script}` | Not present in package |", result.stdout)

    def test_missing_required_asset_fails(self):
        for name in (self.css, self.assembly):
            with self.subTest(name=name):
                files = {self.css: b"css", self.assembly: b"dll"}
                del files[name]
                result = self.report(files)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn(name, result.stderr)


if __name__ == "__main__":
    unittest.main()
