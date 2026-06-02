import XCTest
@testable import ClipRef

final class ClipboardSaverTests: XCTestCase {

    // MARK: - decide(text:hasImage:)

    func testTextIsSaved() {
        XCTAssertEqual(ClipboardSaver.decide(fileURL: nil, text: "hello", hasImage: false), .saveText("hello"))
    }

    func testTextWinsOverImage() {
        XCTAssertEqual(ClipboardSaver.decide(fileURL: nil, text: "hello", hasImage: true), .saveText("hello"))
    }

    func testImageSavedWhenNoText() {
        XCTAssertEqual(ClipboardSaver.decide(fileURL: nil, text: nil, hasImage: true), .saveImage)
        XCTAssertEqual(ClipboardSaver.decide(fileURL: nil, text: "", hasImage: true), .saveImage)
    }

    func testEmptyClipboardIgnored() {
        XCTAssertEqual(ClipboardSaver.decide(fileURL: nil, text: nil, hasImage: false), .ignore)
        XCTAssertEqual(ClipboardSaver.decide(fileURL: nil, text: "", hasImage: false), .ignore)
    }

    func testReferenceIgnoredEvenWithImage() {
        XCTAssertEqual(ClipboardSaver.decide(fileURL: nil, text: "@/Users/me/clip.txt", hasImage: false), .ignore)
        XCTAssertEqual(ClipboardSaver.decide(fileURL: nil, text: "@/Users/me/clip.png", hasImage: true), .ignore)
    }

    // MARK: - decide: files

    func testFileIsCopied() {
        let url = URL(fileURLWithPath: "/tmp/report.pdf")
        XCTAssertEqual(ClipboardSaver.decide(fileURL: url, text: nil, hasImage: false), .copyFile(url))
    }

    func testFileWinsOverTextAndImage() {
        let url = URL(fileURLWithPath: "/tmp/clip.mov")
        XCTAssertEqual(ClipboardSaver.decide(fileURL: url, text: "ignored text", hasImage: true), .copyFile(url))
    }

    func testFoldersAreNotCopyable() throws {
        let dir = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: dir) }
        let file = dir.appendingPathComponent("note.txt")
        try Data("hi".utf8).write(to: file)
        XCTAssertTrue(ClipboardSaver.isCopyableFile(file))
        XCTAssertFalse(ClipboardSaver.isCopyableFile(dir))   // a folder/app bundle
    }

    // MARK: - looksLikeReference

    func testReferenceDetection() {
        XCTAssertTrue(ClipboardSaver.looksLikeReference("@/Users/me/clip.txt"))
        XCTAssertTrue(ClipboardSaver.looksLikeReference("  @~/Developer/x.png  "))   // trimmed
        XCTAssertFalse(ClipboardSaver.looksLikeReference("@here standup notes"))     // whitespace
        XCTAssertFalse(ClipboardSaver.looksLikeReference("@username"))               // no path
        XCTAssertFalse(ClipboardSaver.looksLikeReference("plain text"))
        XCTAssertFalse(ClipboardSaver.looksLikeReference(""))
    }

    // MARK: - uniqueURL

    func testUniqueURLNamingAndCollision() throws {
        let dir = FileManager.default.temporaryDirectory
            .appendingPathComponent("ClipRefTests-\(UUID().uuidString)", isDirectory: true)
        try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: dir) }

        let first = ClipboardSaver.uniqueURL(in: dir, extension: "txt")
        XCTAssertEqual(first.pathExtension, "txt")
        let stem = first.deletingPathExtension().lastPathComponent
        XCTAssertNotNil(
            stem.range(of: #"^clip-\d{4}-\d{2}-\d{2}_\d{2}\.\d{2}\.\d{2}$"#, options: .regularExpression),
            "unexpected name: \(stem)"
        )

        // Once that file exists, the next URL must differ (and not already exist).
        try Data("x".utf8).write(to: first)
        let second = ClipboardSaver.uniqueURL(in: dir, extension: "txt")
        XCTAssertNotEqual(first, second)
        XCTAssertFalse(FileManager.default.fileExists(atPath: second.path))
    }

    func testUniqueURLKeepsOriginalNameAndCollidesFinderStyle() throws {
        let dir = FileManager.default.temporaryDirectory
            .appendingPathComponent("ClipRefTests-\(UUID().uuidString)", isDirectory: true)
        try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: dir) }

        // First save keeps the original name verbatim.
        let first = ClipboardSaver.uniqueURL(in: dir, preferredName: "report.pdf")
        XCTAssertEqual(first.lastPathComponent, "report.pdf")

        // On collision, a Finder-style " 2", " 3" … lands before the extension.
        try Data("a".utf8).write(to: first)
        let second = ClipboardSaver.uniqueURL(in: dir, preferredName: "report.pdf")
        XCTAssertEqual(second.lastPathComponent, "report 2.pdf")

        try Data("b".utf8).write(to: second)
        let third = ClipboardSaver.uniqueURL(in: dir, preferredName: "report.pdf")
        XCTAssertEqual(third.lastPathComponent, "report 3.pdf")
    }

    func testUniqueURLPreferredNameWithoutExtension() throws {
        let dir = FileManager.default.temporaryDirectory
            .appendingPathComponent("ClipRefTests-\(UUID().uuidString)", isDirectory: true)
        try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: dir) }

        let first = ClipboardSaver.uniqueURL(in: dir, preferredName: "Makefile")
        XCTAssertEqual(first.lastPathComponent, "Makefile")
        try Data("x".utf8).write(to: first)
        let second = ClipboardSaver.uniqueURL(in: dir, preferredName: "Makefile")
        XCTAssertEqual(second.lastPathComponent, "Makefile 2")
    }
}
