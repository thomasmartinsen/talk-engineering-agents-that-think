using Microsoft.Extensions.VectorData;

namespace Agents;

public class AgentDataModel<T>
{
    [VectorStoreRecordKey]
    public T id { get; set; }

    [VectorStoreRecordData(IsFilterable = true, StoragePropertyName = "name")]
    public string Name { get; set; }

    [VectorStoreRecordData(IsFullTextSearchable = true, StoragePropertyName = "description")]
    public string Description { get; set; }

    [VectorStoreRecordData(IsFilterable = true, StoragePropertyName = "tags")]
    public string[] Tags { get; set; }
}

public class AgentDataModelVector<T> : AgentDataModel<T>
{
    [VectorStoreRecordVector(Dimensions: 4, DistanceFunction.CosineSimilarity, StoragePropertyName = "vector")]
    public ReadOnlyMemory<float>? Vector { get; set; }
}