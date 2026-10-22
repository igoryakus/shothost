import AppKit
import SwiftUI

/// Окно после загрузки, как диалог в shothost.sh: «ОК» или «Показать», само закрывается через 10 секунд.
@MainActor
enum LinkWindow {
    private static var window: NSPanel?
    private static var closeTask: Task<Void, Never>?

    static func show(link: String) {
        close()
        let view = LinkView(link: link, onOpen: {
            if let url = URL(string: link) { NSWorkspace.shared.open(url) }
            close()
        }, onClose: close)
        let panel = NSPanel(contentViewController: NSHostingController(rootView: view))
        panel.title = "Скриншот загружен"
        panel.styleMask = [.titled, .closable]
        panel.level = .floating
        panel.isReleasedWhenClosed = false
        panel.center()
        window = panel
        NSApp.activate(ignoringOtherApps: true)
        panel.makeKeyAndOrderFront(nil)
        closeTask = Task {
            try? await Task.sleep(nanoseconds: 10_000_000_000)
            if !Task.isCancelled { close() }
        }
    }

    private static func close() {
        closeTask?.cancel()
        closeTask = nil
        window?.close()
        window = nil
    }
}

private struct LinkView: View {
    let link: String
    let onOpen: () -> Void
    let onClose: () -> Void

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Ссылка скопирована в буфер обмена")
                .font(.headline)
            Text(link)
                .font(.callout)
                .foregroundStyle(.secondary)
                .textSelection(.enabled)
                .lineLimit(2)
                .truncationMode(.middle)
            HStack {
                Spacer()
                Button("ОК", action: onClose)
                    .keyboardShortcut(.cancelAction)
                Button("Показать", action: onOpen)
                    .keyboardShortcut(.defaultAction)
            }
        }
        .padding(20)
        .frame(width: 380)
    }
}
