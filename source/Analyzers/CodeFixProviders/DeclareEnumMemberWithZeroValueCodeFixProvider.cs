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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DeclareEnumMemberWithZeroValueCodeFixProvider))]
    [Shared]
    public class DeclareEnumMemberWithZeroValueCodeFixProvider : AbstractCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(DiagnosticIdentifiers.DeclareEnumMemberWithZeroValue); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.GetSyntaxRootAsync().ConfigureAwait(false);

            if (!TryFindFirstAncestorOrSelf(root, context.Span, out EnumDeclarationSyntax enumDeclaration))
                return;

            CodeAction codeAction = CodeAction.Create(
                "Declare enum member with zero value",
                cancellationToken => DeclareEnumMemberWithZeroValueRefactoring.RefactorAsync(context.Document, enumDeclaration, cancellationToken),
                DiagnosticIdentifiers.DeclareEnumMemberWithZeroValue + EquivalenceKeySuffix);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }
}
