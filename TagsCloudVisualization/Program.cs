using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagsCloudVisualization
{
    class Program
    {
        static void Main()
        {
            var cloudLayouter = new CircularCloudLayouter(new Point(512, 512));
            FillCloudWithRandom(cloudLayouter);
            var cloudDrawer = new CloudDrawer();
            cloudDrawer.Draw(cloudLayouter, "cloud.bmp");
        }

        static void FillCloudWithRandom(CircularCloudLayouter cloudLayouter)
        {
            var random = new Random();
            for (var i = 0; i < 200; i++)
            {
                var r1 = 20 + random.Next() % 50;
                var r2 = 20 + random.Next() % 50;
                cloudLayouter.PutNextRectangle(new Size(r1, r2));
            }
        }
    }
}
