﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Common.Core.Test.Fixtures;
using Microsoft.R.Editor.Application.Test;
using Microsoft.UnitTests.Core.XUnit;

[assembly: TestFrameworkOverride]
[assembly: VsAssemblyLoader]
[assembly: AssemblyFixtureImport(typeof(REditorApplicationShellProviderFixture), typeof(ServiceManagerFixture))]
