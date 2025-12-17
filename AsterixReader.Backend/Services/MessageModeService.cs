using System;
using AsterixReader.Backend.Models;

namespace AsterixReader.Backend.Services;

public class MessageModeService : IMessageModeService
{
    private MessageMode _currentMode = MessageMode.Default;
    private readonly object _lock = new object();

    public MessageMode CurrentMode
    {
        get
        {
            lock (_lock)
            {
                return _currentMode;
            }
        }
    }

    public void SetMode(MessageMode mode)
    {
        lock (_lock)
        {
            if (!Enum.IsDefined(typeof(MessageMode), mode))
            {
                throw new ArgumentException($"Invalid MessageMode value: {mode}", nameof(mode));
            }
            _currentMode = mode;
        }
    }
}

