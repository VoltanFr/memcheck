using Microsoft.ApplicationInsights;
using System.Collections.Generic;

namespace MemCheck.Application;

public interface IMemCheckTelemetryClient
{
    void TrackEvent(string eventName, params (string key, string value)[] properties);
}
public sealed class MemCheckTelemetryClient : IMemCheckTelemetryClient
{
    private readonly TelemetryClient telemetryClient;

    public MemCheckTelemetryClient(TelemetryClient telemetryClient)
    {
        this.telemetryClient = telemetryClient;
    }
    public void TrackEvent(string eventName, params (string key, string value)[] properties)
    {
        var dico = new Dictionary<string, string>();
        foreach (var (key, value) in properties)
            dico.Add(key, value);
        telemetryClient.TrackEvent(eventName, dico);
    }
}
