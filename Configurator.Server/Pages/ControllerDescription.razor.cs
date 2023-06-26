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
using Configurator.Models;

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

        protected void EtherCATObjectDataGridRender(DataGridRenderEventArgs<EtherCATObject> args)
        {
            if (args.FirstRender)
            {
                args.Grid.Groups.Add(new GroupDescriptor() { Property = "Source", SortOrder = SortOrder.Ascending });
                StateHasChanged();
            }
        }

        protected async Task SelectDeviceClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<SelectDeviceDialog>($"Select EtherCAT device",
                new Dictionary<string, object>() { },
                new DialogOptions() { Width = "800px", Height = "600px", Resizable = true, Draggable = true });
        }

        protected void SelectDeviceMouseEnter(ElementReference args, TooltipOptions options = null) => TooltipService.Open(args, "Select EtherCAT device", options);
        protected void SelectDeviceMouseLeave(ElementReference args) => TooltipService.Close();

        protected async Task SaveControllerClick(MouseEventArgs args)
        {
        }

        protected void SaveControllerMouseEnter(ElementReference args, TooltipOptions options = null) => TooltipService.Open(args, "Save controller description", options);
        protected void SaveControllerMouseLeave(ElementReference args) => TooltipService.Close();
    }
}