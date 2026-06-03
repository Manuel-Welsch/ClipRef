#!/usr/bin/env bash
#
# Build, Developer ID-sign, notarize, and staple ClipRef.app for distribution.
#
# Requires:
#   - a "Developer ID Application" certificate in the login keychain
#   - a notarytool credential profile (default "NOTARY"; override via NOTARY_PROFILE)
#       xcrun notarytool store-credentials NOTARY --key <p8> --key-id <id> --issuer <id>
#
# Output: build/release/ClipRef-<version>.zip  (signed, notarized, stapled)
#
set -euo pipefail
cd "$(dirname "$0")/.."

PROJECT="ClipRef.xcodeproj"
SCHEME="ClipRef"
TEAM="DN96BWNUEE"
NOTARY_PROFILE="${NOTARY_PROFILE:-NOTARY}"

OUT="build/release"
ARCHIVE="$OUT/ClipRef.xcarchive"
EXPORT="$OUT/export"
APP="$EXPORT/ClipRef.app"

# Version: an explicit VERSION env wins (CI passes the git tag, e.g. 0.1.0),
# otherwise fall back to the project's MARKETING_VERSION, then 0.0.0.
if [ -z "${VERSION:-}" ]; then
  VERSION=$(xcodebuild -project "$PROJECT" -scheme "$SCHEME" -configuration Release -showBuildSettings 2>/dev/null \
    | awk -F' = ' '/ MARKETING_VERSION /{print $2; exit}')
fi
VERSION="${VERSION:-0.0.0}"
ZIP="$OUT/ClipRef-$VERSION.zip"

echo "▶︎ Archiving ClipRef ${VERSION}…"
rm -rf "$OUT"
xcodebuild archive \
  -project "$PROJECT" -scheme "$SCHEME" -configuration Release \
  -archivePath "$ARCHIVE" -derivedDataPath "$OUT/dd" \
  DEVELOPMENT_TEAM="$TEAM" MARKETING_VERSION="$VERSION" -allowProvisioningUpdates >/dev/null

echo "▶︎ Exporting Developer ID-signed app…"
xcodebuild -exportArchive \
  -archivePath "$ARCHIVE" -exportPath "$EXPORT" \
  -exportOptionsPlist scripts/ExportOptions.plist -allowProvisioningUpdates >/dev/null

echo "▶︎ Zipping for notarization…"
ditto -c -k --keepParent "$APP" "$ZIP"

echo "▶︎ Notarizing (can take a few minutes)…"
xcrun notarytool submit "$ZIP" --keychain-profile "$NOTARY_PROFILE" --wait

echo "▶︎ Stapling the ticket onto the app…"
xcrun stapler staple "$APP"

echo "▶︎ Re-zipping the stapled app for distribution…"
rm -f "$ZIP"
ditto -c -k --keepParent "$APP" "$ZIP"

echo "▶︎ Gatekeeper verification:"
spctl -a -vvv -t exec "$APP"

echo "✓ Done → $ZIP"
