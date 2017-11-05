using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TagsCloudVisualization
{
    class CloudDrawer
    {
        public Bitmap Draw(CircularCloudLayouter cloudLayouter)
        {
            var bitmap = new Bitmap(1024, 1024);
            var graphics = Graphics.FromImage(bitmap);
            var pen = new SolidBrush(Color.DarkRed);
            cloudLayouter.Cloud.ForEach(rectangle => graphics.FillRectangle(pen, rectangle));
            return bitmap;
        }
    }
}
