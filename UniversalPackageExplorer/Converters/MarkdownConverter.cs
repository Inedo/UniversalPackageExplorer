using CommonMark;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using BlockTag = CommonMark.Syntax.BlockTag;
using InlineTag = CommonMark.Syntax.InlineTag;

namespace UniversalPackageExplorer.Converters
{
    [ValueConversion(typeof(string), typeof(FlowDocument))]
    public sealed class MarkdownConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var block = CommonMarkConverter.Parse((string)value);

            return block == null ? null : new FlowDocument(Fill(block.FirstChild)) { PagePadding = new Thickness(0) };
        }

        private Block Convert(CommonMark.Syntax.Block block)
        {
            switch (block.Tag)
            {
                case BlockTag.BlockQuote:
                    return new Section(Fill(block.FirstChild)) { Margin = new Thickness(20, 0, 20, 0) };
                case BlockTag.List:
                    var list = new List();
                    var item = block.FirstChild;
                    while (item != null)
                    {
                        list.ListItems.Add(ConvertListItem(block));
                        item = item.NextSibling;
                    }
                    return list;
                case BlockTag.FencedCode:
                case BlockTag.IndentedCode:
                case BlockTag.HtmlBlock:
                    return new BlockUIContainer(new TextBox
                    {
                        IsReadOnly = true,
                        Text = block.StringContent.ToString()
                    });
                case BlockTag.Paragraph:
                    return new Paragraph(Fill(block.InlineContent)) { TextAlignment = TextAlignment.Left };
                case BlockTag.AtxHeading:
                case BlockTag.SetextHeading:
                    return new Paragraph(Fill(block.InlineContent)) { TextAlignment = TextAlignment.Left, FontSize = SystemFonts.MessageFontSize * (7 - block.Heading.Level) / 2 };
                case BlockTag.ThematicBreak:
                    return new Section
                    {
                        BorderBrush = SystemColors.WindowTextBrush,
                        BorderThickness = new Thickness(0, 2, 0, 0),
                        Margin = new Thickness(5)
                    };
                case BlockTag.ReferenceDefinition:
                    return new Section();
            }

            throw new NotImplementedException();
        }

        private ListItem ConvertListItem(CommonMark.Syntax.Block block)
        {
            var item = new ListItem { TextAlignment = TextAlignment.Left };
            for (var child = block.FirstChild; child != null; child = child.NextSibling)
            {
                item.Blocks.Add(Convert(child));
            }
            if (block.InlineContent != null)
            {
                item.Blocks.Add(new Paragraph(Fill(block.InlineContent)) { TextAlignment = TextAlignment.Left });
            }
            return item;
        }

        private Inline Convert(CommonMark.Syntax.Inline inline)
        {
            switch (inline.Tag)
            {
                case InlineTag.String:
                    return new Run(inline.LiteralContent);
                case InlineTag.SoftBreak:
                    return new LineBreak();
                case InlineTag.LineBreak:
                    return new LineBreak();
                case InlineTag.Code:
                case InlineTag.RawHtml:
                    return new Run(inline.LiteralContent)
                    {
                        FontFamily = new FontFamily("GlobalMonospace.CompositeFont")
                    };
                case InlineTag.Emphasis:
                    return new Italic(Fill(inline.FirstChild));
                case InlineTag.Strong:
                    return new Bold(Fill(inline.FirstChild));
                case InlineTag.Link:
                    var link = new Hyperlink(Fill(inline.FirstChild))
                    {
                        NavigateUri = new Uri(inline.TargetUrl),
                        ToolTip = string.IsNullOrWhiteSpace(inline.LiteralContent) ? null : inline.LiteralContent
                    };
                    link.RequestNavigate += NavigateToUri;
                    return link;
                case InlineTag.Image:
                    return new InlineUIContainer(new Image
                    {
                        Source = (ImageSource)new ImageSourceConverter().ConvertFrom(null, CultureInfo.InvariantCulture, inline.TargetUrl),
                        ToolTip = string.IsNullOrWhiteSpace(inline.LiteralContent) ? null : inline.LiteralContent
                    });
                case InlineTag.Strikethrough:
                    return new Span(Fill(inline.FirstChild)) { TextDecorations = TextDecorations.Strikethrough };
                case InlineTag.Placeholder:
                    return new Run(inline.LiteralContent);
            }

            throw new NotImplementedException();
        }

        private static void NavigateToUri(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString()).Dispose();
            e.Handled = true;
        }

        private Block Fill(CommonMark.Syntax.Block firstChild)
        {
            var section = new Section();
            while (firstChild != null)
            {
                section.Blocks.Add(Convert(firstChild));
                firstChild = firstChild.NextSibling;
            }
            return section;
        }

        private Inline Fill(CommonMark.Syntax.Inline firstChild)
        {
            var span = new Span();
            while (firstChild != null)
            {
                span.Inlines.Add(Convert(firstChild));
                firstChild = firstChild.NextSibling;
            }
            return span;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
