using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StoryProcessor.Models;

namespace StoryProcessor.Services
{
    public class WeaviateService
    {
        private readonly HttpClient _client;
        private const string CLASS_NAME = "Story";

        public WeaviateService()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:8181/v1/")
            };
            EnsureSchemaExists().Wait();
        }

        private async Task EnsureSchemaExists()
        {
            try
            {
                var response = await _client.GetAsync($"schema/{CLASS_NAME}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    var schema = new
                    {
                        @class = CLASS_NAME,
                        vectorIndexType = "hnsw",
                        properties = new[]
                        {
                            new { name = "title", dataType = new[] { "text" } },
                            new { name = "content", dataType = new[] { "text" } },
                            new { name = "summary", dataType = new[] { "text" } },
                            new { name = "tags", dataType = new[] { "text[]" } },
                            new { name = "isStory", dataType = new[] { "boolean" } }
                        }
                    };

                    var content = new StringContent(
                        JsonConvert.SerializeObject(schema),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var createResponse = await _client.PostAsync("schema", content);
                    createResponse.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to ensure schema exists: {ex.Message}");
            }
        }

        public async Task SaveStory(Story story)
        {
            try
            {
                var obj = new
                {
                    @class = CLASS_NAME,
                    properties = new
                    {
                        title = story.Title,
                        content = story.Content,
                        summary = story.Summary,
                        tags = story.Tags,
                        isStory = story.IsStory
                    }
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(obj),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _client.PostAsync("objects", content);
                response.EnsureSuccessStatusCode();

                var result = JsonConvert.DeserializeObject<JObject>(
                    await response.Content.ReadAsStringAsync()
                );
                story.Id = result?["id"]?.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save story to Weaviate: {ex.Message}");
            }
        }
    }
} 