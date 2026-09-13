namespace CoffeeNChill.Models
{
    public class StaffDocumentInfo
    {
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public string ContentType { get; set; } = "application/octet-stream";
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
