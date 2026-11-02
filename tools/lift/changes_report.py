#!/usr/bin/env python3
# drawingForCore: lists every lifted file that differs from its upstream source (tools/lift/manifests/*.tsv), with a link
# to the upstream file at the lifted commit and the "drawingForCore:" notes the change carries. Writes docs/lifted-changes.md.
# Usage: changes_report.py <directory holding the upstream checkouts> [repository root]
# Upstream checkouts are read with `git show <commit>:<path>`, so they may be at any commit.
import difflib
import glob
import os
import re
import subprocess
import sys

CHECKOUTS = {  # manifest repository name -> (checkout directory, as tools/lift/fetch-sources.sh names it; GitHub repository)
    "dotnet/runtime": ("dotnet-runtime-6.0", "dotnet/runtime"),
    "dotnet/arcade": ("arcade-6.0", "dotnet/arcade"),
    "dotnet/runtime-assets": ("runtime-assets", "dotnet/runtime-assets"),
    "webformsforcore/WebFormsForCore": ("WebFormsForCore", "webformsforcore/WebFormsForCore"),
}


def git(checkout, *args):
    return subprocess.run(["git", "-C", checkout] + list(args), capture_output=True, check=True).stdout


TREES = {}


def show(checkout, commit, source):
    # Some files were lifted under a corrected case (Windows paths are case-insensitive; Linux ones are not).
    try:
        return git(checkout, "show", f"{commit}:{source}"), source
    except subprocess.CalledProcessError:
        if (checkout, commit) not in TREES:
            names = git(checkout, "ls-tree", "-r", "--name-only", commit).decode().splitlines()
            TREES[(checkout, commit)] = {n.lower(): n for n in names}
        actual = TREES[(checkout, commit)][source.lower()]
        return git(checkout, "show", f"{commit}:{actual}"), actual


def text(data):
    if data.startswith(b"\xef\xbb\xbf"):
        data = data[3:]
    try:
        return data.decode("utf-8").replace("\r\n", "\n")
    except UnicodeDecodeError:
        return None


def entries(manifest):
    lines = open(manifest, encoding="utf-8").read().splitlines()
    header = lines[0]
    m = re.match(r"# (\S+) ([0-9a-f]+)", header)
    for line in lines[1:]:
        parts = line.split("\t")
        if len(parts) == 3:
            repo, commit = parts[0].split()
            yield repo, commit, parts[1], parts[2]
        elif len(parts) == 2 and m:
            yield m.group(1), m.group(2), parts[0], parts[1]


def first_note(lines):
    notes = []
    for l in lines:
        hit = re.search(r"drawingForCore:?\s*(.*)", l)
        if hit and hit.group(1).strip():
            note = hit.group(1).strip().rstrip("*/ ").replace("|", "\\|")
            if note not in notes:
                notes.append(note)
    return "; ".join(n if len(n) <= 160 else n[:157] + "..." for n in notes[:3]) or "(no note; see LIFT.md)"


def main():
    upstream_root = sys.argv[1]
    top = sys.argv[2] if len(sys.argv) > 2 else os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    full = {}
    sections = []
    totals = [0, 0]
    for manifest in sorted(glob.glob(os.path.join(top, "tools", "lift", "manifests", "*.tsv"))):
        name = os.path.splitext(os.path.basename(manifest))[0]
        rows, count, repos = [], 0, set()
        for repo, commit, source, dest in entries(manifest):
            checkout, github = CHECKOUTS[repo]
            checkout = os.path.join(upstream_root, checkout)
            key = (checkout, commit)
            if key not in full:
                full[key] = git(checkout, "rev-parse", commit).decode().strip()
            sha = full[key]
            repos.add(f"[{github}@{sha[:7]}](https://github.com/{github}/tree/{sha})")
            count += 1
            path = os.path.join(top, dest)
            if not os.path.exists(path):
                continue  # copied for local runs only (not committed)
            original, source = show(checkout, commit, source)
            lifted = open(path, "rb").read()
            a, b = text(original), text(lifted)
            if a is not None and b is not None:
                if a == b:
                    continue
                diff = list(difflib.unified_diff(a.split("\n"), b.split("\n"), lineterm="", n=0))
                added = [l[1:] for l in diff if l.startswith("+") and not l.startswith("+++")]
                removed = sum(1 for l in diff if l.startswith("-") and not l.startswith("---"))
                notes = []
                for l in added:
                    hit = re.search(r"drawingForCore:?\s*(.*)", l)
                    if hit and hit.group(1).strip():
                        note = hit.group(1).strip().rstrip("*/ ").replace("|", "\\|")
                        if note not in notes:
                            notes.append(note)
                change = f"+{len(added)} −{removed}"
            elif original == lifted:
                continue
            else:
                notes, change = ["binary file replaced"], "binary"
            link = f"https://github.com/{github}/blob/{sha}/{source}"
            note = "; ".join(n if len(n) <= 160 else n[:157] + "..." for n in notes[:3]) or "(no note; see LIFT.md)"
            rows.append(f"| [`{dest.split('/', 2)[-1]}`]({link}) | {change} | {note} |")
        totals[0] += count
        totals[1] += len(rows)
        body = [f"## {name}", "", f"From {', '.join(sorted(repos))}. {count} files lifted, {len(rows)} changed."]
        if rows:
            body += ["", "| File (links to upstream) | Lines | drawingForCore's note |", "| --- | --- | --- |"] + rows
        sections.append("\n".join(body))
    out = os.path.join(top, "docs", "lifted-changes.md")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", encoding="utf-8") as f:
        f.write("# Lifted files that differ from upstream\n\n"
                "Generated by `tools/lift/changes_report.py` from `tools/lift/manifests/`: each lifted file is compared "
                "with its upstream source at the lifted commit (line endings and byte-order marks ignored). Files not "
                "listed are byte-for-byte copies. The note is the `drawingForCore:` comment the change carries; the "
                "reasoning for each group of changes is in [MODIFICATIONS.md](MODIFICATIONS.md) and `LIFT.md`. "
                "Files that drawingForCore added rather than changed are under `tools/lift/additions/` and are not listed here.\n\n"
                f"**{totals[0]:,} files lifted, {totals[1]:,} changed.**\n\n" + "\n\n".join(sections) + "\n")
    print(f"{totals[0]} files, {totals[1]} changed -> {out}")


if __name__ == "__main__":
    main()
