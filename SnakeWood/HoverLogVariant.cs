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
			WoodItem = "Wood", Station = Station.Workbench, StationLevel = 2, Boss = Boss.Eikthyr, Recipe = "Wood:50,Resin:20,TrophyDeer:4,DeerHide:10",
			TemplateHints = new[] { "wood_pole2", "wood_pole", "wood_beam" },
			Length = 2.6f, Thickness = 1f, Mass = 60f,
			AimPenalty = 4f, RideStamina = 1.5f, MaxSpeed = 7f, Boost = 1.3f, Acceleration = 9f, TurnSpeed = 120f, Grip = 8f,
			HoverHeight = 0.9f, HopHeight = 3f, FallGravity = 1f, CrashSpeed = 7f, DeepWaterSpeed = 0.35f,
			MaxClimb = 30f, LoadCapacity = 210f, Durability = 100f, Noise = 1f,
			Fx = TrailFx.Motes, Glow = new Color(1f, 0.8f, 0.45f),
		},
		new HoverLogVariant("FineWood", "Fine Hover Log", "Light, quick and twitchy. Turns on a coin, but a hard knock throws you off.")
		{
			WoodItem = "FineWood", Station = Station.Forge, StationLevel = 2, Boss = Boss.TheElder, Recipe = "FineWood:50,Resin:30,AncientSeed:5,TrollHide:3",
			TemplateHints = new[] { "darkwood_pole", "darkwood_pole4", "darkwood_beam" },
			Length = 2.3f, Thickness = 0.8f, Mass = 35f,
			AimPenalty = 6f, RideStamina = 3.5f, MaxSpeed = 9f, Boost = 1.45f, Acceleration = 16f, TurnSpeed = 170f, Grip = 12f,
			HoverHeight = 0.8f, HopHeight = 3.5f, FallGravity = 1f, CrashSpeed = 6f, DeepWaterSpeed = 0.35f,
			MaxClimb = 35f, LoadCapacity = 130f, Durability = 60f, Noise = 0.8f,
			Fx = TrailFx.Motes, Glow = new Color(0.75f, 0.9f, 1f),
		},
		new HoverLogVariant("RoundLog", "Core Wood Battering Log", "Bulky and slow to get going, but nothing knocks you off it. Rams creatures and fells trees in its way.")
		{
			WoodItem = "RoundLog", Station = Station.Forge, StationLevel = 3, Boss = Boss.TheElder, Recipe = "RoundLog:60,Resin:30,AncientSeed:5,TrollHide:5",
			TemplateHints = new[] { "wood_pole_log_4", "wood_pole_log", "wood_log_45", "wood_log_26" },
			Length = 3.6f, Thickness = 1.5f, Mass = 250f,
			AimPenalty = 2f, RideStamina = 0.8f, MaxSpeed = 5.5f, Boost = 1.3f, Acceleration = 6f, TurnSpeed = 65f, Grip = 5f,
			HoverHeight = 0.7f, HopHeight = 2f, FallGravity = 1.3f, CrashSpeed = 0f, DeepWaterSpeed = 0.25f,
			MaxClimb = 32f, LoadCapacity = 490f, Durability = 300f, Noise = 1.6f,
			Fx = TrailFx.Dust, Glow = new Color(1f, 0.6f, 0.3f),
			RamDamage = 90f, RamChop = 1f, RamPickaxe = 0.55f, RamPush = 60f, RamToolTier = 2, RamKeepSpeed = 0.85f,
		},
		new HoverLogVariant("ElderBark", "Ancient Bark Hover Log", "Rides high enough to clear swamp roots and shallow water. It trails a poison cloud that sickens creatures caught in it, and its bark poisons whatever it rams.")
		{
			WoodItem = "ElderBark", Station = Station.Forge, StationLevel = 4, Boss = Boss.Bonemass, Recipe = "ElderBark:60,Resin:30,WitheredBone:6,Guck:10",
			TemplateHints = new[] { "wood_pole_log_4", "wood_pole_log" },
			Tint = new Color(0.55f, 0.6f, 0.45f),
			Length = 2.8f, Thickness = 1.15f, Mass = 90f,
			AimPenalty = 4f, RideStamina = 1.5f, MaxSpeed = 7f, Boost = 1.35f, Acceleration = 8f, TurnSpeed = 105f, Grip = 6f,
			HoverHeight = 1.7f, HopHeight = 3f, FallGravity = 0.8f, CrashSpeed = 8f, DeepWaterSpeed = 0.6f,
			MaxClimb = 30f, LoadCapacity = 350f, Durability = 150f, Noise = 0.9f,
			Fx = TrailFx.Poison, Glow = new Color(0.5f, 1f, 0.35f), PoisonCloud = 8f,
			RamDamage = 10f, RamPoison = 3f, RamPush = 20f,
		},
		new HoverLogVariant("YggdrasilWood", "Yggdrasil Hover Log", "Floats and hops highest, can hop again in mid-air, and glides gently down from cliffs, trailing sparks of light.")
		{
			WoodItem = "YggdrasilWood", Station = Station.GaldrTable, StationLevel = 2, Boss = Boss.TheQueen, Recipe = "YggdrasilWood:50,Resin:30,DvergrKeyFragment:4,Sap:10",
			TemplateHints = new[] { "yggdrasil_wood_beam", "Yggdrasil_wood_beam", "yggdrasil_wood_pole", "Yggdrasil_wood_pole" },
			Tint = new Color(0.8f, 0.9f, 1.15f),
			Length = 2.8f, Thickness = 1f, Mass = 50f,
			AimPenalty = 3f, RideStamina = 1.2f, MaxSpeed = 9f, Boost = 1.45f, Acceleration = 10f, TurnSpeed = 130f, Grip = 7f,
			HoverHeight = 2.4f, HopHeight = 5f, AirHops = 2, FallGravity = 0.3f, CrashSpeed = 7f, DeepWaterSpeed = 0.45f,
			MaxClimb = 40f, LoadCapacity = 140f, Durability = 120f, Noise = 0.5f,
			Fx = TrailFx.Sparkles, Glow = new Color(0.45f, 0.75f, 1f),
		},
		new HoverLogVariant("Blackwood", "Ashwood Hover Log", "The fastest log there is (just), and it runs hot: it trails embers and anything it rams catches fire.")
		{
			WoodItem = "Blackwood", Station = Station.BlackForge, StationLevel = 2, Boss = Boss.Fader, Recipe = "Blackwood:60,Resin:30,BellFragment:3",
			TemplateHints = new[] { "ashwood_pole", "ashwood_beam", "blackwood_pole", "blackwood_beam" },
			Tint = new Color(0.55f, 0.45f, 0.42f),
			Length = 2.7f, Thickness = 1f, Mass = 70f,
			AimPenalty = 6f, RideStamina = 2f, MaxSpeed = 9.5f, Boost = 1.45f, Acceleration = 13f, TurnSpeed = 125f, Grip = 9f,
			HoverHeight = 1f, HopHeight = 3.5f, FallGravity = 1f, CrashSpeed = 9f, DeepWaterSpeed = 0.35f,
			MaxClimb = 35f, LoadCapacity = 180f, Durability = 150f, Noise = 1.2f,
			Fx = TrailFx.Embers, Glow = new Color(1f, 0.45f, 0.15f),
			RamDamage = 15f, RamFire = 25f, RamPush = 30f,
		},
		new HoverLogVariant("Frostwood", "Timberwood Hover Log", "Cut from the frozen trees of the Deep North. Sure-footed on snow and steep ice, floats high and glides far, trailing frost; anything it rams is chilled and slowed.")
		{
			WoodItem = "Frostwood", Station = Station.BlackForge, StationLevel = 3, Boss = Boss.Fader, Recipe = "Frostwood:60,Resin:30,FrostCore:3,Ice:10",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Tint = new Color(0.85f, 0.95f, 1.1f),
			Length = 2.9f, Thickness = 1.1f, Mass = 80f,
			AimPenalty = 3f, RideStamina = 1.3f, MaxSpeed = 9.5f, Boost = 1.45f, Acceleration = 12f, TurnSpeed = 130f, Grip = 11f,
			HoverHeight = 1.5f, HopHeight = 4f, AirHops = 1, FallGravity = 0.6f, CrashSpeed = 9f, DeepWaterSpeed = 0.5f,
			MaxClimb = 42f, LoadCapacity = 240f, Durability = 200f, Noise = 0.7f,
			Fx = TrailFx.Sparkles, Glow = new Color(0.6f, 0.85f, 1f),
			RamDamage = 15f, RamFrost = 25f, RamPush = 30f,
		},
		// The sled: pulled by a team of real animals (HoverSled), with a chest and a passenger seat.
		new HoverLogVariant("Sled", "Hover Sled", "A floating sled pulled by a team of two: tamed wolves, or hares harpooned and dragged into harness. Cart-sized chest, a passenger seat, and no stamina cost while it stands still.")
		{
			Sled = true,
			WoodItem = "FineWood", Station = Station.Forge, StationLevel = 2, Boss = Boss.TheElder, Recipe = "FineWood:30,Resin:20,DeerHide:10,BronzeNails:30",
			TemplateHints = new[] { "wood_floor", "wood_floor_1x1" },
			Length = 3f, Thickness = 2.4f, Mass = 120f,
			AimPenalty = 2f, RideStamina = 1f, MaxSpeed = 8f, Boost = 1.25f, Acceleration = 8f, TurnSpeed = 90f, Grip = 9f,
			HoverHeight = 0.8f, HopHeight = 1.5f, FallGravity = 1.1f, CrashSpeed = 9f, DeepWaterSpeed = 0.5f,
			MaxClimb = 30f, LoadCapacity = 200f, Durability = 200f, Noise = 1.2f,
			Fx = TrailFx.Dust, Glow = new Color(0.9f, 0.85f, 0.7f),
		},
		// The fish logs. Each needs a top-level (5) fish (Fish Quality Required), so they're rare and
		// ride well: most are made for water, and each has a trick of its own.
		new HoverLogVariant("Fish1", "Hover Perch", "A top-level perch, enchanted. The friendliest fish to learn on: steady, light on stamina, and quick over rivers and the sea.")
		{
			WoodItem = "Fish1", Station = Station.Workbench, StationLevel = 2, Boss = Boss.Eikthyr, RepairItem = "FishRaw", Recipe = "Fish1:1,Resin:10,TrophyDeer:2",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.3f, Thickness = 0.95f, Mass = 50f,
			AimPenalty = 3f, RideStamina = 0.6f, MaxSpeed = 7.5f, Boost = 1.35f, Acceleration = 10f, TurnSpeed = 130f, Grip = 8f,
			HoverHeight = 0.9f, HopHeight = 3f, FallGravity = 1f, CrashSpeed = 7f, DeepWaterSpeed = 1.6f,
			MaxClimb = 30f, LoadCapacity = 180f, Durability = 90f, Noise = 0.8f,
			Fx = TrailFx.Motes, Glow = new Color(0.8f, 1f, 0.5f),
		},
		new HoverLogVariant("Fish2", "Hover Pike", "A top-level pike, made for the open sea: much faster than a longship over deep water, though slower on land. Long crossings still test your stamina.")
		{
			WoodItem = "Fish2", Station = Station.Forge, StationLevel = 2, Boss = Boss.TheElder, RepairItem = "FishRaw", Recipe = "Fish2:1,Resin:10,SerpentScale:5",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.6f, Thickness = 1f, Mass = 60f,
			AimPenalty = 4f, RideStamina = 0.6f, MaxSpeed = 6f, Boost = 1.35f, Acceleration = 9f, TurnSpeed = 120f, Grip = 7f,
			HoverHeight = 0.7f, HopHeight = 3f, FallGravity = 1f, CrashSpeed = 7f, DeepWaterSpeed = 2.2f,
			MaxClimb = 25f, LoadCapacity = 210f, Durability = 110f, Noise = 0.7f,
			Fx = TrailFx.Motes, Glow = new Color(0.35f, 0.8f, 1f),
		},
		new HoverLogVariant("Fish3", "Hover Tuna", "A top-level tuna: the fastest thing on the open sea, and tireless. Out of the water it's a heavy, sluggish lump.")
		{
			WoodItem = "Fish3", Station = Station.Forge, StationLevel = 5, Boss = Boss.Moder, RepairItem = "FishRaw", Recipe = "Fish3:1,Resin:10,SerpentScale:5",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.8f, Thickness = 1.2f, Mass = 90f,
			AimPenalty = 4f, RideStamina = 0.5f, MaxSpeed = 5f, Boost = 1.35f, Acceleration = 9f, TurnSpeed = 100f, Grip = 7f,
			HoverHeight = 0.6f, HopHeight = 2.5f, FallGravity = 1.1f, CrashSpeed = 8f, DeepWaterSpeed = 2.7f,
			MaxClimb = 20f, LoadCapacity = 280f, Durability = 140f, Noise = 0.8f,
			Fx = TrailFx.Motes, Glow = new Color(0.3f, 0.55f, 1f),
		},
		new HoverLogVariant("Fish4_cave", "Hover Tetra", "A top-level tetra from the mountain caves. Sure-footed on steep, icy slopes and quick to hop, trailing a glitter of frost.")
		{
			WoodItem = "Fish4_cave", Station = Station.Forge, StationLevel = 5, Boss = Boss.Moder, RepairItem = "FishRaw", Recipe = "Fish4_cave:1,Resin:10,FreezeGland:5",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.1f, Thickness = 0.85f, Mass = 40f,
			AimPenalty = 3f, RideStamina = 0.9f, MaxSpeed = 8f, Boost = 1.4f, Acceleration = 12f, TurnSpeed = 150f, Grip = 10f,
			HoverHeight = 1.1f, HopHeight = 4f, AirHops = 1, FallGravity = 0.8f, CrashSpeed = 7f, DeepWaterSpeed = 0.9f,
			MaxClimb = 45f, LoadCapacity = 140f, Durability = 100f, Noise = 0.6f,
			Fx = TrailFx.Sparkles, Glow = new Color(0.6f, 0.9f, 1f),
		},
		new HoverLogVariant("Fish5", "Hover Trollfish", "A top-level trollfish from the swamp: tough, heavy-shouldered and hard to knock off. Rides high over roots and leeches and bowls creatures over.")
		{
			WoodItem = "Fish5", Station = Station.Forge, StationLevel = 4, Boss = Boss.Bonemass, RepairItem = "FishRaw", Recipe = "Fish5:1,Resin:10,WitheredBone:3",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.8f, Thickness = 1.3f, Mass = 150f,
			AimPenalty = 3f, RideStamina = 0.8f, MaxSpeed = 6.5f, Boost = 1.3f, Acceleration = 7f, TurnSpeed = 95f, Grip = 7f,
			HoverHeight = 1.6f, HopHeight = 2.5f, FallGravity = 1.1f, CrashSpeed = 0f, DeepWaterSpeed = 1.4f,
			MaxClimb = 26f, LoadCapacity = 420f, Durability = 250f, Noise = 1.3f,
			Fx = TrailFx.Dust, Glow = new Color(0.55f, 0.8f, 0.4f),
			RamDamage = 30f, RamPush = 50f,
		},
		new HoverLogVariant("Fish6", "Hover Herring", "A top-level giant herring: quiet, agile and quick both on the sea and along misty shores. A scout's fish.")
		{
			WoodItem = "Fish6", Station = Station.GaldrTable, StationLevel = 2, Boss = Boss.TheQueen, RepairItem = "FishRaw", Recipe = "Fish6:1,Resin:10,DvergrKeyFragment:1",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.5f, Thickness = 0.9f, Mass = 45f,
			AimPenalty = 4f, RideStamina = 0.9f, MaxSpeed = 8.5f, Boost = 1.45f, Acceleration = 14f, TurnSpeed = 170f, Grip = 11f,
			HoverHeight = 1f, HopHeight = 3.5f, FallGravity = 0.8f, CrashSpeed = 7f, DeepWaterSpeed = 1.8f,
			MaxClimb = 33f, LoadCapacity = 140f, Durability = 90f, Noise = 0.4f,
			Fx = TrailFx.Motes, Glow = new Color(0.7f, 0.85f, 1f),
		},
		new HoverLogVariant("Fish7", "Hover Grouper", "A top-level grouper: a broad, sturdy sea cruiser that hauls heavy loads across the water and shoves aside whatever it hits.")
		{
			WoodItem = "Fish7", Station = Station.ArtisanTable, StationLevel = 1, Boss = Boss.Yagluth, RepairItem = "FishRaw", Recipe = "Fish7:1,Resin:10,GoblinTotem:2",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.9f, Thickness = 1.35f, Mass = 130f,
			AimPenalty = 3f, RideStamina = 0.7f, MaxSpeed = 6f, Boost = 1.3f, Acceleration = 7f, TurnSpeed = 95f, Grip = 7f,
			HoverHeight = 0.9f, HopHeight = 2.5f, FallGravity = 1.1f, CrashSpeed = 10f, DeepWaterSpeed = 2.1f,
			MaxClimb = 24f, LoadCapacity = 420f, Durability = 220f, Noise = 1.1f,
			Fx = TrailFx.Motes, Glow = new Color(0.9f, 0.75f, 0.5f),
			RamDamage = 25f, RamPush = 60f,
		},
		new HoverLogVariant("Fish8", "Hover Coral Cod", "A top-level coral cod: smooth and steady, the best fish to shoot from, and swift at sea.")
		{
			WoodItem = "Fish8", Station = Station.ArtisanTable, StationLevel = 1, Boss = Boss.Yagluth, RepairItem = "FishRaw", Recipe = "Fish8:1,Resin:10,GoblinTotem:2",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.5f, Thickness = 1.05f, Mass = 70f,
			AimPenalty = 1f, RideStamina = 0.7f, MaxSpeed = 7f, Boost = 1.35f, Acceleration = 10f, TurnSpeed = 125f, Grip = 9f,
			HoverHeight = 1f, HopHeight = 3f, FallGravity = 1f, CrashSpeed = 8f, DeepWaterSpeed = 2f,
			MaxClimb = 30f, LoadCapacity = 210f, Durability = 130f, Noise = 0.8f,
			Fx = TrailFx.Motes, Glow = new Color(1f, 0.55f, 0.45f),
		},
		new HoverLogVariant("Fish9", "Hover Anglerfish", "A top-level anglerfish whose lure lights the way: a bright lamp for night rides and dark mist, and quiet with it.")
		{
			WoodItem = "Fish9", Station = Station.GaldrTable, StationLevel = 2, Boss = Boss.TheQueen, RepairItem = "FishRaw", Recipe = "Fish9:1,Resin:10,DvergrKeyFragment:1",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.4f, Thickness = 1.15f, Mass = 70f,
			AimPenalty = 3f, RideStamina = 0.8f, MaxSpeed = 7.5f, Boost = 1.4f, Acceleration = 10f, TurnSpeed = 130f, Grip = 8f,
			HoverHeight = 1.3f, HopHeight = 3.5f, FallGravity = 0.7f, CrashSpeed = 7f, DeepWaterSpeed = 1.8f,
			MaxClimb = 32f, LoadCapacity = 180f, Durability = 120f, Noise = 0.5f,
			Fx = TrailFx.Sparkles, Glow = new Color(0.55f, 1f, 0.85f), GlowRange = 14f, GlowIntensity = 2.2f,
		},
		new HoverLogVariant("Fish10", "Hover Salmon", "A top-level northern salmon: born to leap. Hops high, leaps again in mid-air, and runs fast through icy northern seas.")
		{
			WoodItem = "Fish10", Station = Station.BlackForge, StationLevel = 2, Boss = Boss.Fader, RepairItem = "FishRaw", Recipe = "Fish10:1,Resin:10,BellFragment:1",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.5f, Thickness = 0.95f, Mass = 55f,
			AimPenalty = 4f, RideStamina = 0.9f, MaxSpeed = 8.5f, Boost = 1.45f, Acceleration = 12f, TurnSpeed = 140f, Grip = 9f,
			HoverHeight = 1.2f, HopHeight = 5f, AirHops = 1, FallGravity = 0.7f, CrashSpeed = 8f, DeepWaterSpeed = 2.1f,
			MaxClimb = 36f, LoadCapacity = 180f, Durability = 130f, Noise = 0.7f,
			Fx = TrailFx.Sparkles, Glow = new Color(1f, 0.6f, 0.5f),
		},
		new HoverLogVariant("Fish11", "Hover Magmafish", "A top-level magmafish, still smouldering: trails embers, sets what it rams on fire, and runs fast across the Ashlands.")
		{
			WoodItem = "Fish11", Station = Station.BlackForge, StationLevel = 2, Boss = Boss.Fader, RepairItem = "FishRaw", Recipe = "Fish11:1,Resin:10,BellFragment:1",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2.5f, Thickness = 1f, Mass = 70f,
			AimPenalty = 4f, RideStamina = 1f, MaxSpeed = 9f, Boost = 1.45f, Acceleration = 12f, TurnSpeed = 130f, Grip = 9f,
			HoverHeight = 1.1f, HopHeight = 3.5f, FallGravity = 1f, CrashSpeed = 9f, DeepWaterSpeed = 1.5f,
			MaxClimb = 35f, LoadCapacity = 180f, Durability = 150f, Noise = 1f,
			Fx = TrailFx.Embers, Glow = new Color(1f, 0.4f, 0.1f),
			RamDamage = 15f, RamFire = 30f, RamPush = 30f,
		},
		new HoverLogVariant("Fish12", "Hover Pufferfish", "A top-level pufferfish, puffed up and prickly: anything it rams is poisoned, and it trails a toxic cloud.")
		{
			WoodItem = "Fish12", Station = Station.Forge, StationLevel = 5, Boss = Boss.Moder, RepairItem = "FishRaw", Recipe = "Fish12:1,Resin:10,SerpentScale:5",
			TemplateHints = new[] { "wood_pole2", "wood_pole" },
			Length = 2f, Thickness = 1.4f, Mass = 60f,
			AimPenalty = 4f, RideStamina = 0.8f, MaxSpeed = 7f, Boost = 1.35f, Acceleration = 9f, TurnSpeed = 120f, Grip = 8f,
			HoverHeight = 1.2f, HopHeight = 3f, FallGravity = 0.8f, CrashSpeed = 8f, DeepWaterSpeed = 1.8f,
			MaxClimb = 30f, LoadCapacity = 180f, Durability = 120f, Noise = 0.8f,
			Fx = TrailFx.Poison, Glow = new Color(0.9f, 1f, 0.4f), PoisonCloud = 6f,
			RamDamage = 10f, RamPoison = 20f, RamPush = 25f,
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
	// Defaults to WoodItem.
	public string? RepairItem;
	public string Recipe = "";
	public string[] TemplateHints = new string[0];
	public Color Tint = Color.white;
	public float Length = 2.6f;
	public float Thickness = 1f;
	public float Mass = 60f;

	public TrailFx Fx;
	public Color Glow = Color.white;
	public float GlowRange = 3.5f;
	public float GlowIntensity = 1.6f;

	// Fish logs need a high-quality fish (see Fish Quality Required).
	public bool IsFish => WoodItem.StartsWith("Fish");

	// A team-pulled sled (HoverSled) rather than a log.
	public bool Sled;

	// Core Wood and later, and the fish, can take a Surtling core as fuel for good.
	public bool CoreFuel => IsFish || WoodItem is "RoundLog" or "ElderBark" or "YggdrasilWood" or "Blackwood" or "Frostwood";

	// The crafting station it's built at (Crafting Station), climbing with the biome its
	// materials come from.
	public Station Station;
	public int StationLevel = 1;

	// The boss that unlocks it with Require Boss Kills on: the one for its materials' biome.
	public Boss Boss;

	public bool Unlocked(Player player)
	{
		string key = SnakeWoodPlugin.BossKey(RequiredBossCfg.Value);
		return !SnakeWoodPlugin.BossGate.Value || key.Length == 0 || player.NoCostCheat()
			|| (ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(key)) || player.HaveUniqueKey(key);
	}

	// Defaults for the handling config entries below.
	public float MaxClimb, LoadCapacity, Durability, Noise;
	public float AimPenalty, RideStamina, MaxSpeed, Boost, Acceleration, TurnSpeed, Grip, HoverHeight, HopHeight, FallGravity, CrashSpeed, DeepWaterSpeed;
	public int AirHops;
	public float RamDamage, RamChop, RamPickaxe, RamFire, RamFrost, RamPoison, RamPush, PoisonCloud;
	public int RamToolTier;
	// Share of its speed a ram keeps after hitting something it damages (Core Wood ploughs on).
	public float RamKeepSpeed;

	public ConfigEntry<bool> Enabled = null!;
	public ConfigEntry<string> RecipeCfg = null!;
	public ConfigEntry<Station> StationCfg = null!;
	public ConfigEntry<int> StationLevelCfg = null!;
	public ConfigEntry<Boss> RequiredBossCfg = null!;
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
	public ConfigEntry<float> MaxClimbCfg = null!;
	public ConfigEntry<float> LoadCapacityCfg = null!;
	public ConfigEntry<float> DurabilityCfg = null!;
	public ConfigEntry<string> RepairItemCfg = null!;
	public ConfigEntry<bool> CoreFuelCfg = null!;
	public ConfigEntry<float> NoiseCfg = null!;
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
		// Valheim shrinks the tooltip font to fit, so this stays short: the first sentence of the
		// flavour text and a few packed lines of stats. The full story is in the README.
		string Label(string label) => $"<color=orange>{label}</color>";
		int stop = Description.IndexOf(". ");
		string text = (stop > 0 ? Description.Substring(0, stop + 1) : Description) + "\n";

		List<string> needs = new();
		if (StationLevelCfg.Value > 1)
		{
			needs.Add($"station level {StationLevelCfg.Value}");
		}
		if (SnakeWoodPlugin.BossGate.Value && RequiredBossCfg.Value != Boss.None)
		{
			needs.Add("beat " + RequiredBossCfg.Value switch
			{
				Boss.TheElder => "The Elder",
				Boss.TheQueen => "The Queen",
				Boss boss => boss.ToString(),
			});
		}
		if (IsFish && SnakeWoodPlugin.FishQuality.Value > 1)
		{
			needs.Add($"a level {SnakeWoodPlugin.FishQuality.Value} fish");
		}
		if (needs.Count > 0)
		{
			text += $"\n{Label("Needs")} {string.Join(", ", needs)}";
		}

		if (Sled)
		{
			text += $"\n{Label("Team")} 2 tamed wolves (strong) or harpooned hares (fast); stars make them better";
			text += $"\n{Label("Carries")} a cart-sized chest and a passenger; the team's strength sets the load";
			text += $"\n{Label("Rider")} {RideStaminaCfg.Value:0.#} stamina/s while moving, none standing still";
			return text;
		}
		string hop = HopHeightCfg.Value > 0f ? $"hop {HopHeightCfg.Value:0.#} m" + (AirHopsCfg.Value > 0 ? $" +{AirHopsCfg.Value} air" : "") : "no hop";
		text += $"\n{Label("Speed")} {MaxSpeedCfg.Value:0.#} m/s (x{BoostCfg.Value:0.##}), sea {DeepWaterSpeedCfg.Value * 100f:0}%, turns {TurnSpeedCfg.Value:0}°/s";
		text += $"\n{Label("Hover")} {HoverHeightCfg.Value:0.#} m, {hop}, climbs {MaxClimbCfg.Value:0}°" + (FallGravityCfg.Value < 0.75f ? ", glides" : "");
		text += $"\n{Label("Rider")} {(RideStaminaCfg.Value > 0f ? $"{RideStaminaCfg.Value:0.#} stamina/s" : "no stamina")}, "
			+ (CrashSpeedCfg.Value > 0f ? $"thrown at {CrashSpeedCfg.Value:0.#} m/s" : "never thrown") + $", aim +{AimPenaltyCfg.Value:0.#}°";
		text += $"\n{Label("Carries")} {LoadCapacityCfg.Value:0}" + (SnakeWoodPlugin.WearEnabled.Value ? $", durability {DurabilityCfg.Value:0}" : "")
			+ (NoiseCfg.Value < 0.75f ? ", quiet" : NoiseCfg.Value > 1.25f ? ", loud" : "");

		List<string> extras = new();
		if (RamDamageCfg.Value > 0f)
		{
			string ram = $"ram {RamDamageCfg.Value:0}";
			if (RamChop > 0f) ram += ", fells trees";
			if (RamPickaxe > 0f) ram += ", mines rock and ore";
			if (RamPoison > 0f) ram += ", poisons";
			if (RamFire > 0f) ram += ", burns";
			if (RamFrost > 0f) ram += ", chills";
			extras.Add(ram);
		}
		if (PoisonCloudCfg != null && PoisonCloudCfg.Value > 0f)
		{
			extras.Add($"poison cloud {PoisonCloudCfg.Value:0}");
		}
		if (extras.Count > 0)
		{
			text += $"\n{Label("Special")} {string.Join("; ", extras)}";
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
		StationCfg = config.Bind(section, "Crafting Station", Station,
			"The crafting station it has to be built near.");
		StationLevelCfg = config.Bind(section, "Crafting Station Level", StationLevel,
			new ConfigDescription("The level that station needs (upgraded with its extensions, like the Forge's bellows and anvils) to build this log.", new AcceptableValueRange<int>(1, 10)));
		RequiredBossCfg = config.Bind(section, "Required Boss", Boss,
			"The boss that has to be defeated first, when Require Boss Kills (3 - Balance) is on. None: no boss needed.");
		AimPenaltyCfg = config.Bind(section, "Aim Penalty", AimPenalty,
			new ConfigDescription("Extra spread, in degrees, on arrows and other projectiles fired from this log, even standing still. Speed adds more on top (see Aim Penalty Per Speed).", new AcceptableValueRange<float>(0f, 45f)));
		RideStaminaCfg = config.Bind(section, "Riding Stamina Per Second", RideStamina,
			new ConfigDescription("Stamina it takes to keep your balance, drained the whole time you ride (and stamina doesn't regenerate meanwhile). When it runs out you lose your grip and fall off. 0 makes riding free.", new AcceptableValueRange<float>(0f, 20f)));
		MaxSpeedCfg = config.Bind(section, "Top Speed", MaxSpeed,
			new ConfigDescription("Top speed in metres per second without boost. A running player does about 7, a ridden Lox a little more. Heavy loads lower it (see Load Capacity).", new AcceptableValueRange<float>(1f, 60f)));
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
		MaxClimbCfg = config.Bind(section, "Max Climb Angle", MaxClimb,
			new ConfigDescription("Steepest slope, in degrees, the log can drive up. Steeper ground stops it (hop over it or go round), and the nose never tips further than this.", new AcceptableValueRange<float>(5f, 80f)));
		LoadCapacityCfg = config.Bind(section, "Load Capacity", LoadCapacity,
			new ConfigDescription("The inventory weight (your carry weight, gear included) where it starts to really struggle. Every bit of weight slows it, lowers it and dulls its handling a little; past this it gets bad fast (see Performance At Load Capacity and Overload Falloff under 3 - Balance).", new AcceptableValueRange<float>(10f, 5000f)));
		DurabilityCfg = config.Bind(section, "Durability", Durability,
			new ConfigDescription("How much wear (crashes, rams, riding) it takes before it's too battered to fly. See 5 - Wear.", new AcceptableValueRange<float>(1f, 5000f)));
		RepairItemCfg = config.Bind(section, "Repair Item", RepairItem ?? WoodItem,
			"What repairs it (the game's item name): Shift+E on a worn log uses these before adding fuel. Each fixes Repair Per Item wear (see 5 - Wear).");
		CoreFuelCfg = config.Bind(section, "Lifetime Fuel", CoreFuel && !Sled,
			"On: one Lifetime Fuel Item (a Surtling core, see 4 - Fuel) fuels it for good.");
		NoiseCfg = config.Bind(section, "Noise", Noise,
			new ConfigDescription("Multiplies how far away creatures hear you riding it (see Noise settings under 3 - Balance).", new AcceptableValueRange<float>(0f, 5f)));
		if (SnakeWoodPlugin.ResetV5 && Key == "RoundLog")
		{
			AccelerationCfg.Value = Acceleration;
			MaxClimbCfg.Value = MaxClimb;
		}
		if (SnakeWoodPlugin.ResetLoads)
		{
			LoadCapacityCfg.Value = LoadCapacity;
		}
		if (SnakeWoodPlugin.ResetRecipes && !IsFish)
		{
			RecipeCfg.Value = Recipe;
		}
		if (SnakeWoodPlugin.ResetSpeeds)
		{
			MaxSpeedCfg.Value = MaxSpeed;
			BoostCfg.Value = Boost;
		}
		RamDamageCfg = config.Bind(section, "Ram Damage", RamDamage,
			new ConfigDescription("Damage dealt at full speed to what the log runs into. Scales down at lower speeds. 0 turns ramming off.", new AcceptableValueRange<float>(0f, 500f)));
		if (SnakeWoodPlugin.ResetV5 && Key == "RoundLog")
		{
			RamDamageCfg.Value = RamDamage;
		}
		if (Fx == TrailFx.Poison)
		{
			PoisonCloudCfg = config.Bind(section, "Poison Cloud Damage", PoisonCloud,
				new ConfigDescription("Poison dealt to each creature caught in the trailing cloud, at most once a second. 0 keeps the cloud just for looks.", new AcceptableValueRange<float>(0f, 100f)));
		}
	}
}
