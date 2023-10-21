using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CaptureTraffic
{
    class SentimentAnalyzer
    {
        static async Task<string> AnalyzeSentiment(string searchTerm)
        {
            string output = "";
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://eu-de.ml.cloud.ibm.com/ml/v1-beta/generation/text?version=2023-05-29");

                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer YOUR_TOKEN");
                var input = "{\r\n  \"model_id\": \"google/flan-t5-xxl\",\r\n  \"input\": \"Classify this search term as a depression, mental exhaustion, fears, problem with learning, loneliness, problem with peers, " +
                    "suicidal, bullying, cyberbullying, abuse, addictions, panic attack, attention deficit, neutral.\\nSearch term:\\n" + searchTerm + "\\n\\nClassification:\\n\",\r\n  \"parameters\": {\r\n    \"decoding_method\": \"greedy\",\r\n    \"max_new_tokens\": 30,\r\n    \"min_new_tokens\": 0,\r\n    \"stop_sequences\": [],\r\n    \"repetition_penalty\": 1\r\n  },\r\n  \"project_id\": \"5d78d326-f680-493f-a45b-f880186106ac\"\r\n}";
                HttpContent httpContent = new StringContent(input, Encoding.UTF8, "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");

                try
                {
                    HttpResponseMessage response = await httpClient.PostAsync("", httpContent);
                    var serializedResponse = ResponseSerializer.FromJson(await response.Content.ReadAsStringAsync());
                    output = serializedResponse.Results[0].GeneratedText;
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Request failed with status code: {response.StatusCode}");
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                }
            }
            return output;
        }
        public static string SyncSentimentAnalyze(string searchTerm)
        {
            Task<string> task = Task.Run<string>(async () => await AnalyzeSentiment(searchTerm));
            return task.Result;
        }
    }
}
