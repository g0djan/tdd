using System;
using System.Drawing;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace TagsCloudVisualization
{
    [TestFixture]
    public class CircularCloudLayouter_Should
    {
        private CircularCloudLayouter cloud;


        [SetUp]
        public void SetUp()
        {
            cloud = new CircularCloudLayouter(new Point(0, 0));
        }

        [TearDown]
        public void TearDown()
        {
            if (TestContext.CurrentContext.Result.FailCount == 0) return;
            var path = Path.Combine(@"C:\Users\godja\Downloads\tdd\TagsCloudVisualization\TestPictures",
                TestContext.CurrentContext.Test.Name + ".bmp");
            var drawer = new CloudDrawer();
            drawer.Draw(cloud, path);
            Console.WriteLine("Tag cloud visualization saved to file {0}", path);
        }

        [TestCase(1, 1, TestName = "I quarter")]
        [TestCase(-1, 1, TestName = "II quarter")]
        [TestCase(-1, -1, TestName = "III quarter")]
        [TestCase(1, -1, TestName = "IV quarter")]
        [TestCase(1, -1, TestName = "Begin Coordinates")]
        public void CorrectInitializeCloudCenter(int x, int y)
        {
            new CircularCloudLayouter(new Point(x, y)).Center.Should().Be(new Point(x, y));
        }

        [Test]
        public void FirstRectanglePlaced_Should_Be_In_OwnLeftUpCorner()
        {
            cloud = new CircularCloudLayouter(new Point(1, 1));
            var size = new Size(1, 1);
            cloud
                .PutNextRectangle(size)
                .Should()
                .Be(new Rectangle(0, 0, 1, 1));
        }

        [Test]
        public void SeveralRectanglesDoesNotIntersect()
        {
            var random = new Random();
            for (int i = 0; i < 30; i++)
            {
                var r1 = 30 + random.Next() % 50;
                var r2 = 30 + random.Next() % 50;
                cloud.PutNextRectangle(new Size(r1, r2));
            }
            cloud.Cloud.Any(rectangle1 =>
                    cloud.Cloud.Any(rectangle2 => rectangle1 != rectangle2 &&
                    CircularCloudLayouter.IsIntersectedNonStrict(rectangle1, rectangle2)))
                .Should().BeFalse();
        }

        [TestCase(0, 0, 1, 1, 1, 0, 1, 1, ExpectedResult = false, TestName = "IntersectOnEdge")]
        [TestCase(0, 0, 1, 1, 1, 1, 1, 1, ExpectedResult = false, TestName = "IntersectOnVertex")]
        [TestCase(0, 0, 2, 2, 1, 1, 1, 1, ExpectedResult = true, TestName = "IntersectOnRectangle")]
        [TestCase(0, 0, 1, 1, 2, 2, 1, 1, ExpectedResult = false, TestName = "EmptyIntersection")]
        public bool TestIsIntersected(
            int x1, int y1, int width1, int height1,
            int x2, int y2, int width2, int height2)
        {
            var point = new Point(x1, y1);
            var size = new Size(width1, height1);
            return CircularCloudLayouter.IsIntersectedNonStrict(size, point, new Rectangle(x2, y2, width2, height2));
        }

        [TestCase(1,1, ExpectedResult = Quarter.I)]
        [TestCase(0,1, ExpectedResult = Quarter.II)]
        [TestCase(1,0, ExpectedResult = Quarter.IV)]
        [TestCase(-1,1, ExpectedResult = Quarter.II)]
        [TestCase(-1,0, ExpectedResult = Quarter.III)]
        [TestCase(-1,-1, ExpectedResult = Quarter.III)]
        [TestCase(0,-1, ExpectedResult = Quarter.III)]
        [TestCase(1,-1, ExpectedResult = Quarter.IV)]
        public Quarter TestGetQuarter(int x, int y)
        {
            var point = new Point(x, y);
            return cloud.GetQuarter(point);
        }

        [TestCase(Math.PI / 4, 1, 1)]
        [TestCase(3 * Math.PI / 4, -4, 1)]
        [TestCase(-3 * Math.PI / 4, -4, -3)]
        [TestCase(- Math.PI / 4, 1, -3)]
        public void TestGetNearestToCenterCorner_AllQuarters(double angle, int expectedX, int expectedY)
        {
            cloud.GetNearestToCenterCornerPoint(angle, 1, new Size(3, 2)).Should().Be(new Point(expectedX, expectedY));
        }
    }
}