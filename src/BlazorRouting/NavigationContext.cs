// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace BlazorRouting
{
    public sealed class NavigationContext
    {
        internal NavigationContext(string path, CancellationToken cancellationToken)
        {
            Path = path;
            CancellationToken = cancellationToken;
        }

        public string Path { get; }

        public CancellationToken CancellationToken { get; }
    }
}
