﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace Roslynator.CSharp.Analyzers.Test
{
    public static class FormatEachStatementOnSeparateLine
    {
        private static void Foo()
        {
            Foo(); Foo();

            {
                Foo(); Foo();
            }

            { Foo(); Foo(); }

            var options = RegexOptions.None;
            switch (options)
            {
                case RegexOptions.CultureInvariant:
                    {
                        Foo(); Foo();
                        break;
                    }
                default:
                    Foo(); Foo();
                    break;
            }

            {
                Foo(); ;
            }

            {
                {
                }
            }
        }
    }
}
