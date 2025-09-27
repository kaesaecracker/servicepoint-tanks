#!/usr/bin/env bash
set -euxo pipefail

dotnet restore --packages out

nuget-to-json out > deps.json
