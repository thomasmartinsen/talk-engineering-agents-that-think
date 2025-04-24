using Microsoft.Extensions.VectorData;

namespace Agents.Models;

public class UserQuery
{
    [VectorStoreRecordKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [VectorStoreRecordData]
    public string Query { get; set; }

    [VectorStoreRecordData]
    public string Summary { get; set; }

    [VectorStoreRecordData]
    public string TimeStamp { get; set; }

    [VectorStoreRecordVector(1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
