using System;
using System.Globalization;
using System.Text;

namespace MemCheck.AzureFunctions;

internal static class StringBuilderExtensions
{
    #region Private classes
    private sealed class HtmlTagwithCloser : IDisposable
    {
        private readonly StringBuilder builder;
        private readonly string tag;
        public HtmlTagwithCloser(StringBuilder builder, string tag)
        {
            builder.Append(CultureInfo.InvariantCulture, $"<{tag}>");
            this.builder = builder;
            this.tag = tag;
        }
        public void Dispose()
        {
            builder.Append(CultureInfo.InvariantCulture, $"</{tag}>");
        }
    }
    #endregion
    public static void AppendHtmlHeader(this StringBuilder builder, int level, string headerText)
    {
        builder.Append(CultureInfo.InvariantCulture, $"<h{level}>{headerText}</h{level}>");
    }
    public static void AppendHtmlParagraph(this StringBuilder builder, string paragraphText)
    {
        using (builder.HtmlParagraph())
            builder.Append(paragraphText);
    }
    public static void AppendHtmlText(this StringBuilder builder, string text, bool addBr = false)
    {
        builder.Append(text);
        if (addBr)
            builder.Append("<br/>");
    }
    public static IDisposable HtmlUl(this StringBuilder builder)
    {
        return new HtmlTagwithCloser(builder, "ul");
    }
    public static IDisposable HtmlLi(this StringBuilder builder)
    {
        return new HtmlTagwithCloser(builder, "li");
    }
    public static IDisposable HtmlParagraph(this StringBuilder builder)
    {
        return new HtmlTagwithCloser(builder, "p");
    }
}
