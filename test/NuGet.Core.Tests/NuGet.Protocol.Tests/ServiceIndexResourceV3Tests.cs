// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using Xunit;

namespace NuGet.Protocol.Tests
{
    public class ServiceIndexResourceV3Tests
    {
        private const string _resourceTypeA500 = "A/5.0.0";
        private const string _resourceTypeAVersioned = "A/Versioned";
        private const string _resourceTypeB500 = "B/5.0.0";
        private const string _resourceTypeBVersioned = "B/Versioned";
        private static readonly Uri _resourceUri500 = new Uri("https://unit.test/5.0.0");
        private static readonly Uri _resourceUri510 = new Uri("https://unit.test/5.1.0");

        private static readonly SemanticVersion _defaultVersion = new SemanticVersion(0, 0, 0);

        [Fact]
        public void Constructor_WithValidArguments_InitializesProperties()
        {
            var serviceIndex = CreateServiceIndex();
            var expectedJson = serviceIndex.ToString();
            var expectedRequestTime = DateTime.UtcNow;
            var resource = new ServiceIndexResourceV3(serviceIndex, expectedRequestTime);

            Assert.Equal(expectedJson, resource.Json);
            Assert.Equal(expectedRequestTime, resource.RequestTime);
            Assert.Equal(1, resource.Entries.Count);
            Assert.Equal("a", resource.Entries[0].Type);
            Assert.Equal("http://unit.test/b", resource.Entries[0].Uri.ToString());
        }

        [Fact]
        public void GetServiceEntry_WhenNoResourceExists_ReturnsNull()
        {
            var resource = CreateServiceIndexResourceV3(Array.Empty<ServiceIndexEntry>());

            var serviceEntry = resource.GetServiceEntry(_resourceTypeA500);

            Assert.Null(serviceEntry);
        }

        [Fact]
        public void GetServiceEntry_WhenOrderedTypesIsEmpty_ReturnsNull()
        {
            var entries = new[]
            {
                new ServiceIndexEntry(_resourceUri500, _resourceTypeA500, _defaultVersion)
            };
            var resource = CreateServiceIndexResourceV3(entries);

            var serviceEntry = resource.GetServiceEntry();

            Assert.Null(serviceEntry);
        }

        [Fact]
        public void GetServiceEntry_WhenOrderedTypesDoesNotHaveExactMatch_ReturnsNull()
        {
            var entries = new[]
            {
                new ServiceIndexEntry(_resourceUri500, _resourceTypeA500, _defaultVersion)
            };
            var resource = CreateServiceIndexResourceV3(entries);

            var serviceEntry = resource.GetServiceEntry(_resourceTypeB500);

            Assert.Null(serviceEntry);
        }

        [Fact]
        public void GetServiceEntry_WhenOrderedTypesHasExactMatch_ReturnsResource()
        {
            var expectedEntry = new ServiceIndexEntry(_resourceUri500, _resourceTypeA500, _defaultVersion);
            var resource = CreateServiceIndexResourceV3(new[] { expectedEntry });

            var actualEntry = resource.GetServiceEntry(_resourceTypeA500);

            AssertAreEqual(expectedEntry, actualEntry);
        }

        [Fact]
        public void GetServiceEntry_WhenOrderedTypesHasMultipleTypesAndFirstTypeHasMatchingResource_ReturnsResource()
        {
            var entries = new[]
            {
                new ServiceIndexEntry(_resourceUri500, _resourceTypeA500, _defaultVersion),
                new ServiceIndexEntry(_resourceUri510, _resourceTypeB500, _defaultVersion)
            };
            var resource = CreateServiceIndexResourceV3(entries);

            var actualEntry = resource.GetServiceEntry(_resourceTypeB500, _resourceTypeA500);

            Assert.Equal(_resourceTypeB500, actualEntry.Type);
        }

        [Fact]
        public void GetServiceEntry_WhenOrderedTypesHasMultipleTypesAndNotFirstButSecondTypeHasMatchingResource_ReturnsResource()
        {
            var entries = new[]
            {
                new ServiceIndexEntry(_resourceUri500, _resourceTypeA500, _defaultVersion),
                new ServiceIndexEntry(_resourceUri510, _resourceTypeB500, _defaultVersion)
            };
            var resource = CreateServiceIndexResourceV3(entries);

            var actualEntry = resource.GetServiceEntry(_resourceTypeBVersioned, _resourceTypeB500);

            Assert.Equal(_resourceTypeB500, actualEntry.Type);
        }

        [Fact]
        public void GetServiceEntry_WithClientVersionedResource_WhenClientVersionIsCompatible_ReturnsResource()
        {
            var expectedEntry = new ServiceIndexEntry(_resourceUri500, _resourceTypeAVersioned, _defaultVersion);
            var resource = CreateServiceIndexResourceV3(new[] { expectedEntry });

            var actualEntry = resource.GetServiceEntry(_resourceTypeAVersioned);

            AssertAreEqual(expectedEntry, actualEntry);
        }

        [Fact]
        public void GetServiceEntry_WithClientVersionedResource_WhenClientVersionIsNotCompatible_DoesNotReturnThatResource()
        {
            var expectedEntry = new ServiceIndexEntry(_resourceUri500, _resourceTypeAVersioned, new SemanticVersion(99, 0, 0));
            var resource = CreateServiceIndexResourceV3(new[] { expectedEntry });

            var actualEntry = resource.GetServiceEntry(_resourceTypeAVersioned);

            Assert.Null(actualEntry);
        }

        private static JObject CreateServiceIndex()
        {
            return new JObject
            {
                { "version", "1.2.3" },
                { "resources", new JArray
                    {
                        new JObject
                        {
                            { "@type", "a" },
                            { "@id", "http://unit.test/b" }
                        }
                    }
                }
            };
        }

        private static ServiceIndexResourceV3 CreateServiceIndexResourceV3(params ServiceIndexEntry[] entries)
        {
            var resources = new JArray();

            foreach (var entry in entries)
            {
                var resource = new JObject(
                    new JProperty("@id", entry.Uri.AbsoluteUri),
                    new JProperty("@type", entry.Type));

                if (entry.ClientVersion != _defaultVersion)
                {
                    resource.Add(new JProperty("clientVersion", entry.ClientVersion.ToNormalizedString()));
                }

                resources.Add(resource);
            }

            var index = new JObject();

            index.Add("version", "3.0.0");
            index.Add("resources", resources);
            index.Add("@context",
                new JObject(
                    new JProperty("@vocab", "http://schema.nuget.org/schema#"),
                    new JProperty("comment", "http://www.w3.org/2000/01/rdf-schema#comment")));

            return new ServiceIndexResourceV3(index, DateTime.UtcNow);
        }

        private static void AssertAreEqual(ServiceIndexEntry a, ServiceIndexEntry b)
        {
            if (ReferenceEquals(a, b))
            {
                return;
            }

            Assert.NotNull(a);
            Assert.NotNull(b);

            Assert.Equal(a.Type, b.Type);
            Assert.Equal(a.Uri.AbsoluteUri, b.Uri.AbsoluteUri);
            Assert.Equal(a.ClientVersion, b.ClientVersion);
        }
    }
}