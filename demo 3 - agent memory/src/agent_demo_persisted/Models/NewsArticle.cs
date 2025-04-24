using Microsoft.Extensions.VectorData;

namespace Agents.Models;

public class NewsArticle
{
    [VectorStoreRecordKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [VectorStoreRecordData(IsFilterable = true)]
    public string Title { get; set; }

    [VectorStoreRecordData]
    public string Content { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public string Url { get; set; }

    [VectorStoreRecordVector(1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
