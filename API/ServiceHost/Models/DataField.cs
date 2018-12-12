namespace DocExtract.API.ServiceHost.Models
{
    public class DataField
    {
        public string FieldName { get; set; }
        public string FieldTitle { get; set; }
        public BoundingBox LabelBox { get; set; }
        public BoundingBox ValueBox { get; set; }
        public string Value { get; set; }
    }
}