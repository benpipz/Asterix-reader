using AsterixReader.Backend.Models;

namespace AsterixReader.Backend.Services;

public interface IDataStorageService
{
    void AddData(ReceivedData data);
    List<ReceivedData> GetAllData();
    ReceivedData? GetDataById(Guid id);
    void ClearData();
    int GetCount();
}

