namespace Configurator.Server.Services;

using Configurator.Models;

public class AppDataService
{
    public EtherCATDevice Device { get; set; } = new EtherCATDevice();
    public Models.CtrlDesc.Controller Controller { get; set; } = new Models.CtrlDesc.Controller();
}