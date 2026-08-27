#!/usr/bin/env bash
# Fast standalone run of the Sudoku.Core EditMode tests.
# Uses the .NET SDK bundled with the Unity install - nothing extra to install.
#
# Two steps, because the fast runner uses a NuGet NUnit that is NEWER than the
# one Unity ships. Compiling the tests against Unity's own NUnit first means a
# test can never pass here and fail to compile in the editor.
set -euo pipefail
DOTNET="/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"

echo "› checking tests compile against Unity's NUnit..."
"$DOTNET" build --nologo -v q "$ROOT/tools/Sudoku.Core.Tests.UnityCheck" >/dev/null

echo "› running tests..."
cd "$ROOT/tools/Sudoku.Core.Tests.Build"
exec "$DOTNET" test --nologo "$@"
