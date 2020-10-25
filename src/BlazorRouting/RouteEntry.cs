// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace BlazorRouting
{
    public class RouteEntry : IComparable<RouteEntry>, IEquatable<RouteEntry>
    {
        private static readonly char Separator = '/';

        public RouteEntry(RouteTemplate template, string[] unusedRouteParameterNames)
        {
            Template = template;
            UnusedRouteParameterNames = unusedRouteParameterNames;
        }

        public RouteTemplate Template { get; }
        public string[] UnusedRouteParameterNames { get; }

        public bool TryMatch(string path, out Dictionary<string, object?> parameters)
        {
            parameters = new Dictionary<string, object?>();
            var segments = path.Trim(Separator).Split(Separator, StringSplitOptions.RemoveEmptyEntries);
            // Individual segments are URL-decoded in order to support arbitrary characters, assuming UTF-8 encoding.
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i] = Uri.UnescapeDataString(segments[i]);
            }

            string? catchAllValue = null;

            // If this template contains a catch-all parameter, we can concatenate the pathSegments
            // at and beyond the catch-all segment's position. For example:
            // Template:        /foo/bar/{*catchAll}
            // PathSegments:    /foo/bar/one/two/three
            if (Template.ContainsCatchAllSegment && segments.Length >= Template.Segments.Length)
            {
                catchAllValue = string.Join(Separator, segments[Range.StartAt(Template.Segments.Length - 1)]);
            }
            // If there are no optional segments on the route and the length of the route
            // and the template do not match, then there is no chance of this matching and
            // we can bail early.
            else if (Template.OptionalSegmentsCount == 0 && Template.Segments.Length != segments.Length)
            {
                return false;
            }

            // Parameters will be lazily initialized.
            //Dictionary<string, object?>? parameters = null;
            var numMatchingSegments = 0;
            for (var i = 0; i < Template.Segments.Length; i++)
            {
                var segment = Template.Segments[i];

                if (segment.IsCatchAll)
                {
                    numMatchingSegments += 1;
                    parameters ??= new Dictionary<string, object?>(StringComparer.Ordinal);
                    parameters[segment.Value] = catchAllValue;
                    break;
                }

                // If the template contains more segments than the path, then
                // we may need to break out of this for-loop. This can happen
                // in one of two cases:
                //
                // (1) If we are comparing a literal route with a literal template
                // and the route is shorter than the template.
                // (2) If we are comparing a template where the last value is an optional
                // parameter that the route does not provide.
                if (i >= segments.Length)
                {
                    // If we are under condition (1) above then we can stop evaluating
                    // matches on the rest of this template.
                    if (!segment.IsParameter && !segment.IsOptional)
                    {
                        break;
                    }
                }

                string? pathSegment = null;
                if (i < segments.Length)
                {
                    pathSegment = segments[i];
                }

                if (pathSegment == null || !segment.Match(pathSegment, out var matchedParameterValue))
                {
                    return false;
                }
                else
                {
                    numMatchingSegments++;
                    if (segment.IsParameter)
                    {
                        parameters ??= new Dictionary<string, object?>(StringComparer.Ordinal);
                        parameters[segment.Value] = matchedParameterValue;
                    }
                }
            }

            // In addition to extracting parameter values from the URL, each route entry
            // also knows which other parameters should be supplied with null values. These
            // are parameters supplied by other route entries matching the same handler.
            if (!Template.ContainsCatchAllSegment && UnusedRouteParameterNames.Length > 0)
            {
                parameters ??= new Dictionary<string, object?>(StringComparer.Ordinal);
                for (var i = 0; i < UnusedRouteParameterNames.Length; i++)
                {
                    parameters[UnusedRouteParameterNames[i]] = null;
                }
            }

            // We track the number of segments in the template that matched
            // against this particular route then only select the route that
            // matches the most number of segments on the route that was passed.
            // This check is an exactness check that favors the more precise of
            // two templates in the event that the following route table exists.
            //  Route 1: /{anythingGoes}
            //  Route 2: /users/{id:int}
            // And the provided route is `/users/1`. We want to choose Route 2
            // over Route 1.
            // Furthermore, literal routes are preferred over parameterized routes.
            // If the two routes below are registered in the route table.
            // Route 1: /users/1
            // Route 2: /users/{id:int}
            // And the provided route is `/users/1`. We want to choose Route 1 over
            // Route 2.
            var allRouteSegmentsMatch = numMatchingSegments >= segments.Length;
            // Checking that all route segments have been matches does not suffice if we are
            // comparing literal templates with literal routes. For example, the template
            // `/this/is/a/template` and the route `/this/`. In that case, we want to ensure
            // that all non-optional segments have matched as well.
            var allNonOptionalSegmentsMatch = numMatchingSegments >= (Template.Segments.Length - Template.OptionalSegmentsCount);
            if (Template.ContainsCatchAllSegment || (allRouteSegmentsMatch && allNonOptionalSegmentsMatch))
            {

                return true;
            }

            return false;
        }

        /// <summary>
        /// Route precedence algorithm.
        /// We collect all the routes and sort them from most specific to
        /// less specific. The specificity of a route is given by the specificity
        /// of its segments and the position of those segments in the route.
        /// * A literal segment is more specific than a parameter segment.
        /// * A parameter segment with more constraints is more specific than one with fewer constraints
        /// * Segment earlier in the route are evaluated before segments later in the route.
        /// For example:
        /// /Literal is more specific than /Parameter
        /// /Route/With/{parameter} is more specific than /{multiple}/With/{parameters}
        /// /Product/{id:int} is more specific than /Product/{id}
        ///
        /// Routes can be ambiguous if:
        /// They are composed of literals and those literals have the same values (case insensitive)
        /// They are composed of a mix of literals and parameters, in the same relative order and the
        /// literals have the same values.
        /// For example:
        /// * /literal and /Literal
        /// /{parameter}/literal and /{something}/literal
        /// /{parameter:constraint}/literal and /{something:constraint}/literal
        ///
        /// To calculate the precedence we sort the list of routes as follows:
        /// * Shorter routes go first.
        /// * A literal wins over a parameter in precedence.
        /// * For literals with different values (case insensitive) we choose the lexical order
        /// * For parameters with different numbers of constraints, the one with more wins
        /// If we get to the end of the comparison routing we've detected an ambiguous pair of routes.
        /// </summary>
        public int CompareTo(RouteEntry? other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            var xTemplate = Template;
            var yTemplate = other!.Template;
            if (xTemplate.Segments.Length != other?.Template.Segments.Length)
            {
                return xTemplate.Segments.Length < other?.Template.Segments.Length ? -1 : 1;
            }
            else
            {
                for (var i = 0; i < xTemplate.Segments.Length; i++)
                {
                    var xSegment = xTemplate.Segments[i];
                    var ySegment = yTemplate.Segments[i];
                    if (!xSegment.IsParameter && ySegment.IsParameter)
                    {
                        return -1;
                    }
                    if (xSegment.IsParameter && !ySegment.IsParameter)
                    {
                        return 1;
                    }

                    if (xSegment.IsParameter)
                    {
                        // Always favor non-optional parameters over optional ones
                        if (!xSegment.IsOptional && ySegment.IsOptional)
                        {
                            return -1;
                        }

                        if (xSegment.IsOptional && !ySegment.IsOptional)
                        {
                            return 1;
                        }

                        if (xSegment.Constraints.Length > ySegment.Constraints.Length)
                        {
                            return -1;
                        }
                        else if (xSegment.Constraints.Length < ySegment.Constraints.Length)
                        {
                            return 1;
                        }
                    }
                    else
                    {
                        var comparison = string.Compare(xSegment.Value, ySegment.Value, StringComparison.OrdinalIgnoreCase);
                        if (comparison != 0)
                        {
                            return comparison;
                        }
                    }
                }

                throw new InvalidOperationException($"The following routes are ambiguous: '{Template.TemplateText}' '{other.Template.TemplateText}'");
            }
        }

        public override int GetHashCode() => HashCode.Combine(Template);

        public override bool Equals(object? obj) => obj is RouteEntry entry && Equals(entry);

        public bool Equals(RouteEntry? other) => Template.Equals(other);
    }
}
