using System;
using System.Collections.Generic;
using System.Drawing;
using FluentAssertions;
using NUnit.Framework;

namespace TagsCloudVisualization
{
    static class RectangleExtensions
    {
        public static PointF[] GetVertexes(this Rectangle rect, ShiftClockwise shift)
        {
            var vertexes = new List<PointF>();
            var halfWidth = (float)rect.Width / 2;
            var halfHeight = (float)rect.Height / 2;
            vertexes.Add(new PointF(rect.X - halfWidth, rect.Y + halfHeight));
            vertexes.Add(new PointF(rect.X + halfWidth, rect.Y + halfHeight));
            vertexes.Add(new PointF(rect.X + halfWidth, rect.Y - halfHeight));
            vertexes.Add(new PointF(rect.X - halfWidth, rect.Y - halfHeight));
            var result = new PointF[4];
            for (var i = 0; i < 4; i++)
                result[i] = vertexes[(i + (int)shift) % 4];
            return result;
        }
    }

    [TestFixture]
    public class RectangleExtensions_Should
    {
        [TestCase(ShiftClockwise.Right)]
        [TestCase(ShiftClockwise.Down)]
        [TestCase(ShiftClockwise.Left)]
        [TestCase(ShiftClockwise.Up)]
        public void GetVertexesTest(ShiftClockwise shift)
        {
            var rect = new Rectangle(1, 1, 2, 2);
            var expectedVertexes = new[]
            {
                new PointF(0, 2),
                new PointF(2, 2),
                new PointF(2, 0),
                new PointF(0, 0),
            };
            int index = 0;
            foreach (var vertex in rect.GetVertexes(shift))
                vertex.Should().Be(expectedVertexes[(index++ + (int)shift) % 4]);
        }
    }

    public enum ShiftClockwise
    {
        Right,
        Down,
        Left,
        Up
    }

    class CircularCloudLayouter
    {
        public Point center { get; }
        private LinkedList<PointF> hull;
        public LinkedListNode<PointF> currentNode;
        private ShiftClockwise shift;

        public CircularCloudLayouter(Point center)
        {
            this.center = center;
            hull = new LinkedList<PointF>();
            shift = ShiftClockwise.Right;
            currentNode = hull.AddFirst(default(PointF));
        }

        public Rectangle PutNextRectangle(Size rectangleSize)
        {
            Rectangle rect;
            if (hull.Count == 1)
            {
                rect = new Rectangle(center, rectangleSize);
                AddPointsToHull(rect, shift);
                currentNode = hull.First;
                shift = ShiftClockwise.Up;
                return rect;
            }
            var rectXY = GetRectCentre(rectangleSize);
            rect = new Rectangle(new Point(rectXY.Item1, rectXY.Item2), rectangleSize);
            AddPointsToHull(rect, shift);
            if (IsConvexAngle(currentNode.Previous.Value, currentNode.Value, currentNode.Next.Value))
            {
                currentNode = currentNode.Next;
                shift = (ShiftClockwise)(((int)shift + 1) % 4);
            }
            if (currentNode.Next != null && IsCollisionsPoints(currentNode.Value, currentNode.Next.Value, 1e-9))
            {
                currentNode = currentNode.Previous;
                shift = (ShiftClockwise)(((int)shift + 1) % 4);
            }
            return rect;
        }

        private Tuple<int, int> GetRectCentre(Size rectangleSize)
        {
            var x = default(double);
            var y = default(double);
            switch (shift)
            {
                case ShiftClockwise.Up:
                    x = currentNode.Value.X + (float) rectangleSize.Width / 2;
                    y = currentNode.Value.Y + (float) rectangleSize.Height / 2;
                    break;
                case ShiftClockwise.Right:
                    x = currentNode.Value.X + (float) rectangleSize.Height / 2;
                    y = currentNode.Value.Y - (float) rectangleSize.Width / 2;
                    break;
            }
            var rectX = x >= 0 ? (int) Math.Ceiling(x) : (int) Math.Floor(x);
            var rectY = y >= 0 ? (int) Math.Ceiling(y) : (int) Math.Floor(y);
            return Tuple.Create(rectX, rectY);
        }

        private bool IsConvexAngle(PointF a, PointF b, PointF c)
        {
            return a.X < b.X && c.Y < b.Y ||
                   a.Y > b.Y && c.X < b.X ||
                   a.X > b.X && c.Y > b.Y ||
                   a.Y < b.Y && c.X > b.X;
        }

        private bool IsCollisionsPoints(PointF a, PointF b, double precision)
        {
            return Math.Abs(a.X - b.X) < precision && Math.Abs(a.Y - b.Y) < precision;
        }

        private void AddPointsToHull(Rectangle rect, ShiftClockwise shift)
        {
            var nodeBefore = currentNode;
            var vertexes = rect.GetVertexes(shift);
            foreach (var vertex in vertexes)
                currentNode = hull.AddAfter(currentNode, vertex);
            hull.Remove(nodeBefore);
        }
    }

    [TestFixture]
    class CircularCloudLayouter_Should
    {
        private CircularCloudLayouter ccl;
        [SetUp]
        public void SetUp()
        {
            ccl = new CircularCloudLayouter(new Point(0, 0));
        }

        [TestCase(0, 0, TestName = "Start coordinates 0, 0")]
        [TestCase(1, 1, TestName = "1st quarter 1, 1")]
        [TestCase(-1, 1, TestName = "2st quarter -1, 1")]
        [TestCase(-1, -1, TestName = "3rd quarter -1, -1")]
        [TestCase(1, -1, TestName = "4th quarter 1, -1")]
        [TestCase(1, 0, TestName = "X axis 1, 0")]
        [TestCase(0, 1, TestName = "Y axis 0, 1")]
        public void CircularCloudLayouter_AddCentre_Correctly(int x, int y)
        {
            var expectedCenter = new Point(x, y);
            var resultCenter = new CircularCloudLayouter(expectedCenter).center;
            resultCenter.Should().Be(expectedCenter);
        }

        [Test]
        public void PutNextRectangle_WhenCCLisEmpty_RectangleInCentre()
        {
            ccl.PutNextRectangle(new Size(6, 6)).Should().Be(new Rectangle(0, 0, 6, 6));
        }

        [TestCase(4, 4, 2, 2, -1, 3, TestName = "WidthSecondLessThanFirst")]
        [TestCase(4, 4, 5, 5, 1, 5, TestName = "WidthSecondMoreThanFirst")]
        public void PutNextRectangle_SecondRectangle_OverFirst(int dx1, int dy1, int dx2, int dy2, int expectedX, int expectedY)
        {
            ccl.PutNextRectangle(new Size(dx1, dy1));
            ccl.PutNextRectangle(new Size(dx2, dy2)).Should().Be(new Rectangle(expectedX, expectedY, dx2, dy2));
        }

        [TestCase(4, 4, 2, 2, 0, 2, TestName = "WidthSecondLessThanFirst")]
        [TestCase(4, 4, 5, 5, 2, 2, TestName = "WidthSecondMoreThanFirst")]
        public void CheckCurrentNode_Put2Rectangles(int dx1, int dy1, int dx2, int dy2, float expectedX, float expectedY)
        {
            ccl.PutNextRectangle(new Size(dx1, dy1));
            ccl.PutNextRectangle(new Size(dx2, dy2));
            ccl.currentNode.Value.Should().Be(new PointF(expectedX, expectedY));
        }

        [TestCase(1, 3, 4, 4, 2, 2, 2, 2, TestName = "Two over first")]
        [TestCase(3, 1, 4, 4, 6, 6, 2, 2, TestName = "First over, second near")]
        [TestCase(3, 3, 4, 4, 4, 2, 2, 2, TestName = "Add rectangle on the angle when make twist")]
        [TestCase(5, -1, 4, 4, 5, 5, 5, 5, TestName = "Over 2 angles twist")]
        public void Put3Rectangles(int expectedX, int expectedY, int dx1, int dy1, int dx2, int dy2, int dx3, int dy3)
        {
            var resultRect = default(Rectangle);
            ccl.PutNextRectangle(new Size(dx1, dy1));
            ccl.PutNextRectangle(new Size(dx2, dy2));
            ccl.PutNextRectangle(new Size(dx3, dy3)).Should().Be(new Rectangle(expectedX, expectedY, dx3, dy3));
        }
    }
}
