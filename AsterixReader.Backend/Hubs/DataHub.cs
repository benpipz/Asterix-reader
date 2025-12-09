using AsterixReader.Backend.Models;
using AsterixReader.Backend.Services;
using Microsoft.AspNetCore.SignalR;

namespace AsterixReader.Backend.Hubs;

public class DataHub : Hub
{
    private readonly IDataStorageService _storageService;

    public DataHub(IDataStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<List<ReceivedData>> GetAllData()
    {
        return _storageService.GetAllData();
    }

    public async Task<ReceivedData?> GetLatestData()
    {
        var allData = _storageService.GetAllData();
        return allData.OrderByDescending(d => d.Timestamp).FirstOrDefault();
    }

    public async Task<int> GetDataCount()
    {
        return _storageService.GetCount();
    }
}

