using System;
using System.Drawing;

namespace TagsCloudVisualization
{
    class Program
    {
        static void Main()
        {
            var cloudLayouter = new CircularCloudLayouter(new Point(512, 512));
            FillCloudWithRandom(cloudLayouter);
            var cloudDrawer = new CloudDrawer();
            var bitmap = cloudDrawer.Draw(cloudLayouter);
            bitmap.Save("cloud.bmp");
        }

        static void FillCloudWithRandom(CircularCloudLayouter cloudLayouter)
        {
            var random = new Random();
            for (var i = 0; i < 500; i++)
            {
                var r1 = random.Next(10, 30);
                var r2 = random.Next(10, 30);
                cloudLayouter.PutNextRectangle(new Size(r1, r2));
            }
        }
    }
}
