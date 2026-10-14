#!/usr/bin/env bash
# (Re)creates Bootstrap, MainMenu, Town, PropertyShell and the CleaningVerticalSlice test scene
# and rewrites the build settings. Existing scenes with the same names are overwritten.
source "$(dirname "$0")/_unity.sh"

"$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" \
  -executeMethod CleanTime.Editor.ContentTools.SceneSetup.CreateBaseScenesFromCommandLine \
  -logFile "$PROJECT/Logs/unity-create-scenes.log"
