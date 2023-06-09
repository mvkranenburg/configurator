namespace Configurator.Models;

public class EtherCATObject
{
    public ushort? Index { get; set; }
    public byte? SubIndex { get; set; }
    public string Type { get; set; } = string.Empty;
    public int BitSize { get; set; }
    public EtherCATObjectAccess? Access { get; set; }
    public EtherCATObjectPdoMapping? PdoMapping { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Comment { get; set; } = string.Empty;
    public IEnumerable<EtherCATObject> Objects { get; set; } = Enumerable.Empty<EtherCATObject>();
    public EtherCATObjectSource? Source { get; set; }
}
