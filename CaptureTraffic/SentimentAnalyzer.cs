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

                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer eyJraWQiOiIyMDIzMTAwODA4MzUiLCJhbGciOiJSUzI1NiJ9.eyJpYW1faWQiOiJJQk1pZC01NTAwMDYxMDQxIiwiaWQiOiJJQk1pZC01NTAwMDYxMDQxIiwicmVhbG1pZCI6IklCTWlkIiwianRpIjoiMmY3NGE0OTEtOWRhZC00MWExLWE4MzEtMmJiNGM4N2U5NmQwIiwiaWRlbnRpZmllciI6IjU1MDAwNjEwNDEiLCJnaXZlbl9uYW1lIjoiS2luZ2EiLCJmYW1pbHlfbmFtZSI6IlNvY2hhY2thIiwibmFtZSI6IktpbmdhIFNvY2hhY2thIiwiZW1haWwiOiJraW5nYS5zb2NoYWNrYTA4QGdtYWlsLmNvbSIsInN1YiI6ImtpbmdhLnNvY2hhY2thMDhAZ21haWwuY29tIiwiYXV0aG4iOnsic3ViIjoia2luZ2Euc29jaGFja2EwOEBnbWFpbC5jb20iLCJpYW1faWQiOiJJQk1pZC01NTAwMDYxMDQxIiwibmFtZSI6IktpbmdhIFNvY2hhY2thIiwiZ2l2ZW5fbmFtZSI6IktpbmdhIiwiZmFtaWx5X25hbWUiOiJTb2NoYWNrYSIsImVtYWlsIjoia2luZ2Euc29jaGFja2EwOEBnbWFpbC5jb20ifSwiYWNjb3VudCI6eyJ2YWxpZCI6dHJ1ZSwiYnNzIjoiZWFkODcxMWJhMmNjNGQwOGExNmZkMzc0MjdmNGYwMWEiLCJpbXNfdXNlcl9pZCI6IjExNDYwNjQ4IiwiZnJvemVuIjp0cnVlLCJpbXMiOiIyMTEyMDcyIn0sImlhdCI6MTY5Nzg4MDAwNiwiZXhwIjoxNjk3ODgzNjA2LCJpc3MiOiJodHRwczovL2lhbS5jbG91ZC5pYm0uY29tL2lkZW50aXR5IiwiZ3JhbnRfdHlwZSI6InVybjppYm06cGFyYW1zOm9hdXRoOmdyYW50LXR5cGU6YXBpa2V5Iiwic2NvcGUiOiJpYm0gb3BlbmlkIiwiY2xpZW50X2lkIjoiZGVmYXVsdCIsImFjciI6MSwiYW1yIjpbInB3ZCJdfQ.nn8-5WU6wli4WEFYc_rFxgv-WoYlMXtXWKcH4nkw2JPJahDVt9CTJWf4LOVh84edVFvG4KFz4Y_wWq_voohdZ9dVPskagHYfMG6x_4OcyXzdgwSE82kA8mtM0DM8wq-nWst6VX98Gk0L2QrSOsBNIMwzL2RcOlUVkk6pTaZTCrz8iKa1kC4m9_Kp58LAuNbuXChDyZ7YjC3L93maj5mXbUX7-IFUO9LT1J8CSrnPCZoMv_VLPdYz1duRSnN9AAy9jBBWOvmMIcUSNg4KfaFAvIFeWW-k6cqn3fASe2u5HD0JM-KPT_8W4tZluZJfSzz35QPQ-cIElmqXNZJWmBOycQ");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var input = "{\r\n  \"model_id\": \"google/flan-t5-xxl\",\r\n  \"input\": \"Classify this search term as a depressed, suicidal, neutral, panic attack.\\nSearch term:\\n" + searchTerm + "\\n\\nClassification:\\n\",\r\n  \"parameters\": {\r\n    \"decoding_method\": \"greedy\",\r\n    \"max_new_tokens\": 30,\r\n    \"min_new_tokens\": 0,\r\n    \"stop_sequences\": [],\r\n    \"repetition_penalty\": 1\r\n  },\r\n  \"project_id\": \"5d78d326-f680-493f-a45b-f880186106ac\"\r\n}";
                HttpContent httpContent = new StringContent(input, Encoding.UTF8, "application/json");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");

                try
                {
                    HttpResponseMessage response = await httpClient.PostAsync("", httpContent);
                    var serializedResponse = ResponseSerializer.FromJson(await response.Content.ReadAsStringAsync());
                    output = serializedResponse.Results[0].GeneratedText;
                    
                    Console.WriteLine("Output: ");
                    Console.WriteLine(output);
                    if (response.IsSuccessStatusCode)
                    {

                    }
                    else
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

//POST: https://iam.cloud.ibm.com/identity/token
