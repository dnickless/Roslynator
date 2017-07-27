// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using static Roslynator.Diagnostics.DebugHelper;

namespace Roslynator.CSharp.CodeFixes
{
    public abstract class AbstractCodeFixProvider : CodeFixProvider
    {
        internal const string EquivalenceKeySuffix = "CodeFixProvider";

        protected static bool TryFindFirstAncestorOrSelf<TNode>(
            SyntaxNode root,
            TextSpan span,
            out TNode node,
            Func<TNode, bool> predicate = null,
            bool findInsideTrivia = false,
            bool getInnermostNodeForTie = true,
            bool ascendOutOfTrivia = true) where TNode : SyntaxNode
        {
            node = root
                .FindNode(span, findInsideTrivia: findInsideTrivia, getInnermostNodeForTie: getInnermostNodeForTie)?
                .FirstAncestorOrSelf(predicate, ascendOutOfTrivia: ascendOutOfTrivia);

            AssertNotNull(node);

            return node != null;
        }

        protected static bool TryFindFirstDescendantOrSelf<TNode>(
            SyntaxNode root,
            TextSpan span,
            out TNode node,
            bool findInsideTrivia = false,
            bool getInnermostNodeForTie = true,
            Func<SyntaxNode, bool> descendIntoChildren = null,
            bool descendIntoTrivia = true) where TNode : SyntaxNode
        {
            node = root
                .FindNode(span, findInsideTrivia: findInsideTrivia, getInnermostNodeForTie: getInnermostNodeForTie)?
                .FirstDescendantOrSelf<TNode>(span, descendIntoChildren: descendIntoChildren, descendIntoTrivia: descendIntoTrivia);

            AssertNotNull(node);

            return node != null;
        }

        protected static bool TryFindNode<TNode>(
            SyntaxNode root,
            TextSpan span,
            out TNode node,
            bool findInsideTrivia = false,
            bool getInnermostNodeForTie = true,
            Func<TNode, bool> predicate = null) where TNode : SyntaxNode
        {
            node = root.FindNode(span, findInsideTrivia: findInsideTrivia, getInnermostNodeForTie: getInnermostNodeForTie) as TNode;

            if (node != null
                && predicate != null
                && !predicate(node))
            {
                node = null;
            }

            AssertNotNull(node);

            return node != null;
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

            Assert(success, nameof(token), kind);

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

            Assert(success, nameof(trivia), kind);

            return success;
        }
    }
}
