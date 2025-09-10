namespace CameraMode.Capture {
	public class CaptureSettings {
		public string Name { get; set; }
		public CaptureFrame Frame { get; set; }
		public int ResolutionScale { get; set; } = 2;
		public CaptureQuality Quality { get; set; } = CaptureQuality.Uncompressed;
	}
}