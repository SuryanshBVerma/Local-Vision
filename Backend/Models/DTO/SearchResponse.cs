using Microsoft.OpenApi.Services;

namespace Backend.Models.DTO
{
    public class SearchResponse
    {
        public List<SearchResult> Results { get; set; } = new();
    }
}
