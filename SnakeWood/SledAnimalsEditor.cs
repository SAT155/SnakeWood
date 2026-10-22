using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace SnakeWood;

// Picked up by name by BepInEx's Configuration Manager (F1), which reads these fields from any
// tag of this type name. Only the ones we use are declared.
internal sealed class ConfigurationManagerAttributes
{
	public Action<ConfigEntryBase>? CustomDrawer;
}

// The Team Animals box in the Configuration Manager: a text field that suggests creature prefab
// names as you type the name part of the entry you're on, flags names the game doesn't know, and
// fills in sensible stats when you pick a suggestion.
internal static class SledAnimalsEditor
{
	private const string NewStats = ":8:100:1:0.8";

	// Used before a world is loaded, when the game's creature list isn't available.
	private static readonly string[] s_fallback =
	{
		"Wolf", "Hare", "Boar", "Deer", "Lox", "Neck", "Greyling", "Greydwarf", "Asksvin", "Hen", "Chicken",
		"Fox", "Volture", "Seeker", "Tick", "Troll", "Bjorn", "Unbjorn", "Fenring", "Ulv", "Serpent",
	};

	private static string[]? s_creatures;

	private static IEnumerable<string> Creatures()
	{
		if (s_creatures == null && ZNetScene.instance != null)
		{
			s_creatures = ZNetScene.instance.m_prefabs
				.Where(p => p != null && p.GetComponent<Character>() != null && p.GetComponent<Player>() == null)
				.Select(p => p.name).Distinct().OrderBy(n => n).ToArray();
		}
		return s_creatures ?? s_fallback;
	}

	public static void Draw(ConfigEntryBase entry)
	{
		string value = (string)entry.BoxedValue;
		GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
		string edited = GUILayout.TextField(value, GUILayout.ExpandWidth(true));
		if (edited != value)
		{
			entry.BoxedValue = value = edited;
		}

		string[] entries = value.Split(',');
		string current = entries[entries.Length - 1];
		string typed = current.Split(':')[0].Trim();
		List<string> names = Creatures().ToList();
		if (typed.Length > 0 && !current.Contains(':'))
		{
			List<string> matches = names
				.Where(n => n.IndexOf(typed, StringComparison.OrdinalIgnoreCase) >= 0)
				.OrderBy(n => n.StartsWith(typed, StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(n => n.Length)
				.Take(6).ToList();
			if (matches.Count > 0)
			{
				GUILayout.BeginHorizontal();
				foreach (string match in matches)
				{
					if (GUILayout.Button(match, GUILayout.ExpandWidth(false)))
					{
						entries[entries.Length - 1] = match + NewStats;
						entry.BoxedValue = string.Join(",", entries);
						GUI.FocusControl(null);
					}
				}
				GUILayout.EndHorizontal();
			}
		}

		// Names the game doesn't know (only checkable in a world).
		if (s_creatures != null)
		{
			string[] unknown = entries.Select(e => e.Split(':')[0].Trim())
				.Where(n => n.Length > 0 && !names.Contains(n)).ToArray();
			if (unknown.Length > 0 && !(unknown.Length == 1 && unknown[0] == typed && !current.Contains(':')))
			{
				GUILayout.Label($"<color=#ff8866>Not a creature: {string.Join(", ", unknown)}</color>");
			}
		}
		GUILayout.Label("<color=#aaaaaa>Prefab:Speed:Strength:Land:Water, comma separated</color>");
		GUILayout.EndVertical();
	}
}
