using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StoryProcessor.Models;

namespace StoryProcessor.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private const string OLLAMA_API_URL = "http://localhost:11434/api/generate";

        public OllamaService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> IsStoryContent(string content)
        {
            var prompt = $"Analyze the following text and determine if it is a story. Respond with only 'true' or 'false':\n\n{content}";
            var response = await GenerateResponse(prompt, "mistral");
            return bool.TryParse(response?.Trim().ToLower(), out bool result) && result;
        }

        public async Task<string> GenerateSummary(string content)
        {
            var prompt = $"Generate a concise summary of the following story:\n\n{content}";
            return await GenerateResponse(prompt, "mistral");
        }

        public async Task<string[]> GenerateTags(string content)
        {
            var prompt = $"Generate 5 relevant tags for the following story. Respond with only the tags, separated by commas:\n\n{content}";
            var response = await GenerateResponse(prompt, "mistral");
            return response?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) 
                   ?? Array.Empty<string>();
        }

        private async Task<string> GenerateResponse(string prompt, string model)
        {
            var request = new
            {
                model = model,
                prompt = prompt,
                stream = false
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(OLLAMA_API_URL, content);
            
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to get response from Ollama: {response.StatusCode}");

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<OllamaResponse>(responseContent);
            return result?.Response;
        }

        private class OllamaResponse
        {
            public string Response { get; set; }
        }
    }
} 