import Foundation

public struct UploadError: LocalizedError {
    public let message: String
    public var errorDescription: String? { message }
}

/// Загрузка через системные /usr/bin/scp и /usr/bin/ssh, как в скрипте.
public struct Uploader {
    let config: Config

    public init(config: Config) {
        self.config = config
    }

    private var sshOptions: [String] {
        [
            "-i", (config.keyPath as NSString).expandingTildeInPath,
            "-o", "BatchMode=yes",
            "-o", "StrictHostKeyChecking=accept-new",
            "-o", "ConnectTimeout=10",
        ]
    }

    public func upload(_ local: URL, as remoteName: String) async throws {
        let args = ["-P", String(config.port)] + sshOptions + [local.path, config.remoteTarget(fileName: remoteName)]
        let (status, output) = try await run("/usr/bin/scp", args)
        guard status == 0 else {
            throw UploadError(message: output.isEmpty ? "scp завершился с кодом \(status)" : output)
        }
    }

    /// Проверяет, что сервер доступен по ключу и папка на нём существует и доступна для записи.
    public func testConnection() async throws {
        let dir = config.uploadDir.replacingOccurrences(of: "'", with: "'\\''")
        let args = ["-p", String(config.port)] + sshOptions + ["\(config.user)@\(config.host)", "test -d '\(dir)' && test -w '\(dir)'"]
        let (status, output) = try await run("/usr/bin/ssh", args)
        switch status {
        case 0: return
        case 1: throw UploadError(message: "Соединение есть, но папка \(config.uploadDir) не найдена или недоступна для записи")
        default: throw UploadError(message: output.isEmpty ? "ssh завершился с кодом \(status)" : output)
        }
    }

    private func run(_ executable: String, _ arguments: [String]) async throws -> (Int32, String) {
        try await withCheckedThrowingContinuation { continuation in
            let process = Process()
            process.executableURL = URL(fileURLWithPath: executable)
            process.arguments = arguments
            process.standardInput = FileHandle.nullDevice
            let pipe = Pipe()
            process.standardOutput = pipe
            process.standardError = pipe
            process.terminationHandler = { finished in
                let data = pipe.fileHandleForReading.readDataToEndOfFile()
                let output = String(decoding: data, as: UTF8.self).trimmingCharacters(in: .whitespacesAndNewlines)
                continuation.resume(returning: (finished.terminationStatus, output))
            }
            do {
                try process.run()
            } catch {
                continuation.resume(throwing: error)
            }
        }
    }
}
