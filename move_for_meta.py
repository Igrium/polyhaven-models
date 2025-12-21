#!/usr/bin/env python3
import os, shutil, sys

root = sys.argv[1] if len(sys.argv) > 1 else "."

# index files by name (excluding .meta)
files_by_name = {}
for d, _, fs in os.walk(root):
    for f in fs:
        if not f.endswith(".meta"):
            files_by_name.setdefault(f, []).append(os.path.join(d, f))

# find .meta files and move them next to their target
for d, _, fs in os.walk(root):
    for f in fs:
        if not f.endswith(".meta"):
            continue
        base = f[:-5]
        matches = files_by_name.get(base)
        if not matches:
            print(f"no match for: {os.path.join(d, f)}")
            continue

        target_dir = os.path.dirname(matches[0])
        src = os.path.join(d, f)
        dst = os.path.join(target_dir, f)

        if d == target_dir:
            continue

        try:
            if os.path.exists(dst):
                os.remove(dst)
                print(f"overrode: {dst}")
            shutil.move(src, dst)
            print(f"moved: {src} -> {dst}")
        except Exception as e:
            print(f"error moving {src}: {e}")
