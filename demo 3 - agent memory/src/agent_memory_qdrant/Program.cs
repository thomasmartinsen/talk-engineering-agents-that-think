using Agents;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

var azureOpenAIEndpoint = Configuration.GetValue("AZURE_OPENAI_ENDPOINT");
var azureOpenAIApiKey = Configuration.GetValue("AZURE_OPENAI_APIKEY");
var azureOpenAIChatModel = Configuration.GetValue("AZURE_OPENAI_CHAT_MODELID");
var azureOpenAIEmbeddedModel = Configuration.GetValue("AZURE_OPENAI_EMBEDDED_MODELID");

var collectionName = "memory";

Kernel kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(azureOpenAIChatModel, azureOpenAIEndpoint, azureOpenAIApiKey)
    .AddInMemoryVectorStore()
    .Build();

var textEmbeddingGenerationService = new AzureOpenAITextEmbeddingGenerationService(
        azureOpenAIEmbeddedModel,
        azureOpenAIEndpoint,
        azureOpenAIApiKey);

var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"));
var collection = vectorStore.GetCollection<ulong, AgentDataModel>(collectionName);
await collection.CreateCollectionIfNotExistsAsync();

var data = await AgentDataModel.CreateSampleDataAsync(textEmbeddingGenerationService);
await Task.WhenAll(data.Select(x => collection.UpsertAsync(x)));

var searchString = "Who is working on my team as architect";
var searchVector = await Agents.Embedding.GenerateEmbeddingAsync(searchString, textEmbeddingGenerationService);
var searchResult = await collection.VectorizedSearchAsync(searchVector, new() { Top = 1 });
var resultRecords = await searchResult.Results.ToListAsync();
var first = resultRecords.FirstOrDefault()?.Record;

Console.WriteLine($"Result: {first?.Name}, {first?.Description}");
Console.WriteLine();
Console.ReadLine();
