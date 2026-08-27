#!/usr/bin/env bash
# Compile-checks the editor tooling without opening the editor.
set -euo pipefail
DOTNET="/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
cd "$(dirname "$0")/Sudoku.Editor.Build"
exec "$DOTNET" build --nologo -v q "$@"
