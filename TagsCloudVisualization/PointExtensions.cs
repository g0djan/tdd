using System.Drawing;

namespace TagsCloudVisualization
{
    public static class PointExtensions
    {
        public static Point Shift(this Point p, int dx, int dy)
        {
            return new Point(p.X + dx, p.Y + dy);
        }

        public static Quarter GetQuarter(this Point point, Point center)
        {
            if (point.X > center.X)
                return point.Y > center.Y ? Quarter.XandYPositive : Quarter.OnlyXPositive;
            return point.Y > center.Y ? Quarter.OnlyYPositive : Quarter.XandYNonPositive;
        }
    }
}
