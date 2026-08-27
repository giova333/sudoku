#!/usr/bin/env bash
# Everything that can be verified without opening the Unity editor.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
"$ROOT/tools/test.sh"
echo "› compile-checking the Unity presentation layer..."
"$ROOT/tools/check-game.sh"
echo "✓ all checks passed"
