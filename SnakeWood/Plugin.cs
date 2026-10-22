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

public enum CrouchAction
{
	Lower,
	Brake,
}

public enum CombatStance
{
	Stand,
	Seated,
}

public enum RiderPose
{
	Straddle,
	SideSaddle,
	Surf,
	LieDown,
}

// The boss whose defeat unlocks a log, by its global key (optional; see Require Boss Kills).
public enum Boss
{
	None,
	Eikthyr,
	TheElder,
	Bonemass,
	Moder,
	Yagluth,
	TheQueen,
	Fader,
}

// The crafting station a log has to be built near.
public enum Station
{
	Workbench,
	Forge,
	Stonecutter,
	ArtisanTable,
	GaldrTable,
	BlackForge,
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
	internal const string PluginVersion = "1.21.1";

	private static SnakeWoodPlugin instance = null!;
	internal static ManualLogSource Log = null!;

	internal static ConfigEntry<SteeringMode> Steering = null!;
	internal static ConfigEntry<RiderPose> Pose = null!;

	// Surf stands on the log: it attaches with the Lox pose and stands up (see RidingControlsPatch).
	internal static string AttachAnimation => Pose.Value switch
	{
		RiderPose.SideSaddle => "attach_chair",
		RiderPose.LieDown => "attach_bed",
		_ => "attach_lox",
	};

	internal static string BossKey(Boss boss) => boss switch
	{
		Boss.Eikthyr => "defeated_eikthyr",
		Boss.TheElder => "defeated_gdking",
		Boss.Bonemass => "defeated_bonemass",
		Boss.Moder => "defeated_dragon",
		Boss.Yagluth => "defeated_goblinking",
		Boss.TheQueen => "defeated_queen",
		Boss.Fader => "defeated_fader",
		_ => "",
	};

	internal static ConfigEntry<bool> BossGate = null!;

	internal static string StationPrefab(Station station) => station switch
	{
		Station.Forge => "forge",
		Station.Stonecutter => "piece_stonecutter",
		Station.ArtisanTable => "piece_artisanstation",
		Station.GaldrTable => "piece_magetable",
		Station.BlackForge => "blackforge",
		_ => "piece_workbench",
	};

	internal static ConfigEntry<float> SeatHeight = null!;
	internal static ConfigEntry<bool> MountedCombat = null!;
	internal static ConfigEntry<bool> Disguise = null!;
	internal static ConfigEntry<CombatStance> Stance = null!;
	internal static ConfigEntry<bool> PitchControl = null!;
	internal static ConfigEntry<float> FishWiggle = null!;
	internal static ConfigEntry<KeyboardShortcut> FreeLookKey = null!;
	internal static ConfigEntry<float> MaxPitch = null!;
	internal static ConfigEntry<ModelStyle> Model = null!;
	internal static ConfigEntry<bool> Glow = null!;
	internal static ConfigEntry<bool> Trails = null!;
	internal static ConfigEntry<float> TrailOpacity = null!;
	internal static ConfigEntry<float> TrailDensity = null!;
	internal static ConfigEntry<float> BoostStamina = null!;
	internal static ConfigEntry<float> HopStamina = null!;
	internal static ConfigEntry<float> DeepWaterDepth = null!;
	internal static ConfigEntry<float> AimPenaltyPerSpeed = null!;
	internal static ConfigEntry<string> FuelItem = null!;
	internal static ConfigEntry<int> FuelCapacity = null!;
	internal static ConfigEntry<float> FuelPerMinute = null!;
	internal static ConfigEntry<float> EitrPerSecond = null!;
	internal static ConfigEntry<float> EitrStaminaFactor = null!;
	internal static ConfigEntry<KeyboardShortcut> TakeFuelKey = null!;
	internal static ConfigEntry<bool> NoMetal = null!;
	internal static ConfigEntry<int> FishQuality = null!;
	internal static ConfigEntry<float> LoadAtCapacity = null!;
	internal static ConfigEntry<float> OverloadFalloff = null!;
	internal static ConfigEntry<CrouchAction> Crouch = null!;
	internal static ConfigEntry<bool> SyncDebug = null!;
	internal static ConfigEntry<KeyboardShortcut> HitchKey = null!;
	internal static ConfigEntry<string> SledAnimals = null!;
	internal static ConfigEntry<string> HareFood = null!;
	internal static ConfigEntry<float> StarSpeedBonus = null!;
	internal static ConfigEntry<float> StarStrengthBonus = null!;
	internal static ConfigEntry<float> TeamDamage = null!;
	internal static ConfigEntry<float> FeedHeal = null!;
	internal static ConfigEntry<float> TeamRegen = null!;
	internal static ConfigEntry<float> NoiseAtRest = null!;
	internal static ConfigEntry<float> NoiseAtSpeed = null!;
	internal static ConfigEntry<bool> WearEnabled = null!;
	internal static ConfigEntry<float> CrashWear = null!;
	internal static ConfigEntry<float> RamWear = null!;
	internal static ConfigEntry<float> RepairPerItem = null!;
	internal static ConfigEntry<bool> RamBuildings = null!;

	// True while binding the per-log entries if this config was written before the speed rebalance,
	// so those logs' Top Speed and Boost are reset to the new defaults once.
	internal static bool ResetSpeeds;
	// Same for the wood logs' recipes, raised in balance version 3.
	internal static bool ResetRecipes;
	// And the load capacities, lowered in balance version 4 when they became the soft limit.
	internal static bool ResetLoads;
	// Balance version 5: Surf became the default pose and Core Wood got stronger.
	internal static bool ResetV5;
	private const int CurrentBalanceVersion = 5;
	internal static ConfigEntry<bool> RamFriendlies = null!;

	private void Awake()
	{
		instance = this;
		Log = Logger;

		Steering = Config.Bind("1 - Riding", "Steering", SteeringMode.Camera,
			"Camera: the log turns toward where you look (like the Lox) and A/D strafe. Keys: A/D turn the log and the camera is free.");
		Pose = Config.Bind("1 - Riding", "Riding Pose", RiderPose.Surf,
			"How you ride. Straddle: astride like the Lox saddle. SideSaddle: sitting sideways, like a chair. Surf: standing on it like a surfboard. LieDown: flat on your back. Takes effect next time you get on.");
		SeatHeight = Config.Bind("1 - Riding", "Seat Height", 0.25f,
			new ConfigDescription("How far above the log's centre the rider sits, in metres. Only affects logs that load after you change it.",
				new AcceptableValueRange<float>(-0.5f, 1.5f)));
		MountedCombat = Config.Bind("1 - Riding", "Mounted Combat", true,
			"Attack, use bows and block while riding. Off: attacking gets you off the log, like on the Lox.");
		Disguise = Config.Bind("1 - Riding", "Vanilla Disguise", true,
			"Saves hover logs as a vanilla fallen-tree log, so players without the mod see a tree log floating along with the rider (they can't ride or chop it). Modded players still see the real log. Applies to logs as they load.");
		Crouch = Config.Bind("1 - Riding", "Crouch Action", CrouchAction.Lower,
			"What holding Crouch (Ctrl) does while riding. Lower: the log sinks right down and creeps along, for squeezing under things and into portals. Brake: stops hard.");
		Stance = Config.Bind("1 - Riding", "Combat Stance", CombatStance.Stand,
			"Stand: the rider stands up on the log to attack, draw a bow or block, then sits back down. Seated (experimental): stays seated while fighting; if attacks don't animate or land this way, switch back to Stand.");
		PitchControl = Config.Bind("1 - Riding", "Pitch With Look", true,
			"Looking up tips the log's nose up so it rides higher at speed and clears rising ground; looking down dips it to ride lower and dive faster.");
		FishWiggle = Config.Bind("1 - Riding", "Fish Swim Wiggle", 7f,
			new ConfigDescription("Fish logs swim rather than steer: they turn toward where you look lazily and dive and climb with your look more than wood does. Their model bobs gently and its tail sways by up to this many degrees (the rider and camera stay steady). 0 turns the sway and bob off.", new AcceptableValueRange<float>(0f, 20f)));
		FreeLookKey = Config.Bind("1 - Riding", "Free Look Key", new KeyboardShortcut(KeyCode.LeftAlt),
			"With camera steering, hold this to look around without turning the log. Standing still, you can always look around freely.");
		MaxPitch = Config.Bind("1 - Riding", "Max Pitch", 20f,
			new ConfigDescription("How far the nose tips up or down, in degrees.", new AcceptableValueRange<float>(0f, 45f)));
		Model = Config.Bind("1 - Riding", "Model", ModelStyle.Mixed,
			"Mixed: the raw wood log with a saddle made from that wood's build piece. RawWood: just the raw wood log. BuildPiece: the build piece laid flat (the original look). Takes effect after a restart.");
		Glow = Config.Bind("1 - Riding", "Hover Glow", true,
			"A soft light under each log, coloured by its wood. Takes effect for logs that load after you change it.");
		Trails = Config.Bind("1 - Riding", "Trails", true,
			"The particle trail each log leaves (dust, sparks, embers, poison cloud). Off hides them; the Ancient Bark poison cloud still hurts. Takes effect for logs that load after you change it.");
		TrailOpacity = Config.Bind("1 - Riding", "Trail Opacity", 1f,
			new ConfigDescription("Multiplies how see-through trails are. Lower is fainter. Takes effect for logs that load after you change it.", new AcceptableValueRange<float>(0f, 2f)));
		TrailDensity = Config.Bind("1 - Riding", "Trail Density", 1.4f,
			new ConfigDescription("Multiplies how many particles trails leave per metre. Takes effect for logs that load after you change it.", new AcceptableValueRange<float>(0.1f, 5f)));
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
		TakeFuelKey = Config.Bind("4 - Fuel", "Take Out Fuel Key", new KeyboardShortcut(KeyCode.R),
			"Press while looking at a log to take the fuel out of it (into your inventory, or onto the ground if there's no room).");
		NoMetal = Config.Bind("3 - Balance", "No Ore Or Metal", false,
			"Logs refuse to carry anything you couldn't take through a portal (ore, metal bars, eggs...): you can't get on carrying it, and picking it up mid-ride throws you off.");
		BossGate = Config.Bind("3 - Balance", "Require Boss Kills", false,
			"On: each log only shows up in the build menu once its Required Boss has been defeated, on top of its crafting station.");
		FishQuality = Config.Bind("3 - Balance", "Fish Quality Required", 5,
			new ConfigDescription("Lowest level (quality) the fish in a fish log's recipe can be. Fish are caught at levels 1 to 5; 5 is the rarest. 1 takes any fish.", new AcceptableValueRange<int>(1, 10)));
		LoadAtCapacity = Config.Bind("3 - Balance", "Performance At Load Capacity", 0.7f,
			new ConfigDescription("How well a log flies carrying exactly its Load Capacity, as a share of empty: top speed, acceleration, turning and hop height, with hover height dropping more gently. Lighter loads ease back towards full performance in a straight line. 1 makes weight not matter up to capacity.", new AcceptableValueRange<float>(0.1f, 1f)));
		OverloadFalloff = Config.Bind("3 - Balance", "Overload Falloff", 1.2f,
			new ConfigDescription("Performance lost for every 100% over Load Capacity (on top of the above), so overloading bites much harder: at the defaults, 50% over capacity leaves 10%. It never stops a log entirely.", new AcceptableValueRange<float>(0f, 5f)));
		NoiseAtRest = Config.Bind("3 - Balance", "Noise At Rest", 8f,
			new ConfigDescription("How far away, in metres, creatures can hear a log idling, before each log's own Noise multiplier. 0 makes logs silent.", new AcceptableValueRange<float>(0f, 100f)));
		NoiseAtSpeed = Config.Bind("3 - Balance", "Noise At Top Speed", 35f,
			new ConfigDescription("How far away creatures can hear a log at top speed, before each log's own Noise multiplier. A running player is heard from about 30 m.", new AcceptableValueRange<float>(0f, 200f)));
		WearEnabled = Config.Bind("5 - Wear", "Wear", true,
			"Logs wear out from crashes and rams. A worn-out log is wrecked: it drops where it is and stays there until it's fully repaired with its own wood (Shift+E; each log's Repair Item). Off: logs never wear.");
		CrashWear = Config.Bind("5 - Wear", "Crash Wear", 1f,
			new ConfigDescription("Multiplies the wear from hitting trees, rocks and walls (harder hits wear more).", new AcceptableValueRange<float>(0f, 10f)));
		RamWear = Config.Bind("5 - Wear", "Ram Wear", 1f,
			new ConfigDescription("Multiplies the wear from ramming things (a twentieth of the ram's damage).", new AcceptableValueRange<float>(0f, 10f)));
		RepairPerItem = Config.Bind("5 - Wear", "Repair Per Item", 10f,
			new ConfigDescription("Wear each repair item fixes.", new AcceptableValueRange<float>(1f, 1000f)));
		HitchKey = Config.Bind("6 - Sled", "Hitch Key", new KeyboardShortcut(KeyCode.H),
			"Press while looking at a sled (nobody on it) to hitch the nearest animal that can pull it: a tamed wolf within 6 m, or a hare dragged within 3 m (harpoon it in). With none near, unhitches the last animal and lets it go.");
		SledAnimals = Config.Bind("6 - Sled", "Team Animals", "Wolf:8.5:180,Hare:10.5:60",
			"Which creatures can pull a sled, as Prefab:Speed:Strength. Speed (m/s) is the animal's share of the sled's top speed (the team's average; a lone animal manages 80%). Strength adds to how much weight the sled pulls (your carry weight plus its cargo) before slowing, like Load Capacity on the logs. Tamable creatures must be tamed first.");
		HareFood = Config.Bind("6 - Sled", "Food For Untamable Animals", "Carrot,Turnip,Blueberries,Raspberry,Cloudberry",
			"What hares (and other animals that have no tamed diet) eat when you feed them. Tamed animals eat what they eat in the game.");
		StarSpeedBonus = Config.Bind("6 - Sled", "Speed Per Star", 0.08f,
			new ConfigDescription("Extra speed each star gives an animal (0.08 = 8%).", new AcceptableValueRange<float>(0f, 1f)));
		StarStrengthBonus = Config.Bind("6 - Sled", "Strength Per Star", 0.35f,
			new ConfigDescription("Extra pulling strength each star gives an animal.", new AcceptableValueRange<float>(0f, 3f)));
		TeamDamage = Config.Bind("6 - Sled", "Team Damage", 0.5f,
			new ConfigDescription("Multiplies the knocks (crashes, rams) the team takes, shared between the animals pulling. An animal at 0 is exhausted and stops pulling until fed; it never dies in harness.", new AcceptableValueRange<float>(0f, 5f)));
		FeedHeal = Config.Bind("6 - Sled", "Feed Heal", 0.35f,
			new ConfigDescription("Share of an animal's health each food item restores (Shift+E with its food).", new AcceptableValueRange<float>(0.01f, 1f)));
		TeamRegen = Config.Bind("6 - Sled", "Rest Heal Per Minute", 5f,
			new ConfigDescription("Percent of health the team recovers per minute while the sled stands still.", new AcceptableValueRange<float>(0f, 100f)));
		SyncDebug = Config.Bind("9 - Debug", "Sync Debug", false,
			"Logs, every two seconds for each log near you, how often its network updates arrive, the longest gap between them, and how far the log you see is from the latest update. Turn on for both the rider and a watcher when hunting multiplayer desync, then share both LogOutput.log files.");
		RamBuildings = Config.Bind("2 - Ramming", "Ram Damages Buildings", false,
			"Ramming logs (Core Wood, Ashwood, Ancient Bark) also damage build pieces they hit. Off by default so a bad landing can't knock a hole in your base.");
		RamFriendlies = Config.Bind("2 - Ramming", "Ram Damages Players And Tames", false,
			"Ramming logs also damage other players and tamed creatures they hit.");

		ConfigEntry<int> balanceVersion = Config.Bind("Internal", "Balance Version", 0,
			"Used by the mod to update old settings after a rebalance. Leave it alone.");
		ResetSpeeds = balanceVersion.Value < 2;
		ResetRecipes = balanceVersion.Value < 3;
		ResetLoads = balanceVersion.Value < 4;
		ResetV5 = balanceVersion.Value < 5;
		if (ResetV5)
		{
			Pose.Value = RiderPose.Surf;
		}
		foreach (HoverLogVariant variant in HoverLogVariant.All)
		{
			variant.BindConfig(Config);
		}
		if (balanceVersion.Value < CurrentBalanceVersion)
		{
			ResetSpeeds = ResetRecipes = ResetLoads = ResetV5 = false;
			balanceVersion.Value = CurrentBalanceVersion;
			Log.LogInfo("Settings changed by a rebalance (log speeds, wood log recipes, load capacities) reset to this version's defaults.");
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
