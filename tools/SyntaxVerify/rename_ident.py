#!/usr/bin/env python3
"""Whole-word identifier rename across given files, CRLF-preserving.

Usage: rename_ident.py <old> <new> <file1> [file2 ...]
Only replaces <old> when it appears as a complete identifier (word boundary
that is not part of a longer C# identifier). Reports per-file counts.
"""
import re
import sys


def main():
    old, new = sys.argv[1], sys.argv[2]
    files = sys.argv[3:]
    # C# identifier chars: letters, digits, underscore. Ensure old is not
    # preceded/followed by an identifier char (so SpokjaktenSceneEntry is safe).
    pattern = re.compile(r'(?<![A-Za-z0-9_])' + re.escape(old) + r'(?![A-Za-z0-9_])')
    total = 0
    for path in files:
        with open(path, 'r', encoding='utf-8', newline='') as f:
            text = f.read()
        new_text, n = pattern.subn(new, text)
        if n > 0:
            with open(path, 'w', encoding='utf-8', newline='') as f:
                f.write(new_text)
            total += n
            print(f"{path}: {n} replacement(s)")
    print(f"TOTAL: {total} replacement(s) of '{old}' -> '{new}'")


if __name__ == "__main__":
    main()
