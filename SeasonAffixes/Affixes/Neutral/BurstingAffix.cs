﻿using HarmonyLib;
using Microsoft.Xna.Framework;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes.Affixes.Neutral
{
	internal sealed class BurstingAffix : BaseSeasonAffix
	{
		private static bool IsHarmonySetup = false;
		private static readonly WeakCounter<GameLocation> MonsterDropCallCounter = new();

		private static string ShortID => "Bursting";
		public override string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public override string LocalizedName => Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.name");
		public override TextureRectangle Icon => new(Game1.objectSpriteSheet, new(368, 176, 16, 16));

		public override string LocalizedDescription
		{
			get
			{
				float totalWeight = Mod.Config.BurstingNoBombWeight + Mod.Config.BurstingCherryBombWeight + Mod.Config.BurstingBombWeight + Mod.Config.BurstingMegaBombWeight;
				if (Mod.Config.BurstingNoBombWeight > 0f)
					return Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.description.chance", new { Chance = $"{(int)((1f - (Mod.Config.BurstingNoBombWeight / totalWeight)) * 100):0.##}%" });
				else
					return Mod.Helper.Translation.Get($"affix.neutral.{ShortID}.description.always");
			}
		}

		public override int GetPositivity(OrdinalSeason season)
			=> 1;

		public override int GetNegativity(OrdinalSeason season)
		=> 1;

		public override double GetProbabilityWeight(OrdinalSeason season)
		{
			if (Mod.Config.BurstingCherryBombWeight <= 0f && Mod.Config.BurstingBombWeight <= 0f && Mod.Config.BurstingMegaBombWeight <= 0f)
				return 0; // invalid config, skipping affix
			return 1;
		}

		public override IReadOnlySet<string> Tags
			=> new HashSet<string> { VanillaSkill.MetalAspect, VanillaSkill.GemAspect, VanillaSkill.Combat.UniqueID };

		public override void OnRegister()
			=> Apply(Mod.Harmony);

		public override void SetupConfig(IManifest manifest)
		{
			var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
			GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
			helper.AddNumberOption($"affix.neutral.{ShortID}.config.weight.noBomb", () => Mod.Config.BurstingNoBombWeight, min: 0f, max: 10f, interval: 0.1f);
			helper.AddNumberOption($"affix.neutral.{ShortID}.config.weight.cherryBomb", () => Mod.Config.BurstingBombWeight, min: 0f, max: 10f, interval: 0.1f);
			helper.AddNumberOption($"affix.neutral.{ShortID}.config.weight.bomb", () => Mod.Config.BurstingCherryBombWeight, min: 0f, max: 10f, interval: 0.1f);
			helper.AddNumberOption($"affix.neutral.{ShortID}.config.weight.megaBomb", () => Mod.Config.BurstingMegaBombWeight, min: 0f, max: 10f, interval: 0.1f);
		}

		private void Apply(Harmony harmony)
		{
			if (IsHarmonySetup)
				return;
			IsHarmonySetup = true;

			harmony.TryPatchVirtual(
				monitor: Mod.Monitor,
				original: () => AccessTools.Method(typeof(GameLocation), nameof(GameLocation.monsterDrop)),
				prefix: new HarmonyMethod(AccessTools.Method(typeof(BurstingAffix), nameof(GameLocation_monsterDrop_Prefix)), priority: Priority.First),
				finalizer: new HarmonyMethod(AccessTools.Method(typeof(BurstingAffix), nameof(GameLocation_monsterDrop_Finalizer)), priority: Priority.Last)
			);
		}

		private static bool TryToPlaceItemRecursively(int itemIndex, GameLocation location, Vector2 centerTile, Farmer player, int maxIterations = 16)
		{
			Queue<Vector2> queue = new();
			queue.Enqueue(centerTile);
			List<Vector2> list = new();
			for (int i = 0; i < maxIterations; i++)
			{
				if (queue.Count <= 0)
					break;

				Vector2 vector = queue.Dequeue();
				list.Add(vector);
				if (!location.isTileOccupied(vector, "ignoreMe") && IsTileOnClearAndSolidGround(location, vector) && location.isTileOccupiedByFarmer(vector) is null && location.doesTileHaveProperty((int)vector.X, (int)vector.Y, "Type", "Back") is not null && location.doesTileHaveProperty((int)vector.X, (int)vector.Y, "Type", "Back").Equals("Stone"))
				{
					PlaceItem(itemIndex, location, vector, player);
					return true;
				}

				Vector2[] directionsTileVectors = Utility.DirectionsTileVectors;
				foreach (Vector2 vector2 in directionsTileVectors)
					if (!list.Contains(vector + vector2))
						queue.Enqueue(vector + vector2);
			}

			return false;
		}

		private static bool IsTileOnClearAndSolidGround(GameLocation location, Vector2 v)
		{
			if (location.map.GetLayer("Back").Tiles[(int)v.X, (int)v.Y] is null)
				return false;
			if (location.map.GetLayer("Front").Tiles[(int)v.X, (int)v.Y] is not null || location.map.GetLayer("Buildings").Tiles[(int)v.X, (int)v.Y] is not null)
				return false;
			if (location is MineShaft && location.getTileIndexAt((int)v.X, (int)v.Y, "Back") == 77)
				return false;
			return true;
		}

		private static void PlaceItem(int itemIndex, GameLocation location, Vector2 point, Farmer player)
		{
			var bomb = new SObject(point, itemIndex, 1);
			bomb.placementAction(location, (int)point.X * 64, (int)point.Y * 64, player);
		}

		private static void GameLocation_monsterDrop_Prefix(GameLocation __instance, Monster __0, Farmer __3)
		{
			if (!Mod.ActiveAffixes.Any(a => a is BurstingAffix))
				return;

			uint counter = MonsterDropCallCounter.Push(__instance);
			if (counter != 1)
				return;

			WeightedRandom<int?> weightedRandom = new();
			weightedRandom.Add(new(Mod.Config.BurstingNoBombWeight, null));
			weightedRandom.Add(new(Mod.Config.BurstingCherryBombWeight, 286));
			weightedRandom.Add(new(Mod.Config.BurstingBombWeight, 287));
			weightedRandom.Add(new(Mod.Config.BurstingMegaBombWeight, 288));

			int? itemToSpawn = weightedRandom.Next(Game1.random);
			if (itemToSpawn is null)
				return;

			TryToPlaceItemRecursively(itemToSpawn.Value, __instance, __0.getTileLocation(), __3, 50);
		}

		private static void GameLocation_monsterDrop_Finalizer(GameLocation __instance)
		{
			if (!Mod.ActiveAffixes.Any(a => a is BurstingAffix))
				return;
			MonsterDropCallCounter.Pop(__instance);
		}
	}
}