namespace DocExtract.API.ServiceHost.Models
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
    }
}