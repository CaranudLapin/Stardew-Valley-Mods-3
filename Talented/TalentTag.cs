﻿using Shockah.Kokoro.Stardew.Skill;
using Shockah.Kokoro.UI;
using System;

namespace Shockah.Talented
{
	public record BasicTalentTag(
		string UniqueID,
		Func<TextureRectangle> IconProvider,
		Func<string> NameProvider,
		ITalentTag? Parent = null
	) : ITalentTag
	{
		public TextureRectangle Icon
			=> IconProvider();

		public string Name
			=> NameProvider();
	}

	public record SkillTalentTag(
		string UniqueID,
		ISkill Skill
	) : ITalentTag
	{
		public TextureRectangle Icon
			=> Skill.Icon!;

		public string Name
			=> Skill.Name;
	}
}
