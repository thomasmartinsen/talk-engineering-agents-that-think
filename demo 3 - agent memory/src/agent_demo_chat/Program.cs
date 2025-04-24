using Agents;
using Agents.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;

var OPENAI_APIKEY = Configuration.GetValue("OPENAI_APIKEY");
var OPENAI_CHAT_MODELID = Configuration.GetValue("OPENAI_CHAT_MODELID");
var OPENAI_EMBEDDING_MODELID = Configuration.GetValue("OPENAI_EMBEDDING_MODELID");
var AZURE_OPENAI_ENDPOINT = Configuration.GetValue("AZURE_OPENAI_ENDPOINT");
var AZURE_OPENAI_APIKEY = Configuration.GetValue("AZURE_OPENAI_APIKEY");
var AZURE_OPENAI_CHAT_MODELID = Configuration.GetValue("AZURE_OPENAI_CHAT_MODELID");
var AZURE_OPENAI_EMBEDDING_MODELID = Configuration.GetValue("AZURE_OPENAI_EMBEDDING_MODELID");

string VECTOR_STORE_HOST = "localhost";
string USER_COLLECTION = "user_queries";
string NEWS_COLLECTION = "news_articles";

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.SemanticKernel", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Extensions.Http", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http", LogLevel.Warning);

CancellationTokenSource appShutdownCancellationTokenSource = new();
CancellationToken appShutdownCancellationToken = appShutdownCancellationTokenSource.Token;
builder.Services.AddKeyedSingleton("AppShutdown", appShutdownCancellationTokenSource);

var kernelBuilder = builder.Services.AddKernel();
kernelBuilder.AddAzureOpenAIChatCompletion(AZURE_OPENAI_CHAT_MODELID, AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_APIKEY);
kernelBuilder.AddOpenAITextEmbeddingGeneration(OPENAI_EMBEDDING_MODELID, OPENAI_APIKEY);
kernelBuilder.AddQdrantVectorStore(VECTOR_STORE_HOST);
kernelBuilder.AddQdrantVectorStoreRecordCollection<Guid, TextSnippet<Guid>>(NEWS_COLLECTION, VECTOR_STORE_HOST);
kernelBuilder.AddQdrantVectorStoreRecordCollection<Guid, UserQuery>(USER_COLLECTION, VECTOR_STORE_HOST);

RegisterServices<Guid>(builder, kernelBuilder);

using IHost host = builder.Build();
await host.RunAsync(appShutdownCancellationToken).ConfigureAwait(false);

void RegisterServices<TKey>(HostApplicationBuilder builder, IKernelBuilder kernelBuilder)
    where TKey : notnull
{
    kernelBuilder.AddVectorStoreTextSearch<TextSnippet<TKey>>(
        new TextSearchStringMapper((result) => (result as TextSnippet<TKey>)!.Text!),
        new TextSearchResultMapper((result) =>
        {
            var castResult = result as TextSnippet<TKey>;
            return new TextSearchResult(value: castResult!.Text!)
            {
                Name = castResult.ReferenceDescription,
                Link = castResult.ReferenceLink
            };
        }));

    builder.Services.AddSingleton<UniqueKeyGenerator<Guid>>(new UniqueKeyGenerator<Guid>(() => Guid.NewGuid()));
    builder.Services.AddSingleton<UniqueKeyGenerator<string>>(new UniqueKeyGenerator<string>(() => Guid.NewGuid().ToString()));

    builder.Services.AddHostedService<ChatServiceWithPrediction<TKey>>();
}
