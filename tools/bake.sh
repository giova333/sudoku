#!/usr/bin/env bash
# Bakes the puzzle banks into Assets/_Project/Resources/Banks.
# Usage: tools/bake.sh [outputDir] [mainCount] [dailyCount]
set -euo pipefail
DOTNET="/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
exec "$DOTNET" run --project tools/Sudoku.Bake -c Release -- "${1:-Assets/_Project/Resources/Banks}" "${2:-2000}" "${3:-750}"
