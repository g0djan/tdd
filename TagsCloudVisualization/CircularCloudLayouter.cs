using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace TagsCloudVisualization
{
    #region Extensions
    static class RectangleExtensions
    {
        public static PointF[] GetVertexes(this Rectangle rect, int shift)
        {
            var halfWidth = (float)rect.Width / 2;
            var halfHeight = (float)rect.Height / 2;
            var vertexes = new List<PointF>
            {
                new PointF(rect.X - halfWidth, rect.Y - halfHeight),
                new PointF(rect.X - halfWidth, rect.Y + halfHeight),
                new PointF(rect.X + halfWidth, rect.Y + halfHeight),
                new PointF(rect.X + halfWidth, rect.Y - halfHeight)
            };
            var result = new PointF[4];
            for (var i = 0; i < 4; i++)
                result[i] = vertexes[(i + shift) % 4];
            return result;
        }
    }

    [TestFixture]
    public class RectangleExtensions_Should
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void GetVertexesTest(int shift)
        {
            var rect = new Rectangle(1, 1, 2, 2);
            var expectedVertexes = new[]
            {
                new PointF(0, 0),
                new PointF(0, 2),
                new PointF(2, 2),
                new PointF(2, 0)
            };
            int index = 0;
            foreach (var vertex in rect.GetVertexes(shift))
                vertex.Should().Be(expectedVertexes[(index++ + shift) % 4]);
        }
    }

    static class LinkedListExtensions
    {
        public static LinkedListNode<PointF> GetNextNode(
            this LinkedList<PointF> linkedList, 
            LinkedListNode<PointF> node)
        {
            if (node == null || node.Next == null)
                return linkedList.First;
            return node.Next;
        }

        public static LinkedListNode<PointF> GetPreviousNode(
            this LinkedList<PointF> linkedList,
            LinkedListNode<PointF> node)
        {
            if (node == null || node.Previous == null)
                return linkedList.Last;
            return node.Previous;
        }
    }

    [TestFixture]
    public class LinkedListExtensions_Should
    {
        private PointF pointF;
        private LinkedList<PointF> linkedList;

        [SetUp]
        public void SetUp()
        {
            pointF = new PointF();
            linkedList = new LinkedList<PointF>();
        }

        [Test]
        public void GetNext_AfterLast_ReturnsFirst()
        {
            linkedList.AddLast(pointF);
            linkedList.GetNextNode(linkedList.Last).Should().Be(linkedList.First);
        }

        [Test]
        public void GetPrevious_BeforeFirst_ReturnsLast()
        {
            linkedList.AddFirst(pointF);
            linkedList.GetPreviousNode(linkedList.First).Should().Be(linkedList.Last);
        }

        [Test]
        public void GetNext_Null_When_NodeIsNull()
        {
            linkedList.GetNextNode(null).Should().Be(null);
        }

        [Test]
        public void GetPrevious_Null_When_NodeIsNull()
        {
            linkedList.GetPreviousNode(null).Should().Be(null);
        }
    }

    public enum Direction
    {
        Right,
        Down,
        Left,
        Up
    }
    #endregion
    class CircularCloudLayouter
    {
        private const double Tolerance = 1e-6;

        #region MyRegion


        //                public Rectangle PutNextRectangle(Size rectangleSize)
        //                {
        //                    Rectangle rect;
        //                    if (hull.Count == 1)
        //                    {
        //                        rect = new Rectangle(center, rectangleSize);
        //                        //AddPointsToHull(rect, shift);
        //                        currentNode = hull.First;
        //                        shift = Direction.Up;
        //                        return rect;
        //                    }
        //                    var rectXY = GetRectCentre(rectangleSize);
        //                    rect = new Rectangle(new Point(rectXY.Item1, rectXY.Item2), rectangleSize);
        //                    //AddPointsToHull(rect, shift);
        //                    if (IsConvexAngle(currentNode.Previous.Value, currentNode.Value, currentNode.Next.Value))
        //                    {
        //                        currentNode = currentNode.Next;
        //                        shift = (Direction)(((int)shift + 1) % 4);
        //                    }
        //        //            if (currentNode.Next != null && IsCollisionsPoints(currentNode.Value, currentNode.Next.Value, 1e-9))
        //        //            {
        //        //                currentNode = currentNode.Previous;
        //        //                shift = (Direction)(((int)shift + 1) % 4);
        //        //            }
        //                    return rect;
        //                }

        #endregion

        public Point center { get; }
        public LinkedList<PointF> hull;
        public LinkedListNode<PointF> currentNode;
        private Dictionary<Direction, int> shifts;
        public Dictionary<string, double> edges;

        public CircularCloudLayouter(Point center)
        {
            this.center = center;
            hull = new LinkedList<PointF>();
            InitShifts();
            InitEdges();
        }

        public Rectangle PutNextRectangle(Size rectangleSize)
        {
            if (hull.Count == 0)
            {
                var rectangle = new Rectangle(center, rectangleSize);
                foreach (var vertex in rectangle.GetVertexes(0))
                {
                    hull.AddLast(vertex);
                    RefreshEdges(vertex);
                }
                currentNode = hull.First;
                return rectangle;
            }
            //            var rememberNode = currentNode;
            //            while (true)
            //            {
            //                RemoveTheSame();
            //                if (IsConcaveAngle(hull.GetPreviousNode(currentNode).Value,
            //                    currentNode.Value,
            //                    hull.GetNextNode(currentNode).Value))
            //                    break;
            //                currentNode = hull.GetNextNode(currentNode);
            //                if (currentNode == rememberNode)
            //                    break;
            //            }
            var dir = GetDirection(currentNode.Value, hull.GetNextNode(currentNode).Value);
            var optimisticCenter = GetOptimisticCenter(rectangleSize);
            var adjCenter = AdjustmentRectangleCentre(new Rectangle(optimisticCenter, rectangleSize));
            AddPoints(new Rectangle(adjCenter, rectangleSize), GetShift(dir));
            if (!CheckEdgeValue(currentNode.Value))
                RemoveSpareNodes();
            var returningSteps = SmoothAngle();
            for (var i = 0; i < returningSteps; i++)
                currentNode = hull.GetNextNode(currentNode);
            var rectCenter = new Point(adjCenter.X + center.X, adjCenter.Y + center.Y);
            return new Rectangle(rectCenter, rectangleSize);
        }

        private void InitShifts()
        {
            shifts = new Dictionary<Direction, int>
            {
                [Direction.Right] = 0,
                [Direction.Down] = 1,
                [Direction.Left] = 2,
                [Direction.Up] = 3
            };
        }

        private void InitEdges()
        {
            edges = new Dictionary<string, double>
            {
                ["maxX"] = center.X,
                ["minX"] = center.X,
                ["maxY"] = center.Y,
                ["minY"] = center.Y
            };
        }

        private double GetDistance(PointF a, PointF b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        public Direction GetDirection(PointF p1, PointF p2)
        {
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            if (Math.Abs(dx) < Tolerance)
                return dy > 0 ? Direction.Up : Direction.Down;
            if (Math.Abs(dy) < Tolerance)
                return dx > 0 ? Direction.Right : Direction.Left;
            throw new InvalidDataException("Points must be have minimum one same coordinate");
        }

        public PointF FixMiddlePoint(PointF a, PointF b, PointF c)
        {
            var first = new PointF(a.X, c.Y);
            var second = new PointF(c.X, a.Y);
            return GetDistance(b, first) < GetDistance(b, second) ? first : second;
        }

        public bool IsConcaveAngle(Direction prevToCurr, Direction currToNext)
        {
            return prevToCurr == Direction.Down && currToNext == Direction.Right ||
                   prevToCurr == Direction.Left && currToNext == Direction.Down ||
                   prevToCurr == Direction.Up && currToNext == Direction.Left ||
                   prevToCurr == Direction.Right && currToNext == Direction.Up;
        }

        public bool IsConcaveAngle(PointF a, PointF b, PointF c)
        {
            var prevToCurr = GetDirection(a, b);
            var currToNext = GetDirection(b, c);
            return IsConcaveAngle(prevToCurr, currToNext);
        }

        public int GetShift(Direction currToNext)
        {
            if (!shifts.ContainsKey(currToNext))
                throw new KeyNotFoundException("Wrong directions for getting shift");
            return shifts[currToNext];
        }

        public void AddPoints(Rectangle rectangle, int shift)
        {
            AddAllVertexes(rectangle, shift);
            var returningSteps = SmoothAngle();
            for (var i = 0; i < 3 + returningSteps; i++)
                currentNode = hull.GetNextNode(currentNode);
        }

        private int SmoothAngle()
        {
            int returnningSteps;
            var next = hull.GetNextNode(currentNode);
            var prev = hull.GetPreviousNode(currentNode);
            var smoothedPoint = FixMiddlePoint(prev.Value, currentNode.Value, next.Value);
            if (IsConcaveAngle(prev.Value, smoothedPoint, next.Value))
            {
                currentNode = hull.GetPreviousNode(currentNode);
                returnningSteps = 1;
            }
            else
            {
                smoothedPoint = FixMiddlePoint(currentNode.Value, next.Value, hull.GetNextNode(next).Value);
                returnningSteps = 0;
            }
            hull.Remove(hull.GetNextNode(currentNode));
            hull.AddAfter(currentNode, smoothedPoint);
            return returnningSteps;
        }

        private void AddAllVertexes(Rectangle rectangle, int shift)
        {
            var vertexes = rectangle.GetVertexes(shift).Skip(1).Reverse();
            currentNode = hull.GetPreviousNode(currentNode);
            hull.Remove(hull.GetNextNode(currentNode));
            foreach (var vertex in vertexes)
            {
                if (currentNode.Value == vertex)
                    continue;
                hull.AddAfter(currentNode, vertex);
                RefreshEdges(vertex);
            }
        }

        public bool ShouldToRemove(PointF point, PointF pointToRemove)
        {
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            var t = dx;
            dx = dy;
            dy = -t;
            return dx * (pointToRemove.X - point.X) + dy * (pointToRemove.Y - point.Y) < 0;
        }

        public void RemoveSpareNodes()
        {
            var point = currentNode.Value;
            var pointToRemove = hull.GetNextNode(currentNode).Value;
            while (ShouldToRemove(point, pointToRemove))
            {
                hull.Remove(hull.GetNextNode(currentNode));
                if (GetDistance(point, center) < GetDistance(pointToRemove, center))
                    throw new ValidationException("It mens that algorithm doesn't work right");
                pointToRemove = hull.GetNextNode(currentNode).Value;
            }
        }

        public Point GetOptimisticCenter(Size size)
        {
            var currToNext = GetDirection(currentNode.Value, hull.GetNextNode(currentNode).Value);
            var x = default(double);
            var y = default(double);
            var halfWidth = (float)size.Width / 2;
            var halfHeight = (float) size.Height / 2;
            switch (currToNext)
            {
                case Direction.Right:
                    x = Math.Ceiling(currentNode.Value.X + halfWidth);
                    y = Math.Ceiling(currentNode.Value.Y + halfHeight);
                    break;
                case Direction.Down:
                    x = Math.Ceiling(currentNode.Value.X + halfWidth);
                    y = Math.Floor(currentNode.Value.Y - halfHeight);
                    break;
                case Direction.Left:
                    x = Math.Floor(currentNode.Value.X - halfWidth);
                    y = Math.Floor(currentNode.Value.Y - halfHeight);
                    break;
                case Direction.Up:
                    x = Math.Floor(currentNode.Value.X - halfWidth);
                    y = Math.Ceiling(currentNode.Value.Y + halfHeight);
                    break;
            }
            return new Point((int)x, (int)y);
        }

        public PointF GetRectangleLastVertex(Size size, Point point)
        {
            var currToNext = GetDirection(currentNode.Value, hull.GetNextNode(currentNode).Value);
            var halfWidth = (float)size.Width / 2;
            var halfHeight = (float)size.Height / 2;
            switch (currToNext)
            {
                case Direction.Right:
                    return new PointF(point.X + halfWidth, point.Y- halfHeight);
                case Direction.Down:                       
                    return new PointF(point.X - halfWidth, point.Y - halfHeight);
                case Direction.Left:                       
                    return new PointF(point.X - halfWidth, point.Y + halfHeight);
                case Direction.Up:                         
                    return new PointF(point.X + halfWidth, point.Y + halfHeight);
            }
            return default(PointF);
        }

        public Point AdjustmentRectangleCentre(Rectangle rectangle)
        {
            var lastVertex = GetRectangleLastVertex(rectangle.Size, GetOptimisticCenter(rectangle.Size));
            var observeNode = hull.GetNextNode(currentNode);
            var currToNext = GetDirection(currentNode.Value, observeNode.Value);
            observeNode = hull.GetNextNode(observeNode);
            var newCenter = new Point(rectangle.X, rectangle.Y);
            var halfWidth = (float)rectangle.Width / 2;
            var halfHeight = (float) rectangle.Height / 2;
            while (ShouldToRemove(lastVertex, observeNode.Value))
            {
                switch (currToNext)
                {
                    case Direction.Right:
                        if (observeNode.Value.Y > lastVertex.Y)
                            newCenter = new Point(newCenter.X, (int)Math.Ceiling(observeNode.Value.Y + halfHeight));
                        break;
                    case Direction.Down:
                        if (observeNode.Value.X > lastVertex.X)
                            newCenter = new Point((int)Math.Ceiling(observeNode.Value.X + halfWidth), newCenter.Y);
                        break;
                    case Direction.Left:
                        if (observeNode.Value.Y < lastVertex.Y)
                            newCenter = new Point(newCenter.X, (int)Math.Floor(observeNode.Value.Y - halfHeight));
                        break;
                    case Direction.Up:
                        if (observeNode.Value.X < lastVertex.X)
                            newCenter = new Point((int)Math.Floor(observeNode.Value.X - halfWidth), newCenter.Y);
                        break;
                }
                lastVertex = GetRectangleLastVertex(rectangle.Size, newCenter);
                observeNode = hull.GetNextNode(observeNode);
            }
            return newCenter;
        }

        private void RefreshEdges(PointF point)
        {
            if (point.X > edges["maxX"])
                edges["maxX"] = point.X;
            if (point.X < edges["minX"])
                edges["minX"] = point.X;
            if (point.Y > edges["maxY"])
                edges["maxY"] = point.Y;
            if (point.Y < edges["minY"])
                edges["minY"] = point.Y;
        }

        private bool CheckEdgeValue(PointF point)
        {
            return Math.Abs(point.X - edges["maxX"]) < Tolerance ||
                   Math.Abs(point.X - edges["minX"]) < Tolerance ||
                   Math.Abs(point.Y - edges["maxY"]) < Tolerance ||
                   Math.Abs(point.Y - edges["minY"]) < Tolerance;
        }

        private void RemoveTheSame()
        {
            if (currentNode.Value != hull.GetNextNode(currentNode).Value)
                return;
            hull.Remove(hull.GetNextNode(currentNode));
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

        [TestCase(4, 4, 2, 2, -3, -1, TestName = "WidthSecondLessThanFirst")]
        [TestCase(4, 4, 5, 5, -5, 1, TestName = "WidthSecondMoreThanFirst")]
        public void PutNextRectangle_SecondRectangle_OverFirst(int dx1, int dy1, int dx2, int dy2, int expectedX, int expectedY)
        {
            ccl.PutNextRectangle(new Size(dx1, dy1));
            ccl.PutNextRectangle(new Size(dx2, dy2)).Should().Be(new Rectangle(expectedX, expectedY, dx2, dy2));
        }

        [TestCase(4, 4, 2, 2, -2, 0, TestName = "WidthSecondLessThanFirstCheckCurrentNode")]
        [TestCase(4, 4, 5, 5, -2.5f, 3.5f, TestName = "WidthSecondMoreThanFirstCheckCurrentNode")]
        public void CheckCurrentNode_Put2Rectangles(int dx1, int dy1, int dx2, int dy2, float expectedX, float expectedY)
        {
            ccl.PutNextRectangle(new Size(dx1, dy1));
            ccl.PutNextRectangle(new Size(dx2, dy2));
            ccl.currentNode.Value.Should().Be(new PointF(expectedX, expectedY));
        }

        [TestCase(-3, 1, 4, 4, 2, 2, 2, 2, TestName = "Two over first")]
        [TestCase(-1, 3, 4, 4, 6, 6, 2, 2, TestName = "First over, second near")]
        [TestCase(-1, 3, 4, 4, 2, 4, 2, 2, TestName = "Add rectangle on the angle when make twist")]
        [TestCase(0, 5, 4, 4, 5, 5, 5, 5, TestName = "Over 2 angles twist")]
        public void Put3Rectangles(int expectedX, int expectedY, int dx1, int dy1, int dx2, int dy2, int dx3, int dy3)
        {
            var resultRect = default(Rectangle);
            ccl.PutNextRectangle(new Size(dx1, dy1));
            ccl.PutNextRectangle(new Size(dx2, dy2));
            ccl.PutNextRectangle(new Size(dx3, dy3)).Should().Be(new Rectangle(expectedX, expectedY, dx3, dy3));
        }

        [TestCase(3, -1, 6, 4, 4, 2, 2, TestName = "To3rdCorner")]
        public void PutSeveralSameRectanglesAroundAnother(int expectedX, int expectedY, int n, int dx0, int dy0, int dx, int dy)
        {
            var size = new Size(dx, dy);
            ccl.PutNextRectangle(new Size(dx0, dy0));
            var rectResult = default(Rectangle);
            for (var i = 0; i < n; i++)
                rectResult = ccl.PutNextRectangle(size);
            rectResult.Should().Be(new Rectangle(expectedX, expectedY, dx, dy));
        }

        [TestCase(0, 0, 0, 1, ExpectedResult = Direction.Up, TestName = "Up")]
        [TestCase(0, 0, 1, 0, ExpectedResult = Direction.Right, TestName = "Right")]
        [TestCase(0, 0, 0, -1, ExpectedResult = Direction.Down,  TestName = "Down")]
        [TestCase(0, 0, -1, 0, ExpectedResult = Direction.Left,  TestName = "Left")]
        public Direction TestGetDirections(float x1, float y1, float x2, float y2)
        {
            var p1 = new PointF(x1, y1);
            var p2 = new PointF(x2, y2);
            return ccl.GetDirection(p1, p2);
        }

        [Test]
        public void GetDirection_PointsWIthBothDifferentCoordinates_ThrowInvalidDataException()
        {
            Assert.Throws<InvalidDataException>(() => ccl.GetDirection(new PointF(0, 0), new PointF(1, 1)));
        }

        [TestCase(0, 0, -3, 0, 1, -1, 0, 2, TestName = "Sharp corner II quarter")]
        [TestCase(0, 0, 0, 2, 1, 1, 4, 0, TestName = "Obtuse corner I quarter")]
        [TestCase(0, 2, 0, 2, 2, 1, 0, 0, TestName = "On one axis")]
        public void TestFixMiddlePoint(float expectedX, int expectedY,
            float x1, float y1, float x2, float y2, float x3, float y3)
        {
            var expected = new PointF(expectedX, expectedY);
            var p1 = new PointF(x1, y1);
            var p2 = new PointF(x2, y2);
            var p3 = new PointF(x3, y3);
            ccl.FixMiddlePoint(p1, p2, p3).Should().Be(expected);
        }

        [TestCase(Direction.Down, Direction.Right, ExpectedResult = true, TestName = "I quarter")]
        [TestCase(Direction.Right, Direction.Down, ExpectedResult = false, TestName = "Reversed I quarter")]
        public bool TestIsConcaveAngle(Direction prevToCurr, Direction currToNext)
        {
            return ccl.IsConcaveAngle(prevToCurr, currToNext);
        }

        [TestCase(Direction.Right, ExpectedResult = 0)]
        [TestCase(Direction.Down, ExpectedResult = 1)]
        [TestCase(Direction.Left, ExpectedResult = 2)]
        [TestCase(Direction.Up, ExpectedResult = 3)]
        public int TestGetShift(Direction currToNext)
        {
            return ccl.GetShift(currToNext);
        }

        [TestCase(1, 6,
            3, 5, 2, 2, 2, 7, 2, 4,
            2, 2, 4, 4, 5,
            7, 6, 6, 4, 4 , TestName = "Points lower than previous point")]
        [TestCase(3, 4,
            2, 1, 4, 3, 3, 3, 0, 3,
            3, 4, 4, 0, 0,
            2.5f, 2.5f, -0.5f, -0.5f, -1f, TestName = "Points over than previous point")]
        public void TestAddPoints(float startX, float startY,
            int x, int y, int width, int height, float lostX, float lostY, float lostX2, float lostY2,
            float x1, float x2, float x3, float x4, float x5,
            float y1, float y2, float y3, float y4, float y5)
        {
            var expectedHull = new LinkedList<PointF>();
            var t = expectedHull.AddFirst(new PointF(startX, startY));
            t = expectedHull.AddAfter(t,new PointF(x1, y1));
            t = expectedHull.AddAfter(t, new PointF(x2, y2));
            t = expectedHull.AddAfter(t, new PointF(x3, y3));
            t = expectedHull.AddAfter(t, new PointF(x4, y4));
            t = expectedHull.AddAfter(t, new PointF(x5, y5));
            ccl.hull = new LinkedList<PointF>();
            ccl.hull.AddFirst(new PointF(startX, startY));
            var prev = ccl.hull.AddAfter(ccl.hull.First, new PointF(lostX, lostY));
            ccl.currentNode = ccl.hull.AddAfter(prev, new PointF(lostX2, lostY2));
            var next = ccl.hull.AddAfter(ccl.currentNode, new PointF(x5, y5));
            ccl.AddPoints(new Rectangle(x, y, width, height),
                ccl.GetShift(ccl.GetDirection(ccl.currentNode.Value, next.Value)));
            ccl.hull.ShouldAllBeEquivalentTo(expectedHull);
        }

        [TestCase(-3, -3, -2, -3, ExpectedResult = true, TestName = "Should remove nodes behind radius")]
        [TestCase(-3, -3, -3, -2, ExpectedResult = false, TestName = "Shouldn't remove nodes in front of radius")]
        public bool TestShouldToRemove(int xNode, int yNode, int xToRemove, int yToRemove)
        {
            var point = new PointF(xNode, yNode);
            var pointToRemove = new PointF(xToRemove, yToRemove);
            return ccl.ShouldToRemove(point, pointToRemove);
        }

        [Test]
        public void TestRemoveSpareNodes()
        {
            var points = new[]
            {
                new PointF(-4, 1),
                new PointF(-3, -1),
                new PointF(-2, 0),
                new PointF(-2, 1)
            };
            var expected = new LinkedList<PointF>();
            expected.AddLast(points[0]);
            expected.AddLast(points[3]);
            foreach (var point in points)
                ccl.hull.AddLast(point);
            ccl.currentNode = ccl.hull.First;
            ccl.RemoveSpareNodes();
            ccl.hull.ShouldBeEquivalentTo(expected);
        }

        [Test]
        public void RemoveSpareNodes_ThrowsException_When_MeetIncorrectPoint()
        {
            ccl.hull.AddLast(new PointF(-4, 1));
            ccl.hull.AddLast(new PointF(-5, 0));
            ccl.currentNode = ccl.hull.First;
            Assert.Throws<ValidationException>(() => ccl.RemoveSpareNodes());
        }

        [TestCase(3, 4, 2, 2, 0, 1, 0, 0, 1, 0, TestName = "Centre with a little shift to riht-up corner")]
        [TestCase(4, 5, 2, -3, 1, 0, 0, 0, 0, -1, TestName = "Centre with a little shift to riht-down corner")]
        [TestCase(3, 3, -2, -2, 0, -1, 0, 0, -1, 0, TestName = "Centre with a little shift to left-down corner")]
        [TestCase(3, 3, -2, 2, -1, 0, 0, 0, 0, 1, TestName = "Centre with a little shift to left-up corner")]
        public void TestGetOptimisticRectangleCentre(
            int width, int height,
            int resultX, int resultY,
            float x1, float y1, float x2,float y2, float x3, float y3)
        {
            InitCorner(x1, x2, x3, y1, y2, y3);
            ccl.GetOptimisticCenter(new Size(width, height)).Should().Be(new Point(resultX, resultY));
        }

        [TestCase(3, 4, 3.5f, 0, 0, 1, 0, 0, 1, 0, TestName = "In a right-down corner")]
        [TestCase(4, 5, 0, -5.5f, 1, 0, 0, 0, 0, -1, TestName = "In a left-down corner")]
        [TestCase(3, 3, -3.5f, -0.5f, 0, -1, 0, 0, -1, 0, TestName = "In a left-up corner")]
        [TestCase(3, 3, -0.5f, 3.5f, -1, 0, 0, 0, 0, 1, TestName = "In a right-up corner")]
        public void TestGetRectangleLastVertex(
            int width, int height,
            float resultX, float resultY,
            float x1, float y1, float x2, float y2, float x3, float y3)
        {
            InitCorner(x1, x2, x3, y1, y2, y3);
            ccl.GetRectangleLastVertex(new Size(width, height), ccl.GetOptimisticCenter(new Size(width, height))).Should().Be(new PointF(resultX, resultY));
        }

        private void InitCorner(float x1, float x2, float x3, float y1, float y2, float y3)
        {
            var p1 = new PointF(x1, y1);
            var p2 = new PointF(x2, y2);
            var p3 = new PointF(x3, y3);
            ccl.hull.AddLast(p1);
            ccl.currentNode = ccl.hull.AddLast(p2);
            ccl.hull.AddLast(p3);
        }

        [Test]
        public void TestAdjustmentRectangleCenter()
        {
            ccl.hull.AddLast(new PointF(0, 1));
            ccl.hull.AddLast(new PointF(0, 0));
            ccl.currentNode = ccl.hull.Last;
            ccl.hull.AddLast(new PointF(1, 0));
            ccl.hull.AddLast(new PointF(3, 1));
            ccl.hull.AddLast(new PointF(4, 2));
            ccl.hull.AddLast(new PointF(5, 1));
            ccl.AdjustmentRectangleCentre(new Rectangle(3, 1, 5, 2)).Should().Be(new Point(3, 3));
        }

        [Test]
        public void Put1Rectangle_RefreshEdges()
        {
            ccl.PutNextRectangle(new Size(4, 4));
            var expectedEdges = new Dictionary<string, int>
            {
                ["maxX"] = 2,
                ["minX"] = -2,
                ["maxY"] = 2,
                ["minY"] = -2
            };
            ccl.edges.ShouldBeEquivalentTo(expectedEdges);
        }

        [Test]
        public void RealTest()
        {
            var sizes = new[] {new Size(5, 3), new Size(2, 3), new Size(9, 3), new Size(1, 5)};
            foreach (var size in sizes)
            {
                var r = ccl.PutNextRectangle(size);
            }
        }
    }
}
