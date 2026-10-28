using System;
using System.Collections.Generic;

namespace ProjectName.Shared.Discovery
{
    /// <summary>
    /// Represents a remote device discovered on the local area network.
    /// </summary>
    public record DiscoveredDevice(
        string DeviceId,
        string DisplayName,
        string Platform,
        string HostOrIp,
        int ControlPort,
        IReadOnlyList<string> Capabilities,
        DateTime LastSeenUtc
    );
}
