using System.Collections.Generic;
using CameraMode.Capture;
using Pug.UnityExtensions;

namespace CameraMode.UserInterface.Options {
	public class CaptureQualityMenuOption : RadicalMenuOption {
		private static readonly List<CaptureQuality> Options = new() {
			CaptureQuality.Lossless,
			CaptureQuality.Compressed
		};
		
		public override void OnParentMenuActivation() {
			SetLevel(Options.IndexOf(Config.Instance.CaptureQuality));
			base.OnParentMenuActivation();
		}
		
		public override void OnActivated() {
			base.OnActivated();
			OnSkimRight();
		}

		public override bool OnSkimRight() {
			ChangeLevel(1);
			return true;
		}

		public override bool OnSkimLeft() {
			ChangeLevel(-1);
			return true;
		}

		private void ChangeLevel(int amount) {
			SetLevel((Options.IndexOf(Config.Instance.CaptureQuality) + amount) % Options.Count);
		}

		private void SetLevel(int level) {
			Config.Instance.CaptureQuality = Options.IsValidIndex(level) ? Options[level] : Options[0];
			UpdateText(Config.Instance.CaptureQuality);
		}

		private void UpdateText(CaptureQuality quality) {
			valueText.Render($"CameraMode:CaptureQuality/{quality}");
		}
	}
}