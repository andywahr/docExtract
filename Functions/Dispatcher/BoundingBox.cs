using System;
using System.Collections.Generic;
using System.Text;

namespace DocExtract.Functions.Dispatcher
{
    public class BoundingBox
    {
        public int x1;
        public int y1;
        public int x2;
        public int y2;
        public int x3;
        public int y3;
        public int x4;
        public int y4;

        public float fx1;
        public float fy1;
        public float fx2;
        public float fy2;
        public float fx3;
        public float fy3;
        public float fx4;
        public float fy4;

        public float fcx;
        public float fcy;

        public float fwidth;
        public float fheight;

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
    }
}
