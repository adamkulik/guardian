using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using LLama.Common;
using LLama;
using LLama.Abstractions;

namespace CaptureTraffic
{
    public class SentimentAnalyzer
    {
        private StatelessExecutor executor;
        private LLamaWeights model;
        public SentimentAnalyzer(string modelPath)
        {
            var parameters = new ModelParams(modelPath)
            {
                ContextSize = 128,
                Seed = 420,
                GpuLayerCount = 20,
                Threads = 6,
                UseMemorymap = true
            };
            model = LLamaWeights.LoadFromFile(parameters);
            executor = new StatelessExecutor(model, parameters);
        }
        public async Task<string> AnalyzeSentiment(string searchTerm)
        {
            var prompt = "Classify this search term to one of following categories: depression, bullying, suicide attempt, " +
                          "panic attack, neutral\r\nSearch term:\r\n" + searchTerm + "\r\nClassification:\r\n";
            StringBuilder sb = new StringBuilder();
            var inferenceParams = new InferenceParams() { Temperature = 0.25f, MaxTokens = 4 };
            await foreach(var text in executor.InferAsync(prompt, inferenceParams))
            {
                sb.Append(text);
            }
            return sb.ToString();
        }
        public string SyncSentimentAnalyze(string searchTerm)
        {
            Task<string> task = Task.Run<string>(async () => await AnalyzeSentiment(searchTerm));
            return task.Result;
        }

    }
}
