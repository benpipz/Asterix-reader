using System.Net;
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

    public event EventHandler<byte[]>? DataReceived;

    public void Configure(PcapReceiverConfig config)
    {
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
            return;

        if (_config == null)
            throw new InvalidOperationException("PcapDataReceiverService must be configured before starting.");

        if (string.IsNullOrEmpty(_config.FilePath))
            throw new InvalidOperationException("PCAP file path is required.");

        if (!File.Exists(_config.FilePath))
            throw new FileNotFoundException($"PCAP file not found: {_config.FilePath}");

        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Open the PCAP file
            _device = new CaptureFileReaderDevice(_config.FilePath);
            _device.Open();

            // Apply filter if provided
            if (!string.IsNullOrEmpty(_config.Filter))
            {
                _device.Filter = _config.Filter;
                Console.WriteLine($"Applied PCAP filter: {_config.Filter}");
            }

            _isRunning = true;

            // Process packets asynchronously
            _processingTask = Task.Run(async () => await ProcessPacketsAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening PCAP file: {ex.Message}");
            _device?.Close();
            _device?.Dispose();
            _device = null;
            throw;
        }
    }

    private async Task ProcessPacketsAsync(CancellationToken cancellationToken)
    {
        if (_device == null)
            return;

        // Process packets synchronously in background thread to avoid ref struct issues with async
        await Task.Run(() =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _isRunning)
                {
                    try
                    {
                        // Get next packet
                        PacketCapture packetCapture;
                        var status = _device.GetNextPacket(out packetCapture);
                        
                        if (status != GetPacketStatus.PacketRead)
                        {
                            // End of file reached
                            Console.WriteLine("Reached end of PCAP file");
                            break;
                        }

                        var rawCapture = packetCapture.GetPacket();

                        // Parse the packet
                        var packet = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

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
                _isRunning = false;
            }
        }, cancellationToken);
    }

    public Task StopAsync()
    {
        _isRunning = false;
        _cancellationTokenSource?.Cancel();
        
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
        }

        _device?.Close();
        _device?.Dispose();
        _device = null;
        
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        StopAsync().Wait(1000);
        _device?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}

