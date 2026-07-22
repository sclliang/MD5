using System.Security.Cryptography;
using System.Text;
using Spectre.Console;

namespace MD5;

internal static class Program
{
    private const string ManifestFileName = "md5.txt";

    private static int Main()
    {
        var exitCode = Run();

        if (!Console.IsInputRedirected)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Markup("[grey]Press any key to exit...[/]");
            Console.ReadKey(intercept: true);
        }

        return exitCode;
    }

    private static int Run()
    {
        var root = Directory.GetCurrentDirectory();
        var manifestPath = Path.Combine(root, ManifestFileName);

        RenderHeader(root);

        try
        {
            var currentHashes = ScanFiles(root, manifestPath);

            if (!File.Exists(manifestPath))
            {
                WriteManifest(manifestPath, currentHashes);
                RenderCreated(currentHashes.Count, manifestPath);
                return 0;
            }

            var expectedHashes = ReadManifest(manifestPath);
            var differences = CompareHashes(expectedHashes, currentHashes);

            if (differences.Count == 0)
            {
                RenderPass(currentHashes.Count);
                return 0;
            }

            RenderDifferences(differences);
            return 1;
        }
        catch (Exception ex)
        {
            RenderError(ex);
            return 2;
        }
    }

    private static void RenderHeader(string root)
    {
        AnsiConsole.Write(
            new FigletText("MD5")
                .Centered()
                .Color(Color.Cyan1));

        var header = new Panel(
                new Markup($"[bold]Folder[/]\n[grey]{Markup.Escape(root)}[/]"))
            .Header("[bold cyan]File Integrity Check[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(Color.Cyan1))
            .Padding(1, 0);

        AnsiConsole.Write(header);
        AnsiConsole.WriteLine();
    }

    private static SortedDictionary<string, string> ScanFiles(string root, string manifestPath)
    {
        var hashes = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .Start("[cyan]Scanning files...[/]", _ =>
            {
                foreach (var filePath in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (Path.GetFullPath(filePath).Equals(manifestPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(root, filePath).Replace(Path.DirectorySeparatorChar, '/');
                    hashes[relativePath] = ComputeMd5(filePath);
                }
            });

        return hashes;
    }

    private static void RenderCreated(int fileCount, string manifestPath)
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn();

        grid.AddRow("[bold]Status[/]", "[green]BASELINE CREATED[/]");
        grid.AddRow("[bold]Files[/]", $"[cyan]{fileCount}[/]");
        grid.AddRow("[bold]Manifest[/]", $"[grey]{Markup.Escape(manifestPath)}[/]");

        AnsiConsole.Write(
            new Panel(grid)
                .Header("[bold green]READY[/]")
                .Border(BoxBorder.Double)
                .BorderStyle(new Style(Color.Green))
                .Padding(1, 0));
    }

    private static void RenderPass(int fileCount)
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn();

        grid.AddRow("[bold]Status[/]", "[bold green]PASS[/]");
        grid.AddRow("[bold]Checked[/]", $"[cyan]{fileCount}[/] file(s)");
        grid.AddRow("[bold]Result[/]", "[green]All files match md5.txt.[/]");

        AnsiConsole.Write(
            new Panel(grid)
                .Header("[bold green]SUCCESS[/]")
                .Border(BoxBorder.Double)
                .BorderStyle(new Style(Color.Green))
                .Padding(1, 0));
    }

    private static string ComputeMd5(string filePath)
    {
        using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024,
            FileOptions.SequentialScan);

        var hashBytes = System.Security.Cryptography.MD5.HashData(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static void WriteManifest(string manifestPath, SortedDictionary<string, string> hashes)
    {
        var builder = new StringBuilder();

        foreach (var (path, hash) in hashes)
        {
            builder.Append(hash).Append("  ").Append(path).AppendLine();
        }

        File.WriteAllText(manifestPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static Dictionary<string, string> ReadManifest(string manifestPath)
    {
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in File.ReadLines(manifestPath))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separatorIndex = line.IndexOf("  ", StringComparison.Ordinal);
            if (separatorIndex <= 0 || separatorIndex + 2 >= line.Length)
            {
                throw new FormatException($"Invalid manifest line: {line}");
            }

            var hash = line[..separatorIndex].Trim();
            var relativePath = line[(separatorIndex + 2)..].Trim();

            hashes[relativePath] = hash;
        }

        return hashes;
    }

    private static List<FileDifference> CompareHashes(
        Dictionary<string, string> expectedHashes,
        SortedDictionary<string, string> currentHashes)
    {
        var differences = new List<FileDifference>();

        foreach (var (path, expectedHash) in expectedHashes)
        {
            if (!currentHashes.TryGetValue(path, out var currentHash))
            {
                differences.Add(new FileDifference("Missing", path, expectedHash, "-"));
                continue;
            }

            if (!string.Equals(expectedHash, currentHash, StringComparison.OrdinalIgnoreCase))
            {
                differences.Add(new FileDifference("Changed", path, expectedHash, currentHash));
            }
        }

        return differences
            .OrderBy(static difference => difference.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static difference => difference.Status, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void RenderDifferences(List<FileDifference> differences)
    {
        var changedCount = differences.Count(static difference => difference.Status == "Changed");
        var missingCount = differences.Count(static difference => difference.Status == "Missing");

        var summary = new Grid()
            .AddColumn(new GridColumn().NoWrap())
            .AddColumn();

        summary.AddRow("[bold]Status[/]", "[bold red]FAILED[/]");
        summary.AddRow("[bold]Differences[/]", $"[red]{differences.Count}[/]");
        summary.AddRow("[bold]Changed[/]", $"[yellow]{changedCount}[/]");
        summary.AddRow("[bold]Missing[/]", $"[red]{missingCount}[/]");

        AnsiConsole.Write(
            new Panel(summary)
                .Header("[bold red]CHECK FAILED[/]")
                .Border(BoxBorder.Double)
                .BorderStyle(new Style(Color.Red))
                .Padding(1, 0));

        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .Title("[bold red]MD5 Differences[/]")
            .AddColumn("Status")
            .AddColumn("File")
            .AddColumn("Expected")
            .AddColumn("Actual");

        foreach (var difference in differences)
        {
            var statusStyle = difference.Status switch
            {
                "Changed" => "yellow",
                "Missing" => "red",
                _ => "white"
            };

            table.AddRow(
                $"[{statusStyle}]{difference.Status}[/]",
                Markup.Escape(difference.Path),
                Markup.Escape(difference.ExpectedHash),
                Markup.Escape(difference.ActualHash));
        }

        AnsiConsole.Write(table);
    }

    private static void RenderError(Exception ex)
    {
        AnsiConsole.Write(
            new Panel(Markup.Escape(ex.Message))
                .Header("[bold red]ERROR[/]")
                .Border(BoxBorder.Double)
                .BorderStyle(new Style(Color.Red))
                .Padding(1, 0));
    }

    private sealed record FileDifference(string Status, string Path, string ExpectedHash, string ActualHash);
}
