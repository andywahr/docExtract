using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocExtract.API.ServiceHost.Models
{
    public class UploadDocumentResult
    {
        public bool Succeeded { get; set; } = false;
        public string Message { get; set; }

        public List<MortgageApplicationDocument> FilesUploaded = new List<MortgageApplicationDocument>();
    }
}
