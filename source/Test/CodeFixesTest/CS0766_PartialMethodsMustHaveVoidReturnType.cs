﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Roslynator.CSharp.CodeFixes.Test
{
    internal static partial class CS0766_PartialMethodsMustHaveVoidReturnType
    {
        private partial class Foo
        {
            partial object Bar()
            {
            }

            partial object Bar();

            partial object Bar2()
            {
                return null;
            }
        }

        private partial class Foo2
        {
            partial object Bar()
            {
            }
        }

        //n

        private partial class Foo3
        {
            partial object Bar()
            {
                return null;
            }
        }

    }
}
