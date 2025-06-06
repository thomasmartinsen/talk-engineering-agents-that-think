﻿using System.Text.Json;
using Agents;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureCosmosDBNoSQL;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Embeddings;

var azureOpenAIEndpoint = Configuration.GetValue("AZURE_OPENAI_ENDPOINT");
var azureOpenAIApiKey = Configuration.GetValue("AZURE_OPENAI_APIKEY");
var azureOpenAIChatModel = Configuration.GetValue("AZURE_OPENAI_CHAT_MODELID");
var azureOpenAIEmbeddedModel = Configuration.GetValue("AZURE_OPENAI_EMBEDDED_MODELID");

var cosmosDbConnectionString = Configuration.GetValue("AZURE_COSMOSDB_CONNECTIONSTRING");
var cosmosDbName = "AgentDB";
var collectionName = "memory";

var jsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

Kernel kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(azureOpenAIChatModel, azureOpenAIEndpoint, azureOpenAIApiKey)
    .AddAzureCosmosDBNoSQLVectorStore(cosmosDbConnectionString, cosmosDbName)
    .Build();

var embeddingService = new AzureOpenAITextEmbeddingGenerationService(azureOpenAIEmbeddedModel, azureOpenAIEndpoint, azureOpenAIApiKey);

var cosmosClient = new CosmosClient(cosmosDbConnectionString, new CosmosClientOptions()
{
    UseSystemTextJsonSerializerWithOptions = jsonSerializerOptions
});
var database = cosmosClient.GetDatabase(cosmosDbName);
var collection = new AzureCosmosDBNoSQLVectorStoreRecordCollection<AgentDataModelVector<string>>(
    database,
    collectionName,
    new()
    {
        JsonSerializerOptions = jsonSerializerOptions,
        PartitionKeyPropertyName = nameof(AgentDataModelVector<string>.Name)
    });
await collection.CreateCollectionIfNotExistsAsync();

var data = ModelFactory<string>.GetVectorDataAsync();
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

//async Task<List<AgentDataModel>> CreateSampleDataEntriesAsync()
//{
//    List<AgentDataModel> data =
//    [
//        new() {
//            id = "10",
//            Name = "Peter",
//            Tags = new[] { "peter", "senior", "developer" },
//            Description = "Senior developer working on multiple projects.",
//        },
//        new() {
//            id = "20",
//            Name = "Hanne",
//            Tags = new[] { "hanne", "project manager" },
//            Description = "Project manager working on 1-2 projects.",
//        },
//        new() {
//            id = "30",
//            Name = "Kim",
//            Tags = new[] { "kim", "architect" },
//            Description = "Architect working on the biggest projects.",
//        }
//    ];

//    await Task.WhenAll(data.Select(entry => Task.Run(async () =>
//    {
//        entry.DescriptionEmbedding = await GenerateEmbeddingAsync(entry.Description);
//    })));

//    return data;
//}

//async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string textToVectorize)
//{
//    var definition = await textEmbeddingGenerationService.GenerateEmbeddingAsync(textToVectorize);
//    return definition.Data;
//}
