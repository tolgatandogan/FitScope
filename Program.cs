using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

internal class Program
{
    private static async Task Main()
    {
        string reportDirectory = "Reports";
        try
        {
            // Configuration

            Directory.CreateDirectory(reportDirectory);

            // 1. INPUT PARAMETERS
            string imageUrl = ""; // Replace with valid public image URL
            string apiKey = ""; // Replace with your actual API key

            // 2. API REQUEST SETUP
            var requestBody = new
            {
                model = "gpt-4o", // Latest vision-capable model (verify from OpenAI docs)
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new List<Dictionary<string, object>>
                        {
                            // Text instruction
                            new Dictionary<string, object>
                            {
                                { "type", "text" },
                                { "text", "List garment features in English:\n- Fabric\n- Color\n- Gender\n- Waist type\n- Cut\n- Leg type\n- Style" }
                            },
                            // Image input
                            new Dictionary<string, object>
                            {
                                { "type", "image_url" },
                                { "image_url", new
                                    {
                                        url = imageUrl,
                                        detail = "auto" // Image quality: "auto", "low", or "high"
                                    }
                                }
                            }
                        }
                    }
                },
                max_tokens = 1000
            };

            // 3. SEND API REQUEST
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions",
                requestBody
            );

            // 4. HANDLE RESPONSE
            if (!response.IsSuccessStatusCode)
            {
                string errorDetails = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error ({response.StatusCode}): {errorDetails}");
            }

            // Parse successful response
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            string analysis = result.GetProperty("choices")[0]
                                 .GetProperty("message")
                                 .GetProperty("content")
                                 .GetString();

            // 5. SAVE REPORT
            string reportPath = Path.Combine(
                reportDirectory,
                $"Analysis_Report_{DateTime.UtcNow:yyyyMMddHHmmss}.txt"
            );

            await File.WriteAllTextAsync(reportPath,
$@"Analysis Report - {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC}
====================================
AI Model: gpt-4o
Image URL: {imageUrl}

Results:
{analysis}
====================================
");

            Console.WriteLine($"✅ Report saved: {Path.GetFullPath(reportPath)}");
        }
        catch (Exception ex)
        {
            // ERROR HANDLING
            string errorLogPath = Path.Combine(
                reportDirectory,
                $"Error_Log_{DateTime.UtcNow:yyyyMMddHHmmss}.txt"
            );

            await File.WriteAllTextAsync(errorLogPath,
$@"ERROR OCCURRED - {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC}
Message: {ex.Message}
Stack Trace: {ex.StackTrace}
------------------------------------
{ex}"
            );

            Console.WriteLine($"❌ Error: Check log - {Path.GetFullPath(errorLogPath)}");
        }
    }
}