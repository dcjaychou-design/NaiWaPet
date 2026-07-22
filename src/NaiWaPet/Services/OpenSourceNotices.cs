using System.IO;
using System.Reflection;
using System.Text;

namespace NaiWaPet.Services;

internal static class OpenSourceNotices
{
    private static readonly Assembly Assembly = typeof(OpenSourceNotices).Assembly;

    public static string Version =>
        Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetName().Version?.ToString(3)
        ?? "未知";

    public static string LoadAll()
    {
        var documents = new (string Title, string Resource)[]
        {
            ("NaiWaPet GNU GPL v3 许可", "NaiWaPet.Licenses.Project.LICENSE.txt"),
            ("项目版权与素材说明", "NaiWaPet.Licenses.Project.NOTICE.md"),
            ("项目第三方组件声明", "NaiWaPet.Licenses.Project.THIRD_PARTY_NOTICES.md"),
            (".NET 10.0.10 许可", "NaiWaPet.Licenses.DotNet.LICENSE.txt"),
            (".NET 10.0.10 第三方声明", "NaiWaPet.Licenses.DotNet.THIRD_PARTY_NOTICES.txt"),
            ("WPF 10.0.10 许可", "NaiWaPet.Licenses.Wpf.LICENSE.txt"),
            ("WPF 10.0.10 第三方声明", "NaiWaPet.Licenses.Wpf.THIRD_PARTY_NOTICES.txt"),
        };

        var text = new StringBuilder();
        foreach (var document in documents)
        {
            if (text.Length > 0)
            {
                text.AppendLine().AppendLine();
            }

            text.AppendLine(new string('=', 72));
            text.AppendLine(document.Title);
            text.AppendLine(new string('=', 72));
            text.AppendLine(Read(document.Resource).TrimEnd());
        }

        return text.ToString();
    }

    private static string Read(string resourceName)
    {
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Embedded notice was not found: {resourceName}");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
