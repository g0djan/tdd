using System.Drawing;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using TagsCloudVisualization;

[TestFixture]
public class CircularCloudLayouter_Should
{
    private CircularCloudLayouter cloud;

    [SetUp]
    public void SetUp()
    {
        cloud = new CircularCloudLayouter(new Point(0, 0));
    }

    [TestCase(1, 1, TestName = "I quarter")]
    [TestCase(-1, 1, TestName = "II quarter")]
    [TestCase(-1, -1, TestName = "III quarter")]
    [TestCase(1, -1, TestName = "IV quarter")]
    [TestCase(1, -1, TestName = "Begin Coordinates")]
    public void CorrectInitializeCloudCenter(int x, int y)
    {
        new CircularCloudLayouter(new Point(x, y)).center.Should().Be(new Point(x, y));
    }

    [Test]
    public void FirstRectanglePlaced_Should_Be_In_OwnLeftUpCorner()
    {
        var cloud = new CircularCloudLayouter(new Point(1, 1));
        var size = new Size(1, 1);
        cloud
            .PutNextRectangle(size)
            .Should()
            .Be(new Rectangle(0, 0, 1, 1));
    }

    [Test]
    public void AddTwoRectangles()
    {
        cloud.PutNextRectangle(new Size(5, 5));
        cloud.PutNextRectangle(new Size(1, 1)).Should().Be(new Rectangle(-3, -3, 1, 1));
    }

    [TestCase(1, -1, -1)]
    [TestCase(2, -2, -2)]
    [TestCase(3, -1, -2)]
    [TestCase(4, 0, -2)]
    [TestCase(5, 0, -1)]
    [TestCase(6, 0, 0)]
    [TestCase(7, -1, 0)]
    [TestCase(8, -2, 0)]
    [TestCase(9, -2, -1)]
    [TestCase(10, -3, -3)]
    public void AddNTheSameRectangles(int n, int expectedX, int expectedY)
    {
        cloud = new CircularCloudLayouter(new Point(-1, -1));
        var size = new Size(1, 1);
        var lastRect = default(Rectangle);
        for (var i = 0; i < n; i++)
            lastRect = cloud.PutNextRectangle(size);
        lastRect.Should().Be(new Rectangle(new Point(expectedX, expectedY), size));
    }

    [TestCase(1, ExpectedResult = 8)]
    [TestCase(2, ExpectedResult = 16)]
    [TestCase(3, ExpectedResult = 24)]
    public int GetPointsOnSquareSides_Count_Should_Be_8n(int n)
    {
        return cloud.GetPointsOnSquareSides(n).Count();
    }

    [Test]
    public void GetPoints_ReturnCircumVentionByClockWise()
    {
        cloud = new CircularCloudLayouter(new Point(1, 1));
        var expected = new[]
        {
            new Point(0, 0),
            new Point(1, 0),
            new Point(2, 0),
            new Point(2, 1),
            new Point(2, 2),
            new Point(1, 2),
            new Point(0, 2),
            new Point(0, 1),
        };
        cloud.GetPointsOnSquareSides(1).ShouldBeEquivalentTo(expected);
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
        return CircularCloudLayouter.IsIntersected(size, point, new Rectangle(x2, y2, width2, height2));
    }
}