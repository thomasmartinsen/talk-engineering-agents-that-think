using Agents;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;

var azureOpenAIEndpoint = Configuration.GetValue("AZURE_OPENAI_ENDPOINT");
var azureOpenAIApiKey = Configuration.GetValue("AZURE_OPENAI_APIKEY");
var azureOpenAIChatModel = Configuration.GetValue("AZURE_OPENAI_CHAT_MODELID");
var azureOpenAIEmbeddedModel = Configuration.GetValue("AZURE_OPENAI_EMBEDDED_MODELID");

var collectionName = "memory";
var host = "localhost";

Kernel kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(azureOpenAIChatModel, azureOpenAIEndpoint, azureOpenAIApiKey)
    .AddInMemoryVectorStore()
    .Build();

var embeddingService = new AzureOpenAITextEmbeddingGenerationService(azureOpenAIEmbeddedModel, azureOpenAIEndpoint, azureOpenAIApiKey);

var vectorStore = new QdrantVectorStore(new QdrantClient(host));
var collection = vectorStore.GetCollection<ulong, AgentDataModelVector<ulong>>(collectionName);
await collection.CreateCollectionIfNotExistsAsync();

var data = ModelFactory<ulong>.GetSemanticKernelSampleData();
foreach (var item in data)
{
    try
    {
        using var httpClient = new HttpClient();
        var content = await httpClient.GetStringAsync(item.Name);
        if (content == null) continue;
        content = content.Length > 8000 ? new string([.. content.Take(8000)]) : content;
        item.Vector = await embeddingService!.GenerateEmbeddingAsync(content);
        await collection.UpsertAsync(item);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing {item.Name}: {ex.Message}");
        continue;
    }
}

var searchString = "What can you tell me about Semantic Kernel?";
var searchVector = await embeddingService.GenerateEmbeddingAsync(searchString);
var searchResult = await collection.VectorizedSearchAsync(searchVector, new() { Top = 1 });
var resultRecords = await searchResult.Results.ToListAsync();
var first = resultRecords.FirstOrDefault()?.Record;

Console.WriteLine($"Result: {first?.Name}, {first?.Description}");
Console.WriteLine();
Console.ReadLine();
