using Microsoft.WindowsAzure.Storage.Blob;
using DocExtract.Functions.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocExtract.Functions.W2Processor
{
    public class W2DataFields
    {
        public string SSN { get; set; }
        public double? Wages { get; set; }
        public double? FederalTax { get; set; }
        public int? Year { get; set; }
    }
}
