﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.R.Components.Controller;
using Microsoft.R.Components.InteractiveWorkflow;

namespace Microsoft.R.Components.Plots.Implementation.Commands {
    internal sealed class PlotDeviceCutCopyCommand : PlotDeviceCommand, IAsyncCommand {
        private bool _cut;

        public PlotDeviceCutCopyCommand(IRInteractiveWorkflow interactiveWorkflow, IRPlotDeviceVisualComponent visualComponent, bool cut)
            : base(interactiveWorkflow, visualComponent) {
            _cut = cut;
        }

        public CommandStatus Status {
            get {
                if (HasCurrentPlot && !IsInLocatorMode) {
                    return CommandStatus.SupportedAndEnabled;
                }

                return CommandStatus.Supported;
            }
        }

        public Task<CommandResult> InvokeAsync() {
            try {
                VisualComponent.CopyToClipboard(_cut);
            } catch (ExternalException ex) {
                InteractiveWorkflow.Shell.ShowErrorMessage(ex.Message);
            }

            return Task.FromResult(CommandResult.Executed);
        }
    }
}
