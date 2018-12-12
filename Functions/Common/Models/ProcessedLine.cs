using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DocExtract.Functions.Common.Models
{
    public class ProcessedLine
    {
        public BoundingBox BoundingBox { get; set; }
        public string Text { get; set; }
        public string Mask { get; set; }

        public ProcessedLine()
        {

        }

        public ProcessedLine(JToken line)
        {
            JToken bbox = line["boundingBox"];
            int[] array = bbox.Values<int>().ToArray();
            int x1 = array[0];
            int y1 = array[1];
            int x2 = array[2];
            int y2 = array[3];
            int x3 = array[4];
            int y3 = array[5];
            int x4 = array[6];
            int y4 = array[7];

            BoundingBox = new BoundingBox(x1, y1, x2, y2, x3, y3, x4, y4);

            Text = line["text"].ToString();

            Mask = getMask(Text);
        }

        public string getMask(string text)
        {
            string m = "";

            // Trim white spaces
            string t = text.Trim();

            // Iterate through each character
            for (int i = 0; i < t.Length; i++)
            {
                char c = t[i];

                if ((c == '$') || (c == '.') || (c == '-') || (c == ',') || (c == '/') || (c == ' '))
                    m = m + c;
                else if (Char.IsDigit(c))
                    m = m + "0";
                else
                    m = m + "?";
            }

            return m;
        }

        public override string ToString()
        {
            return "Parsed line: '" + Text + "' @" + BoundingBox.ToString();
        }
    }
}
