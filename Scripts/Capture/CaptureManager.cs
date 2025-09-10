using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CameraMode.UserInterface;
using CameraMode.Utilities;
using HarmonyLib;
using I2.Loc;
using Pug.RP;
using Pug.Sprite;
using PugMod;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using ShaderIDs = Pug.RP.ShaderIDs;

// ReSharper disable InconsistentNaming

namespace CameraMode.Capture {
	public class CaptureManager : MonoBehaviour {
		/*private class TrackedParticleSystem {
			public ParticleSystem ParticleSystem;
			public float OriginalState;
		}
		private class TrackedAnimator {
			public Animator Animator;
			public float OriginalState;
		}

		private class TrackedEntityMono {
			public EntityMonoBehaviour EntityMono;
			public Entity OriginalEntity;
		}

		private readonly Dictionary<int, TrackedParticleSystem> _trackedParticleSystems = new();
		private readonly Dictionary<int, TrackedAnimator> _trackedAnimators = new();
		private readonly Dictionary<int, EntityMonoBehaviour> _activeEntityMonos = new();*/

		public static CaptureManager Instance { get; private set; }

		public CaptureUI CaptureUI { get; private set; }

		public bool IsCapturing => _settings != null;
		public float CaptureProgress => IsCapturing ? math.clamp((float) AreasCaptured / AreasToCapture, 0f, 1f) : 0f;
		public int AreasCaptured { get; private set; }
		public int AreasToCapture { get; private set; }

		private static bool CanOpenCaptureUI => !Manager.menu.IsAnyMenuActive()
		                                        && Manager.main.player != null
		                                        && !Manager.main.player.isDyingOrDead
		                                        && Manager.main.player.adminPrivileges >= 1;

		private CaptureSettings _settings;
		private bool _uiWasDisabled;
		private int _oldDynamicWaterSetting;
		private float _startTime;

		private Coroutine _captureRoutine;
		private List<Object> _temporaryObjects = new();

		private bool _queueCaptureStoppedMessage;

		private void Awake() {
			Instance = this;

			var captureProgressPrefab = Main.AssetBundle.LoadAsset<GameObject>("Assets/CameraMode/Prefabs/CaptureProgressUI.prefab");
			Instantiate(captureProgressPrefab, Manager.ui.UICamera.transform);

			var capturePrefab = Main.AssetBundle.LoadAsset<GameObject>("Assets/CameraMode/Prefabs/CaptureUI.prefab");
			CaptureUI = Instantiate(capturePrefab, Manager.ui.UICamera.transform).GetComponent<CaptureUI>();
		}

		private void Update() {
			if (IsCapturing && (!CanOpenCaptureUI || !CaptureUI.IsOpen)) {
				EndCapture();
				_queueCaptureStoppedMessage = true;
			}

			if (!Manager.menu.IsAnyMenuActive() && _queueCaptureStoppedMessage) {
				Utils.DisplayChatMessage(LocalizationManager.GetTranslation("CameraMode:CaptureStopped"));
				_queueCaptureStoppedMessage = false;
			}

			if (CanOpenCaptureUI) {
				if (Input.GetKeyDown(KeyCode.F4)) {
					if (CaptureUI.IsOpen) {
						CaptureUI.Close();
					} else {
						CaptureUI.Open();
					}
				}
			} else if (CaptureUI.IsOpen) {
				CaptureUI.Close();
			}
		}

		public void Capture(CaptureSettings settings) {
			if (_settings != null)
				throw new InvalidOperationException("Capture called while a capture is in progress");

			_settings = settings;

			var captureName = _settings.Name ?? $"Capture {DateTime.Now:yyyy-MM-dd HH.mm.ss}";
			var isScreenshot = settings.Frame == null;
			
			if (isScreenshot) {
				_captureRoutine = StartCoroutine(CaptureScreenshotRoutine(imageData => {
					Utils.WriteCapture(captureName, _settings.Quality.GetFileExtension(), imageData);
				}));
			} else {
				var frameSize = settings.Frame.Size;
				if (!settings.Frame.IsComplete)
					return;
				
				AreasCaptured = 0;
				AreasToCapture = Mathf.CeilToInt((frameSize.x * Constants.PIXELS_PER_UNIT_F) / Constants.kScreenPixelWidth) * Mathf.CeilToInt((frameSize.y * Constants.PIXELS_PER_UNIT_F) / Constants.kScreenPixelHeight);
				
				_captureRoutine = StartCoroutine(CaptureFrameRoutine(imageData => {
					Utils.WriteCapture(captureName, _settings.Quality.GetFileExtension(), imageData);
				}));
			}
		}

		private void BeginCapture() {
			_temporaryObjects.Clear();
			
			_uiWasDisabled = Manager.prefs.hideInGameUI;
			_oldDynamicWaterSetting = Manager.prefs.dynamicWater;
			_startTime = Time.time;

			Manager.prefs.hideInGameUI = true;
			if (Utils.IsSingleplayer && AreasToCapture > 0)
				Manager.networking.SetDisableSimulation(true, API.Client.World);
			Manager.prefs.dynamicWater = 0;
			Manager.input.DisableInput();

			Manager.camera.currentCameraStyle = CameraManager.CameraControlStyle.Static;
			Manager.camera.manualControlTargetPosition = Manager.main.player.GetEntityPosition();
		}

		private void EndCapture() {
			_settings = null;

			if (_captureRoutine != null) {
				StopCoroutine(_captureRoutine);
				_captureRoutine = null;
			}

			Manager.prefs.hideInGameUI = _uiWasDisabled;
			if (Utils.IsSimulationDisabled && AreasToCapture > 0)
				Manager.networking.SetDisableSimulation(false, API.Client.World);
			Manager.prefs.dynamicWater = _oldDynamicWaterSetting;
			Manager.input.EnableInput();

			Manager.camera.cameraMovementStyle = CameraManager.CameraMovementStyle.Instant;
			Manager.camera.manualControlTargetPosition = Manager.main.player.GetEntityPosition();
			StartCoroutine(ResetCameraRoutine());
			
			foreach (var temporaryObject in _temporaryObjects)
				Destroy(temporaryObject);
			
			AreasCaptured = 0;
			AreasToCapture = 0;
		}

		private IEnumerator ResetCameraRoutine() {
			yield return new WaitForEndOfFrame();

			Manager.camera.cameraMovementStyle = CameraManager.CameraMovementStyle.Smooth;
			Manager.camera.currentCameraStyle = CameraManager.CameraControlStyle.FollowPlayer;
		}
		
		private IEnumerator CaptureScreenshotRoutine(Action<byte[]> callback) {
			const float StartEndCaptureWaitTime = 0.5f;
			
			BeginCapture();

			var gameCamera = Manager.camera.gameCamera;
			
			var outputWidth = Mathf.CeilToInt(Constants.kScreenPixelWidth * _settings.ResolutionScale);
			var outputHeight = Mathf.CeilToInt(Constants.kScreenPixelHeight * _settings.ResolutionScale);
			var outputPixels = new byte[(outputWidth * outputHeight) * 4];

			yield return new WaitForSeconds(StartEndCaptureWaitTime);
			yield return new WaitForEndOfFrame();
			
			var captureTexture = new Texture2D(Constants.kScreenPixelWidth * _settings.ResolutionScale, Constants.kScreenPixelHeight * _settings.ResolutionScale, TextureFormat.RGB24, false);
			var renderTexture = new RenderTexture(Constants.kScreenPixelWidth * _settings.ResolutionScale, Constants.kScreenPixelHeight * _settings.ResolutionScale, 24);
			
			_temporaryObjects.Add(captureTexture);
			_temporaryObjects.Add(renderTexture);
			
			var oldActiveRenderTexture = RenderTexture.active;
			var oldTargetTexture = gameCamera.targetTexture;

			RenderTexture.active = renderTexture;
			gameCamera.targetTexture = renderTexture;
			gameCamera.Render();

			captureTexture.ReadPixels(new Rect(0, 0, captureTexture.width, captureTexture.height), 0, 0);
			captureTexture.Apply();

			Utils.CopyToPixelBuffer(captureTexture, ref outputPixels, 0, 0, outputWidth, outputHeight);

			gameCamera.targetTexture = oldTargetTexture;
			RenderTexture.active = oldActiveRenderTexture;
			
			var encodedImageData = _settings.Quality.EncodeArrayToImage(outputPixels, GraphicsFormat.R8G8B8A8_SRGB, (uint) outputWidth, (uint) outputHeight);
			callback?.Invoke(encodedImageData);

			yield return new WaitForSeconds(StartEndCaptureWaitTime);
			
			EndCapture();
		}

		private IEnumerator CaptureFrameRoutine(Action<byte[]> callback) {
			const float AreaLoadWaitTime = 1.5f;
			const float CameraMoveLoadWaitTime = 0.25f;
			const float StartEndCaptureWaitTime = 0.5f;
			
			BeginCapture();

			var gameCamera = Manager.camera.gameCamera;
			var framePosition = _settings.Frame.Position;
			var frameSize = _settings.Frame.Size;

			var chunksX = Mathf.CeilToInt((frameSize.x * Constants.PIXELS_PER_UNIT_F) / Constants.kScreenPixelWidth);
			var chunksY = Mathf.CeilToInt((frameSize.y * Constants.PIXELS_PER_UNIT_F) / Constants.kScreenPixelHeight);
			var chunkSizeX = Constants.kScreenPixelWidth * _settings.ResolutionScale;
			var chunkSizeY = Constants.kScreenPixelHeight * _settings.ResolutionScale;

			var outputWidth = Mathf.CeilToInt(frameSize.x * (Constants.PIXELS_PER_UNIT_F * (chunkSizeX / (float) Constants.kScreenPixelWidth)));
			var outputHeight = Mathf.CeilToInt(frameSize.y * (Constants.PIXELS_PER_UNIT_F * (chunkSizeY / (float) Constants.kScreenPixelHeight)));
			var outputPixels = new byte[(outputWidth * outputHeight) * 4];
			
			yield return new WaitForSeconds(StartEndCaptureWaitTime);

			var screenUnitWidth = Constants.kScreenPixelWidth / Constants.PIXELS_PER_UNIT_F;
			var screenUnitHeight = Constants.kScreenPixelHeight / Constants.PIXELS_PER_UNIT_F;

			var captureTexture = new Texture2D(Constants.kScreenPixelWidth * _settings.ResolutionScale, Constants.kScreenPixelHeight * _settings.ResolutionScale, TextureFormat.RGB24, false);
			var renderTexture = new RenderTexture(Constants.kScreenPixelWidth * _settings.ResolutionScale, Constants.kScreenPixelHeight * _settings.ResolutionScale, 24);
			
			_temporaryObjects.Add(captureTexture);
			_temporaryObjects.Add(renderTexture);

			var x = 0;
			var y = 0;
			var direction = 1;

			for (var i = 0; i < chunksX * chunksY; i++) {
				Manager.camera.manualControlTargetPosition = new Vector3(
					framePosition.x + (screenUnitWidth / 2f) - 0.5f + (screenUnitWidth * x),
					Manager.camera.manualControlTargetPosition.y,
					framePosition.y + (screenUnitHeight / 2f) - 0.5f + (screenUnitHeight * y)
				);

				yield return new WaitForSeconds(AreaLoadWaitTime);
				Manager.camera.cameraMovementStyle = CameraManager.CameraMovementStyle.Instant;
				yield return new WaitForSeconds(CameraMoveLoadWaitTime);
				yield return new WaitForEndOfFrame();
				Manager.camera.cameraMovementStyle = CameraManager.CameraMovementStyle.Smooth;

				var oldActiveRenderTexture = RenderTexture.active;
				var oldTargetTexture = gameCamera.targetTexture;

				RenderTexture.active = renderTexture;
				gameCamera.targetTexture = renderTexture;
				gameCamera.Render();

				captureTexture.ReadPixels(new Rect(0, 0, captureTexture.width, captureTexture.height), 0, 0);
				captureTexture.Apply();

				Utils.CopyToPixelBuffer(captureTexture, ref outputPixels, x * chunkSizeX, y * chunkSizeY, outputWidth, outputHeight);

				gameCamera.targetTexture = oldTargetTexture;
				RenderTexture.active = oldActiveRenderTexture;

				AreasCaptured++;

				y += direction;
				if (y < 0 || y >= chunksY) {
					x++;
					y -= direction;
					direction *= -1;
				}
			}
			
			var encodedImageData = _settings.Quality.EncodeArrayToImage(outputPixels, GraphicsFormat.R8G8B8A8_SRGB, (uint) outputWidth, (uint) outputHeight);
			callback?.Invoke(encodedImageData);
			
			yield return new WaitForSeconds(StartEndCaptureWaitTime);

			EndCapture();
		}

		[HarmonyPatch]
		public static class Patches {
			[HarmonyPatch(typeof(CameraManager), "UpdateGameAndUICameras")]
			[HarmonyPostfix]
			public static void CameraManager_UpdateGameAndUICameras(CameraManager __instance, float deltaTime) {
				if (Instance == null || !Instance.IsCapturing)
					return;

				API.Rendering.GameCamera.integerScaling = true;
			}

			[HarmonyPatch(typeof(PugRP), "SetupCameraProperties")]
			[HarmonyPostfix]
			public static void PugRP_SetupCameraProperties(PugRPContext context, CommandBuffer cmd, Camera camera, bool forceSkew) {
				if (Instance == null || !Instance.IsCapturing)
					return;

				var time = Instance._startTime;
				var deltaTime = 0.000001f;
				var smoothDeltaTime = 0.000001f;

				var pTime = time * new Vector4(0.05f, 1f, 2f, 3f);
				var pSinTime = new Vector4(Mathf.Sin(time / 8f), Mathf.Sin(time / 4f), Mathf.Sin(time / 2f), Mathf.Sin(time));
				var pCosTime = new Vector4(Mathf.Cos(time / 8f), Mathf.Cos(time / 4f), Mathf.Cos(time / 2f), Mathf.Cos(time));
				var pDeltaTime = new Vector4(deltaTime, 1f / deltaTime, smoothDeltaTime, 1f / smoothDeltaTime);
				var pTimeParameters = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0f);

				cmd.SetGlobalVector(ShaderIDs.Time, pTime);
				cmd.SetGlobalVector(ShaderIDs.SinTime, pSinTime);
				cmd.SetGlobalVector(ShaderIDs.CosTime, pCosTime);
				cmd.SetGlobalVector(ShaderIDs.DeltaTime, pDeltaTime);
				cmd.SetGlobalVector(ShaderIDs.TimeParameters, pTimeParameters);
				cmd.SetGlobalFloat(_WaterSimDelta, 0.000001f);

				Shader.SetGlobalVector(ShaderIDs.Time, pTime);
				Shader.SetGlobalVector(ShaderIDs.SinTime, pSinTime);
				Shader.SetGlobalVector(ShaderIDs.CosTime, pCosTime);
				Shader.SetGlobalVector(ShaderIDs.DeltaTime, pDeltaTime);
				Shader.SetGlobalVector(ShaderIDs.TimeParameters, pTimeParameters);
				Shader.SetGlobalFloat(_WaterSimDelta, 0.000001f);
			}

			private static readonly MemberInfo MiDeltaTime = typeof(SpriteObject).GetMembersChecked().FirstOrDefault(x => x.GetNameChecked() == "s_deltaTime");

			[HarmonyPatch(typeof(SpriteObject), "GetSpriteArrayCache")]
			[HarmonyPostfix]
			private static void SpriteObject_GetSpriteArrayCache(Dictionary<int, SpriteObject> spriteObjects) {
				if (Instance == null || !Instance.IsCapturing)
					return;

				API.Reflection.SetValue(MiDeltaTime, null, 0f);
			}

			[HarmonyPatch(typeof(WaterSim), "UpdateSimulation")]
			[HarmonyPrefix]
			private static void WaterSim_UpdateSimulation(WaterSim __instance, ref float deltaTime) {
				if (Instance == null || !Instance.IsCapturing)
					return;

				deltaTime = 0.000001f;
			}

			private static readonly int _WaterSimDelta = Shader.PropertyToID("_WaterSimDelta");

			[HarmonyPatch(typeof(WaterSim), "LateUpdate")]
			[HarmonyPostfix]
			private static void WaterSim_LateUpdate(WaterSim __instance) {
				if (Instance == null || !Instance.IsCapturing)
					return;

				Shader.SetGlobalFloat(_WaterSimDelta, 0.000001f);
			}

			[HarmonyPatch(typeof(LightManager), "UpdateLightFlickerParameters")]
			[HarmonyPrefix]
			private static void LightManager_UpdateLightFlickerParameters(LightManager __instance, int id, float min, float max, ref bool enableMovement) {
				if (Instance == null || !Instance.IsCapturing)
					return;

				enableMovement = false;
			}

			[HarmonyPatch(typeof(DroppedItem), "UpdateAnimation")]
			[HarmonyPrefix]
			private static bool DroppedItem_UpdateAnimation(DroppedItem __instance) {
				if (Instance == null || !Instance.IsCapturing)
					return true;

				return false;
			}

			[HarmonyPatch(typeof(PlayerController), "UpdateBlinking")]
			[HarmonyPrefix]
			private static bool PlayerController_UpdateBlinking(PlayerController __instance) {
				if (Instance == null || !Instance.IsCapturing)
					return true;

				return false;
			}

			[HarmonyPatch(typeof(CoreBossOrb), "ManagedLateUpdate")]
			[HarmonyPrefix]
			private static bool CoreBossOrb_ManagedLateUpdate(CoreBossOrb __instance) {
				if (Instance == null || !Instance.IsCapturing)
					return true;

				return false;
			}
		}
	}
}