#!/usr/bin/env bash
# Shared helper: locates the Unity editor that matches ProjectSettings/ProjectVersion.txt.
set -uo pipefail

PROJECT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="$(grep -E '^m_EditorVersion:' "$PROJECT/ProjectSettings/ProjectVersion.txt" | awk '{print $2}')"

if [[ -z "${UNITY:-}" ]]; then
  for candidate in \
    "/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity" \
    "$HOME/Unity/Hub/Editor/$VERSION/Editor/Unity" \
    "/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe"; do
    if [[ -x "$candidate" ]]; then UNITY="$candidate"; break; fi
  done
fi

if [[ -z "${UNITY:-}" ]]; then
  echo "Unity $VERSION not found. Set the UNITY environment variable to the editor executable." >&2
  exit 127
fi

mkdir -p "$PROJECT/Logs"
