import AppKit

final class AppDelegate: NSObject, NSApplicationDelegate {
    private var statusItem: NSStatusItem!
    private let saver = ClipboardSaver.shared

    private enum UI {
        static let defaultSymbol = "doc.on.clipboard"
        static let successSymbol = "checkmark.circle.fill"
        static let warningSymbol = "exclamationmark.triangle.fill"
        static let failureSymbol = "xmark.octagon.fill"
        static let successSound = "Pop"
        static let failureSound = "Funk"
        static let flashDuration: TimeInterval = 1.1
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        if let button = statusItem.button {
            button.image = Self.defaultImage()
            button.imagePosition = .imageOnly
            button.target = self
            button.action = #selector(handleClick)
            button.sendAction(on: [.leftMouseUp, .rightMouseUp])
            button.toolTip = "Left-click: save clipboard (text or image) to a file · Right-click: menu"
        }
        LoginItem.enableOnFirstRun()
        saver.pruneOldFiles()
    }

    // MARK: - Click handling

    /// Left-click saves immediately; right-click (or control-click) opens the menu.
    @objc private func handleClick() {
        guard let event = NSApp.currentEvent else { return }
        let isMenuClick = event.type == .rightMouseUp || event.modifierFlags.contains(.control)
        if isMenuClick {
            showMenu()
        } else {
            performSave()
        }
    }

    private func performSave() {
        switch saver.saveClipboard() {
        case .success:
            flash(symbol: UI.successSymbol, success: true)
        case .noContent:
            flash(symbol: UI.warningSymbol, success: false)
        case .failure(let message):
            flash(symbol: UI.failureSymbol, success: false)
            presentError(message)
        }
    }

    // MARK: - Menu

    private func showMenu() {
        let menu = NSMenu()

        let folderItem = NSMenuItem(title: "Saves → \(saver.folderURL.path)", action: nil, keyEquivalent: "")
        folderItem.isEnabled = false
        menu.addItem(folderItem)
        menu.addItem(.separator())

        addItem(to: menu, title: "Save Clipboard Now", action: #selector(menuSave), key: "s")
        addItem(to: menu, title: "Open Folder", action: #selector(openFolder), key: "o")
        addItem(to: menu, title: "Change Folder…", action: #selector(changeFolder), key: "")
        menu.addItem(.separator())

        let loginMenuItem = addItem(to: menu, title: "Launch at Login", action: #selector(toggleLaunchAtLogin), key: "")
        loginMenuItem.state = LoginItem.isEnabled ? .on : .off
        menu.addItem(.separator())

        menu.addItem(withTitle: "Quit ClipRef", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")

        // Temporarily attach the menu so a click pops it open, then detach so the
        // next left-click triggers the save action again instead of the menu.
        statusItem.menu = menu
        statusItem.button?.performClick(nil)
        statusItem.menu = nil
    }

    @discardableResult
    private func addItem(to menu: NSMenu, title: String, action: Selector, key: String) -> NSMenuItem {
        let item = NSMenuItem(title: title, action: action, keyEquivalent: key)
        item.target = self
        menu.addItem(item)
        return item
    }

    @objc private func menuSave() { performSave() }

    @objc private func openFolder() {
        let folder = (try? saver.makeDestinationFolder()) ?? saver.folderURL
        NSWorkspace.shared.open(folder)
    }

    @objc private func changeFolder() {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        panel.canCreateDirectories = true
        panel.prompt = "Choose"
        panel.message = "Choose the folder where saved clipboard files will be stored"
        panel.directoryURL = saver.folderURL
        NSApp.activate(ignoringOtherApps: true)
        if panel.runModal() == .OK, let url = panel.url {
            saver.setFolder(url)
        }
    }

    @objc private func toggleLaunchAtLogin() {
        do {
            try LoginItem.toggle()
        } catch {
            presentError("Could not change the launch-at-login setting:\n\(error.localizedDescription)")
        }
    }

    // MARK: - Feedback

    private func flash(symbol: String, success: Bool) {
        statusItem.button?.image = NSImage(systemSymbolName: symbol, accessibilityDescription: nil)
        NSSound(named: success ? UI.successSound : UI.failureSound)?.play()
        DispatchQueue.main.asyncAfter(deadline: .now() + UI.flashDuration) { [weak self] in
            self?.statusItem.button?.image = Self.defaultImage()
        }
    }

    private func presentError(_ message: String) {
        let alert = NSAlert()
        alert.messageText = "ClipRef"
        alert.informativeText = message
        alert.alertStyle = .warning
        NSApp.activate(ignoringOtherApps: true)
        alert.runModal()
    }

    private static func defaultImage() -> NSImage? {
        let image = NSImage(systemSymbolName: UI.defaultSymbol, accessibilityDescription: "ClipRef")
        image?.isTemplate = true
        return image
    }
}
