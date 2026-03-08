using System.Reflection;
using MyrientDL;
using Spectre.Console;

var version = Assembly.GetExecutingAssembly().GetName().Version;

var greetingPanel = new Panel(new Rows(
        new Markup(@"[green1]______  ___              _____            _____ [/]") { Justification = Justify.Center },
        new Markup(@"[green1]___   |/  /____  ___________(_)_____________  /_[/]") { Justification = Justify.Center },
        new Markup(@"[green1]__  /|_/ /__  / / /_  ___/_  /_  _ \_  __ \  __/[/]") { Justification = Justify.Center },
        new Markup(@"[green1]_  /  / / _  /_/ /_  /   _  / /  __/  / / / /_  [/]") { Justification = Justify.Center },
        new Markup(@"[green1]/_/  /_/  _\__, / /_/    /_/  \___//_/ /_/\__/  [/]") { Justification = Justify.Center },
        new Markup(@"[green1]          /____/                                [/]") { Justification = Justify.Center },
        Text.NewLine,
        new Markup("[bold]Myrient Downloader - Written by: [/][bold steelblue1]KhaosVoid[/]") { Justification = Justify.Center },
        new Text($"Version: {version!.Major}.{version!.Minor}.{version!.Build}") { Justification = Justify.Center },
        Text.NewLine))
    .SquareBorder()
    .BorderColor(Color.Green1)
    .Expand();

AnsiConsole.Write(greetingPanel);
Console.WriteLine();

var baseMyrientUri = new Uri("https://myrient.erista.me/files/");

Console.Write("Myrient Files Directory Url to download: ");

var myrientDirectoryPath = new Uri(Console.ReadLine() ?? string.Empty);

if (!myrientDirectoryPath.AbsoluteUri.StartsWith(baseMyrientUri.AbsoluteUri))
{
    AnsiConsole.MarkupLine($"[bold red]The specified url does not reside within {baseMyrientUri.AbsoluteUri}.[/]");
    Environment.Exit(-1);
}

Console.Write("Path to download contents to: ");
var myrientDownloadPath = Path.GetFullPath(Console.ReadLine() ?? string.Empty);
Console.WriteLine();

var downloadListExists = MyrientDownloader.TryLoadDownloadList(myrientDirectoryPath.AbsoluteUri, myrientDownloadPath, out var downloadList);

if (downloadListExists)
{
    AnsiConsole.MarkupLine($"Detected saved download list.");
    MyrientDownloader.ReportDownloadSummary(downloadList);
    
    Console.WriteLine();
    Console.Write("Resume Download? [Y/n]: ");

    var key = Console.ReadKey(true);
        
    while (key.Key is not ConsoleKey.Y)
    {
        if (key.Key is ConsoleKey.N)
        {
            Console.Write("N");
            Console.WriteLine();
            Environment.Exit(0);
        }

        key = Console.ReadKey(true);
    }

    Console.Write("Y");
    Console.WriteLine();
}

else
{
    downloadList = await MyrientDownloader.StartGetMyrientFileDownloadListAsync(myrientDirectoryPath.AbsoluteUri);
    
    MyrientDownloader.PromptDownloadSummaryAndConfirmation(downloadList);

    if (!Path.Exists(myrientDownloadPath))
        Directory.CreateDirectory(myrientDownloadPath);

    MyrientDownloader.SaveDownloadList(downloadList, myrientDirectoryPath.AbsoluteUri, myrientDownloadPath);
}

await MyrientDownloader.StartDownloadMyrientFilesAsync(downloadList, myrientDirectoryPath.AbsoluteUri, myrientDownloadPath);
