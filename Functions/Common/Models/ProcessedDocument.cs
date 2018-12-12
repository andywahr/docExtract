using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocExtract.Functions.Common.Models
{
    public class ProcessedDocument
    {
        public string Name { get; set; }
        public List<ProcessedLine> Lines { get; set; }
        public int Min_x { get; set; }
        public int Max_x { get; set; }
        public int Min_y { get; set; }
        public int Max_y { get; set; }
        public int Extend_width { get; set; }
        public int Extend_height { get; set; }

        public ProcessedDocument()
        {

        }

        public ProcessedDocument(string name, JObject recognitionResult)
        {
            Name = name;

            this.Lines = new List<ProcessedLine>();
            JEnumerable<JToken> ll = recognitionResult.GetValue("lines").Children();

            // Iterate through all the lines in the result JSON document            
            foreach (var line in ll)
            {
                // Normalize coordinates 
                ProcessedLine pline = new ProcessedLine(line);

                // and add it to the collection
                Lines.Add(pline);
            }

            // Find the extends and normalize each bounding box's coordinates
            if (FindExtends())
                NormalizeLines();
        }

        private bool FindExtends()
        {
            int minx = 0, miny = 0, maxx = 0, maxy = 0;

            // Iterate through the all the lines and find the min and max extend
            foreach (ProcessedLine line in Lines)
            {
                // min and max X 
                minx = Math.Min(minx, Math.Min(line.BoundingBox.x1, line.BoundingBox.x4));
                maxx = Math.Max(maxx, Math.Max(line.BoundingBox.x2, line.BoundingBox.x3));

                // min and max Y
                miny = Math.Min(miny, Math.Min(line.BoundingBox.y1, line.BoundingBox.y2));
                maxy = Math.Max(maxy, Math.Max(line.BoundingBox.y3, line.BoundingBox.y4));
            }

            Min_x = minx;
            Max_x = maxx;
            Min_y = miny;
            Max_y = maxy;

            Extend_width = Max_x - Min_x + 1;
            Extend_height = Max_x - Min_y + 1;

            return true;
        }

        private bool NormalizeLines()
        {
            // Iterate through all the lines and normalize based on the calculated extends
            foreach (ProcessedLine line in Lines)
            {
                line.BoundingBox.normalize(Extend_width, Extend_height);
            }

            return true;
        }

        public string ToCSV(string delimiter)
        {
            string output = "";

            if (Lines != null)
            {
                Lines.ForEach(delegate (ProcessedLine lline)
                {
                    output += Name + delimiter + Extend_width + delimiter + Extend_height + delimiter;
                    output += lline.Text + delimiter + lline.Mask + delimiter;
                    output += lline.BoundingBox.fcx + delimiter + lline.BoundingBox.fcy + delimiter;
                    output += lline.BoundingBox.fwidth + delimiter + lline.BoundingBox.fheight + '\n';
                });
            }

            return output;
        }

        public override string ToString()
        {
            string output = "Document: " + Name;
            output += " - Size: [ width: " + Extend_width + ", height: " + Extend_height + " ]\n";

            if (Lines != null)
            {
                Lines.ForEach(delegate (ProcessedLine lline)
                {
                    output += lline.ToString() + "\n";
                });
            }

            return output;
        }
    }
}

