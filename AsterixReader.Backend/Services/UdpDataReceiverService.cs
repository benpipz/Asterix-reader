using System.Net;
using System.Net.Sockets;
using AsterixReader.Backend.Configuration;

namespace AsterixReader.Backend.Services;

public class UdpDataReceiverService : IDataReceiverService, IDisposable
{
    private UdpReceiverConfig? _config;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listeningTask;
    private bool _isRunning;
    private readonly object _lock = new object();

    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning;
            }
        }
    }

    public event EventHandler<byte[]>? DataReceived;

    public void Configure(UdpReceiverConfig config)
    {
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource? cancellationTokenSource = null;
        UdpClient? udpClient = null;

        lock (_lock)
        {
            if (_isRunning)
                return;

            if (_config == null)
                throw new InvalidOperationException("UdpDataReceiverService must be configured before starting.");

            // Ensure any previous instance is fully stopped
            StopInternal();

            // Create new cancellation token source inside lock to prevent race condition
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cancellationTokenSource = cancellationTokenSource;
        }

        // Create completely new UDP client outside lock (network operations can be slow)
        var localEndPoint = new IPEndPoint(IPAddress.Parse(_config!.ListenAddress), _config.Port);
        udpClient = new UdpClient(localEndPoint);

        // Join multicast group if configured
        if (_config.JoinMulticastGroup && !string.IsNullOrEmpty(_config.MulticastAddress))
        {
            try
            {
                var multicastAddress = IPAddress.Parse(_config.MulticastAddress);
                udpClient.JoinMulticastGroup(multicastAddress);
                Console.WriteLine($"Joined multicast group: {multicastAddress}");
            }
            catch (Exception ex)
            {
                udpClient?.Dispose();
                udpClient = null;
                Console.WriteLine($"Failed to join multicast group {_config.MulticastAddress}: {ex.Message}");
                // Clean up cancellation token source on error
                lock (_lock)
                {
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
                throw;
            }
        }

        // Use the local cancellationTokenSource reference to avoid race condition
        var token = cancellationTokenSource.Token;
        lock (_lock)
        {
            // Re-check if we were stopped while creating the UDP client
            // (StopInternal could have been called from another thread)
            if (_cancellationTokenSource != cancellationTokenSource)
            {
                // We were stopped, clean up and exit
                udpClient?.Dispose();
                return;
            }

            _udpClient = udpClient;
            _isRunning = true;
            _listeningTask = Task.Run(async () => await ListenForDataAsync(token), token);
        }

        Console.WriteLine($"UDP Receiver started on {_config.ListenAddress}:{_config.Port}");
    }

    public Task StopAsync()
    {
        lock (_lock)
        {
            StopInternal();
        }
        return Task.CompletedTask;
    }

    private void StopInternal()
    {
        if (!_isRunning && _udpClient == null)
            return;

        _isRunning = false;

        // Cancel token first
        _cancellationTokenSource?.Cancel();

        // Close and dispose the socket immediately - this will interrupt ReceiveAsync
        try
        {
            // Leave multicast group if joined
            if (_udpClient != null && _config != null && _config.JoinMulticastGroup && !string.IsNullOrEmpty(_config.MulticastAddress))
            {
                try
                {
                    var multicastAddress = IPAddress.Parse(_config.MulticastAddress);
                    _udpClient.DropMulticastGroup(multicastAddress);
                    Console.WriteLine($"Left multicast group: {multicastAddress}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error leaving multicast group: {ex.Message}");
                }
            }

            // Force close the socket - this will cause ReceiveAsync to throw immediately
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing UDP socket: {ex.Message}");
        }

        // Wait for listening task to complete (with timeout)
        if (_listeningTask != null)
        {
            try
            {
                _listeningTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error waiting for listening task: {ex.Message}");
            }
            _listeningTask = null;
        }

        // Dispose cancellation token
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        Console.WriteLine("UDP Receiver stopped and socket closed");
    }

    private async Task ListenForDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                UdpClient? client;
                lock (_lock)
                {
                    client = _udpClient;
                    if (client == null || !_isRunning)
                        break;
                }

                try
                {
                    var result = await client.ReceiveAsync(cancellationToken);
                    if (result.Buffer.Length > 0 && _isRunning)
                    {
                        DataReceived?.Invoke(this, result.Buffer);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Socket was disposed, exit immediately
                    break;
                }
                catch (SocketException)
                {
                    // Socket error - exit if not running
                    if (!_isRunning)
                        break;
                    // Otherwise wait a bit and check again
                    await Task.Delay(100, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP listener error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            StopInternal();
        }
    }
}

