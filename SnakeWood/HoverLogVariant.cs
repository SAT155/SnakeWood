using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace SnakeWood;

public enum TrailFx
{
	Motes,
	Dust,
	Poison,
	Sparkles,
	Embers,
}

// One rideable log per wood type. The prefab (recipe, look, size, weight) is fixed at startup; the
// handling numbers are config entries, read live by whoever is riding.
public class HoverLogVariant
{
	public static readonly List<HoverLogVariant> All = new()
	{
		new HoverLogVariant("Wood", "Hover Log", "A plain log that floats a little above the ground. Steady and forgiving, but crash into a tree or rock and you're off.")
		{
			WoodItem = "Wood", Recipe = "Wood:20,Resin:10,TrophyDeer:2",
			TemplateHints = new[] { "wood_pole2", "wood_pole", "wood_beam" },
			Length = 2.6f, Thickness = 1f, Mass = 60f,
			AimPenalty = 4f, RideStamina = 1.5f, MaxSpeed = 11f, Boost = 1.5f, Acceleration = 9f, TurnSpeed = 120f, Grip = 8f,
			HoverHeight = 0.9f, HopHeight = 3f, FallGravity = 1f, CrashSpeed = 7f, DeepWaterSpeed = 0.35f,
			Fx = TrailFx.Motes, Glow = new Color(1f, 0.8f, 0.45f),
		},
		new HoverLogVariant("FineWood", "Fine Hover Log", "Light, quick and twitchy. Turns on a coin, but a hard knock throws you off.")
		{
			WoodItem = "FineWood", Recipe = "FineWood:20,Resin:10,AncientSeed:3",
			TemplateHints = new[] { "darkwood_pole", "darkwood_pole4", "darkwood_beam" },
			Length = 2.3f, Thickness = 0.8f, Mass = 35f,
			AimPenalty = 6f, RideStamina = 2f, MaxSpeed = 15f, Boost = 1.6f, Acceleration = 16f, TurnSpeed = 170f, Grip = 12f,
			HoverHeight = 0.8f, HopHeight = 3.5f, FallGravity = 1f, CrashSpeed = 6f, DeepWaterSpeed = 0.35f,
			Fx = TrailFx.Motes, Glow = new Color(0.75f, 0.9f, 1f),
		},
		new HoverLogVariant("RoundLog", "Core Wood Battering Log", "Bulky and slow to get going, but nothing knocks you off it. Rams creatures and fells trees in its way.")
		{
			WoodItem = "RoundLog", Recipe = "RoundLog:20,Resin:10,AncientSeed:3",
			TemplateHints = new[] { "wood_pole_log_4", "wood_pole_log", "wood_log_45", "wood_log_26" },
			Length = 3.6f, Thickness = 1.5f, Mass = 250f,
			AimPenalty = 2f, RideStamina = 0.8f, MaxSpeed = 8f, Boost = 1.35f, Acceleration = 4.5f, TurnSpeed = 65f, Grip = 5f,
			HoverHeight = 0.7f, HopHeight = 2f, FallGravity = 1.3f, CrashSpeed = 0f, DeepWaterSpeed = 0.25f,
			Fx = TrailFx.Dust, Glow = new Color(1f, 0.6f, 0.3f),
			RamDamage = 60f, RamChop = 1f, RamPickaxe = 0.5f, RamPush = 60f, RamToolTier = 2,
		},
		new HoverLogVariant("ElderBark", "Ancient Bark Hover Log", "Rides high enough to clear swamp roots and shallow water. It trails a poison cloud that sickens creatures caught in it, and its bark poisons whatever it rams.")
		{
			WoodItem = "ElderBark", Recipe = "ElderBark:20,Resin:10,WitheredBone:3",
			TemplateHints = new[] { "wood_pole_log_4", "wood_pole_log" },
			Tint = new Color(0.55f, 0.6f, 0.45f),
			Length = 2.8f, Thickness = 1.15f, Mass = 90f,
			AimPenalty = 4f, RideStamina = 1.5f, MaxSpeed = 10f, Boost = 1.4f, Acceleration = 8f, TurnSpeed = 105f, Grip = 6f,
			HoverHeight = 1.7f, HopHeight = 3f, FallGravity = 0.8f, CrashSpeed = 8f, DeepWaterSpeed = 0.6f,
			Fx = TrailFx.Poison, Glow = new Color(0.5f, 1f, 0.35f), PoisonCloud = 8f,
			RamDamage = 10f, RamPoison = 3f, RamPush = 20f,
		},
		new HoverLogVariant("YggdrasilWood", "Yggdrasil Hover Log", "Floats and hops highest, can hop again in mid-air, and glides gently down from cliffs, trailing sparks of light.")
		{
			WoodItem = "YggdrasilWood", Recipe = "YggdrasilWood:20,Resin:10,DvergrKeyFragment:2",
			TemplateHints = new[] { "yggdrasil_wood_beam", "Yggdrasil_wood_beam", "yggdrasil_wood_pole", "Yggdrasil_wood_pole" },
			Tint = new Color(0.8f, 0.9f, 1.15f),
			Length = 2.8f, Thickness = 1f, Mass = 50f,
			AimPenalty = 3f, RideStamina = 1.2f, MaxSpeed = 12f, Boost = 1.5f, Acceleration = 10f, TurnSpeed = 130f, Grip = 7f,
			HoverHeight = 2.4f, HopHeight = 5f, AirHops = 2, FallGravity = 0.3f, CrashSpeed = 7f, DeepWaterSpeed = 0.45f,
			Fx = TrailFx.Sparkles, Glow = new Color(0.45f, 0.75f, 1f),
		},
		new HoverLogVariant("Blackwood", "Ashwood Hover Log", "The fastest log there is, and it runs hot: it trails embers and anything it rams catches fire.")
		{
			WoodItem = "Blackwood", Recipe = "Blackwood:20,Resin:10,BellFragment:1",
			TemplateHints = new[] { "ashwood_pole", "ashwood_beam", "blackwood_pole", "blackwood_beam" },
			Tint = new Color(0.55f, 0.45f, 0.42f),
			Length = 2.7f, Thickness = 1f, Mass = 70f,
			AimPenalty = 6f, RideStamina = 2f, MaxSpeed = 18f, Boost = 1.6f, Acceleration = 13f, TurnSpeed = 125f, Grip = 9f,
			HoverHeight = 1f, HopHeight = 3.5f, FallGravity = 1f, CrashSpeed = 9f, DeepWaterSpeed = 0.35f,
			Fx = TrailFx.Embers, Glow = new Color(1f, 0.45f, 0.15f),
			RamDamage = 15f, RamFire = 25f, RamPush = 30f,
		},
		new HoverLogVariant("Fish2", "Hover Pike", "A giant enchanted pike, made for the open sea: faster than a longship over deep water, but a floundering mess on land. Long crossings are a test of stamina.")
		{
			WoodItem = "Fish2", Recipe = "Fish2:1,Resin:10,SerpentScale:5",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.6f, Thickness = 1f, Mass = 60f,
			AimPenalty = 5f, RideStamina = 0.7f, MaxSpeed = 6f, Boost = 1.3f, Acceleration = 8f, TurnSpeed = 110f, Grip = 6f,
			HoverHeight = 0.6f, HopHeight = 2.5f, FallGravity = 1f, CrashSpeed = 6f, DeepWaterSpeed = 2f,
			Fx = TrailFx.Motes, Glow = new Color(0.35f, 0.8f, 1f),
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
	public string Recipe = "";
	public string[] TemplateHints = new string[0];
	public Color Tint = Color.white;
	public float Length = 2.6f;
	public float Thickness = 1f;
	public float Mass = 60f;

	public TrailFx Fx;
	public Color Glow = Color.white;

	// Defaults for the handling config entries below.
	public float AimPenalty, RideStamina, MaxSpeed, Boost, Acceleration, TurnSpeed, Grip, HoverHeight, HopHeight, FallGravity, CrashSpeed, DeepWaterSpeed;
	public int AirHops;
	public float RamDamage, RamChop, RamPickaxe, RamFire, RamPoison, RamPush, PoisonCloud;
	public int RamToolTier;

	public ConfigEntry<bool> Enabled = null!;
	public ConfigEntry<string> RecipeCfg = null!;
	public ConfigEntry<float> AimPenaltyCfg = null!;
	public ConfigEntry<float> RideStaminaCfg = null!;
	public ConfigEntry<float> MaxSpeedCfg = null!;
	public ConfigEntry<float> BoostCfg = null!;
	public ConfigEntry<float> AccelerationCfg = null!;
	public ConfigEntry<float> TurnSpeedCfg = null!;
	public ConfigEntry<float> HoverHeightCfg = null!;
	public ConfigEntry<float> HopHeightCfg = null!;
	public ConfigEntry<int> AirHopsCfg = null!;
	public ConfigEntry<float> FallGravityCfg = null!;
	public ConfigEntry<float> CrashSpeedCfg = null!;
	public ConfigEntry<float> DeepWaterSpeedCfg = null!;
	public ConfigEntry<float> RamDamageCfg = null!;
	public ConfigEntry<float> PoisonCloudCfg = null!;

	private HoverLogVariant(string key, string name, string description)
	{
		Key = key;
		Name = name;
		Description = description;
	}

	// The build menu tooltip: flavour text plus the live numbers from config.
	public string Describe()
	{
		string Line(string label, string value) => $"\n<color=orange>{label}</color> {value}";
		string text = Description + "\n";
		text += Line("Speed", $"{MaxSpeedCfg.Value:0.#} m/s, boost x{BoostCfg.Value:0.##}");
		text += Line("Turning", $"{TurnSpeedCfg.Value:0}°/s");
		text += Line("Hover", $"{HoverHeightCfg.Value:0.#} m");
		string airHops = AirHopsCfg.Value > 0 ? $", +{AirHopsCfg.Value} in mid-air" : "";
		text += Line("Hop", HopHeightCfg.Value > 0f ? $"{HopHeightCfg.Value:0.#} m{airHops}" : "none");
		if (FallGravityCfg.Value < 0.75f)
		{
			text += Line("Glide", $"falls at {FallGravityCfg.Value * 100f:0}% gravity");
		}
		text += Line("Balance", RideStaminaCfg.Value > 0f ? $"{RideStaminaCfg.Value:0.#} stamina/s" : "free");
		text += Line("Crashes", CrashSpeedCfg.Value > 0f ? $"thrown off above {CrashSpeedCfg.Value:0.#} m/s" : "never thrown off");
		text += Line("Aim", $"+{AimPenaltyCfg.Value:0.#}° spread, more at speed");
		text += Line("Deep water", $"{DeepWaterSpeedCfg.Value * 100f:0}% speed");
		if (RamDamageCfg.Value > 0f)
		{
			List<string> extras = new();
			if (RamChop > 0f) extras.Add("fells trees");
			if (RamPickaxe > 0f) extras.Add("chips rock");
			if (RamPoison > 0f) extras.Add("poisons");
			if (RamFire > 0f) extras.Add("burns");
			text += Line("Ram", $"{RamDamageCfg.Value:0} damage" + (extras.Count > 0 ? ", " + string.Join(", ", extras) : ""));
		}
		if (PoisonCloudCfg != null && PoisonCloudCfg.Value > 0f)
		{
			text += Line("Poison cloud", $"{PoisonCloudCfg.Value:0} poison to creatures in your wake");
		}
		return text;
	}

	public void BindConfig(ConfigFile config)
	{
		string section = "Log - " + Name;
		Enabled = config.Bind(section, "Buildable", true,
			"Shows this log in the hammer's build menu. Logs already built stay rideable either way. Takes effect after a restart.");
		RecipeCfg = config.Bind(section, "Recipe", Recipe,
			"What it costs to build, as Item:Amount pairs separated by commas, using the game's item names (for example Wood, Resin, TrophyDeer, AncientSeed, WitheredBone, DvergrKeyFragment, BellFragment). Unknown items are skipped with a warning in the log.");
		AimPenaltyCfg = config.Bind(section, "Aim Penalty", AimPenalty,
			new ConfigDescription("Extra spread, in degrees, on arrows and other projectiles fired from this log, even standing still. Speed adds more on top (see Aim Penalty Per Speed).", new AcceptableValueRange<float>(0f, 45f)));
		RideStaminaCfg = config.Bind(section, "Riding Stamina Per Second", RideStamina,
			new ConfigDescription("Stamina it takes to keep your balance, drained the whole time you ride (and stamina doesn't regenerate meanwhile). When it runs out you lose your grip and fall off. 0 makes riding free.", new AcceptableValueRange<float>(0f, 20f)));
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
		HopHeightCfg = config.Bind(section, "Hop Height", HopHeight,
			new ConfigDescription("How high a hop (Jump key) lifts the log above its hover height, in metres. 0 turns hopping off.", new AcceptableValueRange<float>(0f, 20f)));
		AirHopsCfg = config.Bind(section, "Air Hops", AirHops,
			new ConfigDescription("Extra hops you can make in mid-air before touching down again.", new AcceptableValueRange<int>(0, 5)));
		FallGravityCfg = config.Bind(section, "Fall Gravity", FallGravity,
			new ConfigDescription("Gravity multiplier when it's well above its hover height, like after driving off a cliff. Lower glides further.", new AcceptableValueRange<float>(0.05f, 3f)));
		CrashSpeedCfg = config.Bind(section, "Crash Speed", CrashSpeed,
			new ConfigDescription("Hitting a tree, rock or wall this hard (speed lost in the impact, metres per second) throws you off. 0 means it never throws you.", new AcceptableValueRange<float>(0f, 50f)));
		DeepWaterSpeedCfg = config.Bind(section, "Deep Water Speed", DeepWaterSpeed,
			new ConfigDescription("Over deep water the log sinks to skim the surface and its top speed is multiplied by this. 1 makes deep water no different from land; above 1 it is faster at sea (the Hover Pike).", new AcceptableValueRange<float>(0.05f, 3f)));
		RamDamageCfg = config.Bind(section, "Ram Damage", RamDamage,
			new ConfigDescription("Damage dealt at full speed to what the log runs into. Scales down at lower speeds. 0 turns ramming off.", new AcceptableValueRange<float>(0f, 500f)));
		if (Fx == TrailFx.Poison)
		{
			PoisonCloudCfg = config.Bind(section, "Poison Cloud Damage", PoisonCloud,
				new ConfigDescription("Poison dealt to each creature caught in the trailing cloud, at most once a second. 0 keeps the cloud just for looks.", new AcceptableValueRange<float>(0f, 100f)));
		}
	}
}
