using System;
using System.Globalization;
using System.Text;

namespace MemCheck.AzureFunctions;

internal static class StringBuilderExtensions
{
    #region Private classes
    private sealed class Li : IDisposable
    {
        private readonly StringBuilder builder;
        public Li(StringBuilder builder)
        {
            builder.Append("<li>");
            this.builder = builder;
        }
        public void Dispose()
        {
            builder.Append("</li>");
        }
    }
    private sealed class Paragraph : IDisposable
    {
        private readonly StringBuilder builder;
        public Paragraph(StringBuilder builder)
        {
            builder.Append("<p>");
            this.builder = builder;
        }
        public void Dispose()
        {
            builder.Append("</p>");
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
    public static IDisposable HtmlLi(this StringBuilder builder)
    {
        return new Li(builder);
    }
    public static IDisposable HtmlParagraph(this StringBuilder builder)
    {
        return new Paragraph(builder);
    }
}
