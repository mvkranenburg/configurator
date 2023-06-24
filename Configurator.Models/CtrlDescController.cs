namespace Configurator.Models;

public class CtrlDescController : CtrlDescObject
{
    public IEnumerable<CtrlDescObject> Objects { get; set; } = Enumerable.Empty<CtrlDescObject>();
}
