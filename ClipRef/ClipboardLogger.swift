import AppKit

/// Result of attempting to save the clipboard to a file.
enum SaveResult {
    case success(URL)
    case noContent
    case failure(String)
}

/// Reads the system clipboard, writes it to a timestamped file inside a
/// user-configurable folder, and then puts an `@`-prefixed absolute path back on
/// the clipboard so it can be pasted straight into Claude Code as a file reference.
///
/// Text is saved as `.txt`; an image on the clipboard is saved as `.png`.
final class ClipboardLogger {
    static let shared = ClipboardLogger()

    private let defaults = UserDefaults.standard
    private let folderKey = "logFolderPath"

    /// The folder files are written to. Defaults to `~/Developer/clipboard-logs`.
    var folderURL: URL {
        if let path = defaults.string(forKey: folderKey), !path.isEmpty {
            let expanded = (path as NSString).expandingTildeInPath
            return URL(fileURLWithPath: expanded, isDirectory: true)
        }
        return Self.defaultFolderURL
    }

    static var defaultFolderURL: URL {
        FileManager.default.homeDirectoryForCurrentUser
            .appendingPathComponent("Developer/clipboard-logs", isDirectory: true)
    }

    func setFolder(_ url: URL) {
        defaults.set(url.path, forKey: folderKey)
    }

    /// Number of days to keep saved files. Override with
    /// `defaults write de.manuelwelsch.ClipRef retentionDays <N>`.
    var retentionDays: Int {
        let value = defaults.integer(forKey: "retentionDays")
        return value > 0 ? value : 7
    }

    /// Saves the clipboard to a new file and replaces the clipboard contents with
    /// an `@<path>` reference. Text wins if present; otherwise an image is saved.
    @discardableResult
    func saveClipboard() -> SaveResult {
        let pasteboard = NSPasteboard.general

        if let text = pasteboard.string(forType: .string), !text.isEmpty {
            return write(extension: "txt", pasteboard: pasteboard) { url in
                try Data(text.utf8).write(to: url, options: .atomic)
            }
        }

        if let png = pngData(from: pasteboard) {
            return write(extension: "png", pasteboard: pasteboard) { url in
                try png.write(to: url, options: .atomic)
            }
        }

        return .noContent
    }

    /// Creates the folder, writes the file via `body`, swaps the clipboard for an
    /// `@<path>` reference, and prunes old files.
    private func write(extension ext: String, pasteboard: NSPasteboard, body: (URL) throws -> Void) -> SaveResult {
        let folder = folderURL
        do {
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        } catch {
            return .failure("Could not create folder:\n\(error.localizedDescription)")
        }

        let fileURL = Self.uniqueURL(in: folder, extension: ext)
        do {
            try body(fileURL)
        } catch {
            return .failure("Could not write file:\n\(error.localizedDescription)")
        }

        // Replace the clipboard with an @-reference ready to paste into Claude Code.
        // The saved content is already safely on disk.
        pasteboard.clearContents()
        pasteboard.setString("@\(fileURL.path)", forType: .string)

        cleanupOldLogs()
        return .success(fileURL)
    }

    /// Extracts PNG data from the clipboard, re-encoding from TIFF or another image
    /// representation when a direct PNG isn't present. Returns nil if there is no image.
    private func pngData(from pasteboard: NSPasteboard) -> Data? {
        if let png = pasteboard.data(forType: .png) {
            return png
        }
        guard let image = NSImage(pasteboard: pasteboard),
              let tiff = image.tiffRepresentation,
              let bitmap = NSBitmapImageRep(data: tiff),
              let png = bitmap.representation(using: .png, properties: [:]) else {
            return nil
        }
        return png
    }

    /// Deletes saved files older than `retentionDays`. Only touches files this app
    /// created (`clip-*.txt` / `clip-*.png`), so it is safe even if the folder holds
    /// other files.
    func cleanupOldLogs() {
        let fileManager = FileManager.default
        guard let entries = try? fileManager.contentsOfDirectory(
            at: folderURL,
            includingPropertiesForKeys: [.contentModificationDateKey],
            options: [.skipsHiddenFiles]
        ) else { return }

        let keepExtensions: Set<String> = ["txt", "png"]
        let cutoff = Date().addingTimeInterval(-Double(retentionDays) * 24 * 60 * 60)
        for url in entries {
            guard url.lastPathComponent.hasPrefix("clip-"),
                  keepExtensions.contains(url.pathExtension.lowercased()) else { continue }
            let modified = (try? url.resourceValues(forKeys: [.contentModificationDateKey]))?.contentModificationDate
            if let modified, modified < cutoff {
                try? fileManager.removeItem(at: url)
            }
        }
    }

    /// A non-existing file URL named `clip-YYYY-MM-DD-HH-mm-ss.<ext>`, appending a
    /// numeric suffix if a file from the same second already exists.
    private static func uniqueURL(in folder: URL, extension ext: String) -> URL {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = "yyyy-MM-dd-HH-mm-ss"
        let stamp = formatter.string(from: Date())

        var url = folder.appendingPathComponent("clip-\(stamp).\(ext)")
        var counter = 2
        while FileManager.default.fileExists(atPath: url.path) {
            url = folder.appendingPathComponent("clip-\(stamp)-\(counter).\(ext)")
            counter += 1
        }
        return url
    }
}
