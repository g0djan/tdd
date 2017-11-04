using System.Drawing;

namespace TagsCloudVisualization
{
    public enum Quarter
    {
        I,
        II,
        III,
        IV
    }

    public static class PointExtensions
    {
        public static Point Shift(this Point p, int dx, int dy)
        {
            return new Point(p.X + dx, p.Y + dy);
        }
    }
}
