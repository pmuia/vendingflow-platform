using System.Net.Http.Json;
using InventoryService.Application.Interfaces;

namespace InventoryService.Infrastructure;

public sealed class TelemetryMachineAvailabilityService : IMachineAvailabilityService, IDisposable
{
    private readonly HttpClient _httpClient;

    public TelemetryMachineAvailabilityService(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(3)
        };
    }

    public async Task<MachineAvailability> CheckAsync(string machineId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                $"api/telemetry/machines/{Uri.EscapeDataString(machineId)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
                return Blocked(machineId, "health could not be verified");

            var health = await response.Content.ReadFromJsonAsync<MachineHealth>(cancellationToken);
            if (health is null || string.IsNullOrWhiteSpace(health.State))
                return Blocked(machineId, "has not reported telemetry yet");

            if (health.State.Equals("OFFLINE", StringComparison.OrdinalIgnoreCase))
                return Blocked(machineId, "is offline");

            if (!string.Equals(health.DoorStatus, "CLOSED", StringComparison.OrdinalIgnoreCase))
                return Blocked(machineId, "door is not securely closed");

            if (string.IsNullOrWhiteSpace(health.DispenserStatus) ||
                health.DispenserStatus.Equals("ERROR", StringComparison.OrdinalIgnoreCase) ||
                health.DispenserStatus.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase))
                return Blocked(machineId, "dispenser is unavailable");

            if (!health.State.Equals("ONLINE", StringComparison.OrdinalIgnoreCase) &&
                !health.State.Equals("DEGRADED", StringComparison.OrdinalIgnoreCase))
                return Blocked(machineId, $"health state is {health.State}");

            return new MachineAvailability(true, string.Empty);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Blocked(machineId, "health check timed out");
        }
        catch (HttpRequestException)
        {
            return Blocked(machineId, "health service is unavailable");
        }
    }

    public void Dispose() => _httpClient.Dispose();

    private static MachineAvailability Blocked(string machineId, string reason) =>
        new(false, $"Purchases are unavailable because {machineId} {reason}.");

    private sealed record MachineHealth(string MachineId, string State, string DispenserStatus, string DoorStatus);
}
