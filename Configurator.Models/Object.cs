namespace Configurator.Models;

public class EtherCATObject
{
    public ushort Index { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}
