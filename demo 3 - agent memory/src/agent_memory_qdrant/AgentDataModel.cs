using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Agents;

public sealed class AgentDataModel
{
    [VectorStoreRecordKey]
    public ulong id { get; set; }

    [VectorStoreRecordData(IsFilterable = true, StoragePropertyName = "name")]
    public string Name { get; set; }

    [VectorStoreRecordData(IsFullTextSearchable = true, StoragePropertyName = "description")]
    public string Description { get; set; }

    [VectorStoreRecordVector(Dimensions: 1536, StoragePropertyName = "embedding")]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    [VectorStoreRecordData(IsFilterable = true, StoragePropertyName = "tags")]
    public string[] Tags { get; set; }

    public static async Task<List<AgentDataModel>> CreateSampleDataAsync(AzureOpenAITextEmbeddingGenerationService embeddingService)
    {
        List<AgentDataModel> data =
        [
            new() {
                id = 10,
                Name = "Peter",
                Tags = new[] { "peter", "senior", "developer" },
                Description = "Senior developer working on multiple projects.",
            },
            new() {
                id = 20,
                Name = "Hanne",
                Tags = new[] { "hanne", "project manager" },
                Description = "Project manager working on 1-2 projects.",
            },
            new() {
                id = 30,
                Name = "Kim",
                Tags = new[] { "kim", "architect" },
                Description = "Architect working on the biggest projects.",
            }
        ];

        await Task.WhenAll(data.Select(entry => Task.Run(async () =>
        {
            entry.DescriptionEmbedding = await Embedding.GenerateEmbeddingAsync(entry.Description, embeddingService);
        })));

        return data;
    }
}