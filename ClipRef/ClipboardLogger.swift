import AppKit

/// Result of attempting to save the clipboard to a log file.
enum SaveResult {
    case success(URL)
    case noText
    case failure(String)
}

/// Reads the system clipboard, writes it to a timestamped text file inside a
/// user-configurable folder, and then puts an `@`-prefixed absolute path back on
/// the clipboard so it can be pasted straight into Claude Code as a file reference.
final class ClipboardLogger {
    static let shared = ClipboardLogger()

    private let defaults = UserDefaults.standard
    private let folderKey = "logFolderPath"

    /// The folder logs are written to. Defaults to `~/Developer/clipboard-logs`.
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

    /// Number of days to keep log files. Override with
    /// `defaults write de.manuelwelsch.ClipRef retentionDays <N>`.
    var retentionDays: Int {
        let value = defaults.integer(forKey: "retentionDays")
        return value > 0 ? value : 7
    }

    /// Saves the current clipboard text to a new file and replaces the clipboard
    /// contents with an `@<path>` reference.
    @discardableResult
    func saveClipboard() -> SaveResult {
        let pasteboard = NSPasteboard.general
        guard let text = pasteboard.string(forType: .string), !text.isEmpty else {
            return .noText
        }

        let folder = folderURL
        do {
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        } catch {
            return .failure("Could not create folder:\n\(error.localizedDescription)")
        }

        let fileURL = folder.appendingPathComponent(Self.makeFileName())
        do {
            try text.write(to: fileURL, atomically: true, encoding: .utf8)
        } catch {
            return .failure("Could not write file:\n\(error.localizedDescription)")
        }

        // Replace the clipboard with an @-reference ready to paste into Claude Code.
        // The log text itself is already safely on disk.
        pasteboard.clearContents()
        pasteboard.setString("@\(fileURL.path)", forType: .string)

        cleanupOldLogs()
        return .success(fileURL)
    }

    /// Deletes log files older than `retentionDays`. Only touches files this app
    /// created (`clip-*.txt`), so it is safe even if the folder holds other files.
    func cleanupOldLogs() {
        let fileManager = FileManager.default
        guard let entries = try? fileManager.contentsOfDirectory(
            at: folderURL,
            includingPropertiesForKeys: [.contentModificationDateKey],
            options: [.skipsHiddenFiles]
        ) else { return }

        let cutoff = Date().addingTimeInterval(-Double(retentionDays) * 24 * 60 * 60)
        for url in entries {
            let name = url.lastPathComponent
            guard name.hasPrefix("clip-"), url.pathExtension == "txt" else { continue }
            let modified = (try? url.resourceValues(forKeys: [.contentModificationDateKey]))?.contentModificationDate
            if let modified, modified < cutoff {
                try? fileManager.removeItem(at: url)
            }
        }
    }

    private static func makeFileName() -> String {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        // Millisecond precision keeps file names unique even on rapid double-saves.
        formatter.dateFormat = "yyyyMMdd-HHmmss-SSS"
        return "clip-\(formatter.string(from: Date())).txt"
    }
}
