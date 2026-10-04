// Encodes a folder of PNG frames (sorted by name) into an H.264 MP4 with macOS's own AVFoundation (round 19).
// No package or download: the mockup's motion has to play where a GIF shows only its first frame.
//   swift ArtPipeline/Archive/19-motion-test/encode_mp4.swift <frames folder> <fps> <out.mp4>
import AVFoundation
import CoreGraphics
import Foundation
import ImageIO

let arguments = CommandLine.arguments
guard arguments.count == 4, let fps = Int32(arguments[2]) else {
    print("usage: swift encode_mp4.swift <frames folder> <fps> <out.mp4>")
    exit(1)
}

let folder = URL(fileURLWithPath: arguments[1])
let output = URL(fileURLWithPath: arguments[3])
let frames = try FileManager.default.contentsOfDirectory(at: folder, includingPropertiesForKeys: nil)
    .filter { $0.pathExtension == "png" }
    .sorted { $0.lastPathComponent < $1.lastPathComponent }
guard !frames.isEmpty else {
    print("no frames in \(folder.path)")
    exit(1)
}

func image(_ url: URL) -> CGImage {
    let source = CGImageSourceCreateWithURL(url as CFURL, nil)!
    return CGImageSourceCreateImageAtIndex(source, 0, nil)!
}

let first = image(frames[0])
let width = first.width
let height = first.height
try? FileManager.default.removeItem(at: output)

let writer = try AVAssetWriter(outputURL: output, fileType: .mp4)
let input = AVAssetWriterInput(mediaType: .video, outputSettings: [
    AVVideoCodecKey: AVVideoCodecType.h264,
    AVVideoWidthKey: width,
    AVVideoHeightKey: height,
    AVVideoCompressionPropertiesKey: [
        AVVideoAverageBitRateKey: 8_000_000,
        AVVideoProfileLevelKey: AVVideoProfileLevelH264HighAutoLevel,
    ],
])
input.expectsMediaDataInRealTime = false
let adaptor = AVAssetWriterInputPixelBufferAdaptor(assetWriterInput: input, sourcePixelBufferAttributes: [
    kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32ARGB,
    kCVPixelBufferWidthKey as String: width,
    kCVPixelBufferHeightKey as String: height,
])
writer.add(input)
guard writer.startWriting() else {
    print("could not start: \(String(describing: writer.error))")
    exit(1)
}
writer.startSession(atSourceTime: .zero)

let sRGB = CGColorSpace(name: CGColorSpace.sRGB)!
for (index, url) in frames.enumerated() {
    while !input.isReadyForMoreMediaData {
        Thread.sleep(forTimeInterval: 0.005)
    }
    var made: CVPixelBuffer?
    CVPixelBufferPoolCreatePixelBuffer(nil, adaptor.pixelBufferPool!, &made)
    let buffer = made!
    CVPixelBufferLockBaseAddress(buffer, [])
    let context = CGContext(data: CVPixelBufferGetBaseAddress(buffer), width: width, height: height, bitsPerComponent: 8,
                            bytesPerRow: CVPixelBufferGetBytesPerRow(buffer), space: sRGB,
                            bitmapInfo: CGImageAlphaInfo.noneSkipFirst.rawValue)!
    context.draw(image(url), in: CGRect(x: 0, y: 0, width: width, height: height))
    CVPixelBufferUnlockBaseAddress(buffer, [])
    adaptor.append(buffer, withPresentationTime: CMTime(value: CMTimeValue(index), timescale: fps))
}

input.markAsFinished()
let done = DispatchSemaphore(value: 0)
writer.finishWriting { done.signal() }
done.wait()
if writer.status == .completed {
    print("wrote \(output.path): \(frames.count) frames, \(width)x\(height), \(fps) fps")
} else {
    print("failed: \(String(describing: writer.error))")
    exit(1)
}
