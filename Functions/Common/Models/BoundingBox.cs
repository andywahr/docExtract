using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DocExtract.Functions.Common.Models
{
    public class BoundingBox
    {
        public int x1 { get; set; }
        public int y1 { get; set; }
        public int x2 { get; set; }
        public int y2 { get; set; }
        public int x3 { get; set; }
        public int y3 { get; set; }
        public int x4 { get; set; }
        public int y4 { get; set; }

        public float fx1 { get; set; }
        public float fy1 { get; set; }
        public float fx2 { get; set; }
        public float fy2 { get; set; }
        public float fx3 { get; set; }
        public float fy3 { get; set; }
        public float fx4 { get; set; }
        public float fy4 { get; set; }

        public float fcx { get; set; }
        public float fcy { get; set; }

        public float fwidth { get; set; }
        public float fheight { get; set; }

        public BoundingBox()
        {

        }

        public BoundingBox(int x1, int y1,
                           int x2, int y2,
                           int x3, int y3,
                           int x4, int y4)
        {
            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
            this.x3 = x3;
            this.y3 = y3;
            this.x4 = x4;
            this.y4 = y4;
        }

        public void normalize(int width, int height)
        {
            float w = width;
            float h = height;

            fx1 = x1 / w;
            fy1 = y1 / h;
            fx2 = x2 / w;
            fy2 = y2 / h;
            fx3 = x3 / w;
            fy3 = y3 / h;
            fx4 = x4 / w;
            fy4 = y4 / h;

            float xleft = (fx1 + fx4) / 2;
            float xright = (fx2 + fx3) / 2;

            float ytop = (fy1 + fy2) / 2;
            float ybottom = (fy3 + fy4) / 2;

            fwidth = xright - xleft + 1;
            fheight = ybottom - ytop + 1;

            fcx = (xleft + xright) / 2;
            fcy = (ytop + ybottom) / 2;
        }

        public override string ToString()
        {
            return "[ (" + fx1 + "," + fy1 + "), " +
                     "(" + fx2 + "," + fy2 + "), " +
                     "(" + fx3 + "," + fy3 + "), " +
                     "(" + fx4 + "," + fy4 + ") - CENTER [ X: " + fcx + ",Y: " + fcy + " ] ]";
        }

        public ProcessedLine FindClosestsBelow(List<ProcessedLine> lines)
        {
            var allBoxesBelow = lines.Where(b => b.BoundingBox.fcy > fcy).Select(b => new KeyValuePair<ProcessedLine, double>(b, DistanceTo(b.BoundingBox)));

            if ( allBoxesBelow.SafeAny() )
            {
                return allBoxesBelow.OrderBy(kvp => kvp.Value).First().Key;
            }

            return null;
        }

        private double DistanceTo(BoundingBox box)
        {
            var a = (double)(box.fcx - fcx);
            var b = (double)(box.fcy - fcy);

            return Math.Sqrt(a * a + b * b);
        }
    }
}
