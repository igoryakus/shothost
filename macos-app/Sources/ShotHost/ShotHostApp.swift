import AppKit
import SwiftUI
import UniformTypeIdentifiers

@main
struct ShotHostApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) private var appDelegate
    @StateObject private var state = AppState()

    var body: some Scene {
        MenuBarExtra {
            MenuContent(state: state)
        } label: {
            Image(systemName: state.activeUploads > 0 ? "arrow.up.circle.fill"
                : (state.isWatching ? "camera.viewfinder" : "pause.circle"))
        }
    }
}

/// У приложения только значок в строке меню, и его может быть не видно: спрятан за вырезом экрана
/// или не разрешён в «Системные настройки → Строка меню». Поэтому повторный запуск из «Программ»
/// открывает настройки, иначе кажется, что приложение не запускается.
final class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool {
        if let state = AppState.shared {
            SettingsWindow.show(state: state)
        }
        return false
    }
}

struct MenuContent: View {
    @ObservedObject var state: AppState

    var body: some View {
        if state.activeUploads > 0 {
            Text("Загрузка… (\(state.activeUploads))")
        } else if let error = state.watcherError {
            Text(error)
        } else if !state.config.isComplete {
            Text("Нужно заполнить настройки")
        } else if state.isPaused {
            Text("Слежение на паузе")
        } else {
            Text("Слежу за папкой \((state.config.watchDir as NSString).lastPathComponent)")
        }

        Divider()

        Toggle("Пауза", isOn: $state.isPaused)
            .disabled(!state.config.isComplete)
        Button("Загрузить файл…") { chooseFiles() }
            .keyboardShortcut("u")

        Menu("Последние загрузки") {
            if state.recent.isEmpty {
                Text("Пока пусто")
            } else {
                ForEach(state.recent) { item in
                    Button(item.name) { state.copyToClipboard(item.url) }
                }
                Divider()
                Button("Очистить") { state.clearRecent() }
            }
        }

        Divider()

        Button("Настройки…") { SettingsWindow.show(state: state) }
            .keyboardShortcut(",")
        Button("Выйти из ShotHost") { NSApp.terminate(nil) }
            .keyboardShortcut("q")
    }

    private func chooseFiles() {
        NSApp.activate(ignoringOtherApps: true)
        let panel = NSOpenPanel()
        panel.allowsMultipleSelection = true
        panel.canChooseDirectories = false
        panel.allowedContentTypes = [.image]
        panel.prompt = "Загрузить"
        if panel.runModal() == .OK {
            state.uploadFiles(panel.urls)
        }
    }
}
