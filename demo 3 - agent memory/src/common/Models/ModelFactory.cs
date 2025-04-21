namespace Agents;

public class ModelFactory<T>
{
    private static T _currentKey;

    static ModelFactory()
    {
        _currentKey = default(T);
    }

    public static List<AgentDataModel<T>> GetData()
    {
        List<AgentDataModel<T>> data =
        [
            new() {
                id = KeyGeneratorReturnNext(),
                Name = "Peter",
                Tags = new[] { "peter", "senior", "developer" },
                Description = "Senior developer working on multiple projects.",
            },
            new() {
                id = KeyGeneratorReturnNext(),
                Name = "Hanne",
                Tags = new[] { "hanne", "project manager" },
                Description = "Project manager working on 1-2 projects.",
            },
            new() {
                id = KeyGeneratorReturnNext(),
                Name = "Kim",
                Tags = new[] { "kim", "architect" },
                Description = "Architect working on the biggest projects.",
            }
        ];

        return data;
    }

    public static List<AgentDataModelVector<T>> GetVectorDataAsync()
    {
        var data = GetData();
        var vectorData = new List<AgentDataModelVector<T>>();

        foreach (var item in data)
        {
            vectorData.Add(new AgentDataModelVector<T>
            {
                id = item.id,
                Name = item.Name,
                Description = item.Description
            });
        }

        return vectorData;
    }

    public static T KeyGeneratorReturnNext()
    {
        _currentKey = typeof(T) switch
        {
            Type t when t == typeof(int) => (T)Convert.ChangeType(Convert.ToInt32(_currentKey) + 1, typeof(T)),
            Type t when t == typeof(long) => (T)Convert.ChangeType(Convert.ToInt64(_currentKey) + 1, typeof(T)),
            Type t when t == typeof(ulong) => (T)Convert.ChangeType(Convert.ToUInt64(_currentKey) + 1, typeof(T)),
            Type t when t == typeof(string) => (T)Convert.ChangeType(Guid.NewGuid().ToString(), typeof(T)),
            _ => throw new InvalidOperationException("Unsupported key type")
        };

        return _currentKey;
    }
}
