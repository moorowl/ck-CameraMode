using System;
using System.Linq;
using CameraMode.Capture;
using CameraMode.Utilities;
using HarmonyLib;
using PugMod;
using Rewired;
using Rewired.Data;
using UnityEngine;
using Object = UnityEngine.Object;
// ReSharper disable InconsistentNaming

namespace CameraMode {
	public class Main : IMod {
		public const string Version = "1.0.5";
		public const string InternalName = "CameraMode";
		public const string DisplayName = "Camera Mode";
		
		private const int ModCategory = 39050;
		public const PlayerInput.InputType ToggleCameraMode = (PlayerInput.InputType) 39050;
		
		internal static AssetBundle AssetBundle { get; private set; }

		public void EarlyInit() {
			var modInfo = API.ModLoader.LoadedMods.FirstOrDefault(modInfo => modInfo.Handlers.Contains(this));
			Debug.Log($"[{DisplayName}] Mod version: {Version}");
			
			AssetBundle = modInfo!.AssetBundles[0];

			InputAdder.OnInit += userData => {
				InputAdder.AddCategory(userData, new InputAdder.CategoryConfiguration(ModCategory, "CameraMode")
					.SetTag("gameplay")
				);
				
				InputAdder.AddAction(userData, new InputAdder.ActionConfiguration((int) ToggleCameraMode, $"{InternalName}:ToggleCameraMode")
					.SetCategory(ModCategory)
					.SetDefaultKeyboardBinding(KeyboardKeyCode.F4)
				);
			};
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
	}
}