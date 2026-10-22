import AppKit
import ShotHostCore

struct RecentUpload: Codable, Identifiable, Equatable {
    var id: String { url }
    let name: String
    let url: String
    let date: Date
}

@MainActor
final class AppState: ObservableObject {
    /// Для AppDelegate, которому SwiftUI не передаёт состояние приложения.
    static private(set) weak var shared: AppState?

    @Published private(set) var config: Config
    @Published var isPaused: Bool {
        didSet {
            UserDefaults.standard.set(isPaused, forKey: "isPaused")
            restartWatcher()
        }
    }
    /// После загрузки показывать окно со ссылкой (как в shothost.sh), а не уведомление.
    @Published var showLinkWindow: Bool {
        didSet { UserDefaults.standard.set(showLinkWindow, forKey: "showLinkWindow") }
    }
    @Published private(set) var recent: [RecentUpload]
    @Published private(set) var activeUploads = 0
    @Published private(set) var watcherError: String?

    private var watcher: FolderWatcher?
    private let notifier = Notifier()

    init() {
        config = Config.load()
        defer { AppState.shared = self }
        isPaused = UserDefaults.standard.bool(forKey: "isPaused")
        showLinkWindow = UserDefaults.standard.object(forKey: "showLinkWindow") as? Bool ?? true
        if let data = UserDefaults.standard.data(forKey: "recent"),
           let saved = try? JSONDecoder().decode([RecentUpload].self, from: data) {
            recent = saved
        } else {
            recent = []
        }
        notifier.requestAuthorization()
        restartWatcher()
        if !config.isComplete {
            Task { @MainActor in SettingsWindow.show(state: self) }
        }
    }

    var isWatching: Bool { watcher != nil }

    func update(config newConfig: Config) {
        config = newConfig
        newConfig.save()
        restartWatcher()
    }

    private func restartWatcher() {
        watcher?.stop()
        watcher = nil
        watcherError = nil
        guard !isPaused, config.isComplete else { return }
        let dir = URL(fileURLWithPath: (config.watchDir as NSString).expandingTildeInPath, isDirectory: true)
        let newWatcher = FolderWatcher(directory: dir) { [weak self] url in
            Task { @MainActor in await self?.processWatched(url) }
        }
        do {
            try newWatcher.start()
            watcher = newWatcher
        } catch {
            watcherError = error.localizedDescription
        }
    }

    /// Новый скриншот в папке: переименовать рядом, как делал скрипт, и загрузить.
    private func processWatched(_ url: URL) async {
        await waitUntilWritten(url)
        guard FileManager.default.fileExists(atPath: url.path),
              let name = Naming.uploadName(for: url.lastPathComponent) else { return }
        let renamed = url.deletingLastPathComponent().appendingPathComponent(name.fileName)
        do {
            try FileManager.default.moveItem(at: url, to: renamed)
        } catch {
            notifier.showError("Не удалось переименовать \(url.lastPathComponent): \(error.localizedDescription)")
            return
        }
        await upload(renamed, name: name)
    }

    /// Загрузка выбранных вручную файлов: оригинал не переименовывается.
    func uploadFiles(_ urls: [URL]) {
        guard config.isComplete else {
            SettingsWindow.show(state: self)
            return
        }
        for url in urls {
            guard let name = Naming.uploadName(for: url.lastPathComponent) else { continue }
            Task { await upload(url, name: name) }
        }
    }

    private func upload(_ file: URL, name: (stem: String, fileName: String)) async {
        let link = config.link(forStem: name.stem)
        // Ссылка нужна сразу, пока файл ещё едет на сервер.
        copyToClipboard(link)
        activeUploads += 1
        defer { activeUploads -= 1 }
        do {
            try await Uploader(config: config).upload(file, as: name.fileName)
            remember(RecentUpload(name: name.fileName, url: link, date: Date()))
            if showLinkWindow {
                LinkWindow.show(link: link)
            } else {
                notifier.showUploaded(link)
            }
        } catch {
            notifier.showError("Не удалось загрузить \(name.fileName): \(error.localizedDescription)")
        }
    }

    func copyToClipboard(_ text: String) {
        NSPasteboard.general.clearContents()
        NSPasteboard.general.setString(text, forType: .string)
    }

    func clearRecent() {
        recent = []
        saveRecent()
    }

    private func remember(_ item: RecentUpload) {
        recent.insert(item, at: 0)
        recent = Array(recent.prefix(10))
        saveRecent()
    }

    private func saveRecent() {
        if let data = try? JSONEncoder().encode(recent) {
            UserDefaults.standard.set(data, forKey: "recent")
        }
    }

    /// Ждём, пока размер файла перестанет меняться (скриншот может записываться не мгновенно).
    private func waitUntilWritten(_ url: URL) async {
        var lastSize: Int?
        for _ in 0..<20 {
            let size = (try? url.resourceValues(forKeys: [.fileSizeKey]).fileSize) ?? 0
            if size > 0, size == lastSize { return }
            lastSize = size
            try? await Task.sleep(nanoseconds: 250_000_000)
        }
    }
}
