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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBracesToDeclarationOrLabeledStatementCodeFixProvider))]
    [Shared]
    public class AddBracesToDeclarationOrLabeledStatementCodeFixProvider : BaseCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CompilerDiagnosticIdentifiers.EmbeddedStatementCannotBeDeclarationOrLabeledStatement); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!Settings.IsCodeFixEnabled(CodeFixIdentifiers.AddBracesToDeclarationOrLabeledStatement))
                return;

            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            StatementSyntax statement = root
                .FindNode(context.Span, getInnermostNodeForTie: true)?
                .FirstAncestorOrSelf<StatementSyntax>();

            Debug.Assert(statement != null, $"{nameof(statement)} is null");

            if (statement == null)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                switch (diagnostic.Id)
                {
                    case CompilerDiagnosticIdentifiers.EmbeddedStatementCannotBeDeclarationOrLabeledStatement:
                        {
                            CodeAction codeAction = CodeAction.Create(
                                "Add braces",
                                cancellationToken =>
                                {
                                    BlockSyntax block = SyntaxFactory.Block(statement).WithFormatterAnnotation();

                                    return context.Document.ReplaceNodeAsync(statement, block, context.CancellationToken);
                                },
                                GetEquivalenceKey(diagnostic));

                            context.RegisterCodeFix(codeAction, diagnostic);
                            break;
                        }
                }
            }
        }
    }
}
