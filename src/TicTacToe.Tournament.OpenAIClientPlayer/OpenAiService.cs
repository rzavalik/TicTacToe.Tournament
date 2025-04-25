using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TicTacToe.Tournament.OpenAIClientPlayer;

public static class OpenAiService
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<string> GetChatGptReplyAsync(
        string token, 
        string prompt, 
        string model = "gpt-4")
    {
        var requestUri = "https://api.openai.com/v1/chat/completions";

        var payload = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = """
                    - You are a world-class Tic Tac Toe bot.
                    - Up to now, you have never lost a game and you are very good in strategies.
                    - Always return only the move in the format ROW=[0-2], COL=[0-2]. Do not write anything else. 
                """ },
                
                new { role = "user", content = prompt }
            },
            temperature = 0.2,
            max_tokens = 50
        };

        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"OpenAI error: {response.StatusCode} - {error}");
        }

        using var responseStream = await response.Content.ReadAsStreamAsync();
        var doc = await JsonDocument.ParseAsync(responseStream);
        var result = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return result?.Trim() ?? "No response.";
    }
}
