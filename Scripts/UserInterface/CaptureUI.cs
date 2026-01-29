using CameraMode.Capture;
using CameraMode.Utilities;
using HarmonyLib;
using Pug.UnityExtensions;
using PugMod;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace CameraMode.UserInterface {
	public class CaptureUI : MonoBehaviour {
		public Transform functionsContainer;
		public Transform settingsContainer;
		public PugText inCameraModeText;
		public GameObject captureFramePrefab;
		public GameObject captureMapFramePrefab;
		public CaptureButtonUI captureFrameButton;
		public CaptureButtonUI clearFrameButton;
		public CaptureButtonUI pinFrameButton;
		public CaptureButtonUI toggleMapButton;
		public CaptureButtonUI settingsButton;

		public bool IsOpen {
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}
		public CaptureFrame Frame { get; private set; }
		public Mode SelectedMode { get; private set; }
		
		public int2? PinPreview { get; private set; }
		
		private Transform _renderAnchor;
		private bool? _faceMouseDirection;
		
		private void Awake() {
			Frame = new CaptureFrame();
			_renderAnchor = Manager.camera.GetRenderAnchor();
			
			Instantiate(captureFramePrefab, _renderAnchor);
			Instantiate(captureMapFramePrefab, Manager.ui.mapUI.mapPartsContainer.transform);
			
			API.Client.OnWorldCreated += () => {
				Frame = new CaptureFrame();
			};
		}

		private void OnDestroy() {
			if (_renderAnchor != null)
				Manager.camera.ReturnRenderAnchor(_renderAnchor);
		}
		
		private void Update() {
			var forceShowInCameraModeText = CaptureManager.Instance.IsCapturing || CaptureManager.Instance.CaptureProgressUI.IsVisible;
			
			functionsContainer.localScale = Manager.ui.CalcGameplayUITargetScaleMultiplier();
			inCameraModeText.transform.localScale = forceShowInCameraModeText ? Vector3.one : functionsContainer.localScale;
			settingsContainer.gameObject.SetActive(SelectedMode == Mode.Settings);

			UpdateInputs();
			UpdateButtonStates();
		}

		private void UpdateInputs() {
			var inputModule = Manager.input.singleplayerInputModule;
			var canPinFrame = SelectedMode == Mode.PinFrame
			                  && inputModule.PrefersKeyboardAndMouse()
			                  && !Manager.ui.mapUI.IsShowingBigMap
			                  && Manager.ui.currentSelectedUIElement == null;
			
			if (!canPinFrame) {
				PinPreview = null;
				return;
			}
			
			var mouseTilePosition = EntityMonoBehaviour.ToWorldFromRender(Manager.ui.mouse.GetMouseGameViewPosition()).RoundToInt2();
			PinPreview = mouseTilePosition;
				
			if (inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.UI_INTERACT))
				Frame.PinA = mouseTilePosition;

			if (inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.UI_SECOND_INTERACT))
				Frame.PinB = mouseTilePosition;
		}

		private void UpdateButtonStates() {
			if (Frame.PinA == null && Frame.PinB == null)
				clearFrameButton.SetDisabled(CaptureButtonUI.DisabledReason.NoFrameSet);
			else
				clearFrameButton.SetEnabled();
			
			if (!Frame.IsComplete)
				captureFrameButton.SetDisabled(CaptureButtonUI.DisabledReason.NoFrameSet);
			else
				captureFrameButton.SetEnabled();
			
			pinFrameButton.isOn = SelectedMode == Mode.PinFrame;
			settingsButton.isOn = SelectedMode == Mode.Settings;
			toggleMapButton.isOn = Manager.ui.mapUI.IsShowingBigMap;
		}
		
		public void Open() {
			IsOpen = true;
			_faceMouseDirection = Manager.prefs.faceMouseDirection;
			Manager.prefs.faceMouseDirection = false;
			PlayToggleSound();
			Update();
		}

		public void Close() {
			IsOpen = false;
			SelectedMode = Mode.None;

			if (_faceMouseDirection != null) {
				Manager.prefs.faceMouseDirection = _faceMouseDirection.Value;
				_faceMouseDirection = null;
			}

			PlayToggleSound();
		}

		private void PlayToggleSound() {
			AudioManager.Sfx(SfxTableID.inventorySFXInfoTab, Manager.main.player.transform.position);
		}

		public void PinFrame() {
			SelectedMode = SelectedMode == Mode.PinFrame ? Mode.None : Mode.PinFrame;
		}
		
		public void ResetFrame() {
			Frame = new CaptureFrame();
		}

		public void ToggleMap() {
			Manager.ui.OnMapToggle();
		}

		public void TakeScreenshotCapture() {
			CaptureManager.Instance.Capture(new ScreenshotCapture());
		}

		public void TakeFrameCapture() {
			if (!Frame.IsComplete)
				return;
			
			CaptureManager.Instance.Capture(new FrameCapture(Frame));
		}
		
		public void OpenFolder() {
			Utils.OpenCaptureDirectory();
		}
		
		public void ToggleSettings() {
			SelectedMode = SelectedMode == Mode.Settings ? Mode.None : Mode.Settings;
		}

		public enum Mode {
			None,
			PinFrame,
			Settings
		}
		
		[HarmonyPatch]
		private static class Patches {
			[HarmonyPatch(typeof(PlayerController), "get_guestMode")]
			[HarmonyPostfix]
			private static void PlayerController_get_guestMode(PlayerController __instance, ref bool __result) {
				if (CaptureManager.Instance.CaptureUI.IsOpen)
					__result = true;
			}
			
			[HarmonyPatch(typeof(StatusText), "LateUpdate")]
			[HarmonyPostfix]
			private static void StatusText_LateUpdate(StatusText __instance) {
				if (CaptureManager.Instance.CaptureUI.IsOpen)
					__instance.container.SetActive(false);
			}
			
			[HarmonyPatch(typeof(SendClientInputSystem), "PlayerInteractionBlocked")]
			[HarmonyPostfix]
			private static void SendClientInputSystem_PlayerInteractionBlocked(SendClientInputSystem __instance, ref bool __result) {
				if (!CaptureManager.Instance.CaptureUI.IsOpen && Time.time <= CaptureButtonUI.LastPressedTime + 0.1f)
					__result = true;
			}
			
			[HarmonyPatch(typeof(MapUI), "UpdateMapFromUserInput")]
			[HarmonyPrefix]
			private static bool MapUI_UpdateMapFromUserInput(MapUI __instance) {
				var captureUI = CaptureManager.Instance.CaptureUI;
				if (!captureUI.IsOpen || captureUI.SelectedMode != Mode.PinFrame || !__instance.IsShowingBigMap || __instance.OpenedMapThisFrame || Time.time <= CaptureButtonUI.LastPressedTime + 0.25f)
					return true;
				
				var input = Manager.input.singleplayerInputModule;
				var cursorWorldPosition = __instance.GetCursorWorldPosition().RoundToInt2();
				
				if (input.IsButtonCurrentlyDown(PlayerInput.InputType.UI_INTERACT))
					captureUI.Frame.PinA = cursorWorldPosition;

				if (input.IsButtonCurrentlyDown(PlayerInput.InputType.UI_SECOND_INTERACT))
					captureUI.Frame.PinB = cursorWorldPosition;

				return false;
			}
		}
	}
}