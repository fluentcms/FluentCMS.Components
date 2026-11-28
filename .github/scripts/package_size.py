import argparse
import gzip
from zipfile import ZipFile


def size_report(package):
    script = "staticwebassets/FluentCMS.Components.lib.module.js"
    assets = ("staticwebassets/fluentcms-components.css", script, "lib/net10.0/FluentCMS.Components.dll")
    rows = [
        "## Release package size", "",
        "Each asset is gzipped separately at level 9. No budget is enforced.", "",
        "| Package entry | Gzip bytes |", "| --- | ---: |",
    ]
    with ZipFile(package) as archive:
        for name in assets:
            if name == script and name not in archive.namelist():
                size = "Not present in package"
            else:
                size = len(gzip.compress(archive.read(name), compresslevel=9, mtime=0))
            rows.append(f"| `{name}` | {size} |")
    return "\n".join(rows)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Report the gzipped sizes of the shipping library assets.")
    parser.add_argument("package", help="path to the Release .nupkg")
    print(size_report(parser.parse_args().package))
