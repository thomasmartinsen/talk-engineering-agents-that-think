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

var data = ModelFactory<ulong>.GetVectorDataAsync();
foreach (var item in data)
{
    item.Vector = await embeddingService.GenerateEmbeddingAsync(item.Description);
    await collection.UpsertAsync(item);
}

var searchString = "Who is working on my team as architect";
var searchVector = await embeddingService.GenerateEmbeddingAsync(searchString);
var searchResult = await collection.VectorizedSearchAsync(searchVector, new() { Top = 1 });
var resultRecords = await searchResult.Results.ToListAsync();
var first = resultRecords.FirstOrDefault()?.Record;

Console.WriteLine($"Result: {first?.Name}, {first?.Description}");
Console.WriteLine();
Console.ReadLine();
