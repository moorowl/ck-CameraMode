using CameraMode.Capture;
using Pug.UnityExtensions;
using Unity.Mathematics;
using UnityEngine;

namespace CameraMode.UserInterface {
	public class CaptureProgressUI : UIelement {
		public GameObject root;
		public GameObject textContainer;
		public SpriteRenderer background;
		public PugText progressText;
		public PugText areasCapturedText;
		public PugText inCameraModeText;
		
		public float fadeOutSpeed = 5f;
		public float fadeInSpeed = 25f;

		private float _opacity;
		private bool _isFadingIn;
		private bool _isFadingOut;

		private bool _wasCapturing;
		
		protected override void LateUpdate() {
			var captureManager = CaptureManager.Instance;
			var isCapturing = captureManager.IsCapturing;
			
			if (isCapturing && !_wasCapturing)
				_isFadingIn = true;
			if (!isCapturing && _wasCapturing)
				_isFadingOut = true;
			
			if (_isFadingIn) {
				_opacity = math.lerp(_opacity, 1f, fadeInSpeed * Time.deltaTime);
				if (math.distance(_opacity, 1f) < 0.005f) {
					_opacity = 1f;
					_isFadingIn = false;
				}
			} else if (_isFadingOut) {
				_opacity = math.lerp(_opacity, 0f, fadeOutSpeed * Time.deltaTime);
				if (math.distance(_opacity, 0f) < 0.005f) {
					_opacity = 0f;
					_isFadingOut = false;
				}
			}

			if (_opacity > 0f) {
				root.SetActive(true);
				textContainer.SetActive(!Manager.menu.IsAnyMenuActive() && captureManager.AreasToCapture > 0);
				
				if (isCapturing) {
					progressText.Render($"{math.ceil(captureManager.CaptureProgress * 100f)}%");
					areasCapturedText.Render($"({captureManager.AreasCaptured}/{captureManager.AreasToCapture})");
				}
				
				background.color = background.color.ColorWithNewAlpha(_opacity);
				inCameraModeText.SetTempColor(inCameraModeText.color.ColorWithNewAlpha(_opacity));
				progressText.SetTempColor(progressText.color.ColorWithNewAlpha(_opacity));
				areasCapturedText.SetTempColor(progressText.color.ColorWithNewAlpha(_opacity));
			} else {
				root.SetActive(false);
			}

			_wasCapturing = isCapturing;
		}
	}
}