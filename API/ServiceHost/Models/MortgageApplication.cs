using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocExtract.API.ServiceHost.Models
{
    public class MortgageApplication
    {
        public string MortgageApplicationId { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<MortgageApplicationDocument> Documents { get; set; }

    }
}
