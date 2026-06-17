using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Codist;

static class ValueHelper
{
	public static IImmutableSet<T> MakeImmutableSet<T>(this IEnumerable<T> values) {
		return values is null ? null : (IImmutableSet<T>)ImmutableHashSet.CreateRange(values);
	}
	public static string ToText(this int value) {
		return value.ToString(CultureInfo.InvariantCulture);
	}
	public static bool Contains(this string text, char character) {
		return text.IndexOf(character) >= 0;
	}
	public static bool ContainsWords(this string text, string[] keywords) {
		if (keywords.Length > 0) {
			int i = 0;
			var name = text;
			var comp = Char.IsUpper(keywords[0][0]) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			foreach (var keyword in keywords) {
				if ((i = name.IndexOf(keyword, i, comp)) < 0) {
					return false;
				}
			}
		}
		return true;
	}
	public static bool IsProgrammaticSymbol(this string text) {
		foreach (var c in text) {
			if ((uint)((c - 65) & -33) <= 25u || (uint)(c - 48) <= 9u || c == '_') {
				// is alpha or numeric or _
				continue;
			}
			return false;
		}
		return true;
	}
	public static bool IsProgrammaticChar(this char c) {
		return (uint)((c - 65) & -33) <= 25u || (uint)(c - 48) <= 9u || c == '_';
	}
}
