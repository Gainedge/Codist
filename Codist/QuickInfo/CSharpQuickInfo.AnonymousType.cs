﻿using System.Collections.Immutable;
using CLR;
using Codist.Controls;
using Microsoft.CodeAnalysis;
using R = Codist.Properties.Resources;

namespace Codist.QuickInfo
{
	partial class CSharpQuickInfo
	{
		static void ShowAnonymousTypeInfo(InfoContainer container, ISymbol symbol) {
			ITypeSymbol t;
			ImmutableArray<ITypeSymbol>.Builder types = null;
			switch (symbol.Kind) {
				case SymbolKind.NamedType:
					if ((t = symbol as ITypeSymbol).IsAnonymousType
						&& Config.Instance.QuickInfoOptions.MatchFlags(QuickInfoOptions.AlternativeStyle) == false) {
						Add(ref types, t);
					}
					break;
				case SymbolKind.Method:
					var m = symbol as IMethodSymbol;
					if (m.IsGenericMethod) {
						foreach (var item in m.TypeArguments) {
							if (item.IsAnonymousType) {
								Add(ref types, item);
							}
						}
					}
					else if (m.MethodKind == MethodKind.Constructor) {
						symbol = m.ContainingSymbol;
						goto case SymbolKind.NamedType;
					}
					break;
				case SymbolKind.Property:
					if ((t = symbol.ContainingType).IsAnonymousType) {
						Add(ref types, t);
					}
					break;
				default: return;
			}
			if (types != null) {
				ShowAnonymousTypes(container, types, symbol);
			}

			void ShowAnonymousTypes(InfoContainer c, ImmutableArray<ITypeSymbol>.Builder anonymousTypes, ISymbol currentSymbol) {
				const string AnonymousNumbers = "abcdefghijklmnopqrstuvwxyz";
				var d = new ThemedTipDocument().AppendTitle(IconIds.AnonymousType, R.T_AnonymousType);
				for (var i = 0; i < anonymousTypes.Count; i++) {
					var type = anonymousTypes[i];
					var content = new ThemedTipText()
						.AddSymbol(type, "'" + AnonymousNumbers[i], __SymbolFormatter)
						.Append(" is { ");
					foreach (var m in type.GetMembers()) {
						if (m.Kind != SymbolKind.Property) {
							continue;
						}
						var pt = m.GetReturnType();
						string alias = null;
						if (pt?.IsAnonymousType == true) {
							Add(ref anonymousTypes, pt);
							alias = "'" + AnonymousNumbers[anonymousTypes.IndexOf(pt)];
						}
						content.AddSymbol(pt, alias, __SymbolFormatter)
							.Append(" ")
							.AddSymbol(m, m == currentSymbol, __SymbolFormatter)
							.Append(", ");
					}
					var run = content.Inlines.LastInline as System.Windows.Documents.Run;
					if (run.Text == ", ") {
						run.Text = " }";
					}
					else {
						run.Text += "}";
					}
					d.Append(new ThemedTipParagraph(content));
				}
				c.Insert(0, d);
			}

			void Add(ref ImmutableArray<ITypeSymbol>.Builder list, ITypeSymbol type) {
				if ((list ?? (list = ImmutableArray.CreateBuilder<ITypeSymbol>())).Contains(type) == false) {
					list.Add(type);
				}
				if (type.ContainingType?.IsAnonymousType == true) {
					Add(ref list, type);
				}
			}
		}
	}
}
