﻿using JetBrains.Annotations;
using SadRogue.Primitives;

namespace GoRogue.MapGeneration.ConnectionPointSelectors
{
    /// <summary>
    /// Implements a the selection algorithm that selects the two points closest to each other in the given <see cref="Area"/> instances.
    /// </summary>
    [PublicAPI]
    public class ClosestConnectionPointSelector : IConnectionPointSelector
    {
        /// <summary>
        /// Distance calculation to use to determine closeness.
        /// </summary>
        public readonly Distance DistanceCalculation;

        /// <summary>
        /// Creates a new point selector.
        /// </summary>
        /// <param name="distanceCalculation">Distance calculation to use to determine closeness.</param>
        public ClosestConnectionPointSelector(Distance distanceCalculation) => DistanceCalculation = distanceCalculation;

        /// <inheritdoc/>
        public (Point area1Position, Point area2Position) SelectConnectionPoints(IReadOnlyArea area1, IReadOnlyArea area2)
        {
            Point c1 = Point.None;
            Point c2 = Point.None;
            double minDist = double.MaxValue;

            foreach (var point1 in area1.Positions)
                foreach (var point2 in area2.Positions)
                {
                    double distance = DistanceCalculation.Calculate(point1, point2);
                    if (distance < minDist)
                    {
                        c1 = point1;
                        c2 = point2;
                        minDist = distance;
                    }
                }

            return (c1, c2);
        }
    }
}
