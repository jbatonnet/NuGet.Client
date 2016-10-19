﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.ProjectModel;

namespace NuGet.ProjectManagement
{
    public class DependencyGraphCacheContext
    {
        public DependencyGraphCacheContext(ILogger logger)
        {
            Logger = logger;
        }

        public DependencyGraphCacheContext()
        {
            Logger = NullLogger.Instance;
        }

        /// <summary>
        /// Unique name to dg
        /// </summary>
        public Dictionary<string, DependencyGraphSpec> Cache { get; set; } =
            new Dictionary<string, DependencyGraphSpec>(StringComparer.Ordinal);

        /// <summary>
        /// Unique name to last modified
        /// </summary>
        public Dictionary<string, DateTimeOffset> LastModified { get; set; } = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);

        public DependencyGraphSpec SolutionSpec { get; set; }

        public int SolutionSpecHash { get; set; }

        /// <summary>
        /// Logger
        /// </summary>
        public ILogger Logger { get; }
    }
}
