using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Radzen;
using Configurator.Server.Shared;
using Configurator.Models;

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

        EventConsole console;
        IEnumerable<Device> devices = Enumerable.Empty<Device>();

        // Progress variables
        int progress = 0;
        bool showProgress = false;
        bool cancelUpload = false;

        // Progress variables
        bool showComplete = false;
        string completeMessage = string.Empty;

        // Error variables
        bool showError = false;
        string errorMessage = string.Empty;

        void OnError(UploadErrorEventArgs args)
        {
            console.Log($"OnError: {args.Message}");

            errorMessage = args.Message;

            showProgress = false;
            showComplete = false;
            showError = true;
        }

        void OnComplete(UploadCompleteEventArgs args)
        {
            console.Log($"OnComplete: Cancelled={args.Cancelled}, Json={args.RawResponse}");

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
            console.Log($"OnProgress: Progress={args.Progress}, Cancel={cancelUpload}");

            args.Cancel = cancelUpload;
            cancelUpload = false;

            progress = args.Progress;

            showProgress = true;
            showComplete = false;
            showError = false;
        }

        void CancelUpload()
        {
            console.Log("CancelUpload");

            cancelUpload = true;
        }
    }
}