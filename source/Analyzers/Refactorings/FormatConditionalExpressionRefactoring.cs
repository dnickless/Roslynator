﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslynator.CSharp.Refactorings
{
    internal static class FormatConditionalExpressionRefactoring
    {
        public static void AnalyzeConditionalExpression(SyntaxNodeAnalysisContext context)
        {
            var conditionalExpression = (ConditionalExpressionSyntax)context.Node;

            if (!conditionalExpression.ContainsDiagnostics
                && !conditionalExpression.SpanContainsDirectives())
            {
                if (IsFixable(conditionalExpression.Condition, conditionalExpression.QuestionToken)
                    || IsFixable(conditionalExpression.WhenTrue, conditionalExpression.ColonToken))
                {
                    context.ReportDiagnostic(
                        DiagnosticDescriptors.FormatConditionalExpression,
                        conditionalExpression);
                }
            }
        }

        private static bool IsFixable(ExpressionSyntax expression, SyntaxToken token)
        {
            SyntaxTriviaList expressionTrailing = expression.GetTrailingTrivia();

            if (expressionTrailing.IsEmptyOrWhitespace())
            {
                SyntaxTriviaList tokenLeading = token.LeadingTrivia;

                if (tokenLeading.IsEmptyOrWhitespace())
                {
                    SyntaxTriviaList tokenTrailing = token.TrailingTrivia;

                    int count = tokenTrailing.Count;

                    if (count == 1)
                    {
                        if (tokenTrailing[0].IsEndOfLineTrivia())
                            return true;
                    }
                    else if (count > 1)
                    {
                        for (int i = 0; i < count - 1; i++)
                        {
                            if (!tokenTrailing[i].IsWhitespaceTrivia())
                                return false;
                        }

                        if (tokenTrailing.Last().IsEndOfLineTrivia())
                            return true;
                    }
                }
            }

            return false;
        }

        public static Task<Document> RefactorAsync(
            Document document,
            ConditionalExpressionSyntax conditionalExpression,
            CancellationToken cancellationToken)
        {
            ExpressionSyntax condition = conditionalExpression.Condition;
            ExpressionSyntax whenTrue = conditionalExpression.WhenTrue;
            ExpressionSyntax whenFalse = conditionalExpression.WhenFalse;
            SyntaxToken questionToken = conditionalExpression.QuestionToken;
            SyntaxToken colonToken = conditionalExpression.ColonToken;

            var writer = new NodeWriter(conditionalExpression);

            writer.WriteLeadingTrivia();
            writer.WriteSpan(condition);

            Write(condition, whenTrue, questionToken, "? ", writer);

            Write(whenTrue, whenFalse, colonToken, ": ", writer);

            writer.WriteTrailingTrivia();

            ExpressionSyntax newNode = SyntaxFactory.ParseExpression(writer.ToString());

            return document.ReplaceNodeAsync(conditionalExpression, newNode, cancellationToken);
        }

        private static void Write(
            ExpressionSyntax expression,
            ExpressionSyntax nextExpression,
            SyntaxToken token,
            string newText,
            NodeWriter writer)
        {
            if (IsFixable(expression, token))
            {
                if (!expression.GetTrailingTrivia().IsEmptyOrWhitespace()
                    || !token.LeadingTrivia.IsEmptyOrWhitespace())
                {
                    writer.WriteTrailingTrivia(expression);
                    writer.WriteLeadingTrivia(token);
                }

                writer.WriteTrailingTrivia(token);
                writer.WriteLeadingTrivia(nextExpression);
                writer.Write(newText);
                writer.WriteSpan(nextExpression);
            }
            else
            {
                writer.WriteTrailingTrivia(expression);
                writer.WriteFullSpan(token);
                writer.WriteLeadingTriviaAndSpan(nextExpression);
            }
        }
    }
}
