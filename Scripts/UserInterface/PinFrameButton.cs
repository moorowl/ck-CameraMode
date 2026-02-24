using System.Collections.Generic;
using CameraMode.Utilities;

namespace CameraMode.UserInterface {
	public class PinFrameButton : CaptureButtonUI {
		public override List<TextAndFormatFields> GetHoverDescription() {
            if (!canBeClicked)
                return base.GetHoverDescription();
            
			var lines = base.GetHoverDescription() ?? new List<TextAndFormatFields>();

			Utils.AppendButtonHint(lines, "CameraMode-ButtonHints/SetPinA", "UIInteract");
			Utils.AppendButtonHint(lines, "CameraMode-ButtonHints/SetPinB", "UISecondInteract");
			
			return lines;
		}
	}
}