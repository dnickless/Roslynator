﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AssignmentExpressionCodeFixProvider))]
    [Shared]
    public class AssignmentExpressionCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CompilerDiagnosticIdentifiers.AssignmentMadeToSameVariable); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!Settings.IsCodeFixEnabled(CodeFixIdentifiers.RemoveRedundantAssignment))
                return;

            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            AssignmentExpressionSyntax assignmentExpression = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<AssignmentExpressionSyntax>();

            Debug.Assert(assignmentExpression != null, $"{nameof(assignmentExpression)} is null");

            if (assignmentExpression == null)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case CompilerDiagnosticIdentifiers.AssignmentMadeToSameVariable:
                        {
                            if (!Settings.IsCodeFixEnabled(CodeFixIdentifiers.RemoveRedundantAssignment))
                                break;

                            if (assignmentExpression.IsParentKind(SyntaxKind.ExpressionStatement))
                            {
                                var statement = (ExpressionStatementSyntax)assignmentExpression.Parent;

                                CodeAction codeAction = CodeAction.Create(
                                    "Remove assignment",
                                    cancellationToken => context.Document.RemoveStatementAsync(statement, cancellationToken),
                                    GetEquivalenceKey(diagnostic));

                                context.RegisterCodeFix(codeAction, diagnostic);
                            }

                            break;
                        }
                }
            }
        }
    }
}
