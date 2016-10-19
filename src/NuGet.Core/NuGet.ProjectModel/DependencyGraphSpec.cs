﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Packaging;

namespace NuGet.ProjectModel
{
    public class DependencyGraphSpec
    {
        private readonly SortedSet<string> _restore = new SortedSet<string>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, PackageSpec> _projects = new SortedDictionary<string, PackageSpec>(StringComparer.Ordinal);

        public DependencyGraphSpec(JObject json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            ParseJson(json);

            Json = json;
        }

        public DependencyGraphSpec()
        {
            Json = new JObject();
        }

        /// <summary>
        /// Projects to restore.
        /// </summary>
        public IReadOnlyList<string> Restore
        {
            get
            {
                return _restore.ToList();
            }
        }

        /// <summary>
        /// All project specs.
        /// </summary>
        public IReadOnlyList<PackageSpec> Projects
        {
            get
            {
                return _projects.Values.ToList();
            }
        }

        /// <summary>
        /// File json.
        /// </summary>
        public JObject Json { get; }

        public PackageSpec GetProjectSpec(string projectUniqueName)
        {
            PackageSpec project;
            _projects.TryGetValue(projectUniqueName, out project);

            return project;
        }

        public IReadOnlyList<string> GetParents(string rootUniqueName)
        {
            return Projects.Select(p => p.RestoreMetadata.ProjectUniqueName)
                .Where(id => GetClosure(id).Any(p => p.RestoreMetadata.ProjectUniqueName == rootUniqueName))
                .ToList();
        }

        /// <summary>
        /// Retrieve the full project closure including the root project itself.
        /// </summary>
        public IReadOnlyList<PackageSpec> GetClosure(string rootUniqueName)
        {
            if (rootUniqueName == null)
            {
                throw new ArgumentNullException(nameof(rootUniqueName));
            }

            var closure = new List<PackageSpec>();

            var added = new SortedSet<string>(StringComparer.Ordinal);
            var toWalk = new Stack<PackageSpec>();

            // Start with the root
            toWalk.Push(GetProjectSpec(rootUniqueName));

            while (toWalk.Count > 0)
            {
                var spec = toWalk.Pop();

                if (spec != null)
                {
                    // Add every spec to the closure
                    closure.Add(spec);

                    // Find children
                    foreach (var projectName in GetProjectReferenceNames(spec))
                    {
                        if (added.Add(projectName))
                        {
                            toWalk.Push(GetProjectSpec(projectName));
                        }
                    }
                }
            }
            return SortPackagesByDependencyOrder(closure);
        }

        private static IEnumerable<string> GetProjectReferenceNames(PackageSpec spec)
        {
            // Handle projects which may not have specs, and which may not have references
            return spec?.RestoreMetadata?
                .TargetFrameworks
                .SelectMany(e => e.ProjectReferences)
                .Select(project => project.ProjectUniqueName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                ?? Enumerable.Empty<string>();
        }

        public void AddRestore(string projectUniqueName)
        {
            _restore.Add(projectUniqueName);
        }

        public void AddProject(PackageSpec projectSpec)
        {
            // Find the unique name in the spec, otherwise generate a new one.
            var projectUniqueName = projectSpec.RestoreMetadata?.ProjectUniqueName
                ?? Guid.NewGuid().ToString();

            _projects.Add(projectUniqueName, projectSpec);
        }

        public static  DependencyGraphSpec Union(IEnumerable<DependencyGraphSpec> dgSpecs )
        {
            var projects =
                dgSpecs.SelectMany(e => e.Projects)
                    .GroupBy(e => e.RestoreMetadata.ProjectUniqueName, StringComparer.Ordinal)
                    .Select(e => e.First())
                    .ToList();

            var newDgSpec = new DependencyGraphSpec();
            foreach (var project in projects)
            {
                newDgSpec.AddProject(project);
            }
            return newDgSpec;
        }

        public static DependencyGraphSpec Load(string path)
        {
            var json = ReadJson(path);

            return Load(json);
        }

        public static DependencyGraphSpec Load(JObject json)
        {
            return new DependencyGraphSpec(json);
        }

        public void Save(string path)
        {
            var json = GetJson(spec: this);

            using (var fileStream = new FileStream(path, FileMode.Create))
            using (var textWriter = new StreamWriter(fileStream))
            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                jsonWriter.Formatting = Formatting.Indented;
                json.WriteTo(jsonWriter);
            }
        }

        public static JObject GetJson(DependencyGraphSpec spec)
        {
            var json = new JObject();
            var restoreObj = new JObject();
            var projectsObj = new JObject();
            var toolsArray = new JArray();
            json["format"] = 1;
            json["restore"] = restoreObj;
            json["projects"] = projectsObj;

            foreach (var restoreName in spec.Restore)
            {
                restoreObj[restoreName] = new JObject();
            }

            foreach (var project in spec.Projects)
            {
                // Convert package spec to json
                var projectObj = new JObject();
                JsonPackageSpecWriter.WritePackageSpec(project, projectObj);

                projectsObj[project.RestoreMetadata.ProjectUniqueName] = projectObj;
            }

            return json;
        }

        private void ParseJson(JObject json)
        {
            var restoreObj = json.GetValue<JObject>("restore");
            if (restoreObj != null)
            {
                _restore.UnionWith(restoreObj.Properties().Select(prop => prop.Name));
            }

            var projectsObj = json.GetValue<JObject>("projects");
            if (projectsObj != null)
            {
                foreach (var prop in projectsObj.Properties())
                {
                    var specJson = (JObject)prop.Value;
                    var spec = JsonPackageSpecReader.GetPackageSpec(specJson);

                    _projects.Add(prop.Name, spec);
                }
            }
        }

        private static JObject ReadJson(string packageSpecPath)
        {
            JObject json;

            using (var stream = new FileStream(packageSpecPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                try
                {
                    json = JObject.Load(reader);
                }
                catch (JsonReaderException ex)
                {
                    throw FileFormatException.Create(ex, packageSpecPath);
                }
            }

            return json;
        }

        public override int GetHashCode()
        {
            //TODO : Write a better hash function
            var jobject = GetJson(this);
            return jobject.ToString().GetHashCode();
        }

        /// <summary>
        /// Order dependencies by children first.
        /// </summary>
        internal static IReadOnlyList<PackageSpec> SortPackagesByDependencyOrder(
            IEnumerable<PackageSpec> packages)
        {
            var sorted = new List<PackageSpec>();
            var toSort = packages.Distinct().ToList();

            while (toSort.Count > 0)
            {
                // Order packages by parent count, take the child with the lowest number of parents
                // and remove it from the list
                var nextPackage = toSort.OrderBy(package => GetParentCount(toSort, package.RestoreMetadata.ProjectUniqueName))
                    .ThenBy(package => package.RestoreMetadata.ProjectUniqueName, StringComparer.Ordinal).First();

                sorted.Add(nextPackage);
                toSort.Remove(nextPackage);
            }

            // the list is ordered by parents first, reverse to run children first
            sorted.Reverse();

            return sorted;
        }

        private static int GetParentCount(List<PackageSpec> packages, string projectUniqueName)
        {
            int count = 0;

            foreach (var package in packages)
            {
                if (package.RestoreMetadata.TargetFrameworks.SelectMany(r=> r.ProjectReferences).Any(dependency =>
                        string.Equals(projectUniqueName, dependency.ProjectUniqueName, StringComparison.OrdinalIgnoreCase)))
                {
                    count++;
                }
            }

            return count;
        }
    }
}