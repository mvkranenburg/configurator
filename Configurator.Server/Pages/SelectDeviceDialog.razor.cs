using System.Text.Json.Nodes;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Radzen;

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

        Shared.EventConsole console;

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

            if (!args.Cancelled)
            {
                var node = JsonNode.Parse(args.RawResponse);
                var name = node["name"].GetValue<string>();
                var size = node["size"].GetValue<uint>();
                var numDevices = node["numDevices"].GetValue<uint>();

                completeMessage = $"Loaded {name} ({size / 1024} KB) containing {numDevices} EtherCAT device{(numDevices == 1 ? "" : "s")}.";
            }

            showProgress = false;
            showComplete = !args.Cancelled;
            showError = false;
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