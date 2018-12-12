using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocExtract.Functions.Dispatcher.Models
{
    public class ImagePredictions
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }
        [JsonProperty("project")]
        public Guid Project { get; set; }
        [JsonProperty("Iteration")]
        public Guid Iteration { get; set; }
        [JsonProperty("created")]
        public DateTimeOffset Created { get; set; }
        [JsonProperty("predictions")]
        public List<Prediction> Predictions { get; set; } = new List<Prediction>();
    }

    public class Prediction
    {
        [JsonProperty("probability")]
        public double Probability { get; set; }
        [JsonProperty("tagId")]
        public Guid TagId { get; set; }
        [JsonProperty("tagName")]
        public string TagName { get; set; }
    }
}
