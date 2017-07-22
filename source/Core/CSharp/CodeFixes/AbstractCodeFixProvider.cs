// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.CodeFixes
{
    public abstract class AbstractCodeFixProvider : CodeFixProvider
    {
        internal const string EquivalenceKeySuffix = "CodeFixProvider";

        protected static bool TryFindFirstAncestorOrSelf<TNode>(
            SyntaxNode root,
            TextSpan span,
            out TNode result,
            Func<TNode, bool> predicate = null,
            bool findInsideTrivia = false,
            bool getInnermostNodeForTie = true,
            bool ascendOutOfTrivia = true) where TNode : SyntaxNode
        {
            result = root
                .FindNode(span, findInsideTrivia: findInsideTrivia, getInnermostNodeForTie: getInnermostNodeForTie)?
                .FirstAncestorOrSelf(predicate, ascendOutOfTrivia: ascendOutOfTrivia);

            DebugIfNull(result);

            return result != null;
        }

        protected static bool TryFindFirstDescendantOrSelf<TNode>(
            SyntaxNode root,
            TextSpan span,
            out TNode result,
            bool findInsideTrivia = false,
            bool getInnermostNodeForTie = true,
            Func<SyntaxNode, bool> descendIntoChildren = null,
            bool descendIntoTrivia = true) where TNode : SyntaxNode
        {
            result = root
                .FindNode(span, findInsideTrivia: findInsideTrivia, getInnermostNodeForTie: getInnermostNodeForTie)?
                .FirstDescendantOrSelf<TNode>(span, descendIntoChildren: descendIntoChildren, descendIntoTrivia: descendIntoTrivia);

            DebugIfNull(result);

            return result != null;
        }

        protected static bool TryFindNode<TNode>(
            SyntaxNode root,
            TextSpan span,
            out TNode result,
            bool findInsideTrivia = false,
            bool getInnermostNodeForTie = true) where TNode : SyntaxNode
        {
            result = root.FindNode(span, findInsideTrivia: findInsideTrivia, getInnermostNodeForTie: getInnermostNodeForTie) as TNode;

            DebugIfNull(result);

            return result != null;
        }

        protected static bool TryFindToken(
            SyntaxNode root,
            int position,
            out SyntaxToken token,
            SyntaxKind kind = SyntaxKind.None,
            bool findInsideTrivia = true)
        {
            token = root.FindToken(position, findInsideTrivia: findInsideTrivia);

            bool success = (kind == SyntaxKind.None) ? !token.IsKind(SyntaxKind.None) : token.IsKind(kind);

            DebugIf(success, nameof(token), kind);

            return success;
        }

        protected static bool TryFindTrivia(
            SyntaxNode root,
            int position,
            out SyntaxTrivia trivia,
            SyntaxKind kind = SyntaxKind.None,
            bool findInsideTrivia = true)
        {
            trivia = root.FindTrivia(position, findInsideTrivia: findInsideTrivia);

            bool success = (kind == SyntaxKind.None) ? !trivia.IsKind(SyntaxKind.None) : trivia.IsKind(kind);

            DebugIf(success, nameof(trivia), kind);

            return success;
        }

        [Conditional("DEBUG")]
        private static void DebugIfNull<TNode>(TNode result) where TNode : SyntaxNode
        {
            Debug.Assert(result != null, $"{nameof(result)} is null");
        }

        [Conditional("DEBUG")]
        private static void DebugIf(bool condition, string name, SyntaxKind kind = SyntaxKind.None)
        {
            Debug.Assert(condition, (kind == SyntaxKind.None) ? $"{name} is {kind}" : $"{name} is not {kind}");
        }
    }
}
