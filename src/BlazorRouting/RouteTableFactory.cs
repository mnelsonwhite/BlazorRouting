// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace BlazorRouting
{
    internal static class RouteTableFactory
    {
        private static readonly ConcurrentDictionary<Key, HandlerRouteEntry<Type>[]> _cache =
            new ConcurrentDictionary<Key, HandlerRouteEntry<Type>[]>();

        public static HandlerRouteEntry<Type>[] GetRouteEntries(IEnumerable<Assembly> assemblies)
        {
            var key = new Key(assemblies.OrderBy(a => a.FullName).ToArray());
            if (_cache.TryGetValue(key, out var resolvedComponents))
            {
                return resolvedComponents;
            }

            var componentTypes = key.Assemblies.SelectMany(a => a.ExportedTypes.Where(t => typeof(IComponent).IsAssignableFrom(t)));
            var routeEntries = GetRouteEntries(componentTypes);
            _cache.TryAdd(key, routeEntries);
            return routeEntries;
        }

        internal static HandlerRouteEntry<Type>[] GetRouteEntries(IEnumerable<Type> componentTypes)
        {
            var templatesByHandler = new Dictionary<Type, string[]>();
            foreach (var componentType in componentTypes)
            {
                // We're deliberately using inherit = false here.
                //
                // RouteAttribute is defined as non-inherited, because inheriting a route attribute always causes an
                // ambiguity. You end up with two components (base class and derived class) with the same route.
                var routeAttributes = componentType.GetCustomAttributes<RouteAttribute>(inherit: false);

                var templates = routeAttributes.Select(t => t.Template).ToArray();
                templatesByHandler.Add(componentType, templates);
            }
            return GetRouteEntries(templatesByHandler);
        }

        internal static HandlerRouteEntry<Type>[] GetRouteEntries(Dictionary<Type, string[]> templatesByHandler)
        {
            var routes = new List<HandlerRouteEntry<Type>>();
            foreach (var keyValuePair in templatesByHandler)
            {
                var parsedTemplates = keyValuePair.Value.Select(v => TemplateParser.ParseTemplate(v)).ToArray();
                var allRouteParameterNames = parsedTemplates
                    .SelectMany(GetParameterNames)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                foreach (var parsedTemplate in parsedTemplates)
                {
                    var unusedRouteParameterNames = allRouteParameterNames
                        .Except(GetParameterNames(parsedTemplate), StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    var entry = new HandlerRouteEntry<Type>(parsedTemplate, keyValuePair.Key, unusedRouteParameterNames);
                    routes.Add(entry);
                }
            }

            return routes.OrderBy(route => route).ToArray();
        }

        private static string[] GetParameterNames(RouteTemplate routeTemplate)
        {
            return routeTemplate.Segments
                .Where(s => s.IsParameter)
                .Select(s => s.Value)
                .ToArray();
        }

        private readonly struct Key : IEquatable<Key>
        {
            public readonly Assembly[] Assemblies;

            public Key(Assembly[] assemblies)
            {
                Assemblies = assemblies;
            }

            public override bool Equals(object? obj)
            {
                return obj is Key other ? base.Equals(other) : false;
            }

            public bool Equals(Key other)
            {
                if (Assemblies == null && other.Assemblies == null)
                {
                    return true;
                }
                else if ((Assemblies == null) || (other.Assemblies == null))
                {
                    return false;
                }
                else if (Assemblies.Length != other.Assemblies.Length)
                {
                    return false;
                }

                for (var i = 0; i < Assemblies.Length; i++)
                {
                    if (!Assemblies[i].Equals(other.Assemblies[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                var hash = new HashCode();

                if (Assemblies != null)
                {
                    for (var i = 0; i < Assemblies.Length; i++)
                    {
                        hash.Add(Assemblies[i]);
                    }
                }

                return hash.ToHashCode();
            }
        }
    }
}
