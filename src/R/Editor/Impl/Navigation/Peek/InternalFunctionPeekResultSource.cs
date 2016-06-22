﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Languages.Editor.Shell;
using Microsoft.Languages.Editor.Tasks;
using Microsoft.R.Components.ContentTypes;
using Microsoft.R.Core.Formatting;
using Microsoft.R.Editor.Settings;
using Microsoft.R.Host.Client;
using Microsoft.R.Host.Client.Session;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.R.Editor.Navigation.Peek {
    internal sealed class InternalFunctionPeekResultSource : IPeekResultSource {
        private readonly InternalFunctionPeekItem _peekItem;
        private Exception _exception;
        private IDocumentPeekResult _result;

        internal Task<IDocumentPeekResult> LookupTask { get; }

        public InternalFunctionPeekResultSource(string sourceFileName, Span sourceSpan, string functionName, InternalFunctionPeekItem peekItem) {
            _peekItem = peekItem;
            // Start asynchronous function fetching so by the time FindResults 
            // is called the task may be already completed or close to that.
            LookupTask = FindFunctionAsync(sourceFileName, sourceSpan, functionName);
        }

        public void FindResults(string relationshipName,
                                IPeekResultCollection resultCollection,
                                CancellationToken cancellationToken,
                                IFindPeekResultsCallback callback) {
            if (relationshipName != PredefinedPeekRelationships.Definitions.Name) {
                return;
            }

            // If task is still running, wait a bit, but not too long.
            LookupTask.Wait(2000);

            if (LookupTask.IsCompleted) {
                resultCollection.Add(LookupTask.Result);
            } else if (_exception != null) {
                callback.ReportFailure(_exception);
            }
        }

        private async Task<IDocumentPeekResult> FindFunctionAsync(string sourceFileName, Span sourceSpan, string functionName) {
            try {
                string code = await GetFunctionCode(functionName);
                if (!string.IsNullOrEmpty(code)) {
                    string tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".r");
                    using (var sw = new StreamWriter(tempFile)) {
                        sw.Write(code);
                    }

                    using (var displayInfo = new PeekResultDisplayInfo(
                        label: functionName, labelTooltip: functionName,
                        title: functionName, titleTooltip: functionName)) {

                        _result = _peekItem.PeekResultFactory.Create(displayInfo, tempFile, new Span(0, 0), 0, isReadOnly: true);
                        
                        // Editor opens external items as plain text. When file opens, change content type to R.
                        IdleTimeAction.Create(() => {
                            if (_result.Span.IsDocumentOpen) {
                                var rs = EditorShell.Current.ExportProvider.GetExportedValue<IContentTypeRegistryService>();
                                var ct = rs.GetContentType(RContentTypeDefinition.ContentType);
                                _result.Span.Document.TextBuffer.ChangeContentType(ct, this.GetType());
                                try { File.Delete(tempFile); } catch(IOException) { } catch(AccessViolationException) { }
                            }
                        }, 50, GetType(), EditorShell.Current);

                        return _result;
                     }
                }
            } catch (Exception ex) {
                _exception = ex;
            }
            return null;
        }

        private async Task<string> GetFunctionCode(string functionName) {
            var sessionProvider = EditorShell.Current.ExportProvider.GetExportedValue<IRSessionProvider>();
            var rSession = sessionProvider.GetInteractiveWindowRSession();
            string functionCode = await rSession.GetFunctionCodeAsync(functionName);
            if (!string.IsNullOrEmpty(functionCode)) {
                var formatter = new RFormatter(REditorSettings.FormatOptions);
                functionCode = formatter.Format(functionCode);
            }
            return functionCode;
        }
    }
}
