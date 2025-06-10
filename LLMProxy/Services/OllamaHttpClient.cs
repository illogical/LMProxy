    public static class OllamaHttpClient
    {
        public static async Task<OllamaModelsResponse> ListModelsAsync(HttpClient httpClient, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync("api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<OllamaModelsResponse>(cancellationToken: cancellationToken);
            return result ?? new OllamaModelsResponse { Models = new List<OllamaModel>() };
        }

        public class OllamaModelsResponse
        {
            public List<OllamaModel> Models { get; set; } = new();
        }

        public class OllamaModel
        {
            public string Name { get; set; } = string.Empty;
        }

    }