namespace MachineService.Domain.Entities.MachineModule;

public sealed class Machine
{
    public Guid Id { get; set; }
    public string MachineCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
    public MachineStatus Status { get; set; } = MachineStatus.OFFLINE;
    public DateTimeOffset? LastHeartbeatAt { get; set; }
    public DateTimeOffset InstalledAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
