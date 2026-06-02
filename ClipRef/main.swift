import AppKit

// Headless one-shot mode, useful for scripting and verification:
//   ClipRef --save-once
// Writes the current clipboard to a new log file, copies an @<path> reference
// back onto the clipboard, prints the file path, and exits.
if CommandLine.arguments.contains("--save-once") {
    switch ClipboardLogger.shared.saveClipboard() {
    case .success(let url):
        FileHandle.standardOutput.write(Data("\(url.path)\n".utf8))
        exit(0)
    case .noText:
        FileHandle.standardError.write(Data("Clipboard has no text to save\n".utf8))
        exit(2)
    case .failure(let message):
        FileHandle.standardError.write(Data("\(message)\n".utf8))
        exit(1)
    }
}

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.setActivationPolicy(.accessory) // menu-bar agent: no Dock icon, no app menu
app.run()
