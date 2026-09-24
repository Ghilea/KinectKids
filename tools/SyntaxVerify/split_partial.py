#!/usr/bin/env python3
"""Deterministic partial-class splitter for Unity C# files.

Reads a source .cs file, keeps a set of 1-based line ranges in the ORIGINAL
file (the "core" ranges) and extracts other ranges into new partial files.

The class declaration line is rewritten from 'class' to 'partial class'.
Each generated partial file is wrapped with the file's original using-block
and namespace, plus a 'partial class' shell, so it compiles identically.

Config is a JSON file:
{
  "source": "path/to/File.cs",
  "class_decl_line": 6,           # 1-based line containing 'class X'
  "class_indent": "    ",         # indentation of the class body members
  "namespace_open": ["using ...;", "", "namespace NS", "{"],  # verbatim lines
  "namespace_close": ["}"],       # verbatim closing lines
  "class_open": "    public sealed partial class X",  # NEW decl line for shells
  "class_open_brace": "    {",
  "class_close": "    }",
  "core_file": "path/File.cs",    # rewritten original (partial)
  "core_ranges": [[1, 341]],      # kept in core AFTER header handling
  "parts": [
     {"file": "path/File.Run.cs", "ranges": [[342, 727]]},
     ...
  ]
}

Simplest usage below: we pass the header lines explicitly and body ranges are
emitted verbatim between a generated shell header and footer.
"""
import json
import sys


def read_lines(path):
    with open(path, "r", encoding="utf-8", newline="") as f:
        raw = f.read()
    eol = "\r\n" if "\r\n" in raw else "\n"
    # Normalize to split cleanly; strip a single trailing eol to avoid phantom line
    text = raw
    lines = text.split(eol)
    return lines, eol


def slice_ranges(lines, ranges):
    out = []
    for start, end in ranges:
        # convert 1-based inclusive to 0-based
        out.extend(lines[start - 1:end])
    return out


def write_file(path, text_lines, eol):
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(eol.join(text_lines))


def main(cfg_path):
    cfg = json.load(open(cfg_path, "r", encoding="utf-8"))
    lines, eol = read_lines(cfg["source"])

    header = cfg["shell_header"]       # list of verbatim lines before body
    footer = cfg["shell_footer"]       # list of verbatim lines after body

    # Compute core body FIRST (before writing anything, since a part file may
    # equal the source path in other configs).
    core_body = slice_ranges(lines, cfg["core_ranges"])
    old = cfg["class_decl_old"]
    new = cfg["class_decl_new"]
    replaced = 0
    for i, ln in enumerate(core_body):
        if ln == old:
            core_body[i] = new
            replaced += 1
    if replaced != 1:
        print(f"ERROR: class decl rewrite matched {replaced} lines (expected 1)")
        sys.exit(2)

    # Precompute part bodies before any write
    part_outputs = []
    for part in cfg["parts"]:
        body = slice_ranges(lines, part["ranges"])
        content = list(header) + body + list(footer)
        part_outputs.append((part["file"], content, part["ranges"]))

    # Now write everything
    for path, content, ranges in part_outputs:
        write_file(path, content, eol)
        print(f"WROTE {path}  ({sum(e-s+1 for s,e in ranges)} body lines)")

    write_file(cfg["core_file"], core_body, eol)
    print(f"WROTE {cfg['core_file']}  (core, decl rewritten)")


if __name__ == "__main__":
    main(sys.argv[1])
