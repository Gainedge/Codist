﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.CodeAnalysis;

namespace Codist.Controls
{
	sealed class SymbolList : ListBox, IMemberFilterable {
		public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(UIElement), typeof(SymbolList));
		public static readonly DependencyProperty FooterProperty = DependencyProperty.Register("Footer", typeof(UIElement), typeof(SymbolList));
		public static readonly DependencyProperty ItemsControlMaxHeightProperty = DependencyProperty.Register("ItemsControlMaxHeight", typeof(double), typeof(SymbolList));
		Predicate<object> _Filter;

		public SymbolList() {
			SetValue(VirtualizingPanel.IsVirtualizingProperty, true);
			SetValue(VirtualizingPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);
			ItemsControlMaxHeight = 500;
			HorizontalContentAlignment = HorizontalAlignment.Stretch;
			Symbols = new List<SymbolItem>();
			FilteredSymbols = new ListCollectionView(Symbols);
			Resources = SharedDictionaryManager.SymbolList;
		}
		public UIElement Header {
			get => GetValue(HeaderProperty) as UIElement;
			set => SetValue(HeaderProperty, value);
		}
		public UIElement Footer {
			get => GetValue(FooterProperty) as UIElement;
			set => SetValue(FooterProperty, value);
		}
		public double ItemsControlMaxHeight {
			get => (double)GetValue(ItemsControlMaxHeightProperty);
			set => SetValue(ItemsControlMaxHeightProperty, value);
		}
		public SymbolItem HighlightedItem { get; internal set; }
		public List<SymbolItem> Symbols { get; }
		public ListCollectionView FilteredSymbols { get; }
		public FrameworkElement Container { get; set; }
		public bool IsVsProject { get; set; }
		public SymbolItemType ContainerType { get; set; }
		public Func<SymbolItem, Image> IconProvider { get; set; }

		public SymbolItem Add(SyntaxNode node, SemanticContext context) {
			var item = new SymbolItem(node, context, this);
			Symbols.Add(item);
			return item;
		}
		public SymbolItem Add(ISymbol symbol, SemanticContext context, bool includeContainerType) {
			var item = new SymbolItem(symbol, context, this, includeContainerType);
			Symbols.Add(item);
			return item;
		}
		public SymbolItem Add(ISymbol symbol, SemanticContext context, ISymbol containerType) {
			var item = new SymbolItem(symbol, context, this, containerType);
			Symbols.Add(item);
			return item;
		}
		public void RefreshSymbols() {
			if (_Filter != null) {
				FilteredSymbols.Filter = _Filter;
				ItemsSource = FilteredSymbols;
			}
			else {
				ItemsSource = Symbols;
			}
		}
		public void ScrollToSelectedItem() {
			if (SelectedIndex == -1) {
				return;
			}
			UpdateLayout();
			ScrollIntoView(ItemContainerGenerator.Items[SelectedIndex]);
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e) {
			base.OnPreviewKeyDown(e);
			if (e.Key == Key.Tab) {
				e.Handled = true;
				return;
			}
			if (e.OriginalSource is TextBox == false) {
				return;
			}
			switch (e.Key) {
				case Key.Enter:
					if (SelectedIndex == -1 && HasItems) {
						(ItemContainerGenerator.Items[0] as SymbolItem)?.GoToSource();
					}
					else {
						(SelectedItem as SymbolItem)?.GoToSource();
					}
					e.Handled = true;
					break;
				case Key.Up:
					if (SelectedIndex > 0) {
						SelectedIndex--;
						ScrollToSelectedItem();
					}
					else if (HasItems) {
						SelectedIndex = this.ItemCount() - 1;
						ScrollToSelectedItem();
					}
					e.Handled = true;
					break;
				case Key.Down:
					if (SelectedIndex < this.ItemCount() - 1) {
						SelectedIndex++;
						ScrollToSelectedItem();
					}
					else if (HasItems) {
						SelectedIndex = 0;
						ScrollToSelectedItem();
					}
					e.Handled = true;
					break;
				case Key.PageUp:
					if (SelectedIndex >= 10) {
						SelectedIndex -= 10;
						ScrollToSelectedItem();
					}
					else if (HasItems) {
						SelectedIndex = 0;
						ScrollToSelectedItem();
					}
					e.Handled = true;
					break;
				case Key.PageDown:
					if (SelectedIndex >= this.ItemCount() - 10) {
						SelectedIndex = this.ItemCount() - 1;
						ScrollToSelectedItem();
					}
					else if (HasItems) {
						SelectedIndex += 10;
						ScrollToSelectedItem();
					}
					e.Handled = true;
					break;
			}
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
			base.OnRenderSizeChanged(sizeInfo);
			if (sizeInfo.WidthChanged) {
				var left = Canvas.GetLeft(this);
				//if (sizeInfo.PreviousSize.Width > 0) {
				//	left += sizeInfo.PreviousSize.Width - sizeInfo.NewSize.Width;
				//}
				if (left < 0) {
					Canvas.SetLeft(this, 0);
				}
				else {
					Canvas.SetLeft(this, left);
				}
				if (Container != null) {
					var top = Canvas.GetTop(this);
					if (top + sizeInfo.NewSize.Height > Container.RenderSize.Height) {
						Canvas.SetTop(this, Container.RenderSize.Height - sizeInfo.NewSize.Height);
					}
					if (left + sizeInfo.NewSize.Width > Container.RenderSize.Width) {
						Canvas.SetLeft(this, Container.ActualWidth - sizeInfo.NewSize.Width);
					}
				}
			}
		}
		void IMemberFilterable.Filter(string[] keywords, MemberFilterTypes filterTypes) {
			var noKeyword = keywords.Length == 0;
			if (noKeyword && filterTypes == MemberFilterTypes.All) {
				_Filter = null;
			}
			else if (noKeyword) {
				_Filter = o => {
					var i = (SymbolItem)o;
					return MemberFilterBox.FilterByImageId(filterTypes, i.ImageId);
				};
			}
			else {
				_Filter = o => {
					var i = (SymbolItem)o;
					return MemberFilterBox.FilterByImageId(filterTypes, i.ImageId)
							&& keywords.All(p => i.Content.GetText().IndexOf(p, StringComparison.OrdinalIgnoreCase) != -1);
				};
			}
			RefreshSymbols();
		}
	}

	// HACK: put the symbol list on top of the WpfTextView
	// don't use AdornmentLayer to do so, otherwise the menu will go up and down when scrolling code window
	sealed class ExternalAdornment : Canvas
	{
		readonly Microsoft.VisualStudio.Text.Editor.IWpfTextView _View;

		public ExternalAdornment(Microsoft.VisualStudio.Text.Editor.IWpfTextView view) {
			UseLayoutRounding = true;
			SnapsToDevicePixels = true;
			Grid.SetColumn(this, 1);
			Grid.SetRow(this, 1);
			Grid.SetIsSharedSizeScope(this, true);
			var grid = view.VisualElement.GetParent<Grid>();
			if (grid != null) {
				grid.Children.Add(this);
			}
			else {
				view.VisualElement.Loaded += VisualElement_Loaded;
			}
			_View = view;
		}

		void VisualElement_Loaded(object sender, RoutedEventArgs e) {
			_View.VisualElement.Loaded -= VisualElement_Loaded;
			_View.VisualElement.GetParent<Grid>().Children.Add(this);
		}

		public void Add(UIElement element) {
			Children.Add(element);
			element.MouseLeave -= ReleaseQuickInfo;
			element.MouseLeave += ReleaseQuickInfo;
			element.MouseEnter -= SuppressQuickInfo;
			element.MouseEnter += SuppressQuickInfo;
		}
		public void Clear() {
			foreach (UIElement item in Children) {
				item.MouseLeave -= ReleaseQuickInfo;
				item.MouseEnter -= SuppressQuickInfo;
			}
			Children.Clear();
			_View.Properties.RemoveProperty(nameof(ExternalAdornment));
			_View.VisualElement.Focus();
		}

		void ReleaseQuickInfo(object sender, MouseEventArgs e) {
			_View.Properties.RemoveProperty(nameof(ExternalAdornment));
		}

		void SuppressQuickInfo(object sender, MouseEventArgs e) {
			_View.Properties[nameof(ExternalAdornment)] = true;
		}
	}

	sealed class SymbolItem /*: INotifyPropertyChanged*/
	{
		readonly SemanticContext _SemanticContext;
		Image _Icon;
		int _ImageId;
		ThemedMenuText _Content;
		string _Hint;
		readonly bool _IncludeContainerType;

		//public event PropertyChangedEventHandler PropertyChanged;
		public int ImageId => _ImageId != 0 ? _ImageId : (_ImageId = Symbol != null ? Symbol.GetImageId() : SyntaxNode != null ? SyntaxNode.GetImageId() : -1);
		public Image Icon => _Icon ?? (_Icon = Container.IconProvider != null ? Container.IconProvider(this) : ThemeHelper.GetImage(ImageId != -1 ? ImageId : 0));
		public string Hint {
			get => _Hint ?? (_Hint = Symbol != null ? GetSymbolConstaintValue(Symbol) : String.Empty);
			set => _Hint = value;
		}
		public SymbolItemType Type { get; set; }
		public bool IsExternal => Type == SymbolItemType.External
			|| Container.ContainerType != SymbolItemType.VsKnownImage && Symbol?.ContainingAssembly.GetSourceType() == AssemblySource.Metadata;
		public ThemedMenuText Content {
			get => _Content ?? (_Content = Symbol != null ? CreateContentForSymbol(Symbol, _IncludeContainerType, true) : SyntaxNode != null ? new ThemedMenuText(SyntaxNode.GetDeclarationSignature()) : new ThemedMenuText());
			set => _Content = value;
		}
		public Location Location { get; set; }
		public SyntaxNode SyntaxNode { get; private set; }
		public ISymbol Symbol { get; private set; }
		public SymbolList Container { get; }

		public SymbolItem(SymbolList list) {
			Container = list;
			Content = new ThemedMenuText();
			_ImageId = -1;
		}
		public SymbolItem(ISymbol symbol, SemanticContext semanticContext, SymbolList list, ISymbol containerSymbol)
			: this (symbol, semanticContext, list, false) {
			_ImageId = containerSymbol.GetImageId();
			_Content = CreateContentForSymbol(containerSymbol, false, true);
		}
		public SymbolItem(ISymbol symbol, SemanticContext semanticContext, SymbolList list, bool includeContainerType) {
			Symbol = symbol;
			_SemanticContext = semanticContext;
			Container = list;
			_IncludeContainerType = includeContainerType;
		}

		public SymbolItem(SyntaxNode node, SemanticContext semanticContext, SymbolList list) {
			SyntaxNode = node;
			_SemanticContext = semanticContext;
			Container = list;
		}

		public void GoToSource() {
			if (Location != null) {
				Location.GoToSource();
			}
			else if (Symbol != null) {
				RefreshSymbol();
				Symbol.GoToSource();
			}
			else if (SyntaxNode != null) {
				RefreshSyntaxNode();
				SyntaxNode.GetIdentifierToken().GetLocation().GoToSource();
			}
		}
		public bool SelectIfContainsPosition(int position) {
			if (IsExternal == false && SyntaxNode != null && SyntaxNode.FullSpan.Contains(position)) {
				Container.SelectedItem = this;
				return true;
			}
			return false;
		}
		public ThemedMenuText CreateContentForSymbol(ISymbol symbol, bool includeType, bool includeParameter) {
			var t = new ThemedMenuText();
			if (includeType && symbol.ContainingType != null) {
				t.Append(symbol.ContainingType.Name + symbol.ContainingType.GetParameterString() + ".", ThemeHelper.SystemGrayTextBrush);
			}
			var b = symbol.Kind == SymbolKind.Property || symbol.Kind == SymbolKind.Field ? QuickInfo.ColorQuickInfo.GetBrush(symbol, Container.IsVsProject) : null;
			if (b != null) {
				t.Append(
					new System.Windows.Documents.InlineUIContainer(
						new Border {
							BorderThickness = WpfHelper.TinyMargin,
							BorderBrush = ThemeHelper.MenuTextBrush,
							Margin = WpfHelper.GlyphMargin,
							SnapsToDevicePixels = true,
							Background = b,
							Height = ThemeHelper.DefaultIconSize,
							Width = ThemeHelper.DefaultIconSize,
						}
						) {
						BaselineAlignment = BaselineAlignment.TextTop
					});
			}
			t.Append(symbol.Name);
			if (includeParameter) {
				t.Append(symbol.GetParameterString(), ThemeHelper.SystemGrayTextBrush);
			}
			return t;
		}

		internal void Item_ToolTipOpening(object sender, ToolTipEventArgs args) {
			var e = args.Source as FrameworkElement;
			if (e.ToolTip == null) {
				return;
			}
			if (e.ToolTip is string) {
				if (Symbol != null) {
					RefreshSymbol();
					e.ToolTip = ToolTipFactory.CreateToolTip(Symbol, false, _SemanticContext.SemanticModel.Compilation);
					e.SetTipOptions();
					return;
				}
				if (SyntaxNode != null) {
					if (Symbol != null) {
						RefreshSymbol();
					}
					else {
						Symbol = _SemanticContext.GetSymbolAsync(SyntaxNode).ConfigureAwait(false).GetAwaiter().GetResult();
					}
					if (Symbol != null) {
						e.ToolTip = ToolTipFactory.CreateToolTip(Symbol, true, _SemanticContext.SemanticModel.Compilation);
						e.SetTipOptions();
						return;
					}
				}
				e.ToolTip = null;
			}
		}

		static string GetSymbolConstaintValue(ISymbol symbol) {
			if (symbol.Kind == SymbolKind.Field) {
				var f = symbol as IFieldSymbol;
				if (f.HasConstantValue) {
					return f.ConstantValue?.ToString();
				}
			}
			return null;
		}
		void RefreshSyntaxNode() {
			var node = _SemanticContext.RelocateDeclarationNode(SyntaxNode);
			if (node != null && node != SyntaxNode) {
				SyntaxNode = node;
			}
		}
		void RefreshSymbol() {
			var symbol = _SemanticContext.RelocateSymbolAsync(Symbol).ConfigureAwait(false).GetAwaiter().GetResult();
			if (symbol != null && symbol != Symbol) {
				Symbol = symbol;
			}
		}
	}

	public class SymbolItemTemplateSelector : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container) {
			var element = container as FrameworkElement;
			var i = item as SymbolItem;
			if (i != null && (i.Symbol != null || i.SyntaxNode != null)) {
				element.ToolTip = String.Empty;
				element.ToolTipOpening += i.Item_ToolTipOpening;
				return element.FindResource("SymbolItemTemplate") as DataTemplate;
			}
			else {
				return element.FindResource("LabelTemplate") as DataTemplate;
			}
		}
	}

	enum SymbolItemType
	{
		Normal,
		External,
		Container,
		VsKnownImage
	}
}
