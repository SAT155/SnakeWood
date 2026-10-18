using UnityEngine;

namespace SnakeWood;

// The magic: a coloured glow under each log and a particle trail per wood. Built on every game
// that loads the log (not networked): trails emit by distance moved, so they follow the synced
// transform on watchers' games too. Particles reuse a material from the game's own effects, with
// a soft round texture drawn here, so no asset bundle is needed.
public static class HoverLogFx
{
	private static Material? s_material;
	private static bool s_searched;

	public static void Attach(HoverLog log, HoverLogVariant variant)
	{
		if (SnakeWoodPlugin.Glow.Value)
		{
			GameObject glow = new("snakewood_glow");
			glow.transform.SetParent(log.transform, false);
			glow.transform.localPosition = new Vector3(0f, -0.6f, 0f);
			Light light = glow.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = variant.Glow;
			light.range = 3.5f;
			light.intensity = 1.6f;
			light.shadows = LightShadows.None;
		}

		Material? material = SnakeWoodPlugin.Trails.Value ? ParticleMaterial() : null;
		if (material == null)
		{
			return;
		}
		GameObject trail = new("snakewood_trail");
		trail.transform.SetParent(log.transform, false);
		trail.transform.localPosition = new Vector3(0f, -0.2f, -0.4f * variant.Length);
		ParticleSystem particles = trail.AddComponent<ParticleSystem>();
		particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

		ParticleSystem.MainModule main = particles.main;
		main.loop = true;
		main.playOnAwake = true;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
		main.maxParticles = 400;
		main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
		ParticleSystem.EmissionModule emission = particles.emission;
		emission.rateOverTime = 0f;
		ParticleSystem.ShapeModule shape = particles.shape;
		shape.shapeType = ParticleSystemShapeType.Sphere;
		shape.radius = 0.3f;

		Color color = variant.Glow;
		float lifetime, sizeMin, sizeMax, perMetre, gravity;
		switch (variant.Fx)
		{
			case TrailFx.Poison:
				// A thick, slow-drifting green cloud that lingers where the log has been.
				color = new Color(0.35f, 0.75f, 0.2f, 0.2f);
				lifetime = PoisonCloud.Lifetime; sizeMin = 0.8f; sizeMax = 1.5f; perMetre = 0.9f; gravity = -0.01f;
				shape.radius = 0.8f;
				break;
			case TrailFx.Sparkles:
				// Small bright motes that drift upward, alternating sky blue and gold.
				lifetime = 2.5f; sizeMin = 0.08f; sizeMax = 0.22f; perMetre = 6f; gravity = -0.15f;
				shape.radius = 0.5f;
				break;
			case TrailFx.Embers:
				color = new Color(1f, 0.5f, 0.15f, 1f);
				lifetime = 1.2f; sizeMin = 0.06f; sizeMax = 0.16f; perMetre = 5f; gravity = -0.3f;
				break;
			case TrailFx.Dust:
				// A light haze of kicked-up dust, not a smoke screen.
				color = new Color(0.6f, 0.52f, 0.42f, 0.12f);
				lifetime = 1.2f; sizeMin = 0.2f; sizeMax = 0.45f; perMetre = 0.6f; gravity = 0.02f;
				shape.radius = 0.5f;
				break;
			default:
				color.a = 0.8f;
				lifetime = 1f; sizeMin = 0.05f; sizeMax = 0.12f; perMetre = 2.5f; gravity = -0.05f;
				break;
		}
		float opacity = SnakeWoodPlugin.TrailOpacity.Value;
		if (variant.Fx != TrailFx.Sparkles)
		{
			color.a *= opacity;
			main.startColor = color;
		}
		else
		{
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.85f, 1f, opacity), new Color(1f, 0.85f, 0.4f, opacity));
		}
		main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
		main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
		main.gravityModifier = gravity;
		emission.rateOverDistance = perMetre;

		ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
		fade.enabled = true;
		Gradient gradient = new();
		gradient.SetKeys(
			new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
			new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
		fade.color = new ParticleSystem.MinMaxGradient(gradient);

		ParticleSystemRenderer renderer = trail.GetComponent<ParticleSystemRenderer>();
		renderer.sharedMaterial = material;
		renderer.renderMode = ParticleSystemRenderMode.Billboard;
		particles.Play();
	}

	// Borrows the first particle material in the game that has a texture, and swaps in a soft dot.
	private static Material? ParticleMaterial()
	{
		if (s_searched)
		{
			return s_material;
		}
		if (ZNetScene.instance == null)
		{
			return null;
		}
		s_searched = true;
		foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
		{
			if (prefab == null)
			{
				continue;
			}
			foreach (ParticleSystemRenderer candidate in prefab.GetComponentsInChildren<ParticleSystemRenderer>(true))
			{
				Material shared = candidate.sharedMaterial;
				if (shared == null || shared.shader == null || !shared.HasProperty("_MainTex")
					|| shared.shader.name.IndexOf("particle", System.StringComparison.OrdinalIgnoreCase) < 0)
				{
					continue;
				}
				s_material = new Material(shared) { name = "snakewood_particle", mainTexture = SoftDot() };
				if (s_material.HasProperty("_Color"))
				{
					s_material.SetColor("_Color", Color.white);
				}
				if (s_material.HasProperty("_TintColor"))
				{
					s_material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
				}
				SnakeWoodPlugin.Log.LogInfo($"Trail particles use the {shared.shader.name} shader from {prefab.name}.");
				return s_material;
			}
		}
		SnakeWoodPlugin.Log.LogWarning("No particle material found in the game; logs won't leave trails.");
		return null;
	}

	private static Texture2D SoftDot()
	{
		const int size = 64;
		Texture2D texture = new(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
		Color[] pixels = new Color[size * size];
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dx = (x + 0.5f) / size * 2f - 1f;
				float dy = (y + 0.5f) / size * 2f - 1f;
				float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
				pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
			}
		}
		texture.SetPixels(pixels);
		texture.Apply();
		return texture;
	}
}

// Shared by the trail's look (HoverLogFx) and its damage (HoverLog.UpdatePoisonCloud).
internal static class PoisonCloud
{
	public const float Lifetime = 4f;
	public const float Radius = 2f;
}
