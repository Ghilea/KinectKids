#!/usr/bin/env python3
"""Verify a partial-class split preserved all body lines with no loss/dup.

Compares the multiset of 'body' lines (everything except the repeated shell
header/footer scaffolding) between the original backup and the union of the
split files.

Usage: verify_split.py <original.bak> <shell_json> <file1> <file2> ...
The shell_json contains {"header": [...], "footer": [...]} verbatim lines that
were added to each PART file. The core file has NO added header/footer.
"""
import json
import sys
from collections import Counter


def read_lines(path):
    with open(path, "r", encoding="utf-8", newline="") as f:
        raw = f.read()
    eol = "\r\n" if "\r\n" in raw else "\n"
    return raw.split(eol)


def main():
    original = sys.argv[1]
    shell = json.load(open(sys.argv[2], "r", encoding="utf-8"))
    part_files = sys.argv[3:]

    header = shell["header"]
    footer = shell["footer"]

    orig = read_lines(original)
    # Original body = strip its own top usings/namespace/class-open (header_core)
    # and trailing class-close/ns-close (footer_core).
    hc = shell["header_core"]
    fc = shell["footer_core"]
    orig_body = orig[len(hc): len(orig) - len(fc)]

    union = []
    for pf in part_files:
        lines = read_lines(pf)
        is_core = shell.get("core_file", "") and pf.replace("\\", "/").endswith(shell["core_file"])
        if is_core:
            body = lines[len(hc): len(lines) - len(fc)]
        else:
            body = lines[len(header): len(lines) - len(footer)]
        union.extend(body)

    co = Counter(l for l in orig_body if l.strip() != "")
    cu = Counter(l for l in union if l.strip() != "")

    missing = co - cu
    extra = cu - co
    ok = not missing and not extra
    print(f"original body lines: {sum(co.values())}  union body lines: {sum(cu.values())}")
    if missing:
        print("MISSING (in original, not in split):")
        for line, n in list(missing.items())[:30]:
            print(f"  x{n}: [{line}]")
    if extra:
        print("EXTRA (in split, not in original):")
        for line, n in list(extra.items())[:30]:
            print(f"  x{n}: [{line}]")
    print("RESULT:", "OK - all body lines preserved" if ok else "MISMATCH")
    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
