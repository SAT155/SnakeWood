using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace SnakeWood;

// One rideable log per wood type. The prefab (recipe, look, size, weight) is fixed at startup; the
// handling numbers are config entries, read live by whoever is riding.
public class HoverLogVariant
{
	public static readonly List<HoverLogVariant> All = new()
	{
		new HoverLogVariant("Wood", "Hover Log", "A plain log that floats a little above the ground. Steady and forgiving.")
		{
			WoodItem = "Wood", WoodAmount = 20, ExtraItem = "Resin", ExtraAmount = 5,
			TemplateHints = new[] { "wood_pole2", "wood_pole", "wood_beam" },
			Length = 2.6f, Thickness = 1f, Mass = 60f,
			MaxSpeed = 11f, Boost = 1.5f, Acceleration = 9f, TurnSpeed = 120f, Grip = 8f,
			HoverHeight = 0.9f, HopSpeed = 6f, FallGravity = 1f,
		},
		new HoverLogVariant("FineWood", "Fine Hover Log", "Light, quick and twitchy. Turns on a coin and runs away from everything.")
		{
			WoodItem = "FineWood", WoodAmount = 20, ExtraItem = "Feathers", ExtraAmount = 5,
			TemplateHints = new[] { "darkwood_pole", "darkwood_pole4", "darkwood_beam" },
			Length = 2.3f, Thickness = 0.8f, Mass = 35f,
			MaxSpeed = 15f, Boost = 1.6f, Acceleration = 16f, TurnSpeed = 170f, Grip = 12f,
			HoverHeight = 0.8f, HopSpeed = 7f, FallGravity = 1f,
		},
		new HoverLogVariant("RoundLog", "Core Wood Battering Log", "Bulky and slow to get going, but it doesn't stop for much. Rams creatures and fells trees in its way.")
		{
			WoodItem = "RoundLog", WoodAmount = 20, ExtraItem = "Resin", ExtraAmount = 10,
			TemplateHints = new[] { "wood_pole_log_4", "wood_pole_log", "wood_log_45", "wood_log_26" },
			Length = 3.6f, Thickness = 1.5f, Mass = 250f,
			MaxSpeed = 8f, Boost = 1.35f, Acceleration = 4.5f, TurnSpeed = 65f, Grip = 5f,
			HoverHeight = 0.7f, HopSpeed = 4f, FallGravity = 1.3f,
			RamDamage = 60f, RamChop = 1f, RamPickaxe = 0.5f, RamPush = 60f, RamToolTier = 2,
		},
		new HoverLogVariant("ElderBark", "Ancient Bark Hover Log", "Rides high enough to clear swamp roots and shallow water. Its bark leaves a poisonous welt on whatever it hits.")
		{
			WoodItem = "ElderBark", WoodAmount = 20, ExtraItem = "Guck", ExtraAmount = 3,
			TemplateHints = new[] { "wood_pole_log_4", "wood_pole_log" },
			Tint = new Color(0.55f, 0.6f, 0.45f),
			Length = 2.8f, Thickness = 1.15f, Mass = 90f,
			MaxSpeed = 10f, Boost = 1.4f, Acceleration = 8f, TurnSpeed = 105f, Grip = 6f,
			HoverHeight = 1.7f, HopSpeed = 6f, FallGravity = 0.8f,
			RamDamage = 10f, RamPoison = 3f, RamPush = 20f,
		},
		new HoverLogVariant("YggdrasilWood", "Yggdrasil Hover Log", "Floats highest of all, hops highest, and glides gently down from cliffs.")
		{
			WoodItem = "YggdrasilWood", WoodAmount = 20, ExtraItem = "Sap", ExtraAmount = 3,
			TemplateHints = new[] { "yggdrasil_wood_beam", "Yggdrasil_wood_beam", "yggdrasil_wood_pole", "Yggdrasil_wood_pole" },
			Tint = new Color(0.8f, 0.9f, 1.15f),
			Length = 2.8f, Thickness = 1f, Mass = 50f,
			MaxSpeed = 12f, Boost = 1.5f, Acceleration = 10f, TurnSpeed = 130f, Grip = 7f,
			HoverHeight = 2.4f, HopSpeed = 10f, FallGravity = 0.3f,
		},
		new HoverLogVariant("Blackwood", "Ashwood Hover Log", "The fastest log there is, and it runs hot: anything it rams catches fire.")
		{
			WoodItem = "Blackwood", WoodAmount = 20, ExtraItem = "SurtlingCore", ExtraAmount = 2,
			TemplateHints = new[] { "ashwood_pole", "ashwood_beam", "blackwood_pole", "blackwood_beam" },
			Tint = new Color(0.55f, 0.45f, 0.42f),
			Length = 2.7f, Thickness = 1f, Mass = 70f,
			MaxSpeed = 18f, Boost = 1.6f, Acceleration = 13f, TurnSpeed = 125f, Grip = 9f,
			HoverHeight = 1f, HopSpeed = 7f, FallGravity = 1f,
			RamDamage = 15f, RamFire = 25f, RamPush = 30f,
		},
	};

	public static HoverLogVariant? Get(string key)
	{
		foreach (HoverLogVariant variant in All)
		{
			if (variant.Key == key)
			{
				return variant;
			}
		}
		return null;
	}

	public readonly string Key;
	public readonly string Name;
	public readonly string Description;
	public string PrefabName => "SnakeWood_HoverLog_" + Key;

	// Recipe and look; fixed once the prefab is built.
	public string WoodItem = "Wood";
	public int WoodAmount = 20;
	public string ExtraItem = "";
	public int ExtraAmount;
	public string[] TemplateHints = new string[0];
	public Color Tint = Color.white;
	public float Length = 2.6f;
	public float Thickness = 1f;
	public float Mass = 60f;

	// Defaults for the handling config entries below.
	public float MaxSpeed, Boost, Acceleration, TurnSpeed, Grip, HoverHeight, HopSpeed, FallGravity;
	public float RamDamage, RamChop, RamPickaxe, RamFire, RamPoison, RamPush;
	public int RamToolTier;

	public ConfigEntry<bool> Enabled = null!;
	public ConfigEntry<float> MaxSpeedCfg = null!;
	public ConfigEntry<float> BoostCfg = null!;
	public ConfigEntry<float> AccelerationCfg = null!;
	public ConfigEntry<float> TurnSpeedCfg = null!;
	public ConfigEntry<float> HoverHeightCfg = null!;
	public ConfigEntry<float> HopSpeedCfg = null!;
	public ConfigEntry<float> FallGravityCfg = null!;
	public ConfigEntry<float> RamDamageCfg = null!;

	private HoverLogVariant(string key, string name, string description)
	{
		Key = key;
		Name = name;
		Description = description;
	}

	public void BindConfig(ConfigFile config)
	{
		string section = "Log - " + Name;
		Enabled = config.Bind(section, "Buildable", true,
			"Shows this log in the hammer's build menu. Logs already built stay rideable either way. Takes effect after a restart.");
		MaxSpeedCfg = config.Bind(section, "Top Speed", MaxSpeed,
			new ConfigDescription("Top speed in metres per second without boost. A running player does about 7.", new AcceptableValueRange<float>(1f, 60f)));
		BoostCfg = config.Bind(section, "Boost Multiplier", Boost,
			new ConfigDescription("Top speed multiplier while holding Run (Shift).", new AcceptableValueRange<float>(1f, 4f)));
		AccelerationCfg = config.Bind(section, "Acceleration", Acceleration,
			new ConfigDescription("How quickly it speeds up, in metres per second per second.", new AcceptableValueRange<float>(0.5f, 60f)));
		TurnSpeedCfg = config.Bind(section, "Turn Speed", TurnSpeed,
			new ConfigDescription("How fast it turns, in degrees per second.", new AcceptableValueRange<float>(10f, 720f)));
		HoverHeightCfg = config.Bind(section, "Hover Height", HoverHeight,
			new ConfigDescription("How high it floats above ground or water, in metres.", new AcceptableValueRange<float>(0.2f, 10f)));
		HopSpeedCfg = config.Bind(section, "Hop Strength", HopSpeed,
			new ConfigDescription("Upward speed of a hop (Jump key), in metres per second. 0 turns hopping off.", new AcceptableValueRange<float>(0f, 30f)));
		FallGravityCfg = config.Bind(section, "Fall Gravity", FallGravity,
			new ConfigDescription("Gravity multiplier when it's well above its hover height, like after driving off a cliff. Lower glides further.", new AcceptableValueRange<float>(0.05f, 3f)));
		RamDamageCfg = config.Bind(section, "Ram Damage", RamDamage,
			new ConfigDescription("Damage dealt at full speed to what the log runs into. Scales down at lower speeds. 0 turns ramming off.", new AcceptableValueRange<float>(0f, 500f)));
	}
}
