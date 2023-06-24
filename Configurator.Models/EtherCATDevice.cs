namespace Configurator.Models;

public class EtherCATDevice
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public uint ProductCode { get; set; }
    public uint RevisionNo { get; set; }
    public IEnumerable<EtherCATObject> Objects { get; set; } = Enumerable.Empty<EtherCATObject>();
}
