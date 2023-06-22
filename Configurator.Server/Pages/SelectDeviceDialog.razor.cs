using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Radzen;
using Configurator.Models;
using Configurator.Server.Services;

namespace Configurator.Server.Pages
{
    public partial class SelectDeviceDialog
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

        IEnumerable<EtherCATDevice> devices = Enumerable.Empty<EtherCATDevice>();

        // Select ESI variables
        int progress = 0;
        bool showProgress = false;
        bool cancelUpload = false;

        bool showComplete = false;
        string completeMessage = string.Empty;

        bool showError = false;
        string errorMessage = string.Empty;

        // Select device variables
        string pagingSummaryFormat = "Page {0} of {1} ({2} devices)";
        IList<EtherCATDevice> selectedDevices = new List<EtherCATDevice>();

        void OnError(UploadErrorEventArgs args)
        {
            errorMessage = args.Message;

            showProgress = false;
            showComplete = false;
            showError = true;
        }

        void OnComplete(UploadCompleteEventArgs args)
        {
            showProgress = false;
            if (args.Cancelled)
            {
                showComplete = false;
                showError = false;
            }
            else
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var response = JsonSerializer.Deserialize<UploadEsiResponse>(args.RawResponse, options);

                    devices = response.Devices;
                    var numDevices = devices.Count();

                    completeMessage = $"Loaded {response.Name} ({response.Size / 1024} KB) containing {numDevices} EtherCAT device{(numDevices == 1 ? "" : "s")}.";

                    showComplete = true;
                    showError = false;
                }
                catch (Exception ex)
                {
                    errorMessage = $"Internal {ex.GetType().Name}: {ex.Message}";

                    showComplete = false;
                    showError = true;
                }
            }
        }

        void OnProgress(UploadProgressArgs args)
        {
            args.Cancel = cancelUpload;
            cancelUpload = false;

            progress = args.Progress;

            showProgress = true;
            showComplete = false;
            showError = false;
        }

        void CancelUpload()
        {
            cancelUpload = true;
        }

        async Task OnSelectClick()
        {
            AppDataService.Device = selectedDevices.First();
            DialogService.Close(true);
        }
    }
}