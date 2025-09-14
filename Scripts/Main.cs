using System.Linq;
using CameraMode.Capture;
using HarmonyLib;
using PugMod;
using UnityEngine;
using Object = UnityEngine.Object;
// ReSharper disable InconsistentNaming

namespace CameraMode {
	public class Main : IMod {
		public const string Version = "1.0.3";
		public const string InternalName = "CameraMode";
		public const string DisplayName = "Camera Mode";
		
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
			[HarmonyPatch(typeof(MenuManager), "Init")]
			[HarmonyPostfix]
			public static void MenuManager_Init(MenuManager __instance) {
				var menu = __instance.gameplayOptionsMenu;
				var root = menu.transform.Find("Options");
				var scroll = root.GetChild(0);
				
				InsertMenuItem(menu, scroll, "Assets/CameraMode/Prefabs/Options/CaptureQuality.prefab");
				InsertMenuItem(menu, scroll, "Assets/CameraMode/Prefabs/Options/CaptureResolutionScale.prefab");
			}

			private static void InsertMenuItem(RadicalMenu menu, Transform scroll, string prefabPath) {
				var prefab = AssetBundle.LoadAsset<GameObject>(prefabPath);
				var instance = Object.Instantiate(prefab, scroll);
				instance.transform.SetSiblingIndex(scroll.childCount - 2);

				var menuOption = instance.GetComponent<RadicalMenuOption>();
				menuOption.SetParentMenu(menu);
				menu.menuOptions.Add(menuOption);
			}
		}
	}
}