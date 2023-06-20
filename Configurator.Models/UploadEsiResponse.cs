namespace Configurator.Models;

public class UploadEsiResponse
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public IEnumerable<Device> Devices { get; set; } = Enumerable.Empty<Device>();
}
