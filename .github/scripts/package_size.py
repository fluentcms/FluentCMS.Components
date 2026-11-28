import argparse
import gzip
import re
from zipfile import ZipFile


def required_entry(names, path, pattern=None):
    matcher = re.compile(pattern or re.escape(path))
    matches = [name for name in names if matcher.fullmatch(name)]
    if not matches:
        raise SystemExit(f"Missing required package entry: {path}")
    if len(matches) > 1:
        raise SystemExit(f"Ambiguous package entry for {path}: {', '.join(matches)}")
    return matches[0]


def size_report(package):
    rows = [
        "## Release package size", "",
        "Each asset is gzipped separately at level 9. No budget is enforced.", "",
        "| Package entry | Gzip bytes |", "| --- | ---: |",
    ]
    with ZipFile(package) as archive:
        names = archive.namelist()
        assets = (
            required_entry(names, "staticwebassets/fluentcms-components.css"),
            required_entry(names, "staticwebassets/FluentCMS.Components.lib.module.js",
                           r"staticwebassets/FluentCMS\.Components(?:\.[a-z0-9]{10})?\.lib\.module\.js"),
            required_entry(names, "lib/net10.0/FluentCMS.Components.dll"),
        )
        for name in assets:
            size = len(gzip.compress(archive.read(name), compresslevel=9, mtime=0))
            rows.append(f"| `{name}` | {size} |")
    return "\n".join(rows)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Report the gzipped sizes of the shipping library assets.")
    parser.add_argument("package", help="path to the Release .nupkg")
    print(size_report(parser.parse_args().package))
