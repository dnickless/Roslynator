// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Roslynator.Diagnostics
{
    internal static class DebugHelper
    {
        [Conditional("DEBUG")]
        public static void AssertNotNull<TNode>(TNode node) where TNode : SyntaxNode
        {
            if (node == null)
                WriteLine(node);
        }

        [Conditional("DEBUG")]
        public static void Assert(bool condition, string name, SyntaxKind kind = SyntaxKind.None)
        {
            if (!condition)
                WriteLine(name, kind);
        }

        [Conditional("DEBUG")]
        public static void WriteLine<TNode>(TNode node) where TNode : SyntaxNode
        {
            Debug.WriteLine($"{nameof(node)} is null");
        }

        [Conditional("DEBUG")]
        private static void WriteLine(string name, SyntaxKind kind)
        {
            Debug.WriteLine((kind == SyntaxKind.None) ? $"{name} is {kind}" : $"{name} is not {kind}");
        }
    }
}
