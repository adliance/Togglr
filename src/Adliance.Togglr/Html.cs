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

    public void Write(string html)
    {
        _sb.Append(html);
    }

    public void WriteLine(string html)
    {
        _sb.AppendLine(html);
    }

    public async Task WriteToFile(string fileName)
    {
        HtmlHelper.WriteHtmlEnd(_sb);

        if (!fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) fileName += ".html";
        var file = new FileInfo(Path.Combine(_configuration.OutputPath, fileName));
        await File.WriteAllTextAsync(file.FullName, _sb.ToString());
    }
}
