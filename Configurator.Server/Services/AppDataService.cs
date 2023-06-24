namespace Configurator.Server.Services;

using Configurator.Models;

public class AppDataService
{
    public EtherCATDevice Device { get; set; } = new EtherCATDevice();
    public CtrlDescController Controller { get; set; } = new CtrlDescController();
}