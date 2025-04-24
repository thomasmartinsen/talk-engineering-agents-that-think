using Microsoft.Extensions.VectorData;

namespace Agents.Models;

public class UserData
{
    [VectorStoreRecordKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [VectorStoreRecordData(IsFilterable = true)]
    public string Kind { get; set; }

    [VectorStoreRecordData]
    public string Text { get; set; }

    [VectorStoreRecordData]
    public string TimeStamp { get; set; }

    [VectorStoreRecordVector(1536, DistanceFunction.CosineSimilarity, IndexKind.Hnsw)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
