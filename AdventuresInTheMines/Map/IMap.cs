﻿using Shockah.CommonModCode;
using System;

namespace Shockah.AdventuresInTheMines.Map
{
	public interface IMap<TTile>
	{
		TTile this[IntPoint point] { get; }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested in another interface")]
		public interface WithKnownSize : IMap<TTile>
		{
			int MinX { get; }
			int MaxX { get; }
			int MinY { get; }
			int MaxY { get; }

			int Width { get; }
			int Height { get; }
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested in another interface")]
		public interface Writable : IMap<TTile>
		{
			new TTile this[IntPoint point] { get;  set; }
		}
	}
}