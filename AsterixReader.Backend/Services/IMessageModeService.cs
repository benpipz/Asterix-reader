using AsterixReader.Backend.Models;

namespace AsterixReader.Backend.Services;

public interface IMessageModeService
{
    MessageMode CurrentMode { get; }
    void SetMode(MessageMode mode);
}

