using System.Net;
using System.Net.Sockets;
using AsterixReader.Backend.Configuration;
using Microsoft.Extensions.Options;

namespace AsterixReader.Backend.Services;

public class UdpDataReceiverService : IDataReceiverService, IDisposable
{
    private readonly UdpSettings _settings;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning;

    public event EventHandler<byte[]>? DataReceived;

    public UdpDataReceiverService(IOptions<UdpSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
            return;

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(_settings.ListenAddress), _settings.Port));
        _isRunning = true;

        _ = Task.Run(async () => await ListenForDataAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
    }

    public Task StopAsync()
    {
        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        _udpClient?.Close();
        _udpClient?.Dispose();
        return Task.CompletedTask;
    }

    private async Task ListenForDataAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _isRunning)
        {
            try
            {
                if (_udpClient == null)
                    break;

                var result = await _udpClient.ReceiveAsync(cancellationToken);
                if (result.Buffer.Length > 0)
                {
                    DataReceived?.Invoke(this, result.Buffer);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                // Log error but continue listening
                Console.WriteLine($"UDP Socket Error: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in UDP listener: {ex.Message}");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        StopAsync().Wait(1000);
        _udpClient?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}

