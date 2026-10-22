import Foundation

/// Настройки, которые раньше правились в начале shothost.sh.
public struct Config: Codable, Equatable {
    /// Папка, в которую делаются скриншоты.
    public var watchDir = ""
    /// IP или домен сервера.
    public var host = ""
    public var port = 22
    public var user = ""
    /// Папка проекта ShotHost на сервере; файлы загружаются в её подпапку files/.
    /// Ключ остался прежним, чтобы читались сохранённые настройки.
    public var remoteDir = "/var/www/html/shothost/"
    /// Приватный SSH-ключ (без .pub).
    public var keyPath = ""
    /// Сайт, на котором открывается ShotHost: https://домен или IP, без пути.
    /// Путь в ссылке берётся из названия папки проекта, поэтому ссылка и папка не могут разойтись.
    public var baseURL = ""

    public init() {}

    public var isComplete: Bool {
        ![watchDir, host, user, remoteDir, keyPath, baseURL].contains { $0.trimmingCharacters(in: .whitespaces).isEmpty }
    }

    /// Название папки проекта, оно же путь на сайте: /var/www/html/shothost/ → shothost.
    public var projectName: String {
        (remoteDir as NSString).lastPathComponent
    }

    public func link(forStem stem: String) -> String {
        // Если в поле всё же вписали адрес с путём, путь отбрасывается.
        var site = Config.site(fromStored: baseURL)
        while site.hasSuffix("/") { site.removeLast() }
        return "\(site)/\(projectName)/\(stem)"
    }

    /// Папка, куда загружаются файлы: files/ внутри папки проекта.
    public var uploadDir: String {
        (remoteDir.hasSuffix("/") ? remoteDir : remoteDir + "/") + "files/"
    }

    func remoteTarget(fileName: String) -> String {
        "\(user)@\(host):\(uploadDir)\(fileName)"
    }

    /// Раньше в настройках указывалась сама папка files/, теперь папка проекта.
    static func projectDir(fromStored path: String) -> String {
        var dir = path
        while dir.count > 1, dir.hasSuffix("/") { dir.removeLast() }
        guard (dir as NSString).lastPathComponent == "files" else { return path }
        return (dir as NSString).deletingLastPathComponent + "/"
    }

    /// Раньше в адресе указывался и путь (https://example.com/shothost/), теперь только сайт.
    static func site(fromStored address: String) -> String {
        guard var components = URLComponents(string: address.trimmingCharacters(in: .whitespaces)),
              components.host != nil else { return address }
        components.path = ""
        components.query = nil
        components.fragment = nil
        return components.string ?? address
    }

    private static let defaultsKey = "config"

    public static func load(from defaults: UserDefaults = .standard) -> Config {
        guard let data = defaults.data(forKey: defaultsKey),
              var config = try? JSONDecoder().decode(Config.self, from: data) else { return Config() }
        config.remoteDir = projectDir(fromStored: config.remoteDir)
        config.baseURL = site(fromStored: config.baseURL)
        return config
    }

    public func save(to defaults: UserDefaults = .standard) {
        if let data = try? JSONEncoder().encode(self) {
            defaults.set(data, forKey: Self.defaultsKey)
        }
    }
}
