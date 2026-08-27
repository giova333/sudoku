#!/usr/bin/env bash
# Fast standalone run of the Sudoku.Core EditMode tests.
# Uses the .NET SDK bundled with the Unity install - nothing extra to install.
# The same tests also run inside Unity via the Test Runner; this is just faster.
set -euo pipefail
DOTNET="/Applications/Unity/Hub/Editor/6000.5.10f1/Unity.app/Contents/Resources/Scripting/DotNetSdk/dotnet"
cd "$(dirname "$0")/Sudoku.Core.Tests.Build"
exec "$DOTNET" test --nologo "$@"
