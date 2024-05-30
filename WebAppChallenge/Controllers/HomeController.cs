using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebAppChallenge.Modelos;

namespace WebAppChallenge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private static string urlHackerNews = "https://hacker-news.firebaseio.com/v0";

        public NewsController(IHttpClientFactory httpClientFactory, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        [HttpGet]
        public async Task<IActionResult> GetNewestSories(int page = 1, int pageSize = 20)
        {
            var cacheKey = $"news_{page}_{pageSize}";
            if (!_cache.TryGetValue(cacheKey, out List<Story> stories)) {
                var client = this._httpClientFactory.CreateClient();
                var response = await client.GetFromJsonAsync<List<int>>(urlHackerNews + "/newstories.json");
                if (response == null) return NoContent();

                var storyTasks = response.Skip((page - 1) * pageSize).Take(pageSize).Select(async id =>
                {
                    return await client.GetFromJsonAsync<Story>(urlHackerNews + $"/item/{id}.json");
                });

                stories = (await Task.WhenAll(storyTasks)).ToList();
                _cache.Set(cacheKey, stories, TimeSpan.FromMinutes(10));

            }
            
             return Ok(stories);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchStories(string query, int page = 1, int pageSize = 10) {

            if (page < 1 || pageSize < 1)
            {
                return BadRequest("Invalid page or page size");
            }

            var cacheKey = "allItems";
            if (!_cache.TryGetValue(cacheKey, out List<Story> stories))
            {
                var client = this._httpClientFactory.CreateClient();
                var response = await client.GetFromJsonAsync<List<int>>(urlHackerNews + "/newstories.json");
                if (response == null) return NoContent();

                var storyTasks = response.Select(async id =>
                {
                    return await client.GetFromJsonAsync<Story>(urlHackerNews + $"/item/{id}.json");
                });

                stories = (await Task.WhenAll(storyTasks)).ToList();
                _cache.Set(cacheKey, stories, TimeSpan.FromMinutes(10));

            }

            List<Story> filterStories = new List<Story>();

            if (stories != null) {
                filterStories = stories.Where(story => story.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            

            var pagedItems = filterStories
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

            return Ok(new
            {
                Items = pagedItems,
                TotalCount = filterStories.Count
            });
        }

    }
}
