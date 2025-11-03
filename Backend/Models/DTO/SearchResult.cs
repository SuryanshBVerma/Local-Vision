namespace Backend.Models.DTO
{
    public class SearchResult
    {
        public string Etag { get; set; }
        public string Caption { get; set; }
        public double Score { get; set; }
        public string bucket { get; set; }
    }
}
