﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Roslynator.CSharp.CodeFixes.Test
{
    internal static class CS1105_ExtensionMethodMustBeStatic
    {
        private void Foo(this string value)
        {
        }    
    }
}
