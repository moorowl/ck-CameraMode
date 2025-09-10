using System.Collections.Generic;
using CameraMode.Capture;
using UnityEngine;

namespace CameraMode.UserInterface {
	public class CaptureButtonUI : ToggleUIElement {
		public static float LastPressedTime { get; private set; }
		
		public bool canBeToggled;
		public SpriteRenderer icon;
		public SpriteRenderer background;
		public Color onBackgroundColor;
		public Color offBackgroundColor;
		public Color canBeClickedIconColor;
		public Color cantBeClickedIconColor;
		public SpecialDescriptionType specialDescriptionType;

		private bool _isSelected;
		
		public override void OnLeftClicked(bool mod1, bool mod2) {
			if (CaptureManager.Instance.CaptureProgressUI.IsFadingInOrOut)
				return;
			
			base.OnLeftClicked(mod1, mod2);

			if (!canBeToggled && isOn)
				isOn = false;

			LastPressedTime = Time.time;
		}

		protected override void LateUpdate() {
			base.LateUpdate();

			var isSelectedOrOn = canBeClicked && (_isSelected || (canBeToggled && isOn));

			icon.color = canBeClicked ? canBeClickedIconColor : cantBeClickedIconColor;
			background.color = isSelectedOrOn ? onBackgroundColor : offBackgroundColor;
		}

		public override void OnSelected() {
			_isSelected = true;
		}

		public override void OnDeselected(bool playEffect = true) {
			_isSelected = false;
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
				
				if (specialDescriptionType == SpecialDescriptionType.CaptureFrame && !canBeClicked) {
					lines.Add(new TextAndFormatFields {
						text = "CameraMode:Functions/CaptureFrameSpecialDesc",
						color = Manager.ui.brokenColor
					});
				}

				return lines;
			}
			return base.GetHoverDescription();
		}

		public void OnValidate() {
			background.color = onBackgroundColor;
		}

		public enum SpecialDescriptionType {
			None,
			CaptureFrame
		}
	}
}