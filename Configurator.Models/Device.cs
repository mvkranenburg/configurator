namespace Configurator.Models;

public class Device
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public uint ProductCode { get; set; }
    public uint RevisionNo { get; set; }

    public override string ToString()
    {
        return $"Device {Type}, 0x{ProductCode:X8}, 0x{RevisionNo:X8}: {Name}";
    }
}
