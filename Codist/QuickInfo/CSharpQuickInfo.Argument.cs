﻿using System;
using System.Text;
using CLR;
using Codist.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using R = Codist.Properties.Resources;

namespace Codist.QuickInfo
{
	partial class CSharpQuickInfo
	{
		static void ShowArgumentInfo(Context ctx) {
			var argument = ctx.node;
			if (argument is null) {
				return;
			}
			if (argument.IsKind(SyntaxKind.NullLiteralExpression)) {
				argument = argument.Parent;
			}
			int depth = 0;
			SyntaxNode parent;
			do {
				if (argument.IsAnyKind(SyntaxKind.Argument, SyntaxKind.AttributeArgument)) {
					ShowArgumentInfo(ctx, argument);
					return;
				}
				parent = argument.Parent;
				if (parent is null) {
					return;
				}
				var parentKind = parent.Kind();
				switch (parentKind) {
					case SyntaxKind.SimpleLambdaExpression:
					case SyntaxKind.ParenthesizedLambdaExpression:
					case SyntaxKind.CompilationUnit:
						return;
					case SyntaxKind.ArrayInitializerExpression:
					case SyntaxKind.CollectionInitializerExpression:
					case SyntaxKind.ObjectInitializerExpression:
					case SyntaxKind.ComplexElementInitializerExpression:
					case CodeAnalysisHelper.CollectionExpression:
						ShowLocationOfInitializerExpression(ctx.Container, argument, parent);
						if (parent.IsKind(CodeAnalysisHelper.CollectionExpression) == false) {
							var initSymbol = ctx.semanticModel.GetCollectionInitializerSymbolInfo((ExpressionSyntax)argument, ctx.cancellationToken).Symbol;
							if (initSymbol != null) {
								ctx.Container.Add(new ThemedTipText()
									.SetGlyph(initSymbol.GetImageId())
									.AddSymbol(initSymbol.ContainingType, false, __SymbolFormatter)
									.Append(".".Render(__SymbolFormatter.PlainText))
									.AddSymbol(initSymbol, true, __SymbolFormatter)
									.AddParameters(initSymbol.GetParameters(), __SymbolFormatter));
							}
						}
						return;
					default:
						if (parentKind.IsDeclaration()) {
							return;
						}
						break;
				}
			} while ((argument = parent) != null && ++depth < 4);
		}

		static void ShowLocationOfInitializerExpression(InfoContainer qiContent, SyntaxNode argument, SyntaxNode n) {
			int argIndex;
			if (n is InitializerExpressionSyntax ie) {
				argIndex = ie.Expressions.IndexOf((ExpressionSyntax)argument);
			}
			else if (n.IsKind(CodeAnalysisHelper.CollectionExpression)) {
				var elements = ((ExpressionSyntax)n).GetCollectionExpressionElements();
				argIndex = 0;
				var argSpan = argument.Span;
				foreach (var item in elements) {
					if (item.FullSpan.Contains(argSpan)) {
						goto RETURN;
					}
					++argIndex;
				}
				return;
			}
			else {
				return;
			}
		RETURN:
			qiContent.Add(new ThemedTipText(R.T_ExpressionNOfInitializer.Replace("<N>", (++argIndex).ToString())).SetGlyph(IconIds.Argument));
		}

		static void ShowArgumentInfo(Context ctx, SyntaxNode argument) {
			var qiContent = ctx.Container;
			var semanticModel = ctx.semanticModel;
			var argList = argument.Parent;
			SeparatedSyntaxList<ArgumentSyntax> arguments;
			int argIndex, argCount;
			string argName;
			switch (argList.Kind()) {
				case SyntaxKind.ArgumentList:
					arguments = ((ArgumentListSyntax)argList).Arguments;
					argIndex = arguments.IndexOf(argument as ArgumentSyntax);
					argCount = arguments.Count;
					argName = ((ArgumentSyntax)argument).NameColon?.Name.ToString();
					break;
				case SyntaxKind.TupleExpression:
					arguments = ((TupleExpressionSyntax)argList).Arguments;
					argIndex = arguments.IndexOf(argument as ArgumentSyntax);
					argCount = arguments.Count;
					argName = null;
					break;
				//case SyntaxKind.BracketedArgumentList: arguments = (argList as BracketedArgumentListSyntax).Arguments; break;
				case SyntaxKind.AttributeArgumentList:
					var aa = ((AttributeArgumentListSyntax)argument.Parent).Arguments;
					argIndex = aa.IndexOf((AttributeArgumentSyntax)argument);
					argCount = aa.Count;
					argName = ((AttributeArgumentSyntax)argument).NameColon?.Name.ToString();
					break;
				default:
					return;
			}
			if (argIndex == -1) {
				return;
			}
			var symbol = ctx.semanticModel.GetSymbolInfo(argList.Parent, ctx.cancellationToken);
			if (symbol.Symbol != null) {
				IMethodSymbol m;
				switch (symbol.Symbol.Kind) {
					case SymbolKind.Method: m = symbol.Symbol as IMethodSymbol; break;
					case CodeAnalysisHelper.FunctionPointerType: m = (symbol.Symbol as ITypeSymbol).GetFunctionPointerTypeSignature(); break;
					default: return;
				}
				if (m == null) { // in a very rare case m can be null
					return;
				}
				var om = m.OriginalDefinition;
				IParameterSymbol p = null;
				if (argName != null) {
					var mp = om.Parameters;
					for (int i = 0; i < mp.Length; i++) {
						if (mp[i].Name == argName) {
							argIndex = i;
							p = mp[i];
							break;
						}
					}
				}
				else if (argIndex != -1) {
					var mp = om.Parameters;
					if (argIndex < mp.Length) {
						argName = (p = mp[argIndex]).Name;
					}
					else if (mp.Length > 0 && mp[mp.Length - 1].IsParams) {
						argIndex = mp.Length - 1;
						argName = (p = mp[argIndex]).Name;
					}
				}
				var doc = argName != null ? new XmlDoc(om.MethodKind == MethodKind.DelegateInvoke ? om.ContainingSymbol : om, semanticModel.Compilation) : null;
				var paramDoc = doc?.GetParameter(argName);
				var content = new ThemedTipText(R.T_Argument, true)
					.Append(R.T_ArgumentOf)
					.AddSymbol(om.ReturnType, om.MethodKind == MethodKind.Constructor ? "new" : null, __SymbolFormatter)
					.Append(" ")
					.AddSymbol(om.MethodKind != MethodKind.DelegateInvoke ? om : (ISymbol)om.ContainingType, true, __SymbolFormatter)
					.AddParameters(om.Parameters, __SymbolFormatter, argIndex);
				var info = new ThemedTipDocument().Append(new ThemedTipParagraph(IconIds.Argument, content));
				if (paramDoc != null) {
					content.Append("\n" + argName, true, false, __SymbolFormatter.Parameter)
						.Append(": ")
						.AddXmlDoc(paramDoc, new XmlDocRenderer(semanticModel.Compilation, __SymbolFormatter));
				}
				if (m.IsGenericMethod) {
					for (int i = 0; i < m.TypeArguments.Length; i++) {
						content.Append("\n");
						__SymbolFormatter.ShowTypeArgumentInfo(m.TypeParameters[i], m.TypeArguments[i], content);
						var typeParamDoc = doc.GetTypeParameter(m.TypeParameters[i].Name);
						if (typeParamDoc != null) {
							content.Append(": ")
								.AddXmlDoc(typeParamDoc, new XmlDocRenderer(semanticModel.Compilation, __SymbolFormatter));
						}
					}
				}
				if (p?.Type.TypeKind == TypeKind.Delegate) {
					var invoke = ((INamedTypeSymbol)p.Type).DelegateInvokeMethod;
					info.Append(new ThemedTipParagraph(IconIds.Delegate,
						new ThemedTipText(R.T_DelegateSignature, true).Append(": ")
							.AddSymbol(invoke.ReturnType, false, __SymbolFormatter)
							.Append(" ").Append(p.Name, true, false, __SymbolFormatter.Parameter)
							.AddParameters(invoke.Parameters, __SymbolFormatter)
						));
				}
				foreach (var item in content.Inlines) {
					if (item.Foreground == null) {
						item.Foreground = ThemeHelper.ToolTipTextBrush;
					}
				}
				if (p != null && Config.Instance.QuickInfoOptions.MatchFlags(QuickInfoOptions.Attributes)) {
					var attrs = p.GetAttributes();
					if (attrs.Length > 0) {
						var para = new ThemedTipParagraph(
							IconIds.Attribute,
							new ThemedTipText().Append(R.T_AttributeOf).Append(p.Name, true, false, __SymbolFormatter.Parameter).Append(":")
						);
						foreach (var attr in attrs) {
							__SymbolFormatter.Format(para.Content.AppendLine().Inlines, attr, 0);
						}
						info.Append(para);
					}
				}
				qiContent.Add(info);
			}
			else if (symbol.CandidateSymbols.Length > 0) {
				var info = new ThemedTipDocument();
				info.Append(new ThemedTipParagraph(IconIds.ParameterCandidate, new ThemedTipText(R.T_MaybeArgument, true).Append(R.T_MaybeArgumentOf)));
				foreach (var candidate in symbol.CandidateSymbols) {
					info.Append(
						new ThemedTipParagraph(
							candidate.GetImageId(),
							new ThemedTipText().AddSymbolDisplayParts(
								candidate.ToDisplayParts(CodeAnalysisHelper.QuickInfoSymbolDisplayFormat),
								__SymbolFormatter,
								argName == null ? argIndex : Int32.MinValue)
						)
					);
				}
				qiContent.Add(info);
			}
			else if (argList.Parent.IsKind(SyntaxKind.InvocationExpression)) {
				var methodName = ((InvocationExpressionSyntax)argList.Parent).Expression.ToString();
				if (methodName == "nameof" && argCount == 1) {
					return;
				}
				qiContent.Add(new ThemedTipText(R.T_ArgumentNOf.Replace("<N>", (++argIndex).ToString())).Append(methodName, true));
			}
			else if (argList.IsKind(SyntaxKind.TupleExpression) && argIndex >= 0) {
				if (semanticModel.GetTypeInfo(argList).Type is INamedTypeSymbol type) {
					var tuples = type.TupleElements;
					if (tuples.Length != 0) {
						ctx.symbol = tuples[argIndex];
					}
				}
				qiContent.Add(new ThemedTipText(R.T_TupleElementN.Replace("<N>", (argIndex + 1).ToString())).SetGlyph(IconIds.ValueType));
			}
			else {
				qiContent.Add(new ThemedTipText(R.T_ArgumentN.Replace("<N>", (++argIndex).ToString())).SetGlyph(IconIds.Argument));
			}
		}
	}
}
