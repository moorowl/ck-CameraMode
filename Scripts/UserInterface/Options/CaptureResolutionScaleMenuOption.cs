using System.Collections.Generic;
using Pug.UnityExtensions;

namespace CameraMode.UserInterface.Options {
	public class CaptureResolutionScaleMenuOption : RadicalMenuOption {
		private static readonly List<int> Options = new() {
			1,
			2,
			4,
			8
		};
		
		public override void OnParentMenuActivation() {
			SetLevel(Options.IndexOf(Config.Instance.CaptureResolutionScale));
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
			SetLevel((Options.IndexOf(Config.Instance.CaptureResolutionScale) + amount) % Options.Count);
		}

		private void SetLevel(int level) {
			Config.Instance.CaptureResolutionScale = Options.IsValidIndex(level) ? Options[level] : Options[1];
			UpdateText(Config.Instance.CaptureResolutionScale);
		}

		private void UpdateText(int resolutionScale) {
			valueText.Render($"{resolutionScale}x");
		}
	}
}