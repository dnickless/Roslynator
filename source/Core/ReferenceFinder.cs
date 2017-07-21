// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Roslynator
{
    internal static class ReferenceFinder
    {
        public static async Task<ImmutableArray<DocumentNodeInfo>> FindReferences(
            ISymbol symbol,
            Solution solution,
            bool allowCandidate = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            List<DocumentNodeInfo> infos = null;

            foreach (ReferencedSymbol referencedSymbol in await SymbolFinder.FindReferencesAsync(
                symbol,
                solution,
                cancellationToken).ConfigureAwait(false))
            {
                foreach (IGrouping<Document, ReferenceLocation> grouping in referencedSymbol.Locations.GroupBy(f => f.Document))
                {
                    Document document = grouping.Key;

                    List<SyntaxNode> nodes = null;

                    SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    foreach (ReferenceLocation referenceLocation in grouping)
                    {
                        if (!referenceLocation.IsImplicit
                            && (allowCandidate || !referenceLocation.IsCandidateLocation))
                        {
                            Location location = referenceLocation.Location;

                            if (location.IsInSource)
                            {
                                SyntaxNode node = root.FindNode(location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);

                                Debug.Assert(node != null);

                                (nodes ?? (nodes = new List<SyntaxNode>())).Add(node);
                            }
                        }
                    }

                    if (nodes != null)
                    {
                        var info = new DocumentNodeInfo(document, root, nodes.ToImmutableArray());

                        (infos ?? (infos = new List<DocumentNodeInfo>())).Add(info);
                    }
                }
            }

            if (infos != null)
            {
                return ImmutableArray.CreateRange(infos);
            }
            else
            {
                return ImmutableArray<DocumentNodeInfo>.Empty;
            }
        }
    }
}
