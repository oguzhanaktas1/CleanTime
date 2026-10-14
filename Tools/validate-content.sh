#!/usr/bin/env bash
# Fails (exit 1) when the content catalog has duplicate ids, broken references, unlock cycles or orphans.
source "$(dirname "$0")/_unity.sh"

"$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" \
  -executeMethod CleanTime.Editor.Validation.ContentValidationMenu.ValidateFromCommandLine \
  -logFile "$PROJECT/Logs/unity-validate-content.log"
STATUS=$?

grep -E "\[CleanTime\]" "$PROJECT/Logs/unity-validate-content.log" || true
exit $STATUS
