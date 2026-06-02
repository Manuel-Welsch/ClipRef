import AppKit
import ServiceManagement

final class AppDelegate: NSObject, NSApplicationDelegate {
    private var statusItem: NSStatusItem!
    private let logger = ClipboardLogger.shared

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
        enableLaunchAtLoginOnFirstRun()
        logger.cleanupOldLogs()
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
        switch logger.saveClipboard() {
        case .success:
            flash(symbol: "checkmark.circle.fill", success: true)
        case .noContent:
            flash(symbol: "exclamationmark.triangle.fill", success: false)
        case .failure(let message):
            flash(symbol: "xmark.octagon.fill", success: false)
            presentError(message)
        }
    }

    // MARK: - Menu

    private func showMenu() {
        let menu = NSMenu()

        let folderItem = NSMenuItem(title: "Saves → \(logger.folderURL.path)", action: nil, keyEquivalent: "")
        folderItem.isEnabled = false
        menu.addItem(folderItem)
        menu.addItem(.separator())

        addItem(to: menu, title: "Save Clipboard Now", action: #selector(menuSave), key: "s")
        addItem(to: menu, title: "Open Folder", action: #selector(openFolder), key: "o")
        addItem(to: menu, title: "Change Folder…", action: #selector(changeFolder), key: "")
        menu.addItem(.separator())

        let loginItem = addItem(to: menu, title: "Launch at Login", action: #selector(toggleLaunchAtLogin), key: "")
        loginItem.state = (SMAppService.mainApp.status == .enabled) ? .on : .off
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
        let folder = logger.folderURL
        try? FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
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
        panel.directoryURL = logger.folderURL
        NSApp.activate(ignoringOtherApps: true)
        if panel.runModal() == .OK, let url = panel.url {
            logger.setFolder(url)
        }
    }

    @objc private func toggleLaunchAtLogin() {
        do {
            if SMAppService.mainApp.status == .enabled {
                try SMAppService.mainApp.unregister()
            } else {
                try SMAppService.mainApp.register()
            }
        } catch {
            presentError("Could not change the launch-at-login setting:\n\(error.localizedDescription)")
        }
    }

    /// Registers as a login item the first time the app runs (the user opted in).
    /// Guarded by a flag so the user can later disable it via the menu and have it stay off.
    private func enableLaunchAtLoginOnFirstRun() {
        let key = "didConfigureLoginItem"
        guard !UserDefaults.standard.bool(forKey: key) else { return }
        UserDefaults.standard.set(true, forKey: key)
        if SMAppService.mainApp.status != .enabled {
            try? SMAppService.mainApp.register()
        }
    }

    // MARK: - Feedback

    private func flash(symbol: String, success: Bool) {
        statusItem.button?.image = NSImage(systemSymbolName: symbol, accessibilityDescription: nil)
        NSSound(named: success ? "Pop" : "Funk")?.play()
        DispatchQueue.main.asyncAfter(deadline: .now() + 1.1) { [weak self] in
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
        let image = NSImage(systemSymbolName: "doc.on.clipboard", accessibilityDescription: "ClipRef")
        image?.isTemplate = true
        return image
    }
}
