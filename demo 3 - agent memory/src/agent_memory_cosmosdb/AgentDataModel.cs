using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace Agents;

public sealed class AgentDataModel
{
    [VectorStoreRecordKey]
    public string id { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public string Name { get; set; }

    [VectorStoreRecordData(IsFullTextSearchable = true)]
    public string Description { get; set; }

    [VectorStoreRecordVector(Dimensions: 4, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public string[] Tags { get; set; }

    public static async Task<List<AgentDataModel>> CreateSampleDataAsync(AzureOpenAITextEmbeddingGenerationService embeddingService)
    {
        List<AgentDataModel> data =
        [
            new() {
                id = "10",
                Name = "Peter",
                Tags = new[] { "peter", "senior", "developer" },
                Description = "Senior developer working on multiple projects.",
            },
            new() {
                id = "20",
                Name = "Hanne",
                Tags = new[] { "hanne", "project manager" },
                Description = "Project manager working on 1-2 projects.",
            },
            new() {
                id = "30",
                Name = "Kim",
                Tags = new[] { "kim", "architect" },
                Description = "Architect working on the biggest projects.",
            }
        ];

        await Task.WhenAll(data.Select(entry => Task.Run(async () =>
        {
            entry.DescriptionEmbedding = await Agents.Embedding.GenerateEmbeddingAsync(entry.Description, embeddingService);
        })));

        return data;
    }
}
