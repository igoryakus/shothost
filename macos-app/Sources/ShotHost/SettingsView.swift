import AppKit
import ServiceManagement
import ShotHostCore
import SwiftUI

/// Окно настроек открывается из AppKit: у приложения в строке меню нет обычных окон,
/// и так оно надёжно выходит на передний план.
@MainActor
enum SettingsWindow {
    private static var window: NSWindow?

    static func show(state: AppState) {
        // Каждый раз новое окно, чтобы форма показывала сохранённые настройки, а не старый черновик.
        if window?.isVisible != true {
            let controller = NSHostingController(rootView: SettingsView(state: state))
            let newWindow = NSWindow(contentViewController: controller)
            newWindow.title = "Настройки ShotHost"
            newWindow.styleMask = [.titled, .closable]
            newWindow.isReleasedWhenClosed = false
            newWindow.center()
            window = newWindow
        }
        NSApp.activate(ignoringOtherApps: true)
        window?.makeKeyAndOrderFront(nil)
    }
}

struct SettingsView: View {
    @ObservedObject var state: AppState
    @State private var draft: Config
    @State private var checkResult: String?
    @State private var isChecking = false
    @State private var launchAtLogin = SMAppService.mainApp.status == .enabled

    init(state: AppState) {
        self.state = state
        _draft = State(initialValue: state.config)
    }

    var body: some View {
        Form {
            Section("Скриншоты") {
                pathRow("Папка", path: $draft.watchDir, chooseDirectory: true)
                Text("Новые файлы в этой папке загружаются автоматически.")
                    .font(.caption).foregroundStyle(.secondary)
            }

            Section("Сервер") {
                TextField("Сервер (IP или домен)", text: $draft.host, prompt: Text("203.0.113.10"))
                TextField("SSH-порт", value: $draft.port, format: .number.grouping(.never))
                TextField("Пользователь", text: $draft.user, prompt: Text("root"))
                pathRow("SSH-ключ", path: $draft.keyPath, chooseDirectory: false)
            }

            Section {
                TextField("Папка ShotHost", text: $draft.remoteDir, prompt: Text("/var/www/html/shothost/"))
                TextField("Сайт", text: $draft.baseURL, prompt: Text("https://example.com"))
                Text("Файлы загружаются по SSH в \(draft.uploadDir). Ссылка строится из сайта и названия папки, поэтому адрес отдельно вводить не нужно.")
                    .font(.caption).foregroundStyle(.secondary)
                    .fixedSize(horizontal: false, vertical: true)
                if !draft.baseURL.isEmpty {
                    Text("Пример ссылки: \(draft.link(forStem: "screenshot_0123456789abcdef0123"))")
                        .font(.caption).foregroundStyle(.secondary)
                        .textSelection(.enabled)
                }
            } header: {
                Text("ShotHost на сервере")
            }

            Section {
                Toggle("Показывать окно со ссылкой после загрузки", isOn: $state.showLinkWindow)
                Text("В окне кнопка «Показать» открывает скриншот в браузере. Если выключено, приходит уведомление.")
                    .font(.caption).foregroundStyle(.secondary)
                    .fixedSize(horizontal: false, vertical: true)
                Toggle("Запускать при входе в систему", isOn: $launchAtLogin)
                    .onChange(of: launchAtLogin) { enabled in setLaunchAtLogin(enabled) }
            }

            HStack {
                Button(isChecking ? "Проверяю…" : "Проверить соединение") { check() }
                    .disabled(isChecking || draft.host.isEmpty || draft.user.isEmpty || draft.keyPath.isEmpty)
                Spacer()
                Button("Сохранить") {
                    state.update(config: draft)
                    NSApp.keyWindow?.close()
                }
                .keyboardShortcut(.defaultAction)
                .disabled(!draft.isComplete)
            }
            if let checkResult {
                Text(checkResult)
                    .font(.caption)
                    .textSelection(.enabled)
                    .fixedSize(horizontal: false, vertical: true)
            }
        }
        .formStyle(.grouped)
        // Form в стиле .grouped прокручивается и не сообщает свою высоту окну, поэтому размер задан явно.
        .frame(width: 520, height: 720)
    }

    private func pathRow(_ title: String, path: Binding<String>, chooseDirectory: Bool) -> some View {
        LabeledContent(title) {
            HStack {
                Text(path.wrappedValue.isEmpty ? "не выбрано" : (path.wrappedValue as NSString).abbreviatingWithTildeInPath)
                    .foregroundStyle(path.wrappedValue.isEmpty ? .secondary : .primary)
                    .lineLimit(1)
                    .truncationMode(.middle)
                Button("Выбрать…") {
                    if let chosen = choosePath(directory: chooseDirectory) { path.wrappedValue = chosen }
                }
            }
        }
    }

    private func choosePath(directory: Bool) -> String? {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = directory
        panel.canChooseFiles = !directory
        panel.allowsMultipleSelection = false
        if directory {
            panel.canCreateDirectories = true
        } else {
            panel.showsHiddenFiles = true
            panel.directoryURL = FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent(".ssh")
            panel.message = "Выберите приватный ключ (файл без .pub)"
        }
        return panel.runModal() == .OK ? panel.url?.path : nil
    }

    private func check() {
        isChecking = true
        checkResult = nil
        let config = draft
        Task {
            do {
                try await Uploader(config: config).testConnection()
                checkResult = "✅ Соединение работает, папка на сервере доступна для записи."
            } catch {
                checkResult = "❌ \(error.localizedDescription)"
            }
            isChecking = false
        }
    }

    private func setLaunchAtLogin(_ enabled: Bool) {
        do {
            if enabled {
                try SMAppService.mainApp.register()
            } else {
                try SMAppService.mainApp.unregister()
            }
        } catch {
            checkResult = "❌ Автозапуск: \(error.localizedDescription)"
            launchAtLogin = SMAppService.mainApp.status == .enabled
        }
    }
}
