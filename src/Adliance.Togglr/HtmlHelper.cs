using System.Globalization;
using System.Text;

namespace Adliance.Togglr;

public static class HtmlHelper
{
    public static void WriteHtmlBegin(StringBuilder sb)
    {
        sb.AppendLine("<!doctype html><html>");
        sb.AppendLine("<head><meta charset=\"utf-8\">");
        sb.AppendLine("<link href=\"https://cdnjs.cloudflare.com/ajax/libs/bulma/0.7.5/css/bulma.min.css\" rel=\"stylesheet\">");
        sb.AppendLine("<script src=\"https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.10.2/js/all.min.js\" integrity=\"sha256-iZGp5HAiwRmkbOKVYv5FUER4iXp5QbiEudkZOdwLrjw=\" crossorigin=\"anonymous\"></script>");
        sb.AppendLine("</head><body>");
    }

    public static void WriteHtmlEnd(StringBuilder sb)
    {
        sb.AppendLine("</body></html>");
    }

    public static void WriteDocumentTitle(StringBuilder sb, string user)
    {
        sb.AppendLine("<section class=\"hero is-primary\"><div class=\"hero-body\">");
        sb.AppendLine(CultureInfo.CurrentCulture, $"<div class=\"container\"><h1 class=\"title\">Arbeitszeit von {user}</h1></div>");
        sb.AppendLine("</div>\n</section>");
    }
}
