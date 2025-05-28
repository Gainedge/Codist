﻿using System;
using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml.Linq;
using CLR;
using Codist.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace Codist.QuickInfo
{
	abstract class InfoBlock
	{
		public abstract UIElement ToUI();
	}

	sealed class GeneralInfoBlock : InfoBlock
	{
		Chain<BlockItem> _Items;

		public BlockTitle Title { get; set; }

		public GeneralInfoBlock() { }

		public GeneralInfoBlock(int iconId, string text) {
			Title = new BlockTitle(iconId, text);
		}

		public GeneralInfoBlock(BlockItem item) {
			_Items = new Chain<BlockItem>(item);
		}

		public bool HasItem => _Items?.IsEmpty == false;
		public Chain<BlockItem> Items => _Items ?? (_Items = new Chain<BlockItem>());

		public void Add(BlockItem item) {
			if (_Items == null) {
				_Items = new Chain<BlockItem>(item);
			}
			else {
				_Items.Add(item);
			}
		}

		public override UIElement ToUI() {
			if (Title == null && _Items == null) {
				return null;
			}
			var doc = new ThemedTipDocument();
			if (Title != null) {
				doc.AppendTitle(Title.IconId, Title.Text);
			}
			if (_Items != null) {
				foreach (var item in _Items) {
					doc.AppendParagraph(item.IconId, item.ToTextBlock());
				}
			}
			return doc;
		}
	}

	sealed class BlockTitle
	{
		public BlockTitle(int iconId, string text) {
			IconId = iconId;
			Text = text;
		}

		public int IconId { get; }
		public string Text { get; }
	}

	sealed class BlockItem : InfoBlock
	{
		public BlockItem() {
			Segments = new Chain<Segment>();
		}
		public BlockItem(int iconId) : this() {
			IconId = iconId;
		}
		public BlockItem(int iconId, string text) {
			IconId = iconId;
			Segments = new Chain<Segment>(new TextSegment(text));
		}
		public BlockItem(int iconId, string text, bool bold) {
			IconId = iconId;
			Segments = new Chain<Segment>(new TextSegment(text, bold ? SegmentStyle.Bold : SegmentStyle.Default));
		}

		public int IconId { get; set; }
		public Chain<Segment> Segments { get; }

		public BlockItem SetGlyph(int iconId) {
			IconId = iconId;
			return this;
		}

		public BlockItem Append(string text) {
			Segments.Add(new TextSegment(text));
			return this;
		}

		public BlockItem Append(string text, bool bold) {
			Segments.Add(new TextSegment(text, bold ? SegmentStyle.Bold : SegmentStyle.Default));
			return this;
		}

		public BlockItem Append(string text, Brush foreground) {
			Segments.Add(new TextSegment(text) { Foreground = foreground });
			return this;
		}

		public BlockItem Append(string text, bool bold, Brush foreground) {
			Segments.Add(new TextSegment(text, bold ? SegmentStyle.Bold : SegmentStyle.Default) { Foreground = foreground });
			return this;
		}

		public BlockItem Append(Segment segment) {
			Segments.Add(segment);
			return this;
		}

		public BlockItem AppendIcon(int imageId) {
			Segments.Add(new IconSegment(imageId));
			return this;
		}

		public BlockItem AppendLine() {
			Segments.Add(new LineBreakSegment());
			return this;
		}

		public BlockItem AddSymbol(ISymbol symbol) {
			Segments.Add(new SymbolSegment(symbol));
			return this;
		}

		public BlockItem AddSymbol(ISymbol symbol, bool bold) {
			Segments.Add(new SymbolSegment(symbol, bold));
			return this;
		}

		public BlockItem AddSymbol(ISymbol symbol, bool bold, SymbolFormatter formatter) {
			Segments.Add(new SymbolSegment(symbol, bold) { Formatter = formatter });
			return this;
		}

		public BlockItem AddSymbol(ISymbol symbol, string alias, Brush foreground) {
			Segments.Add(new SymbolSegment(symbol) { Alias = alias, Foreground = foreground });
			return this;
		}

		public BlockItem AddSymbol(ISymbol symbol, string alias, SymbolFormatter formatter) {
			Segments.Add(new SymbolSegment(symbol) { Alias = alias, Formatter = formatter });
			return this;
		}

		public BlockItem AddSymbol(ISymbol symbol, string alias) {
			Segments.Add(new SymbolSegment(symbol) { Alias = alias });
			return this;
		}

		public BlockItem AddSymbolDisplayParts(ImmutableArray<SymbolDisplayPart> parts, SymbolFormatter formatter) {
			Segments.Add(new SymbolDisplayPartsSegment(parts, formatter));
			return this;
		}

		public BlockItem AddSymbolDisplayParts(ImmutableArray<SymbolDisplayPart> parts, SymbolFormatter formatter, int argIndex) {
			Segments.Add(new SymbolDisplayPartsSegment(parts, formatter) { ArgumentIndex = argIndex });
			return this;
		}

		public BlockItem AddParameters(ImmutableArray<IParameterSymbol> parameters) {
			Segments.Add(new ParameterListSegment(parameters));
			return this;
		}

		public BlockItem AddParameters(ImmutableArray<IParameterSymbol> parameters, int argIndex) {
			Segments.Add(new ParameterListSegment(parameters) { ArgumentIndex = argIndex });
			return this;
		}

		public BlockItem AddTypeParameterInfo(ITypeParameterSymbol typeParameter, ITypeSymbol argumentType) {
			Segments.Add(new TypeParameterInfoSegment(typeParameter, argumentType));
			return this;
		}

		public BlockItem AddXmlDoc(XElement xmlDoc, Compilation compilation) {
			Segments.Add(new XmlDocSegment(xmlDoc, compilation));
			return this;
		}

		public BlockItem Append(SnapshotSpan snapshotSpan, string text) {
			Segments.Add(new SnapshotSpanSegment(snapshotSpan, text));
			return this;
		}

		public BlockItem Append(SyntaxNodeOrToken syntax, ITextSnapshot textSnapshot, string text) {
			Segments.Add(new SyntaxSegment(syntax, textSnapshot, text));
			return this;
		}

		public GeneralInfoBlock MakeBlock() {
			return new GeneralInfoBlock(this);
		}

		public TextBlock ToTextBlock() {
			var text = new ThemedTipText();
			var inlines = text.Inlines;
			foreach (var item in Segments) {
				item.ToUI(inlines);
			}
			return text;
		}

		public override UIElement ToUI() {
			return ToTextBlock();
		}
	}

	[Flags]
	enum SegmentStyle
	{
		Default,
		Bold = 1,
		Italic = 1 << 1,
		Underline = 1 << 2,
	}

	abstract class Segment
	{
		public abstract void ToUI(InlineCollection inlines);
	}

	class TextSegment : Segment
	{
		public TextSegment() { }

		public TextSegment(string text) {
			Text = text;
		}
		public TextSegment(string text, SegmentStyle style) {
			Text = text;
			Style = style;
		}

		public string Text { get; set; }
		public SegmentStyle Style { get; set; }
		public Brush Foreground { get; set; }

		public override void ToUI(InlineCollection inlines) {
			var run = new Run(Text);
			ApplySegmentStyle(run);
			inlines.Add(run);
		}

		protected void ApplySegmentStyle(Inline inline) {
			if (Style.MatchFlags(SegmentStyle.Bold)) {
				inline.FontWeight = FontWeights.Bold;
			}
			if (Style.MatchFlags(SegmentStyle.Italic)) {
				inline.FontStyle = FontStyles.Italic;
			}
			if (Style.MatchFlags(SegmentStyle.Underline)) {
				inline.TextDecorations.Add(TextDecorations.Underline);
			}
			inline.Foreground = Foreground ?? SymbolFormatter.Instance.PlainText;
		}
	}

	sealed class SymbolSegment : Segment
	{
		public SymbolSegment(ISymbol symbol) {
			Symbol = symbol;
		}
		public SymbolSegment(ISymbol symbol, bool bold) {
			Symbol = symbol;
			Bold = bold;
		}

		public ISymbol Symbol { get; }
		public bool Bold { get; }
		public string Alias { get; set; }
		public SymbolFormatter Formatter { get; set; }
		public Brush Foreground { get; set; }

		public override void ToUI(InlineCollection inlines) {
			if (Foreground != null) {
				inlines.Add(Symbol.Render(Alias, Bold, Foreground));
			}
			else {
				(Formatter ?? SymbolFormatter.Instance).Format(inlines, Symbol, Alias, Bold);
			}
		}
	}

	sealed class SymbolDisplayPartsSegment : Segment
	{
		public SymbolDisplayPartsSegment(ImmutableArray<SymbolDisplayPart> displayParts, SymbolFormatter formatter) {
			DisplayParts = displayParts;
			Formatter = formatter;
		}

		public ImmutableArray<SymbolDisplayPart> DisplayParts { get; }
		public SymbolFormatter Formatter { get; }
		public int ArgumentIndex { get; set; } = -1;

		public override void ToUI(InlineCollection inlines) {
			Formatter.Format(inlines, DisplayParts, ArgumentIndex);
		}
	}

	sealed class ParameterListSegment : Segment
	{
		public ParameterListSegment(ImmutableArray<IParameterSymbol> parameters) {
			Parameters = parameters;
		}

		public ImmutableArray<IParameterSymbol> Parameters { get; }
		public bool ShowDefault { get; set; }
		public bool ShowParameterName { get; set; }
		public bool IsProperty { get; set; }
		public int ArgumentIndex { get; set; } = -1;

		public override void ToUI(InlineCollection inlines) {
			SymbolFormatter.Instance.ShowParameters(inlines, Parameters, ShowParameterName, ShowDefault, ArgumentIndex, IsProperty);
		}
	}

	sealed class SnapshotSpanSegment : TextSegment
	{
		public SnapshotSpanSegment(SnapshotSpan span, string text) {
			Span = span;
			Text = text;
		}

		public SnapshotSpan Span { get; }

		public override void ToUI(InlineCollection inlines) {
			var inline = Span.Render(Text);
			ApplySegmentStyle(inline);
			inlines.Add(inline);
		}
	}

	sealed class SyntaxSegment : TextSegment
	{
		public SyntaxSegment(SyntaxNodeOrToken syntax, ITextSnapshot snapshot, string text) {
			Syntax = syntax;
			Snapshot = snapshot;
			Text = text;
		}

		public SyntaxNodeOrToken Syntax { get; }
		public ITextSnapshot Snapshot { get; }

		public override void ToUI(InlineCollection inlines) {
			var inline = Syntax.Render(Snapshot, Text);
			ApplySegmentStyle(inline);
			inlines.Add(inline);
		}
	}

	sealed class AttributeDataSegment : Segment
	{
		public AttributeDataSegment(AttributeData data, int dataType) {
			Data = data;
			DataType = dataType;
		}

		public AttributeData Data { get; }
		public int DataType { get; }

		public override void ToUI(InlineCollection inlines) {
			SymbolFormatter.Instance.Format(inlines, Data, DataType);
		}
	}

	sealed class XmlDocSegment : Segment
	{
		public XmlDocSegment(XElement xmlDoc, Compilation compilation) {
			XmlDoc = xmlDoc;
			Compilation = compilation;
		}

		public XElement XmlDoc { get; }
		public Compilation Compilation { get; }

		public override void ToUI(InlineCollection inlines) {
			new XmlDocRenderer(Compilation, SymbolFormatter.Instance).Render(XmlDoc, inlines);
		}
	}

	class TypeParameterInfoSegment : Segment
	{
		readonly ITypeParameterSymbol _TypeParameter;
		readonly ITypeSymbol _ArgumentType;

		public TypeParameterInfoSegment(ITypeParameterSymbol typeParameter, ITypeSymbol argumentType) {
			_TypeParameter = typeParameter;
			_ArgumentType = argumentType;
		}

		public override void ToUI(InlineCollection inlines) {
			SymbolFormatter.Instance.ShowTypeArgumentInfo(_TypeParameter, _ArgumentType, inlines);
		}
	}

	sealed class IconSegment : Segment
	{
		public IconSegment(int iconId) {
			IconId = iconId;
		}

		public int IconId { get; set; }
		public Thickness Margin { get; set; } = WpfHelper.GlyphMargin;
		public double Opacity { get; set; }

		public override void ToUI(InlineCollection inlines) {
			var item = VsImageHelper.GetImage(IconId).WrapMargin(Margin);
			if (Opacity != 0) {
				item.Opacity = Opacity;
			}
			inlines.Add(new InlineUIContainer(item) { BaselineAlignment = BaselineAlignment.TextTop });
		}
	}

	sealed class LineBreakSegment : Segment
	{
		public override void ToUI(InlineCollection inlines) {
			inlines.Add(new LineBreak());
		}
	}
}
