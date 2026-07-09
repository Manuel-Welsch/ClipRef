# Test parity — macOS `ClipboardSaverTests` → Windows xUnit

This table maps every case in the macOS reference suite
[`ClipRefTests/ClipboardSaverTests.swift`](../ClipRefTests/ClipboardSaverTests.swift) to its counterpart in the
Windows port's xUnit suite (`windows/ClipRefTests/`). It exists so a reviewer can confirm, at a glance, that the
Windows port reproduces the macOS behavioural spec — the same decision core, naming/dedup, size limit, ownership
tagging, and prune.

The Windows tests follow the seam discipline from the project's ADRs: pure logic and orchestration are unit-tested
over in-memory doubles, and the two cases that exercise the **real persistence layer** (real `xattr` / real
`FileManager` on macOS) are covered by real-NTFS integration tests, tagged `[Trait("Category","Integration")]`.

## Mapping

| # | macOS test (`ClipboardSaverTests.swift`) | Behaviour | Windows xUnit counterpart | File |
|---|---|---|---|---|
| 1 | `testTextIsSaved` | Text is saved | `TextIsSaved` | `ClipboardSaverTests.cs` |
| 2 | `testTextWinsOverImage` | Text wins over a simultaneous image | `TextWinsOverImage` | `ClipboardSaverTests.cs` |
| 3 | `testImageSavedWhenNoText` | Image saved when there's no text (nil/empty) | `ImageSavedWhenNoText` | `ClipboardSaverTests.cs` |
| 4 | `testEmptyClipboardIgnored` | Empty clipboard is ignored | `EmptyClipboardIgnored` | `ClipboardSaverTests.cs` |
| 5 | `testReferenceIgnoredEvenWithImage` | Our own `@`-reference is ignored, even with an image | `ReferenceIgnoredEvenWithImage` | `ClipboardSaverTests.cs` |
| 6 | `testFileIsCopied` | A clipboard file is copied | `FileIsCopied` | `ClipboardSaverTests.cs` |
| 7 | `testFileWinsOverTextAndImage` | A file wins over text and image | `FileWinsOverTextAndImage` | `ClipboardSaverTests.cs` |
| 8 | `testFoldersAreNotCopyable` | Files are copyable, folders are not | `RegularFileIsCopyable`, `DirectoryIsNotCopyable` (+ `DirectoryWithExtraFlagsIsNotCopyable`, `MissingOrUnreadableFileIsNotCopyable`, `ReparsePointFileIsStillCopyable`) | `ClipboardSaverTests.cs` |
| 9 | `testReferenceDetection` | `looksLikeReference` recognises a path-bearing `@`-ref, rejects the rest | `LooksLikeReferenceDetectsWindowsReferencesOnly` (`[Theory]`, 11 cases) | `ClipboardSaverTests.cs` |
| 10 | `testUniqueURLNamingAndCollision` | `clip-<timestamp>.<ext>` name; counter on collision | `GeneratedNameUsesClipPrefixStampAndExtension`, `GeneratedImageNameUsesGivenExtension`, `GeneratedNameAppendsCounterOnCollision`, `GeneratedNameSkipsToThirdOnDoubleCollision` | `ClipboardSaverTests.cs` |
| 11 | `testUniqueURLKeepsOriginalNameAndCollidesFinderStyle` | Original name kept; Finder-style ` 2`, ` 3` on collision | `PreferredNameDedupesFinderStyle` (`[Theory]`, 3 cases) | `ClipboardSaverTests.cs` |
| 12 | `testUniqueURLPreferredNameWithoutExtension` | Extension-less name kept; dedup adds ` 2` with no trailing dot | `PreferredNameWithoutExtensionKeptAsIs`, `PreferredNameWithoutExtensionDedupesWithoutTrailingDot` | `ClipboardSaverTests.cs` |
| 13 | `testSizeLimitAcceptsSmallRejectsLarge` | `fitsSizeLimit` accepts ≤ limit, rejects above | `FitsSizeLimitComparesAgainstMaxBytes` (`[Theory]`, 3 cases), `UnreadableSizeDoesNotBlock`, `DefaultLimitIsHundredMegabytesDecimal` | `ClipboardSaverTests.cs` |
| 14 | `testSavedDateRoundTripsThroughXattr` | Ownership tag round-trips; an untagged file reads `null` | **Contract (in-memory):** `RoundTrip_TagThenReadReturnsInstant`, `NoTag_ReturnsNull` — `FileTaggerContractTests.cs`<br>**Real NTFS ADS:** `TaggedFile_SavedDateRoundTripsTheInstant`, `UntaggedFile_SavedDateIsNull`, `UnparseableTagValue_SavedDateIsNull` — `NtfsFileTaggerIntegrationTests.cs` | (see cells) |
| 15 | `testPruneDeletesOnlyOurExpiredFiles` | Prune deletes only our own expired (tagged) files; untagged user files are never touched | **Orchestrator (in-memory):** `Prune_DeletesExpiredTaggedFile`, `Prune_KeepsRecentTaggedFile`, `Prune_KeepsUntaggedForeignFile`, `Prune_BoundaryExactlyAtCutoff_Kept`, `Prune_EmptyOrMissingFolder_NoOp`, `Prune_DeleteFailureOnOneFile_StillPrunesOthers`, `Save_TriggersPrune` — `ClipboardSaveServiceTests.cs`<br>**Real on-disk:** `Prune_DeletesExpired_KeepsRecent_AndNeverTouchesUntagged` — `PruneIntegrationTests.cs` | (see cells) |

All 15 macOS cases are covered. Rows 14–15 are split into the unit-level **contract** over in-memory doubles and a
**real-persistence integration** test, because the macOS originals use the real `xattr`/`FileManager` directly — the
integration tests reproduce that exact path on a real NTFS volume.

## Where the Windows port goes beyond the macOS spec

The macOS file tests the pure decision core only; the Windows suite adds coverage the original leaves implicit or
platform-specific:

- **Windows reference rules** — `looksLikeReference` is pinned for drive-root, UNC (`\\server\share`), rooted
  backslash, and case-insensitive drive letters, and confirms the macOS cues (`/…`, `~/…`) are intentionally
  **not** recognised on Windows.
- **`isCopyableFile` variants** — directories with extra attribute flags, missing/unreadable files, and reparse
  points (symlink/junction → file) are each pinned, beyond the macOS file-vs-folder pair.
- **Size-limit edges** — exact-boundary (`==` limit), unreadable size (best-effort, doesn't block), and the real
  100 MB decimal default are explicit.
- **Write-side orchestration** — `ClipboardSaveServiceTests` covers the `saveClipboard` *write* half (precedence,
  guard-abort, unique naming on collision, verbatim `@`-path put-back, folder-create / write-failure mapping, and
  ownership-tag stamping on every success), which the macOS test file does not unit-test directly.
- **Prune edges** — boundary-at-cutoff, empty/missing folder, single-file delete failure isolation, and
  prune-runs-after-save are pinned in addition to the core "only our expired files" behaviour.

## Running

```sh
dotnet test windows/ClipRef.sln                                # full suite (138)
dotnet test windows/ClipRef.sln --filter Category=Integration  # real-NTFS integration tests only (4)
dotnet test windows/ClipRef.sln --filter "Category!=Integration"  # unit tests only (134)
```

The integration tests require a volume that persists NTFS Alternate Data Streams (any NTFS disk, including the
`windows-latest` CI runner). On a non-NTFS volume they skip gracefully.
