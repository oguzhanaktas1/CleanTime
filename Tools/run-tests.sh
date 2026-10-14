#!/usr/bin/env bash
# Usage: Tools/run-tests.sh [editmode|playmode]   (default: editmode)
# Close the Unity editor first: a project can only be open in one editor at a time.
source "$(dirname "$0")/_unity.sh"

PLATFORM="${1:-editmode}"
case "$PLATFORM" in
  editmode) UNITY_PLATFORM=EditMode ;;
  playmode) UNITY_PLATFORM=PlayMode ;;
  *) echo "Unknown platform '$PLATFORM' (use editmode or playmode)" >&2; exit 64 ;;
esac

RESULTS="$PROJECT/Logs/TestResults-$UNITY_PLATFORM.xml"
rm -f "$RESULTS"

"$UNITY" -batchmode -nographics -projectPath "$PROJECT" \
  -runTests -testPlatform "$UNITY_PLATFORM" \
  -testResults "$RESULTS" -logFile "$PROJECT/Logs/unity-tests-$UNITY_PLATFORM.log"
STATUS=$?

grep -oE '<test-run[^>]*(total|passed|failed)="[0-9]+"' "$RESULTS" 2>/dev/null | head -1 | sed -E 's/.*(total="[0-9]+").*/\1/' || true
echo "Results: $RESULTS (exit $STATUS)"
exit $STATUS
