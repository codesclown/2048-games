#!/bin/zsh
set -e
project_dir="$(cd -- "$(dirname -- "$0")" && pwd)"
unity_app="/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app"
open -n "$unity_app" --args -projectPath "$project_dir"
