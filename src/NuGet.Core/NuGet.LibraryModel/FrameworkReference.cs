// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace NuGet.LibraryModel
{
    public class FrameworkDependency : IEquatable<FrameworkDependency>, IComparable<FrameworkDependency>
    {
        public string Name { get; }

        public FrameworkDependencyFlags PrivateAssets { get; }

        public FrameworkDependency(
            string name,
            FrameworkDependencyFlags privateAssets
            )
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            PrivateAssets = privateAssets;

        }

        public int CompareTo(FrameworkDependency other)
        {
            throw new NotImplementedException();
        }

        public bool Equals(FrameworkDependency other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return StringComparer.OrdinalIgnoreCase.Equals(Name, other.Name) &&
                   PrivateAssets.Equals(other.PrivateAssets);
        }
    }
}
