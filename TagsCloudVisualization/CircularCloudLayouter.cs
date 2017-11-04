using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NUnit.Framework.Internal.Execution;

namespace TagsCloudVisualization
{
    class CircularCloudLayouter
    {
        private int radius;
        public Point Center { get; }
        public List<Rectangle> Cloud { get; }
        private Dictionary<Quarter, Func<Rectangle, Point>> shifts;

        public CircularCloudLayouter(Point center)
        {
            Center = center;
            radius = 1;
            Cloud = new List<Rectangle>();
            shifts = InitShifts();
        }

        private Dictionary<Quarter, Func<Rectangle, Point>> InitShifts()
        {
            return new Dictionary<Quarter, Func<Rectangle, Point>>
            {
                [Quarter.I] = GetShift(false, false),
                [Quarter.II] = GetShift(true, false),
                [Quarter.III] = GetShift(true, true),
                [Quarter.IV] = GetShift(false, true)
            };
        }

        private Func<Rectangle, Point> GetShift(bool isDx, bool isDy)
        {
            return rectangle => new Point(
                rectangle.X - (isDx ? 1 : 0) * rectangle.Width, 
                rectangle.Y - (isDy ? 1 : 0) * rectangle.Height);
        }

        public Rectangle PutNextRectangle(Size rectangleSize)
        {
            if (!Cloud.Any())
            {
                var rectPlaced = new Point((int) (Center.X - rectangleSize.Width / 2d),
                    (int) (Center.Y - rectangleSize.Height / 2d));
                var rectangle = new Rectangle(rectPlaced, rectangleSize);
                Cloud.Add(rectangle);
                return rectangle;
            }
            Cloud.Add(FindNextRectangle(rectangleSize));
            return Cloud.Last();
        }

        private Rectangle FindNextRectangle(Size rectangleSize)
        {
            while (true)
            {
                Point forRectanglePlace;
                try
                {
                    forRectanglePlace = GetPointsCircleWithoutCollisions(radius, rectangleSize)
                        .First(point => Cloud.All(rectangle => 
                        !IsIntersectedNonStrict(rectangleSize, point, rectangle)));
                }
                catch (InvalidOperationException)
                {
                    radius++;
                    continue;
                }
                return new Rectangle(forRectanglePlace, rectangleSize);
            }
        }

        public static bool IsIntersectedNonStrict(Size rectangleSize, Point point, Rectangle rectangle)
        {
            var potential = new Rectangle(point, rectangleSize);
            return IsIntersectedNonStrict(potential, rectangle);
        }

        public static bool IsIntersectedNonStrict(Rectangle potential, Rectangle rectangle)
        {
            potential.Intersect(rectangle);
            return !(potential.IsEmpty || potential.Width == 0 || potential.Height == 0);
        }

        private IEnumerable<Point> GetPointsCircleWithoutCollisions(int r, Size rectangleSize)
        {
            return GetPointsCircle(r, rectangleSize)
                .GroupBy(p => p)
                .Select(group => group.Key);
        }

        private IEnumerable<Point> GetPointsCircle(int r, Size rectangleSize)
        {
            if (r < 0)
                throw new ArgumentException();
            if (r == 0)
            {
                yield return Center;
                yield break;
            }
            var shiftAngle = 1 / (double)r;
            var angle = 0d;
            const double twoPi = Math.PI * 2;
            while (angle < twoPi)
            {
                yield return GetNearestToCenterCornerPoint(angle, r, rectangleSize);
                angle += shiftAngle;
            }
        }

        public Point GetNearestToCenterCornerPoint(double angle, int r, Size size)
        {
            var point = Center.Shift((int) Math.Round(Math.Cos(angle) * r), (int) Math.Round(Math.Sin(angle) * r));
            return shifts[GetQuarter(point)](new Rectangle(point, size));
        }

        public Quarter GetQuarter(Point point)
        {
            if (point.X > Center.X)
                return point.Y > Center.Y ? Quarter.I : Quarter.IV;
            return point.Y > Center.Y ? Quarter.II : Quarter.III;
        }
    }
}

    