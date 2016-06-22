﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Common.Core.Test.Utility;
using Microsoft.Languages.Core.Test.Utility;
using Microsoft.Languages.Editor.Outline;
using Microsoft.Languages.Editor.Shell;
using Microsoft.R.Components.ContentTypes;
using Microsoft.R.Editor.Outline;
using Microsoft.R.Editor.Test.Mocks;
using Microsoft.R.Editor.Tree;
using Microsoft.VisualStudio.Editor.Mocks;

namespace Microsoft.R.Editor.Test.Outline {
    [ExcludeFromCodeCoverage]
    public class OutlineTest {
        public static OutlineRegionCollection BuildOutlineRegions(string content) {
            TextBufferMock textBuffer = new TextBufferMock(content, RContentTypeDefinition.ContentType);
            using (EditorTree tree = new EditorTree(textBuffer)) {
                tree.Build();

                EditorDocumentMock editorDocument = new EditorDocumentMock(tree);
                ROutlineRegionBuilder ob = new ROutlineRegionBuilder(editorDocument, EditorShell.Current);
                OutlineRegionCollection rc = new OutlineRegionCollection(0);
                ob.BuildRegions(rc);

                return rc;
            }
        }

        // change to true in debugger if you want all baseline tree files regenerated
        private static bool _regenerateBaselineFiles = false;

        public static void OutlineFile(EditorTestFilesFixture fixture, string name) {
            string testFile = fixture.GetDestinationPath(name);
            string baselineFile = testFile + ".outline";
            string text = fixture.LoadDestinationFile(name);

            OutlineRegionCollection rc = BuildOutlineRegions(text);
            string actual = TextRangeCollectionWriter.WriteCollection(rc);

            if (_regenerateBaselineFiles) {
                baselineFile = Path.Combine(fixture.SourcePath, Path.GetFileName(testFile)) + ".outline";
                TestFiles.UpdateBaseline(baselineFile, actual);
            } else {
                TestFiles.CompareToBaseLine(baselineFile, actual);
            }
        }
    }
}
