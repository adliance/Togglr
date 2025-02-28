using System.Globalization;
using System.Text;

namespace Adliance.Togglr;

public static class HtmlHelper
{
    public static void WriteHtmlBegin(StringBuilder sb)
    {
        sb.AppendLine("<!doctype html><html>");
        sb.AppendLine("<head><meta charset=\"utf-8\">");
        sb.AppendLine("<link href=\"https://cdn.jsdelivr.net/npm/bulma@1.0.1/css/bulma.min.css\" rel=\"stylesheet\">");
        sb.AppendLine("<link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.2/css/all.min.css\" />");

        sb.AppendLine("""
                      <style>
                        tr.has-text-grey-light td {
                            color: #abb1bf !important;
                        }
                      </style>
                      """);

        sb.AppendLine("</head><body>");
    }

    public static void WriteHtmlEnd(StringBuilder sb)
    {
        sb.AppendLine("</body></html>");
    }

    public static void WriteDocumentTitle(StringBuilder sb, string title)
    {
        sb.AppendLine("<section class=\"hero is-primary\"><div class=\"hero-body\">");
        sb.AppendLine(CultureInfo.CurrentCulture, $"<div class=\"container\"><h1 class=\"title\">{title}</h1></div>");
        sb.AppendLine("</div>\n</section>");
    }
}
