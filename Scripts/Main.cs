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
		public const string Version = "1.1";
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
				
				var newAction = new InputAction();
				newAction.SetValue("_id", (int) ToggleCameraMode);
				newAction.SetValue("_categoryId", 17);
				newAction.SetValue("_name", "ControlMapper/CameraMode:ToggleCameraMode");
				newAction.SetValue("_type", InputActionType.Button);
				newAction.SetValue("_descriptiveName", "ControlMapper/CameraMode:ToggleCameraMode");
				newAction.SetValue("_userAssignable", true);

				userData.GetValue<List<InputAction>>("actions").Add(newAction);
				userData.GetValue<ActionCategoryMap>("actionCategoryMap").AddAction(17, (int) ToggleCameraMode);

				var keyboardMap = userData.GetValue<List<ControllerMap_Editor>>("keyboardMaps")[5];
				
				var keyboardActionElementMap = new ActionElementMap();
				keyboardActionElementMap.SetValue("_actionId", (int) ToggleCameraMode);
				keyboardActionElementMap.SetValue("_elementType", ControllerElementType.Button);
				keyboardActionElementMap.SetValue("_actionCategoryId", 17);
				keyboardActionElementMap.SetValue("_keyboardKeyCode", KeyboardKeyCode.F4);
				
				keyboardMap.actionElementMaps.Add(keyboardActionElementMap);	
			}
		}
	}
}