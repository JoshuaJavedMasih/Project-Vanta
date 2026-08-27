using Vanta.Models;

namespace Vanta.Services;

public interface ITelemetryProvider
{
    TelemetrySnapshot Capture();
}
