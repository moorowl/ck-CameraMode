using System.Collections.Generic;
using System.Linq;
using CameraMode.Capture;
using CameraMode.Utilities.Extensions;
using HarmonyLib;
using PugMod;
using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;
using UnityEngine;
using Object = UnityEngine.Object;
// ReSharper disable InconsistentNaming

namespace CameraMode {
	public class Main : IMod {
		public const string Version = "1.2";
		public const string InternalName = "CameraMode";
		public const string DisplayName = "Camera Mode";
		
		public const PlayerInput.InputType ToggleCameraMode = (PlayerInput.InputType) 39050;
		
		internal static AssetBundle AssetBundle { get; private set; }

		public void EarlyInit() {
			var modInfo = API.ModLoader.LoadedMods.FirstOrDefault(modInfo => modInfo.Handlers.Contains(this));
			Debug.Log($"[{DisplayName}] Mod version: {Version}");
			
			AssetBundle = modInfo!.AssetBundles[0];
		}

		public void Init() {
			var gameObject = new GameObject("Camera Mode Managers");
			Object.DontDestroyOnLoad(gameObject);
			gameObject.AddComponent<CaptureManager>();
			
			Config.Instance.Init();
		}

		public void Update() { }
		
		public void Shutdown() { }

		public void ModObjectLoaded(Object obj) { }

		[HarmonyPatch]
		public static class Patches {
			[HarmonyPatch(typeof(InputManager), "Init")]
			[HarmonyPrefix]
			public static void InputManager_Init(InputManager __instance) {
				var inputManagerBase = Resources.Load<InputManager_Base>("Rewired Input Manager");
				var userData = inputManagerBase.userData;

				AddKeybind(userData, (int) ToggleCameraMode, $"{InternalName}-ToggleCameraMode", KeyboardKeyCode.F4);
			}

			private static void AddKeybind(UserData userData, int id, string name, KeyboardKeyCode defaultKeyCode) {
				const int gameplayCategoryId = 17;
				const int gameplayKeyboardMapIndex = 5;
				
				var newAction = new InputAction();
				newAction.SetValue("_id", id);
				newAction.SetValue("_categoryId", gameplayCategoryId);
				newAction.SetValue("_name", $"ControlMapper/{name}");
				newAction.SetValue("_type", InputActionType.Button);
				newAction.SetValue("_descriptiveName", $"ControlMapper/{name}");
				newAction.SetValue("_userAssignable", true);

				userData.GetValue<List<InputAction>>("actions").Add(newAction);
				userData.GetValue<ActionCategoryMap>("actionCategoryMap").AddAction(gameplayCategoryId, id);

				var keyboardMap = userData.GetValue<List<ControllerMap_Editor>>("keyboardMaps")[gameplayKeyboardMapIndex];
				
				var keyboardActionElementMap = new ActionElementMap();
				keyboardActionElementMap.SetValue("_actionId", id);
				keyboardActionElementMap.SetValue("_elementType", ControllerElementType.Button);
				keyboardActionElementMap.SetValue("_actionCategoryId", gameplayCategoryId);
				keyboardActionElementMap.SetValue("_keyboardKeyCode", defaultKeyCode);
				
				keyboardMap.actionElementMaps.Add(keyboardActionElementMap);	
			}
		}
	}
}