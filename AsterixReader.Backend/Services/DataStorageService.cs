using System.Collections.Concurrent;
using AsterixReader.Backend.Models;

namespace AsterixReader.Backend.Services;

public class DataStorageService : IDataStorageService
{
    private readonly ConcurrentBag<ReceivedData> _data = new();

    public void AddData(ReceivedData data)
    {
        _data.Add(data);
    }

    public List<ReceivedData> GetAllData()
    {
        return _data.ToList();
    }

    public ReceivedData? GetDataById(Guid id)
    {
        return _data.FirstOrDefault(d => d.Id == id);
    }

    public void ClearData()
    {
        while (!_data.IsEmpty)
        {
            _data.TryTake(out _);
        }
    }

    public int GetCount()
    {
        return _data.Count;
    }
}


