﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslynator.CSharp.Refactorings;

namespace Roslynator.CSharp.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WhileStatementCodeFixProvider))]
    [Shared]
    public class WhileStatementCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIdentifiers.AvoidUsageOfWhileStatementToCreateInfiniteLoop); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out WhileStatementSyntax whileStatement))
                return;

            CodeAction codeAction = CodeAction.Create(
                "Use for to create an infinite loop",
                cancellationToken =>
                {
                    return AvoidUsageOfWhileStatementToCreateInfiniteLoopRefactoring.RefactorAsync(
                        context.Document,
                        whileStatement,
                        cancellationToken);
                },
                DiagnosticIdentifiers.AvoidUsageOfWhileStatementToCreateInfiniteLoop + EquivalenceKeySuffix);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }
}
