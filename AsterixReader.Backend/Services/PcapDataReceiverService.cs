using System.Threading;
using AsterixReader.Backend.Configuration;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using static SharpPcap.ICaptureDevice;

namespace AsterixReader.Backend.Services;

public class PcapDataReceiverService : IDataReceiverService, IDisposable
{
    private PcapReceiverConfig? _config;
    private ICaptureDevice? _device;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isRunning;
    private Task? _processingTask;
    private readonly object _lock = new object();
    private DisplayFilterEvaluator? _displayFilterEvaluator;
    private bool _useDisplayFilter;

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

    public void Configure(PcapReceiverConfig config)
    {
        _config = config;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource? cancellationTokenSource = null;
        ICaptureDevice? device = null;

        lock (_lock)
        {
            if (_isRunning)
                return Task.CompletedTask;

            if (_config == null)
                throw new InvalidOperationException("PcapDataReceiverService must be configured before starting.");

            if (string.IsNullOrEmpty(_config.FilePath))
                throw new InvalidOperationException("PCAP file path is required.");

            if (!File.Exists(_config.FilePath))
                throw new FileNotFoundException($"PCAP file not found: {_config.FilePath}");

            // Ensure any previous instance is fully stopped
            StopInternal();

            // Create cancellation token source inside lock to prevent race condition
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cancellationTokenSource = cancellationTokenSource;

            // Open the PCAP file inside lock
            device = new CaptureFileReaderDevice(_config.FilePath);
            device.Open();
            _device = device;
        }

        try
        {

            // Check if filter is display filter syntax or BPF syntax
            _useDisplayFilter = false;
            if (!string.IsNullOrEmpty(_config.Filter))
            {
                _useDisplayFilter = DisplayFilterEvaluator.IsDisplayFilterSyntax(_config.Filter);
                
                if (_useDisplayFilter)
                {
                    // Use post-processing display filter
                    _displayFilterEvaluator = new DisplayFilterEvaluator(_config.Filter);
                    Console.WriteLine($"Using display filter (post-processing): {_config.Filter}");
                }
                else
                {
                    // Use BPF filter (efficient, applied at libpcap level)
                    try
                    {
                        device.Filter = _config.Filter;
                        Console.WriteLine($"Applied BPF filter: {_config.Filter}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: BPF filter failed, falling back to display filter: {ex.Message}");
                        // Fall back to display filter if BPF fails
                        _useDisplayFilter = true;
                        _displayFilterEvaluator = new DisplayFilterEvaluator(_config.Filter);
                    }
                }
            }

            // Use the local cancellationTokenSource reference to avoid race condition
            var token = cancellationTokenSource.Token;
            lock (_lock)
            {
                _isRunning = true;
                // Process packets asynchronously
                _processingTask = Task.Run(async () => await ProcessPacketsAsync(token), token);
            }

            Console.WriteLine($"PCAP Receiver started with file: {_config.FilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening PCAP file: {ex.Message}");
            lock (_lock)
            {
                _device?.Close();
                _device?.Dispose();
                _device = null;
                // Clean up cancellation token source on error
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
            throw;
        }

        return Task.CompletedTask;
    }

    private async Task ProcessPacketsAsync(CancellationToken cancellationToken)
    {
        // Process packets synchronously in background thread to avoid ref struct issues with async
        await Task.Run(() =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    ICaptureDevice? device;
                    bool isRunning;

                    // Get device reference and running state with lock
                    lock (_lock)
                    {
                        device = _device;
                        isRunning = _isRunning;
                    }

                    // Exit if device is null or not running
                    if (device == null || !isRunning)
                        break;

                    try
                    {
                        // Re-check device is still valid before using it (device could be disposed by StopInternal)
                        lock (_lock)
                        {
                            if (_device == null || _device != device || !_isRunning)
                                break;
                        }

                        // Get next packet - device is validated and captured
                        PacketCapture packetCapture;
                        var status = device.GetNextPacket(out packetCapture);
                        
                        if (status != GetPacketStatus.PacketRead)
                        {
                            // End of file reached - automatically stop
                            Console.WriteLine("Reached end of PCAP file - stopping receiver");
                            lock (_lock)
                            {
                                _isRunning = false;
                            }
                            // Stop the receiver cleanly
                            StopInternal();
                            break;
                        }

                        var rawCapture = packetCapture.GetPacket();

                        // Parse the packet
                        var packet = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

                        // Apply display filter if enabled (post-processing)
                        if (_useDisplayFilter && _displayFilterEvaluator != null)
                        {
                            if (!_displayFilterEvaluator.Matches(packet))
                            {
                                // Packet doesn't match display filter, skip it
                                continue;
                            }
                        }

                        // Extract payload based on packet type
                        byte[]? payload = null;

                        // Try to extract UDP payload
                        var udpPacket = packet.Extract<UdpPacket>();
                        if (udpPacket != null && udpPacket.PayloadData != null && udpPacket.PayloadData.Length > 0)
                        {
                            payload = udpPacket.PayloadData;
                        }
                        // If no UDP payload, use raw packet data
                        else if (rawCapture.Data != null && rawCapture.Data.Length > 0)
                        {
                            payload = rawCapture.Data;
                        }

                        if (payload != null && payload.Length > 0)
                        {
                            DataReceived?.Invoke(this, payload);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Device was disposed, exit
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing packet: {ex.Message}");
                        // Continue processing other packets
                    }

                    // Small delay to prevent CPU spinning
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PCAP processing loop: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _isRunning = false;
                }
            }
        }, cancellationToken);
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
        if (!_isRunning && _device == null)
            return;

        _isRunning = false;
        _cancellationTokenSource?.Cancel();

        // Wait for processing task to complete (with timeout)
        if (_processingTask != null)
        {
            try
            {
                _processingTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error waiting for processing task: {ex.Message}");
            }
            _processingTask = null;
        }

        // Close and dispose device
        try
        {
            _device?.Close();
            _device?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing PCAP device: {ex.Message}");
        }
        finally
        {
            _device = null;
        }

        // Dispose cancellation token
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        Console.WriteLine("PCAP Receiver stopped and device closed");
    }

    public void Dispose()
    {
        lock (_lock)
        {
            StopInternal();
        }
    }
}
