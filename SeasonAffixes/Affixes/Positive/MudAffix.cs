﻿using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using System.Collections.Generic;

namespace Shockah.SeasonAffixes;

internal sealed class MudAffix : BaseSeasonAffix, ISeasonAffix
{
	private static bool IsHarmonySetup = false;

	private static string ShortID => "Mud";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description");
	public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(288, 208, 16, 16));

	public MudAffix() : base(ShortID, "positive") { }

	public int GetPositivity(OrdinalSeason season)
		=> 1;

	public int GetNegativity(OrdinalSeason season)
		=> 0;

	public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.CropsAspect, VanillaSkill.FlowersAspect };

	public double GetProbabilityWeight(OrdinalSeason season)
	{
		if (Game1.whichFarm != 6)
			return 0;
		if (Mod.Config.ChoicePeriod == AffixSetChoicePeriod.Day)
			return 0;
		if (!Mod.Config.WinterCrops && season.Season == Season.Winter)
			return 0;
		return 1;
	}

	public void OnRegister()
		=> Apply(Mod.Harmony);

	private void Apply(Harmony harmony)
	{
		if (IsHarmonySetup)
			return;
		IsHarmonySetup = true;

		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(GameLocation), nameof(GameLocation.doesTileHaveProperty)),
			postfix: new HarmonyMethod(GetType(), nameof(GameLocation_doesTileHaveProperty_Postfix))
		);

		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(GameLocation), nameof(GameLocation.doesTileHavePropertyNoNull)),
			postfix: new HarmonyMethod(GetType(), nameof(GameLocation_doesTileHavePropertyNoNull_Postfix))
		);
	}

	private static void GameLocation_doesTileHaveProperty_Postfix(GameLocation __instance, string propertyName, ref string? __result)
	{
		if (!Mod.IsAffixActive(a => a is MudAffix))
			return;
		if (__instance is Farm && propertyName == "NoSprinklers")
			__result = null;
	}

	private static void GameLocation_doesTileHavePropertyNoNull_Postfix(GameLocation __instance, string propertyName, ref string __result)
	{
		if (!Mod.IsAffixActive(a => a is MudAffix))
			return;
		if (__instance is Farm && propertyName == "NoSprinklers")
			__result = "";
	}
}