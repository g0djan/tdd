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
            cloudDrawer.Draw(cloudLayouter);
        }

        static void FillCloudWithRandom(CircularCloudLayouter cloudLayouter)
        {
            var random = new Random();
            for (var i = 0; i < 100; i++)
            {
                var r1 = random.Next() % 100;
                var r2 = random.Next() % 100;
                cloudLayouter.PutNextRectangle(new Size(r1, r2));
            }
        }
    }
}
