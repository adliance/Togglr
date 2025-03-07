using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Adliance.Togglr.Report;

namespace Adliance.Togglr;

public class Html
{
    private StringBuilder _sb;
    private ReportParameter _configuration;

    public Html(ReportParameter configuration)
    {
        _sb = new StringBuilder();
        _configuration = configuration;

        HtmlHelper.WriteHtmlBegin(_sb);
    }

    public void Title(string title)
    {
        HtmlHelper.WriteDocumentTitle(_sb, title);
    }

    public void Spacer()
    {
        _sb.AppendLine("<br /><br /><br />");
    }

    public void TableStart(params string[] headers)
    {
        _sb.AppendLine("<div class=\"container\">");
        _sb.AppendLine("<table class=\"table is-size-7 is-fullwidth\" style=\"margin:2rem 0 0 0; max-width:100%;\">");
        _sb.AppendLine("<thead><tr>");
        for (var i = 0; i < headers.Length; i++)
        {
            _sb.AppendLine($"<th {(i > 1 ? "class=\"has-text-right\"" : "")}>" + headers[i] + "</th>");
        }
        _sb.AppendLine("</tr></thead>");
        _sb.AppendLine("<tbody>");
    }

    public void TableRow(params string[] cells)
    {
        _sb.AppendLine("<tr>");
        for (var i = 0; i < cells.Length; i++)
        {
            _sb.AppendLine($"<td {(i > 1 ? "class=\"has-text-right\"" : "")}>" + cells[i] + "</td>");
        }

        _sb.AppendLine("</tr>");
    }

    public void TableEnd()
    {
        _sb.AppendLine("</tbody></table></div>");
    }

    public void Write(string html)
    {
        _sb.Append(html);
    }

    public void WriteLine(string html)
    {
        _sb.AppendLine(html);
    }

    public async Task SaveToFile(string fileName)
    {
        HtmlHelper.WriteHtmlEnd(_sb);

        if (!fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) fileName += ".html";
        var file = new FileInfo(Path.Combine(_configuration.OutputPath, fileName));
        await File.WriteAllTextAsync(file.FullName, _sb.ToString());
    }
}
