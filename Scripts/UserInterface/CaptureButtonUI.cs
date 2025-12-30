using System.Collections.Generic;
using CameraMode.Capture;
using UnityEngine;

namespace CameraMode.UserInterface {
	public class CaptureButtonUI : ToggleUIElement {
		public static float LastPressedTime { get; private set; }

		public bool canBeToggled;
		public GameObject optionalToggledMarker;
		
		private DisabledReason _disabledReason;

		public void SetEnabled() {
			canBeClicked = true;
			_disabledReason = DisabledReason.None;
		}
		
		public void SetDisabled(DisabledReason disabledReason = DisabledReason.None) {
			canBeClicked = false;
			_disabledReason = disabledReason;
		}
		
		public override void OnLeftClicked(bool mod1, bool mod2) {
			if (CaptureManager.Instance.IsCapturing || !canBeClicked) {
				AudioManager.SfxUI(SfxID.menu_denied, 1.15f, false, 0.5f, 0.05f);
				return;
			}
			
			base.OnLeftClicked(mod1, mod2);
			
			LastPressedTime = Time.time;
		}

		protected override void LateUpdate() {
			base.LateUpdate();
			
			if (optionalSelectedMarker != null && optionalSelectedMarker.activeSelf && !canBeClicked)
				optionalSelectedMarker.SetActive(false);
			
			if (optionalToggledMarker != null)
				optionalToggledMarker.SetActive(canBeClicked && (!canBeToggled || isOn || leftClickIsHeldDown));
		}
		
		public override HoverWindowAlignment GetHoverWindowAlignment() {
			return HoverWindowAlignment.BOTTOM_RIGHT_OF_CURSOR;
		}
		
		public override List<TextAndFormatFields> GetHoverDescription() {
			if (showHoverDesc) {
				var lines = new List<TextAndFormatFields> {
					new() {
						text = optionalHoverDesc.mTerm,
						color = Manager.text.GetRarityColor(Rarity.Poor)
					}
				};
				
				if (!canBeClicked && _disabledReason != DisabledReason.None) {
					lines.Add(new TextAndFormatFields {
						text = "CameraMode:DisabledReason/" + _disabledReason,
						color = Manager.ui.brokenColor
					});
				}

				return lines;
			}
			return base.GetHoverDescription();
		}

		public enum DisabledReason {
			None,
			NoFrameSet
		}
	}
}