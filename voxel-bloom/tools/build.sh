#!/usr/bin/env bash
# Local headless build.  Usage:
#   UNITY=/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity tools/build.sh ios
#   UNITY=~/Unity/Hub/Editor/6000.6.7f1/Editor/Unity tools/build.sh android
# Optional env: VB_VERSION (1.0.0), VB_BUILD_NUMBER (1), APPLE_TEAM_ID,
#   ANDROID_KEYSTORE_NAME / ANDROID_KEYSTORE_PASS / ANDROID_KEYALIAS_NAME / ANDROID_KEYALIAS_PASS, VB_ANDROID_FORMAT=apk
set -euo pipefail
TARGET="${1:-android}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
: "${UNITY:?Set UNITY to the Unity editor executable}"
case "$TARGET" in
  ios)     BT=iOS;     METHOD=VoxelBloom.EditorTools.BuildScript.BuildIOS ;;
  android) BT=Android; METHOD=VoxelBloom.EditorTools.BuildScript.BuildAndroid ;;
  *) echo "target must be ios or android"; exit 1 ;;
esac
mkdir -p "$ROOT/Builds"
"$UNITY" -batchmode -nographics -quit -projectPath "$ROOT" -buildTarget "$BT" \
  -executeMethod "$METHOD" -logFile "$ROOT/Builds/build-$TARGET.log"
echo "Done. Output in $ROOT/Builds ($TARGET). Log: Builds/build-$TARGET.log"
