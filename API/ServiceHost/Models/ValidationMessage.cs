using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DocExtract.API.ServiceHost.Models
{
    public class ValidationMessage
    {
        public string Message { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ValidationLevel ValidationLevel { get; set; }
    }

    public enum ValidationLevel
    {
        Info,
        Warning,
        Error
    }
}