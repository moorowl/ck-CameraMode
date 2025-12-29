using System.Collections.Generic;

namespace CameraMode.UserInterface.Settings {
	// ReSharper disable once InconsistentNaming
	public class SettingsCycleButtonUI_Fullbright : SettingsCycleButtonUI<bool> {
		protected override List<bool> Values => new() {
			false, true
		};
		protected override bool DefaultValue => false;
		
		protected override bool GetConfigValue() {
			return Config.Instance.CaptureFullbright;
		}

		protected override void SetConfigValue(bool value) {
			Config.Instance.CaptureFullbright = value;
		}

		protected override void UpdateText() {
			valueText.Render(GetConfigValue() ? "CameraMode:CaptureLighting/Fullbright" : "CameraMode:CaptureLighting/Normal");
		}
	}
}