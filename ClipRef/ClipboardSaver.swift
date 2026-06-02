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
final class ClipboardSaver {
    static let shared = ClipboardSaver()

    private enum Const {
        static let filePrefix = "clip-"
        static let timestampFormat = "yyyy-MM-dd-HH-mm-ss"
        static let textExtension = "txt"
        static let imageExtension = "png"
        static let defaultRetentionDays = 7
    }

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

    /// Creates the destination folder if it does not exist and returns it.
    @discardableResult
    func makeDestinationFolder() throws -> URL {
        let folder = folderURL
        try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
        return folder
    }

    /// Number of days to keep saved files. Override with
    /// `defaults write de.manuelwelsch.ClipRef retentionDays <N>`.
    var retentionDays: Int {
        let value = defaults.integer(forKey: "retentionDays")
        return value > 0 ? value : Const.defaultRetentionDays
    }

    /// Saves the clipboard to a new file and replaces the clipboard contents with
    /// an `@<path>` reference. Text wins if present; otherwise an image is saved.
    @discardableResult
    func saveClipboard() -> SaveResult {
        let pasteboard = NSPasteboard.general
        let imageData = pngData(from: pasteboard)

        switch Self.decide(text: pasteboard.string(forType: .string), hasImage: imageData != nil) {
        case .saveText(let text):
            return write(extension: Const.textExtension, pasteboard: pasteboard) { url in
                try Data(text.utf8).write(to: url, options: .atomic)
            }
        case .saveImage:
            guard let imageData else { return .noContent }
            return write(extension: Const.imageExtension, pasteboard: pasteboard) { url in
                try imageData.write(to: url, options: .atomic)
            }
        case .ignore:
            return .noContent
        }
    }

    /// What `saveClipboard` should do, decided purely from the clipboard's text and
    /// whether an image is present: text wins, our own `@<path>` references are ignored,
    /// otherwise an image is saved. Pure and side-effect-free, so it can be unit-tested.
    enum SaveDecision: Equatable {
        case saveText(String)
        case saveImage
        case ignore
    }

    static func decide(text: String?, hasImage: Bool) -> SaveDecision {
        if let text, !text.isEmpty {
            return looksLikeReference(text) ? .ignore : .saveText(text)
        }
        return hasImage ? .saveImage : .ignore
    }

    /// True when the clipboard already holds one of our `@<path>` references: a single
    /// token starting with `@` followed by an absolute (`/`) or home (`~`) path. Used to
    /// skip re-saving a reference that the previous click just put on the clipboard.
    static func looksLikeReference(_ text: String) -> Bool {
        let trimmed = text.trimmingCharacters(in: .whitespacesAndNewlines)
        guard trimmed.hasPrefix("@"),
              !trimmed.contains(where: { $0.isWhitespace }) else { return false }
        let path = trimmed.dropFirst()
        return path.hasPrefix("/") || path.hasPrefix("~")
    }

    /// Creates the folder, writes the file via `body`, swaps the clipboard for an
    /// `@<path>` reference, and prunes old files.
    private func write(extension ext: String, pasteboard: NSPasteboard, body: (URL) throws -> Void) -> SaveResult {
        let folder: URL
        do {
            folder = try makeDestinationFolder()
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

        pruneOldFiles()
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
    func pruneOldFiles() {
        let fileManager = FileManager.default
        guard let entries = try? fileManager.contentsOfDirectory(
            at: folderURL,
            includingPropertiesForKeys: [.contentModificationDateKey],
            options: [.skipsHiddenFiles]
        ) else { return }

        let keepExtensions: Set<String> = [Const.textExtension, Const.imageExtension]
        let cutoff = Date().addingTimeInterval(-Double(retentionDays) * 24 * 60 * 60)
        for url in entries {
            guard url.lastPathComponent.hasPrefix(Const.filePrefix),
                  keepExtensions.contains(url.pathExtension.lowercased()) else { continue }
            let modified = (try? url.resourceValues(forKeys: [.contentModificationDateKey]))?.contentModificationDate
            if let modified, modified < cutoff {
                try? fileManager.removeItem(at: url)
            }
        }
    }

    /// A non-existing file URL named `clip-YYYY-MM-DD-HH-mm-ss.<ext>`, appending a
    /// numeric suffix if a file from the same second already exists.
    static func uniqueURL(in folder: URL, extension ext: String) -> URL {
        let formatter = DateFormatter()
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = Const.timestampFormat
        let stamp = formatter.string(from: Date())

        var url = folder.appendingPathComponent("\(Const.filePrefix)\(stamp).\(ext)")
        var counter = 2
        while FileManager.default.fileExists(atPath: url.path) {
            url = folder.appendingPathComponent("\(Const.filePrefix)\(stamp)-\(counter).\(ext)")
            counter += 1
        }
        return url
    }
}
