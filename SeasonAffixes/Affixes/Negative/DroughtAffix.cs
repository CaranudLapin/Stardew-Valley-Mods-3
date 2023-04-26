﻿using HarmonyLib;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace Shockah.SeasonAffixes.Affixes.Negative
{
	internal sealed class DroughtAffix : BaseSeasonAffix
	{
		private static bool IsHarmonySetup = false;

		private static string ShortID => "Drought";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.name");
		public override string LocalizedDescription => Mod.Helper.Translation.Get($"affix.negative.{ShortID}.description");
		public override TextureRectangle Icon => new(Game1.mouseCursors, new(413, 333, 13, 13));

		public override int GetPositivity(OrdinalSeason season)
			=> 0;

		public override int GetNegativity(OrdinalSeason season)
			=> 1;

		public override IReadOnlySet<string> Tags
			=> new HashSet<string> { VanillaSkill.CropsAspect, VanillaSkill.FishingAspect };

		public override double GetProbabilityWeight(OrdinalSeason season)
			=> season.Season == Season.Winter ? 0 : 1;

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatch(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(Game1), nameof(Game1.getWeatherModificationsForDate)),
				postfix: new HarmonyMethod(AccessTools.Method(typeof(DroughtAffix), nameof(Game1_getWeatherModificationsForDate_Postfix)))
			);
		}

		private static void Game1_getWeatherModificationsForDate_Postfix(ref int __result)
		{
			if (!Mod.ActiveAffixes.Any(a => a is DroughtAffix))
				return;
			if (__result is Game1.weather_rain or Game1.weather_lightning)
				__result = Game1.weather_sunny;
		}
	}
}