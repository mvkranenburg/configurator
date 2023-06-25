namespace Configurator.Models;

public class EtherCATObject
{
    public ushort Index { get; set; }
    public string Type { get; set; } = string.Empty;
    public int BitSize { get; set; }
    public EtherCATObjectAccess Access { get; set; }
    public EtherCATObjectPdoMapping PdoMapping { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public EtherCATObjectSource? Source { get; set; }
}
