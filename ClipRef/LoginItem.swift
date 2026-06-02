import ServiceManagement

/// Launch-at-login state for the app, wrapping `SMAppService.mainApp`.
enum LoginItem {
    static var isEnabled: Bool {
        SMAppService.mainApp.status == .enabled
    }

    /// Registers if currently disabled, unregisters if enabled. Throws on failure.
    static func toggle() throws {
        if isEnabled {
            try SMAppService.mainApp.unregister()
        } else {
            try SMAppService.mainApp.register()
        }
    }

    /// Registers as a login item the first time the app runs (the user opted in).
    /// Guarded by a flag so a later manual disable stays disabled.
    static func enableOnFirstRun() {
        let key = "didConfigureLoginItem"
        guard !UserDefaults.standard.bool(forKey: key) else { return }
        UserDefaults.standard.set(true, forKey: key)
        if !isEnabled {
            try? SMAppService.mainApp.register()
        }
    }
}
