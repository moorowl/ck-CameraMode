using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace CameraMode.Capture {
	public enum CaptureQuality {
		Lossless,
		Compressed
	}
	
	public static class CaptureQualityExtensions {
		public static string GetFileExtension(this CaptureQuality quality) {
			return quality switch {
				CaptureQuality.Lossless => "png",
				CaptureQuality.Compressed => "jpg",
				_ => throw new ArgumentOutOfRangeException()
			};
		}
		
		public static byte[] EncodeArrayToImage(this CaptureQuality quality, byte[] data, GraphicsFormat format, uint width, uint height, uint rowBytes = 0u) {
			return quality switch {
				CaptureQuality.Lossless => ImageConversion.EncodeArrayToPNG(data, format, width, height, rowBytes),
				CaptureQuality.Compressed => ImageConversion.EncodeArrayToJPG(data, format, width, height, rowBytes),
				_ => throw new ArgumentOutOfRangeException()
			};
		}
	}
}