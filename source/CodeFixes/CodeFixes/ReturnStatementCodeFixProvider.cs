﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Refactorings;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReturnStatementCodeFixProvider))]
    [Shared]
    public class ReturnStatementCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    CompilerDiagnosticIdentifiers.CannotReturnValueFromIterator,
                    CompilerDiagnosticIdentifiers.SinceMethodReturnsVoidReturnKeywordMustNotBeFollowedByObjectExpression,
                    CompilerDiagnosticIdentifiers.SinceMethodIsAsyncMethodThatReturnsTaskReturnKeywordMustNotBeFollowedByObjectExpression);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!Settings.IsAnyCodeFixEnabled(
                CodeFixIdentifiers.UseYieldReturnInsteadOfReturn,
                CodeFixIdentifiers.RemoveReturnKeyword,
                CodeFixIdentifiers.RemoveReturnExpression))
            {
                return;
            }

            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out ReturnStatementSyntax returnStatement))
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case CompilerDiagnosticIdentifiers.CannotReturnValueFromIterator:
                        {
                            if (!Settings.IsCodeFixEnabled(CodeFixIdentifiers.UseYieldReturnInsteadOfReturn))
                                break;

                            ExpressionSyntax expression = returnStatement.Expression;

                            if (expression != null)
                            {
                                SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                                ISymbol containingSymbol = semanticModel.GetEnclosingSymbol(returnStatement.SpanStart, context.CancellationToken);

                                if (containingSymbol?.IsKind(SymbolKind.Method) == true)
                                {
                                    var methodSymbol = (IMethodSymbol)containingSymbol;

                                    ITypeSymbol returnType = methodSymbol.ReturnType;

                                    var replacementKind = SyntaxKind.None;

                                    if (returnType.SpecialType == SpecialType.System_Collections_IEnumerable)
                                    {
                                        if (semanticModel
                                            .GetTypeSymbol(expression, context.CancellationToken)
                                            .IsIEnumerableOrConstructedFromIEnumerableOfT())
                                        {
                                            replacementKind = SyntaxKind.ForEachStatement;
                                        }
                                        else
                                        {
                                            replacementKind = SyntaxKind.YieldReturnStatement;
                                        }
                                    }
                                    else if (returnType.IsNamedType())
                                    {
                                        var namedTypeSymbol = (INamedTypeSymbol)returnType;

                                        if (namedTypeSymbol.IsConstructedFromIEnumerableOfT())
                                        {
                                            if (semanticModel.IsImplicitConversion(expression, namedTypeSymbol.TypeArguments[0]))
                                            {
                                                replacementKind = SyntaxKind.YieldReturnStatement;
                                            }
                                            else
                                            {
                                                replacementKind = SyntaxKind.ForEachStatement;
                                            }
                                        }
                                    }

                                    if (replacementKind == SyntaxKind.YieldReturnStatement
                                        || (replacementKind == SyntaxKind.ForEachStatement && !returnStatement.SpanContainsDirectives()))
                                    {
                                        CodeAction codeAction = CodeAction.Create(
                                            "Use yield return instead of return",
                                            cancellationToken => UseYieldReturnInsteadOfReturnRefactoring.RefactorAsync(context.Document, returnStatement, replacementKind, semanticModel, cancellationToken),
                                            GetEquivalenceKey(diagnostic));

                                        context.RegisterCodeFix(codeAction, diagnostic);
                                    }
                                }
                            }

                            break;
                        }
                    case CompilerDiagnosticIdentifiers.SinceMethodReturnsVoidReturnKeywordMustNotBeFollowedByObjectExpression:
                    case CompilerDiagnosticIdentifiers.SinceMethodIsAsyncMethodThatReturnsTaskReturnKeywordMustNotBeFollowedByObjectExpression:
                        {
                            if (Settings.IsCodeFixEnabled(CodeFixIdentifiers.RemoveReturnExpression))
                            {
                                SemanticModel semanticModel = await context.GetSemanticModelAsync().ConfigureAwait(false);

                                ISymbol symbol = semanticModel.GetEnclosingSymbol(returnStatement.SpanStart, context.CancellationToken);

                                if (symbol?.IsMethod() == true)
                                {
                                    var methodSymbol = (IMethodSymbol)symbol;

                                    if (methodSymbol.ReturnsVoid
                                        || methodSymbol.ReturnType.Equals(semanticModel.GetTypeByMetadataName(MetadataNames.System_Threading_Tasks_Task)))
                                    {
                                        CodeAction codeAction = CodeAction.Create(
                                            "Remove return expression",
                                            cancellationToken =>
                                            {
                                                ReturnStatementSyntax newNode = returnStatement
                                                    .WithExpression(null)
                                                    .WithFormatterAnnotation();

                                                return context.Document.ReplaceNodeAsync(returnStatement, newNode, cancellationToken);
                                            },
                                            GetEquivalenceKey(diagnostic, CodeFixIdentifiers.RemoveReturnExpression));

                                        context.RegisterCodeFix(codeAction, diagnostic);
                                    }
                                }
                            }

                            if (Settings.IsCodeFixEnabled(CodeFixIdentifiers.RemoveReturnKeyword))
                            {
                                ExpressionSyntax expression = returnStatement.Expression;

                                if (expression.IsKind(
                                        SyntaxKind.InvocationExpression,
                                        SyntaxKind.ObjectCreationExpression,
                                        SyntaxKind.PreDecrementExpression,
                                        SyntaxKind.PreIncrementExpression,
                                        SyntaxKind.PostDecrementExpression,
                                        SyntaxKind.PostIncrementExpression)
                                    || expression is AssignmentExpressionSyntax)
                                {
                                    CodeAction codeAction = CodeAction.Create(
                                        "Remove 'return'",
                                        cancellationToken =>
                                        {
                                            SyntaxTriviaList leadingTrivia = returnStatement
                                                .GetLeadingTrivia()
                                                .AddRange(returnStatement.ReturnKeyword.TrailingTrivia.EmptyIfWhitespace())
                                                .AddRange(expression.GetLeadingTrivia().EmptyIfWhitespace());

                                            ExpressionStatementSyntax newNode = SyntaxFactory.ExpressionStatement(
                                                expression.WithLeadingTrivia(leadingTrivia),
                                                returnStatement.SemicolonToken);

                                            return context.Document.ReplaceNodeAsync(returnStatement, newNode, cancellationToken);
                                        },
                                        GetEquivalenceKey(diagnostic, CodeFixIdentifiers.RemoveReturnKeyword));

                                    context.RegisterCodeFix(codeAction, diagnostic);
                                }
                            }

                            break;
                        }
                }
            }
        }
    }
}
