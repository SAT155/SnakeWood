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

public enum ModelStyle
{
	Mixed,
	RawWood,
	BuildPiece,
}

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public class SnakeWoodPlugin : BaseUnityPlugin
{
	internal const string PluginGUID = "com.snakesquad.snakewood";
	internal const string PluginName = "SnakeWood";
	internal const string PluginVersion = "1.2.0";

	private static SnakeWoodPlugin instance = null!;
	internal static ManualLogSource Log = null!;

	internal static ConfigEntry<SteeringMode> Steering = null!;
	internal static ConfigEntry<string> AttachAnimation = null!;
	internal static ConfigEntry<float> SeatHeight = null!;
	internal static ConfigEntry<bool> MountedCombat = null!;
	internal static ConfigEntry<ModelStyle> Model = null!;
	internal static ConfigEntry<bool> Glow = null!;
	internal static ConfigEntry<bool> Trails = null!;
	internal static ConfigEntry<float> TrailOpacity = null!;
	internal static ConfigEntry<float> BoostStamina = null!;
	internal static ConfigEntry<float> HopStamina = null!;
	internal static ConfigEntry<float> DeepWaterDepth = null!;
	internal static ConfigEntry<float> AimPenaltyPerSpeed = null!;
	internal static ConfigEntry<string> FuelItem = null!;
	internal static ConfigEntry<int> FuelCapacity = null!;
	internal static ConfigEntry<float> FuelPerMinute = null!;
	internal static ConfigEntry<float> EitrPerSecond = null!;
	internal static ConfigEntry<float> EitrStaminaFactor = null!;
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
		MountedCombat = Config.Bind("1 - Riding", "Mounted Combat", true,
			"Attack, use bows and block while riding. Off: attacking gets you off the log, like on the Lox.");
		Model = Config.Bind("1 - Riding", "Model", ModelStyle.Mixed,
			"Mixed: the raw wood log with a saddle made from that wood's build piece. RawWood: just the raw wood log. BuildPiece: the build piece laid flat (the original look). Takes effect after a restart.");
		Glow = Config.Bind("1 - Riding", "Hover Glow", true,
			"A soft light under each log, coloured by its wood. Takes effect for logs that load after you change it.");
		Trails = Config.Bind("1 - Riding", "Trails", true,
			"The particle trail each log leaves (dust, sparks, embers, poison cloud). Off hides them; the Ancient Bark poison cloud still hurts. Takes effect for logs that load after you change it.");
		TrailOpacity = Config.Bind("1 - Riding", "Trail Opacity", 1f,
			new ConfigDescription("Multiplies how see-through trails are. Lower is fainter. Takes effect for logs that load after you change it.", new AcceptableValueRange<float>(0f, 2f)));
		BoostStamina = Config.Bind("3 - Balance", "Boost Stamina Per Second", 6f,
			new ConfigDescription("Stamina boosting (Run) drains per second. You can't boost with an empty stamina bar. 0 makes boosting free.", new AcceptableValueRange<float>(0f, 50f)));
		HopStamina = Config.Bind("3 - Balance", "Hop Stamina", 8f,
			new ConfigDescription("Stamina each hop costs. You can't hop with too little stamina. 0 makes hopping free.", new AcceptableValueRange<float>(0f, 50f)));
		DeepWaterDepth = Config.Bind("3 - Balance", "Deep Water Depth", 4f,
			new ConfigDescription("Water deeper than this (metres from surface to sea bed) counts as deep: the log sinks to skim the surface and slows to its Deep Water Speed.", new AcceptableValueRange<float>(0.5f, 100f)));
		AimPenaltyPerSpeed = Config.Bind("3 - Balance", "Aim Penalty Per Speed", 0.6f,
			new ConfigDescription("Extra projectile spread, in degrees, for every metre per second the log is moving, on top of each log's own Aim Penalty.", new AcceptableValueRange<float>(0f, 5f)));
		FuelItem = Config.Bind("4 - Fuel", "Fuel Item", "Resin",
			"The item logs burn to stay up (the game's item name). Add it with Shift+E on a log, or use it on the log from your hotbar.");
		FuelCapacity = Config.Bind("4 - Fuel", "Fuel Capacity", 20,
			new ConfigDescription("How much fuel a log holds.", new AcceptableValueRange<int>(1, 200)));
		FuelPerMinute = Config.Bind("4 - Fuel", "Fuel Per Minute", 1f,
			new ConfigDescription("Fuel burned per minute of riding. A parked log burns nothing. Out of fuel, a log won't lift off, and one running dry mid-ride sputters along low and slow. 0 turns fuel off.", new AcceptableValueRange<float>(0f, 20f)));
		EitrPerSecond = Config.Bind("3 - Balance", "Eitr Per Second", 0.5f,
			new ConfigDescription("If you have eitr, riding draws this much per second to help hold the log steady, and your stamina drain drops (see Stamina Drain With Eitr). 0 turns this off.", new AcceptableValueRange<float>(0f, 10f)));
		EitrStaminaFactor = Config.Bind("3 - Balance", "Stamina Drain With Eitr", 0.25f,
			new ConfigDescription("While eitr is helping, riding stamina drain is multiplied by this.", new AcceptableValueRange<float>(0f, 1f)));
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

		Config.SettingChanged += (_, _) => HoverLogPrefabs.RefreshPieces();

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
