using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SnakeWood;

public enum SteeringMode
{
	Camera,
	Keys,
}

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class SnakeWoodPlugin : BaseUnityPlugin
{
	internal const string PluginGUID = "com.snakesquad.snakewood";
	internal const string PluginName = "SnakeWood";
	internal const string PluginVersion = "1.0.0";

	private static SnakeWoodPlugin instance = null!;
	internal static ManualLogSource Log = null!;

	internal static ConfigEntry<SteeringMode> Steering = null!;
	internal static ConfigEntry<string> AttachAnimation = null!;
	internal static ConfigEntry<float> SeatHeight = null!;
	internal static ConfigEntry<bool> RamBuildings = null!;
	internal static ConfigEntry<bool> RamFriendlies = null!;

	private void Awake()
	{
		instance = this;
		Log = Logger;

		Steering = Config.Bind("1 - Riding", "Steering", SteeringMode.Camera,
			"Camera: the log turns toward where you look (like the Lox) and A/D strafe. Keys: A/D turn the log and the camera is free.");
		AttachAnimation = Config.Bind("1 - Riding", "Rider Pose", "attach_lox",
			"Animation the rider uses while on a log. attach_lox straddles it like the Lox saddle; attach_chair sits side-saddle like a chair.");
		SeatHeight = Config.Bind("1 - Riding", "Seat Height", 0.25f,
			new ConfigDescription("How far above the log's centre the rider sits, in metres. Only affects logs that load after you change it.",
				new AcceptableValueRange<float>(-0.5f, 1.5f)));
		RamBuildings = Config.Bind("2 - Ramming", "Ram Damages Buildings", false,
			"Ramming logs (Core Wood, Ashwood, Ancient Bark) also damage build pieces they hit. Off by default so a bad landing can't knock a hole in your base.");
		RamFriendlies = Config.Bind("2 - Ramming", "Ram Damages Players And Tames", false,
			"Ramming logs also damage other players and tamed creatures they hit.");

		foreach (HoverLogVariant variant in HoverLogVariant.All)
		{
			variant.BindConfig(Config);
		}

		Config.Bind("1 - Riding", "Reload Config From Disk", false,
			new ConfigDescription("Click to reload this plugin's settings from the .cfg file on disk immediately, without restarting Valheim - for after hand-editing the file.",
				null, new ConfigurationManagerAttributes { CustomDrawer = DrawReloadConfigButton }));

		new Harmony(PluginGUID).PatchAll(Assembly.GetExecutingAssembly());
	}

	// Configuration Manager picks this up by name (it doesn't need to reference this assembly), and
	// draws the entry with CustomDrawer instead of its default checkbox.
	private class ConfigurationManagerAttributes
	{
		public Action<ConfigEntryBase>? CustomDrawer;
	}

	private static void DrawReloadConfigButton(ConfigEntryBase _)
	{
		if (GUILayout.Button("Reload now"))
		{
			instance.Config.Reload();
			Log.LogInfo("Config reloaded from disk.");
		}
	}
}
