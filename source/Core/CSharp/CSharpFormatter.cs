﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Roslynator.CSharp.CSharpFactory;

namespace Roslynator.CSharp
{
    internal static class CSharpFormatter
    {
        public static Task<Document> ToSingleLineAsync<TNode>(
            Document document,
            TNode condition,
            CancellationToken cancellationToken = default(CancellationToken)) where TNode : SyntaxNode
        {
            TNode newNode = ToSingleLine(condition);

            return document.ReplaceNodeAsync(condition, newNode, cancellationToken);
        }

        public static TNode ToSingleLine<TNode>(TNode node) where TNode : SyntaxNode
        {
            return node
                .RemoveWhitespaceOrEndOfLineTrivia(node.Span)
                .WithFormatterAnnotation();
        }

        public static Task<Document> ToSingleLineAsync(
            Document document,
            InitializerExpressionSyntax initializer,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            InitializerExpressionSyntax newInitializer = initializer
                .ReplaceWhitespaceOrEndOfLineTrivia(ElasticSpace, TextSpan.FromBounds(initializer.FullSpan.Start, initializer.Span.End))
                .WithFormatterAnnotation();

            SyntaxNode parent = initializer.Parent;

            SyntaxNode newParent;

            switch (parent.Kind())
            {
                case SyntaxKind.ObjectCreationExpression:
                    {
                        var expression = (ObjectCreationExpressionSyntax)parent;

                        expression = expression.WithInitializer(newInitializer);

                        ArgumentListSyntax argumentList = expression.ArgumentList;

                        if (argumentList != null)
                        {
                            newParent = expression.WithArgumentList(argumentList.WithoutTrailingTrivia());
                        }
                        else
                        {
                            newParent = expression.WithType(expression.Type.WithoutTrailingTrivia());
                        }

                        break;
                    }
                case SyntaxKind.ArrayCreationExpression:
                    {
                        var expression = (ArrayCreationExpressionSyntax)parent;

                        newParent = expression
                            .WithInitializer(newInitializer)
                            .WithType(expression.Type.WithoutTrailingTrivia());

                        break;
                    }
                case SyntaxKind.ImplicitArrayCreationExpression:
                    {
                        var expression = (ImplicitArrayCreationExpressionSyntax)parent;

                        newParent = expression
                            .WithInitializer(newInitializer)
                            .WithCloseBracketToken(expression.CloseBracketToken.WithoutTrailingTrivia());

                        break;
                    }
                default:
                    {
                        Debug.Fail(parent.Kind().ToString());

                        return document.ReplaceNodeAsync(initializer, newInitializer, cancellationToken);
                    }
            }

            return document.ReplaceNodeAsync(parent, newParent, cancellationToken);
        }

        public static Task<Document> ToMultiLineAsync(
            Document document,
            ConditionalExpressionSyntax conditionalExpression,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ConditionalExpressionSyntax newNode = ToMultiLine(conditionalExpression, cancellationToken);

            return document.ReplaceNodeAsync(conditionalExpression, newNode, cancellationToken);
        }

        public static ConditionalExpressionSyntax ToMultiLine(ConditionalExpressionSyntax conditionalExpression, CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxTriviaList leadingTrivia = GetIncreasedIndentation(conditionalExpression, cancellationToken);

            leadingTrivia = leadingTrivia.Insert(0, NewLine());

            return ConditionalExpression(
                    conditionalExpression.Condition.WithoutTrailingTrivia(),
                    Token(leadingTrivia, SyntaxKind.QuestionToken, TriviaList(Space)),
                    conditionalExpression.WhenTrue.WithoutTrailingTrivia(),
                    Token(leadingTrivia, SyntaxKind.ColonToken, TriviaList(Space)),
                    conditionalExpression.WhenFalse.WithoutTrailingTrivia());
        }

        public static Task<Document> ToMultiLineAsync(
            Document document,
            ParameterListSyntax parameterList,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ParameterListSyntax newNode = ToMultiLine(parameterList);

            return document.ReplaceNodeAsync(parameterList, newNode, cancellationToken);
        }

        public static ParameterListSyntax ToMultiLine(ParameterListSyntax parameterList, CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxTriviaList leadingTrivia = GetIncreasedIndentation(parameterList, cancellationToken);

            var nodesAndTokens = new List<SyntaxNodeOrToken>();

            SeparatedSyntaxList<ParameterSyntax>.Enumerator en = parameterList.Parameters.GetEnumerator();

            if (en.MoveNext())
            {
                nodesAndTokens.Add(en.Current.WithLeadingTrivia(leadingTrivia));

                while (en.MoveNext())
                {
                    nodesAndTokens.Add(CommaToken().WithTrailingTrivia(NewLine()));

                    nodesAndTokens.Add(en.Current.WithLeadingTrivia(leadingTrivia));
                }
            }

            return ParameterList(
                OpenParenToken().WithTrailingTrivia(NewLine()),
                SeparatedList<ParameterSyntax>(nodesAndTokens),
                parameterList.CloseParenToken);
        }

        public static Task<Document> ToMultiLineAsync(
            Document document,
            InitializerExpressionSyntax initializer,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            InitializerExpressionSyntax newNode = ToMultiLine(initializer, cancellationToken)
                .WithFormatterAnnotation();

            return document.ReplaceNodeAsync(initializer, newNode, cancellationToken);
        }

        public static InitializerExpressionSyntax ToMultiLine(InitializerExpressionSyntax initializer, CancellationToken cancellationToken)
        {
            SyntaxNode parent = initializer.Parent;

            if (parent.IsKind(SyntaxKind.ObjectCreationExpression)
                && !initializer.IsKind(SyntaxKind.CollectionInitializerExpression))
            {
                return initializer
                    .WithExpressions(
                        SeparatedList(
                            initializer.Expressions.Select(expression => expression.WithLeadingTrivia(NewLine()))));
            }
            else
            {
                SyntaxTrivia trivia = GetIndentation(initializer, cancellationToken);

                SyntaxTriviaList braceTrivia = TriviaList(NewLine(), trivia);
                SyntaxTriviaList expressionTrivia = TriviaList(NewLine(), trivia, ComputeIndentation(trivia));

                return initializer
                    .WithExpressions(
                        SeparatedList(
                            initializer.Expressions.Select(expression => expression.WithLeadingTrivia(expressionTrivia))))
                    .WithOpenBraceToken(initializer.OpenBraceToken.WithLeadingTrivia(braceTrivia))
                    .WithCloseBraceToken(initializer.CloseBraceToken.WithLeadingTrivia(braceTrivia));
            }
        }

        public static Task<Document> ToMultiLineAsync(
            Document document,
            ArgumentListSyntax argumentList,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentListSyntax newNode = ToMultiLine(argumentList);

            return document.ReplaceNodeAsync(argumentList, newNode, cancellationToken);
        }

        public static ArgumentListSyntax ToMultiLine(ArgumentListSyntax argumentList, CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxTriviaList leadingTrivia = GetIncreasedIndentation(argumentList, cancellationToken);

            var nodesAndTokens = new List<SyntaxNodeOrToken>();

            SeparatedSyntaxList<ArgumentSyntax>.Enumerator en = argumentList.Arguments.GetEnumerator();

            if (en.MoveNext())
            {
                nodesAndTokens.Add(en.Current
                    .TrimTrailingTrivia()
                    .WithLeadingTrivia(leadingTrivia));

                while (en.MoveNext())
                {
                    nodesAndTokens.Add(CommaToken().WithTrailingTrivia(NewLine()));

                    nodesAndTokens.Add(en.Current
                        .TrimTrailingTrivia()
                        .WithLeadingTrivia(leadingTrivia));
                }
            }

            return ArgumentList(
                OpenParenToken().WithTrailingTrivia(NewLine()),
                SeparatedList<ArgumentSyntax>(nodesAndTokens),
                argumentList.CloseParenToken.WithoutLeadingTrivia());
        }

        public static Task<Document> ToMultiLineAsync(
            Document document,
            MemberAccessExpressionSyntax[] expressions,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            MemberAccessExpressionSyntax expression = expressions[0];

            SyntaxTriviaList leadingTrivia = GetIncreasedIndentation(expression, cancellationToken);

            leadingTrivia = leadingTrivia.Insert(0, NewLine());

            MemberAccessExpressionSyntax newNode = expression.ReplaceNodes(expressions, (node, node2) =>
            {
                SyntaxToken operatorToken = node.OperatorToken;

                if (!operatorToken.HasLeadingTrivia)
                {
                    return node2.WithOperatorToken(operatorToken.WithLeadingTrivia(leadingTrivia));
                }
                else
                {
                    return node2;
                }
            });

            newNode = newNode.WithFormatterAnnotation();

            return document.ReplaceNodeAsync(expression, newNode, cancellationToken);
        }

        public static Task<Document> ToMultiLineAsync(
            Document document,
            AttributeArgumentListSyntax argumentList,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            AttributeArgumentListSyntax newNode = ToMultiLine(argumentList, cancellationToken);

            return document.ReplaceNodeAsync(argumentList, newNode, cancellationToken);
        }

        private static AttributeArgumentListSyntax ToMultiLine(AttributeArgumentListSyntax argumentList, CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxTriviaList leadingTrivia = GetIncreasedIndentation(argumentList, cancellationToken);

            var nodesAndTokens = new List<SyntaxNodeOrToken>();

            SeparatedSyntaxList<AttributeArgumentSyntax>.Enumerator en = argumentList.Arguments.GetEnumerator();

            if (en.MoveNext())
            {
                nodesAndTokens.Add(en.Current
                    .TrimTrailingTrivia()
                    .WithLeadingTrivia(leadingTrivia));

                while (en.MoveNext())
                {
                    nodesAndTokens.Add(CommaToken().WithTrailingTrivia(NewLine()));

                    nodesAndTokens.Add(en.Current
                        .TrimTrailingTrivia()
                        .WithLeadingTrivia(leadingTrivia));
                }
            }

            return AttributeArgumentList(
                OpenParenToken().WithTrailingTrivia(NewLine()),
                SeparatedList<AttributeArgumentSyntax>(nodesAndTokens),
                argumentList.CloseParenToken.WithoutLeadingTrivia());
        }

        public static Task<Document> ToMultiLineAsync(
            Document document,
            BinaryExpressionSyntax condition,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxTriviaList leadingTrivia = GetIncreasedIndentation(condition, cancellationToken);

            leadingTrivia = leadingTrivia.Insert(0, NewLine());

            var rewriter = new BinaryExpressionToMultiLineRewriter(leadingTrivia);

            var newCondition = (ExpressionSyntax)rewriter.Visit(condition);

            return document.ReplaceNodeAsync(condition, newCondition, cancellationToken);
        }

        public static Task<Document> ToMultiLineAsync(
            Document document,
            AccessorDeclarationSyntax accessor,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            AccessorDeclarationSyntax newAccessor = ToMultiLine(accessor);

            return document.ReplaceNodeAsync(accessor, newAccessor, cancellationToken);
        }

        private static AccessorDeclarationSyntax ToMultiLine(AccessorDeclarationSyntax accessor)
        {
            BlockSyntax body = accessor.Body;

            if (body != null)
            {
                SyntaxToken closeBrace = body.CloseBraceToken;

                if (!closeBrace.IsMissing)
                {
                    AccessorDeclarationSyntax newAccessor = accessor
                   .WithBody(
                       body.WithCloseBraceToken(
                           closeBrace.WithLeadingTrivia(
                               closeBrace.LeadingTrivia.Add(NewLine()))));

                    return newAccessor.WithFormatterAnnotation();
                }
            }

            return accessor;
        }

        private static SyntaxTrivia GetIndentation(SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxTree tree = node.SyntaxTree;

            if (tree == null)
                return default(SyntaxTrivia);

            TextSpan span = node.Span;

            int lineStartIndex = span.Start - tree.GetLineSpan(span, cancellationToken).StartLinePosition.Character;

            while (!node.FullSpan.Contains(lineStartIndex))
                node = node.Parent;

            SyntaxTriviaList trivia = node.GetLeadingTrivia();

            return (trivia.Any()) ? trivia.Last() : default(SyntaxTrivia);
        }

        private static SyntaxTriviaList GetIncreasedIndentation(SyntaxNode node, CancellationToken cancellationToken = default(CancellationToken))
        {
            SyntaxTrivia trivia = GetIndentation(node, cancellationToken);

            return IncreaseIndentation(trivia);
        }

        internal static SyntaxTriviaList IncreaseIndentation(SyntaxTrivia trivia)
        {
            return TriviaList(trivia, ComputeIndentation(trivia));
        }

        private static SyntaxTrivia ComputeIndentation(SyntaxTrivia trivia)
        {
            if (trivia.IsWhitespaceTrivia())
            {
                string s = trivia.ToString();

                int length = s.Length;

                if (length > 0)
                {
                    if (s.All(f => f == '\t'))
                    {
                        return Tab;
                    }
                    else if (s.All(f => f == ' '))
                    {
                        if (length % 4 == 0)
                            return Whitespace("    ");

                        if (length % 3 == 0)
                            return Whitespace("   ");

                        if (length % 2 == 0)
                            return Whitespace("  ");
                    }
                }
            }

            return DefaultIndentation;
        }
    }
}
