using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Himzo_watcher
{
    internal class Messager
    {
        private static readonly HttpClient _client = new HttpClient();

        public static async Task SendMessageAsync(string message)
        {
            // 1. Send to Slack (uses "text" in JSON)
            if (!string.IsNullOrEmpty(Config.SlackUrl))
            {
                string slackJson = $"{{\"text\":\"{message}\"}}";
                await PostToWebhookAsync("Slack", Config.SlackUrl, slackJson);
            }

            // 2. Send to Discord (uses "content" in JSON)
            if (!string.IsNullOrEmpty(Config.DiscordUrl))
            {
                string discordJson = $"{{\"content\":\"{message}\"}}";
                await PostToWebhookAsync("Discord", Config.DiscordUrl, discordJson);
            }
        }

        /// <summary>
        /// Helper method to handle the actual HTTP POST to avoid code duplication.
        /// </summary>
        private static async Task PostToWebhookAsync(string serviceName, string url, string jsonPayload)
        {
            try
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await _client.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{serviceName} Error] Status: {response.StatusCode}");
                }
                else
                {
                    // Uncomment if you want confirmation for every message sent
                    // Console.WriteLine($"[{serviceName}] Message sent.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{serviceName} Exception] {ex.Message}");
            }
        }
    }
}