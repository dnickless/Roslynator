﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;

namespace Roslynator.CSharp.CodeFixes
{
    public abstract class BaseCodeFixProvider : CodeFixProvider
    {
        public const string EquivalenceKeySuffix = "CodeFixProvider";

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        protected virtual bool IsCodeFixEnabled(string id)
        {
            return CodeFixSettings.Current.IsCodeFixEnabled(id);
        }
    }
}