// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace BlazorRouting
{
    [DebuggerDisplay("{TemplateText}")]
    public class RouteTemplate : IEquatable<RouteTemplate>
    {
        public RouteTemplate(string templateText, TemplateSegment[] segments)
        {
            TemplateText = templateText;
            Segments = segments;
            OptionalSegmentsCount = segments.Count(template => template.IsOptional);
            ContainsCatchAllSegment = segments.Any(template => template.IsCatchAll);
        }

        public string TemplateText { get; }
        public TemplateSegment[] Segments { get; }
        public int OptionalSegmentsCount { get; }
        public bool ContainsCatchAllSegment { get; }

        public bool Equals(RouteTemplate other)
        {
            return string.Equals(TemplateText, other.TemplateText, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(TemplateText);
        }

        public override bool Equals(object obj)
        {
            return obj is RouteTemplate template && Equals(template);
        }
    }

}
