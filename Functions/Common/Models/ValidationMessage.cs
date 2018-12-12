using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocExtract.Functions.Common.Models
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
