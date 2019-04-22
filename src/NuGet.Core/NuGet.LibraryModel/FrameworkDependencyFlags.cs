// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.LibraryModel
{
    public enum FrameworkDependencyFlags : ushort
    {
        None = 0,
        All = 1 << 0,
    }
}
