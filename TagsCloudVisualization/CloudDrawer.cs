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
        public void Draw(CircularCloudLayouter cloudLayouter, string name)
        {
            var bitmap = new Bitmap(1024, 1024);
            var graphics = Graphics.FromImage(bitmap);
            var pen = new Pen(Color.Firebrick, 5);
            cloudLayouter.Cloud.ForEach(rectangle => graphics.DrawRectangle(pen, rectangle));
            bitmap.Save(name);
        }
    }
}
