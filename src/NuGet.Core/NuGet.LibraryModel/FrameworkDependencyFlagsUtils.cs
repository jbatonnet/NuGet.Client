// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGet.LibraryModel
{
    public class FrameworkDependencyFlagsUtils
    {
        public static FrameworkDependencyFlags Default = FrameworkDependencyFlags.None;

        /// <summary>
        /// Convert set of flag strings into a FrameworkDependencyFlags.
        /// </summary>
        public static FrameworkDependencyFlags GetFlags(IEnumerable<string> flags)
        {
            if (flags == null)
            {
                throw new ArgumentNullException(nameof(flags));
            }

            var result = FrameworkDependencyFlags.None;

            foreach (var flag in flags)
            {
                switch (flag.ToLowerInvariant())
                {
                    case "all":
                        result |= FrameworkDependencyFlags.All;
                        break;
                    default:
                        break;
                        // None is a noop here
                }
            }

            return result;
        }

        /// <summary>
        /// Convert framework dependency flags to a friendly string.
        /// </summary>
        public static string GetFlagString(FrameworkDependencyFlags flags)
        {

            switch (flags)
            {
                case FrameworkDependencyFlags.All:
                    return "all";
                case FrameworkDependencyFlags.None:
                    return "none";
                default:
                    return "none";
            }
        }

        /// <summary>
        /// Convert set of flag strings into a LibraryIncludeFlags.
        /// </summary>
        public static FrameworkDependencyFlags GetFlags(string flags)
        {
            var result = FrameworkDependencyFlags.None;

            if (!string.IsNullOrEmpty(flags))
            {
                var splitFlags = flags.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();

                if (splitFlags.Length > 0)
                {
                    result = GetFlags(splitFlags);
                }
            }

            return result;
        }
    }
}
