using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using Configurator.Server.Services;

namespace Configurator.Server.Pages
{
    public partial class ControllerDescription
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected AppDataService AppDataService { get; set; }

        protected async Task OpenSelectDeviceDialog(MouseEventArgs args)
        {
            await DialogService.OpenAsync<SelectDeviceDialog>($"Select EtherCAT device",
                new Dictionary<string, object>() { },
                new DialogOptions() { Width = "800px", Height = "600px", Resizable = true, Draggable = true });
        }
    }
}