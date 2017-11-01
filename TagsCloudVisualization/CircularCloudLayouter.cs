using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TagsCloudVisualization
{
    class CircularCloudLayouter
    {
        public Point center { get; }
        public List<Rectangle> cloud { get; }

        public CircularCloudLayouter(Point center)
        {
            this.center = center;
            cloud = new List<Rectangle>();
        }

        public Rectangle PutNextRectangle(Size rectangleSize)
        {
            if (!cloud.Any())
            {
                var rectPlaced = new Point((int) (center.X - rectangleSize.Width / 2d),
                    (int) (center.Y - rectangleSize.Height / 2d));
                var rectangle = new Rectangle(rectPlaced, rectangleSize);
                cloud.Add(rectangle);
                return rectangle;
            }
            cloud.Add(FindNextRectangle(rectangleSize));
            return cloud.Last();
        }

        private Rectangle FindNextRectangle(Size rectangleSize)
        {
            var squareSideSize = 1;
            while (true)
            {
                Point forRectanglePlace;
                try
                {
                    forRectanglePlace = GetPointsOnSquareSides(squareSideSize)
                        .First(point => cloud.All(rectangle => !IsIntersected(rectangleSize, point, rectangle)));
                }
                catch (InvalidOperationException)
                {
                    squareSideSize++;
                    continue;
                }
                return new Rectangle(forRectanglePlace, rectangleSize);
            }
        }

        public static bool IsIntersected(Size rectangleSize, Point point, Rectangle rectangle)
        {
            var potential = new Rectangle(point, rectangleSize);
            potential.Intersect(rectangle);
            return !(potential.IsEmpty || potential.Width == 0 || potential.Height == 0);
        }

        public IEnumerable<Point> GetPointsOnSquareSides(int n)
        {
            var rangeX = Enumerable.Range(-n + center.X, 2 * n + 1);
            var rangeY = Enumerable.Range(-n + center.Y, 2 * n + 1);
            var downSide = rangeX.Select(k => new Point(k, center.Y - n)).ToArray();
            var rightSide = rangeY.Select(k => new Point(center.X + n, k)).ToArray();
            var upSide = rangeX.Reverse().Select(k => new Point(k, center.Y + n)).ToArray();
            var leftSide = rangeY.Reverse().Select(k => new Point(center.X - n, k)).ToArray();
            return downSide.Concat(rightSide).Concat(upSide).Concat(leftSide)
                .GroupBy(p => p)
                .Select(kvpair => kvpair.Key);
        }
    }
}