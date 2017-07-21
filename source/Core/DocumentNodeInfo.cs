// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Roslynator
{
    internal struct DocumentNodeInfo
    {
        public DocumentNodeInfo(Document document, SyntaxNode root, ImmutableArray<SyntaxNode> nodes)
        {
            Document = document;
            Root = root;
            Nodes = nodes;
        }

        public Document Document { get; }
        public SyntaxNode Root { get; }
        public ImmutableArray<SyntaxNode> Nodes { get; }

        public SyntaxTree SyntaxTree
        {
            get { return (!Nodes.IsDefaultOrEmpty) ? Nodes[0].SyntaxTree : null; }
        }
    }
}
