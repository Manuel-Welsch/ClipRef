using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Shared helpers for the <c>[Trait("Category","Integration")]</c> tests that touch a real volume:
/// a self-deleting temp directory, an Alternate-Data-Stream support probe, and the skip guard built
/// on it. These tests exercise the production <see cref="NtfsFileTagger"/> / <see cref="FileSystem"/>
/// real-I/O paths that the unit suite deliberately fakes (ADR-0007/0006), so they need a real folder
/// on disk and must degrade gracefully on a volume that doesn't persist ADS (FAT/exFAT/network share).
/// </summary>
internal static class IntegrationTestSupport
{
    /// <summary>
    /// A uniquely-named directory under the system temp path, recursively removed on
    /// <see cref="Dispose"/> so a failed assertion never leaks it. Use with <c>using</c>.
    /// </summary>
    internal sealed class TempDir : IDisposable
    {
        internal string Path { get; }

        internal TempDir()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ClipRefIT-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        /// <summary>A path to <paramref name="name"/> inside this temp directory (not created).</summary>
        internal string File(string name) => System.IO.Path.Combine(Path, name);

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Best-effort cleanup: a leaked temp dir is harmless and must never fail a green test.
            }
        }
    }

    /// <summary>
    /// True when the volume backing <paramref name="directory"/> persists an NTFS Alternate Data
    /// Stream. Probes empirically — writes a tiny stream to a throwaway file, reads it back, and
    /// compares — so it is correct for FAT/exFAT/ReFS/network shares, not just a volume-name check.
    /// Callers early-<c>return</c> when this is <c>false</c> to skip gracefully: xUnit v2 has no
    /// dynamic skip and we decline a NuGet for it (ADR-0002 spirit), so the no-op reads as a pass.
    /// NTFS dev machines and the <c>windows-latest</c> CI runner support ADS, so the guard only
    /// trips on FAT/exFAT/network-share environments — the safe-degradation case from ADR-0007.
    /// </summary>
    internal static bool AdsSupported(string directory)
    {
        var probe = System.IO.Path.Combine(directory, "ads-probe-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllText(probe, string.Empty);
            var stream = probe + ":clipref-probe";
            File.WriteAllText(stream, "1");
            return File.ReadAllText(stream) == "1";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
        finally
        {
            try
            {
                File.Delete(probe);
            }
            catch (Exception)
            {
                // Best-effort: the temp dir's Dispose removes the probe anyway.
            }
        }
    }
}
