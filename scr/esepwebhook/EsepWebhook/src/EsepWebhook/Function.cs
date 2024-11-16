using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Function to handle the GitHub webhook payload and send a Slack notification.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(object input, ILambdaContext context)
        {
            context.Logger.LogInformation($"FunctionHandler received: {input}");

            // Deserialize the incoming JSON payload from the GitHub webhook.
            dynamic json = JsonConvert.DeserializeObject(input.ToString());
            
            // Check if the issue part exists and has a valid html_url field.
            if (json?.issue?.html_url == null)
            {
                context.Logger.LogError("No issue or html_url found in the GitHub webhook payload.");
                return "Error: No issue or html_url found in the payload.";
            }

            string issueUrl = json.issue.html_url;

            // Prepare the message for Slack (using a simple format).
            string payload = JsonConvert.SerializeObject(new
            {
                text = $"Issue Created: {issueUrl}"
            });

            // Retrieve the Slack webhook URL from environment variables.
            string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
            if (string.IsNullOrEmpty(slackUrl))
            {
                context.Logger.LogError("SLACK_URL environment variable not set.");
                return "Error: SLACK_URL environment variable is not set.";
            }

            // Send the payload to Slack via HTTP POST.
            try
            {
                var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                HttpResponseMessage response = await client.SendAsync(webRequest);

                using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                {
                    string responseBody = await reader.ReadToEndAsync();
                    context.Logger.LogInformation($"Slack response: {responseBody}");
                    return responseBody;
                }
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error sending message to Slack: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}
