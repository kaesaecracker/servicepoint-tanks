#!/usr/bin/env bash
set -euxo pipefail

# first time
# dotnet restore --packages out
# nuget-to-json out > deps.json

# update
nix build ".?submodules=1#servicepoint-tanks-backend.fetch-deps"
./result deps.json
