﻿using System;

namespace Codist.Refactorings
{
	static class All
	{
		internal static readonly IRefactoring[] Refactorings = new IRefactoring[] {
			ReplaceNode.ConcatToInterpolatedString,
			ReplaceToken.InvertOperator,
			ReplaceNode.MergeToConditional,
			ReplaceNode.WrapInElse,
			ReplaceNode.MultiLineExpression,
			ReplaceNode.MultiLineList,
			ReplaceNode.MultiLineMemberAccess,
			ReplaceNode.ConditionalToIf,
			ReplaceNode.IfToConditional,
			ReplaceNode.MergeCondition,
			ReplaceNode.SwapConditionResults,
			ReplaceNode.InlineVariable,
			ReplaceNode.While,
			ReplaceNode.AsToCast,
			ReplaceText.SealClass,
			ReplaceNode.DuplicateMethodDeclaration,
			ReplaceText.MakePublic,
			ReplaceText.MakeProtected,
			ReplaceText.MakeInternal,
			ReplaceText.MakePrivate,
			ReplaceNode.SwapOperands,
			ReplaceNode.NestCondition,
			ReplaceNode.AddBraces,
			ReplaceNode.WrapInUsing,
			ReplaceNode.WrapInIf,
			ReplaceNode.WrapInTryCatch,
			ReplaceNode.WrapInTryFinally,
			ReplaceToken.UseStaticDefault,
			ReplaceToken.UseExplicitType,
			ReplaceNode.DeleteCondition,
			ReplaceNode.RemoveContainingStatement,
			ReplaceText.CommentToRegion,
			ReplaceText.WrapInRegion,
			ReplaceText.WrapInIf,
		};
	}
}
