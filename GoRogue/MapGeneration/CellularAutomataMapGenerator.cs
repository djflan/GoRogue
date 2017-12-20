﻿using GoRogue.Random;
using System;
using System.Collections.Generic;

namespace GoRogue.MapGeneration
{
	/// <summary>
	/// Uses a cellular automata genereation algorithm to generate a cave-like map.
	/// </summary>
	/// <remarks>
	/// Generates a map by randomly filling the map surface with floor or wall values (true and false respectively) based on a probability
	/// given, then iteratively smoothing it via the process outlined in the cited roguebasin article.
	/// 
	/// After generate is called, the passed in map will have had a value of true set to all floor tiles, and a value of false set to all wall tiles.
	///
	/// Like RandomRoomsMapGenerator, it is recommended to use ArrayMapOf&lt;bool&gt; as the SettableMapOf instance passed in, as this class must
	/// call set for each location very likely more than one time, overwriting any previous values(thus doing significant processing on each
	/// call to set is inadvisable).  It will likely be faster to take the resulting ArrayMapOf after completion, and process it to do any required
	/// translation.
	///
	/// Based on the C# roguelike library RogueSharp's implementation, and the roguebasin article below:
	/// http://www.roguebasin.com/index.php?title=Cellular_Automata_Method_for_Generating_Random_Cave-Like_Levels.
	/// </remarks>
	public class CellularAutomataMapGenerator : IMapGenerator
	{
		/// <summary>
		/// Represents the percent chance that a given cell will be a floor cell when the map is initially
		/// randomly filled.  Recommended to be in range [40, 60] (40 is used in roguebasin article).
		/// </summary>
		public int FillProbability;
		/// <summary>
		/// Total number of times the cellular automata-based smoothing algorithm is executed.
		/// Recommended to be in range [2, 10] (7 is used on roguebasin article).
		/// </summary>
		public int TotalIterations;
		/// <summary>
		/// Total number of times the cellular automata smoothing variation that is more likely to result
		/// in "breaking up" large areas will be run before switching to the more standard nearest neighbors
		/// version.  Recommended to be in range [2, 7] (4 is used in roguebasin article).
		/// </summary>
		public int CutoffBigAreaFill;

		private ISettableMapOf<bool> map;
		private IRandom rng;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="map">The map to fill with values when generate is called.</param>
		/// <param name="rng">The RNG to use to initially fill the map.</param>
		/// <param name="fillProbability">Represents the percent chance that a given cell will be a floor cell
		/// when the map is initially randomly filled.  Recommended to be in range [40, 60] (40 is used in
		/// the roguebasin article).</param>
		/// <param name="totalIterations">Total number of times the cellular automata-based smoothing algorithm
		/// is executed. Recommended to be in range [2, 10] (7 is used on roguebasin article).</param>
		/// <param name="cutoffBigAreaFill">Total number of times the cellular automata smoothing variation
		/// that is more likely to result in "breaking up" large areas will be run before switching to the
		/// more standard nearest neighbors version.  Recommended to be in range [2, 7] (4 is used in roguebasin
		/// article).</param>
		public CellularAutomataMapGenerator(ISettableMapOf<bool> map, IRandom rng, int fillProbability = 40, int totalIterations = 7, int cutoffBigAreaFill = 4)
		{
			this.map = map;
			FillProbability = fillProbability;
			TotalIterations = totalIterations;
			CutoffBigAreaFill = cutoffBigAreaFill;
			this.rng = rng;
		}

		/// <summary>
		/// Generates the map.  Floor tiles will be set to true in the provided map, and wall tiles will be
		/// set to false.
		/// </summary>
		public void Generate()
		{
			randomlyFillCells();

			for (int i = 0; i < TotalIterations; i++)
			{
				if (i < CutoffBigAreaFill)
					cellAutoBigAreaAlgo();
				else
					cellAutoNearestNeighborsAlgo();
			}

			// Ensure it's enclosed before we try to connect, so we can't possibly connect a path that ruins the enclosure.
			// Doing this before connection ensures that filling it can't kill the path to an area.
			fillToRectangle();
			connectCaves();
		}

		private void randomlyFillCells()
		{
			for (int x = 0; x < map.Width; x++)
				for (int y = 0; y < map.Height; y++)
				{
					if (x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1) // Borders are always walls
						map[x, y] = false;
					else if (rng.Next(99) < FillProbability)
						map[x, y] = true;
					else
						map[x, y] = false;
				}
		}

		private void fillToRectangle()
		{
			for (int x = 0; x < map.Width; x++)
			{
				map[x, 0] = false;
				map[x, map.Height - 1] = false;
			}

			for (int y = 0; y < map.Height; y++)
			{
				map[0, y] = false;
				map[map.Width - 1, y] = false;
			}
		}

		private void cellAutoBigAreaAlgo()
		{
			var oldMap = new ArrayMapOf<bool>(map.Width, map.Height);

			for (int x = 0; x < map.Width; x++)
				for (int y = 0; y < map.Height; y++)
					oldMap[x, y] = map[x, y];

			for (int x = 0; x < map.Width; x++)
				for (int y = 0; y < map.Height; y++)
				{
					if (x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1)
						continue;

					if (countWallsNear(oldMap, x, y, 1) >= 5 || countWallsNear(oldMap, x, y, 2) <= 2)
						map[x, y] = false;
					else
						map[x, y] = true;
				}
		}

		private void cellAutoNearestNeighborsAlgo()
		{
			var oldMap = new ArrayMapOf<bool>(map.Width, map.Height);

			for (int x = 0; x < map.Width; x++)
				for (int y = 0; y < map.Height; y++)
					oldMap[x, y] = map[x, y];

			for (int x = 0; x < map.Width; x++)
				for (int y = 0; y < map.Height; y++)
				{
					if (x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1)
						continue;

					if (countWallsNear(oldMap, x, y, 1) >= 5)
						map[x, y] = false;
					else
						map[x, y] = true;
				}
		}

		private void connectCaves()
		{
			var areaFinder = new MapAreaFinder(map, Distance.MANHATTAN);
			areaFinder.FindMapAreas();

			var ds = new DisjointSet(areaFinder.Count);
			while (ds.Count > 1) // Haven't unioned all sets into one
			{
				for (int i = 0; i < areaFinder.Count; i++)
				{
					int iClosest = findNearestMapArea(areaFinder.MapAreas, i, ds);

					int iCoordIndex = rng.Next(areaFinder.MapAreas[i].Positions.Count - 1);
					int iClosestCoordIndex = rng.Next(areaFinder.MapAreas[iClosest].Positions.Count - 1);
					// Choose from MapArea to make sure we actually get an open Coord on both sides
					List<Coord> tunnelPositions = Coord.CardinalPositionsOnLine(areaFinder.MapAreas[i].Positions[iCoordIndex],
																		areaFinder.MapAreas[iClosest].Positions[iClosestCoordIndex]);

					Coord previous = null;
					foreach (var pos in tunnelPositions)
					{
						if (pos == null)
							throw new Exception("Bad nullage");

						if (map == null)
							throw new Exception("Really bad nullage");

						map[pos] = true;
						// Previous cell, and we're going vertical, go 2 wide so it looks nicer
						// Make sure not to break rectangles (less than last index)!
						if (previous != null)
							if (pos.Y != previous.Y)
								if (pos.X + 1 < map.Width - 1)
									map[pos.X + 1, pos.Y] = true;

						previous = pos;
					}
					ds.MakeUnion(i, iClosest);
				}
			}
		}

		private static int findNearestMapArea(IList<MapArea> mapAreas, int mapAreaIndex, DisjointSet ds)
		{
			int closestIndex = mapAreaIndex;
			double distance = double.MaxValue;

			for (int i = 0; i < mapAreas.Count; i++)
			{
				if (i == mapAreaIndex)
					continue;

				if (ds.InSameSet(i, mapAreaIndex))
					continue;

				double distanceBetween = Distance.MANHATTAN.DistanceBetween(mapAreas[mapAreaIndex].Bounds.Center, mapAreas[i].Bounds.Center);
				if (distanceBetween < distance)
				{
					distance = distanceBetween;
					closestIndex = i;
				}
			}

			return closestIndex;
		}

		private static int countWallsNear(ISettableMapOf<bool> mapToUse, int posX, int posY, int distance)
		{
			int count = 0;
			int xMin = Math.Max(posX - distance, 0);
			int xMax = Math.Min(posX + distance, mapToUse.Width - 1);
			int yMin = Math.Max(posY - distance, 0);
			int yMax = Math.Min(posY + distance, mapToUse.Height - 1);

			for (int x = xMin; x <= xMax; x++)
				for (int y = yMin; y <= yMax; y++)
				{
					if (x == posX && y == posY)
						continue;

					if (!mapToUse[x, y])
						++count;
				}

			return count;
		}
	}
}