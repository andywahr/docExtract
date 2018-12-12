using Microsoft.WindowsAzure.Storage.Blob;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using DocExtract.Functions.Common.Models;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace DocExtract.Functions.Common
{
    public static class MarkupService
    {
        public static async Task HighlightHits(ProcessedDocument procesedDocument, Stream originalImage, CloudBlockBlob markedUpImage)
        {
            using (Image<Rgba32> image = Image.Load<Rgba32>(originalImage))
            {
                foreach (var line in procesedDocument.Lines)
                {
                    int maxX = max(line.BoundingBox.x1, line.BoundingBox.x2, line.BoundingBox.x3, line.BoundingBox.x4);
                    int maxY = max(line.BoundingBox.y1, line.BoundingBox.y2, line.BoundingBox.y3, line.BoundingBox.y4);
                    int minX = min(line.BoundingBox.x1, line.BoundingBox.x2, line.BoundingBox.x3, line.BoundingBox.x4);
                    int minY = min(line.BoundingBox.y1, line.BoundingBox.y2, line.BoundingBox.y3, line.BoundingBox.y4);

                    using (Image<Rgba32> highlight = new Image<Rgba32>(new Configuration(), maxX - minX, maxY - minY, Rgba32.Yellow))
                    {
                        image.Mutate(x =>
                        {
                             x.DrawImage(highlight, .5f, new SixLabors.Primitives.Point(minX, minY));
                        });
                    }
                }

                markedUpImage.Properties.ContentType = "image/jpeg";
                using (var outboundStream = await markedUpImage.OpenWriteAsync())
                {
                    image.Save(outboundStream, ImageFormats.Jpeg);
                }
            }
        }

        private static int min(params int[] vals)
        {
            return vals.Min();
        }

        private static int max(params int[] vals)
        {
            return vals.Max();
        }
    }
}
