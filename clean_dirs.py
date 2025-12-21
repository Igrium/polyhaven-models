#!/usr/bin/env python3
import os, sys

root = os.path.abspath(sys.argv[1]) if len(sys.argv) > 1 else os.path.abspath('.')

for dirpath, dirnames, filenames in os.walk(root, topdown=False):
    if os.path.abspath(dirpath) == root:
        continue
    if dirnames:
        continue
    if all(os.path.splitext(f)[1].endswith('_c') for f in filenames):
        try:
            os.rmdir(dirpath)
            print("removed:", dirpath)
        except OSError:
            pass
