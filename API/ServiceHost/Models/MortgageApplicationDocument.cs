using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocExtract.API.ServiceHost.Models
{
    public class MortgageApplicationDocument
    {
        public MortgageApplicationDocument()
        {

        }

        public void PopuplateFromBlobProperties(CloudBlockBlob blob)
        {
            string tmpVal = string.Empty;
            FileName = blob.Metadata["FileName"];
            MortgageApplicationId = blob.Metadata["MortgageApplicationId"];
            DocumentId = blob.Metadata["DocumentId"];

            if (blob.Metadata.TryGetValue("DocumentType", out tmpVal))
            {
                DocumentType = tmpVal;
                Status = DocumentType.Equals("N/A") ? MortgageApplicationStatus.NotIdentified : MortgageApplicationStatus.Identitfied;
            }

            if (blob.Metadata.TryGetValue("CreateTime", out tmpVal))
            {
                DateUploaded = DateTimeOffset.Parse(tmpVal);
            }

            if (blob.Metadata.TryGetValue("ParsedTime", out tmpVal))
            {
                DateParsed = DateTimeOffset.Parse(tmpVal);
            }

            if (blob.Metadata.TryGetValue("ProcessedTime", out tmpVal))
            {
                DateProcessed = DateTimeOffset.Parse(tmpVal);
            }
        }

        public void SetBlobProperties(CloudBlockBlob blob)
        {
            blob.Metadata.Add("MortgageApplicationId", MortgageApplicationId);
            blob.Metadata.Add("DocumentId", DocumentId);
            blob.Metadata.Add("FileName", FileName);
            if (DateParsed.HasValue) blob.Metadata.Add("ParsedTime", DateParsed.Value.ToString());
            if (DateProcessed.HasValue) blob.Metadata.Add("ProcessedTime", DateProcessed.Value.ToString());
            blob.Metadata.Add("CreateTime", DateUploaded.ToString());
            if (!DocumentType.IsEmpty()) blob.Metadata.Add("DocumentType", DocumentType);
        }

        public string MortgageApplicationId { get; set; }
        public string DocumentId { get; set; }
        public string FileName { get; set; }
        public string DocumentType { get; set; }
        public DateTimeOffset? DateProcessed { get; set; }
        public DateTimeOffset? DateParsed { get; set; }
        public DateTimeOffset DateUploaded { get; set; }
        public bool HasParsedResults { get; set; } = false;
        public bool HasMarkedupResults { get; set; } = false;
        [JsonConverter(typeof(StringEnumConverter))]
        public MortgageApplicationStatus Status { get; set; } = MortgageApplicationStatus.Uploaded;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<DataField> DataFields { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ValidationMessage> Validations { get; set; }
    }

    public enum MortgageApplicationStatus
    {
        Uploaded,
        Identitfied,
        NotIdentified,
        Processed,
        Error
    }
}
