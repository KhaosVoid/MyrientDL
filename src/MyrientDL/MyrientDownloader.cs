using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using MyrientDL.Data;
using Spectre.Console;

namespace MyrientDL;

public static class MyrientDownloader
{
    private static readonly HttpClient _httpClient = new();

    public static async Task<List<IMyrientEntry>> StartGetMyrientFileDownloadListAsync(string myrientDirectoryPath)
    {
        List<IMyrientEntry> downloadList = null;
        
        await AnsiConsole.Progress()
            .Columns(
                new SpinnerColumn(),
                new TaskDescriptionColumn() { Alignment = Justify.Left},
                new ProgressBarColumn() { Width = 40},
                new PercentageColumn(),
                new ElapsedTimeColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"Retrieving Download List...", new ProgressTaskSettings() { AutoStart = false, MaxValue = 0});
                
                downloadList = await GetMyrientFileDownloadListAsync(myrientDirectoryPath, task);
            });

        return downloadList;
    }

    public static void SaveDownloadList(List<IMyrientEntry> downloadList, string myrientDirectoryPath, string myrientDownloadPath)
    {
        var jsonSerializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowOutOfOrderMetadataProperties = true
        };
        
        var downloadListJson = JsonSerializer.Serialize(downloadList, jsonSerializerOptions);
        var downloadListFileName = $"{Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(myrientDirectoryPath)))}.json";
        
        File.WriteAllText(Path.Combine(myrientDownloadPath, downloadListFileName), downloadListJson);
    }

    public static bool TryLoadDownloadList(string myrientDirectoryPath, string myrientDownloadPath, out List<IMyrientEntry> downloadList)
    {
        downloadList = [];
        
        var downloadListFileName = $"{Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(myrientDirectoryPath)))}.json";

        if (!File.Exists(Path.Combine(myrientDownloadPath, downloadListFileName)))
            return false;
        
        var jsonSerializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowOutOfOrderMetadataProperties = true
        };
        
        downloadList = JsonSerializer.Deserialize<List<IMyrientEntry>>(File.ReadAllText(Path.Combine(myrientDownloadPath, downloadListFileName)), jsonSerializerOptions);
        
        return true;
    }
    
    private static async Task<List<IMyrientEntry>> GetMyrientFileDownloadListAsync(string myrientDirectoryPath, ProgressTask? task)
    {
        var downloadList = new List<IMyrientEntry>();
        
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        var myrientDirectoryHtmlDocument = await context.OpenAsync(new DocumentRequest(new Url(myrientDirectoryPath)));

        if (myrientDirectoryHtmlDocument.StatusCode is not HttpStatusCode.OK)
        {
            AnsiConsole.MarkupLine($"[red]Failed to retrieve download list for: {myrientDirectoryPath}[/]");
            Environment.Exit(-1);
        }
        
        var tableRows = myrientDirectoryHtmlDocument
            .QuerySelectorAll("table > tbody > tr")
            .Where(tr => tr.QuerySelector("td:nth-child(1) > a")!.TextContent is not "Parent directory/" and not "./" and not "../")
            .ToList();

        if (task is not null && !task.IsStarted)
        {
            task.MaxValue = tableRows.Count;
            task.IsIndeterminate = true;
            task.StartTask();
        }

        foreach (var tableRow in tableRows)
        {
            var fileElement = (IHtmlAnchorElement)tableRow.QuerySelector("td:nth-child(1) > a");
            var sizeElement = tableRow.QuerySelector("td:nth-child(2)");

            if (fileElement!.TextContent.EndsWith('/'))
            {
                var myrientDirectoryDownloadList = await GetMyrientFileDownloadListAsync(fileElement.Href, null);
                
                var myrientDirectory = new MyrientDirectory()
                {
                    Name = fileElement.TextContent.TrimEnd('/'),
                    Path = new Uri(fileElement.Href),
                    Size = CalculateMyrientEntriesSize(myrientDirectoryDownloadList),
                    Entries = myrientDirectoryDownloadList
                };
                
                downloadList.Add(myrientDirectory);
            }

            else
            {
                downloadList.Add(new MyrientFile()
                {
                    Name = fileElement.TextContent,
                    Path = new Uri(fileElement.Href),
                    Size = FileSize.FromByteString(sizeElement?.TextContent ?? string.Empty)
                });
            }

            task?.IsIndeterminate = false;
            task?.Increment(1);
        }
        
        return downloadList;
    }

    public static void ReportDownloadSummary(List<IMyrientEntry> downloadList)
    {
        var downloadSize = CalculateMyrientEntriesSize(downloadList);
        var totalDirectories = CalculateMyrientDirectoriesCount(downloadList);
        var totalFiles = CalculateMyrientFilesCount(downloadList);

        AnsiConsole.MarkupLine($"There {(totalDirectories == 1 ? "is" : "are")} [bold yellow]{totalDirectories:N0} {(totalDirectories == 1 ? "directory" : "directories")}[/].");
        AnsiConsole.MarkupLine($"There {(totalFiles == 1 ? "is" : "are")} [bold green1]{totalFiles:N0} {(totalFiles <= 1 ? "file" : "files")}[/].");
        AnsiConsole.MarkupLine($"Total download size is approximately [bold green1]{downloadSize}[/].");
    }
    
    public static void PromptDownloadSummaryAndConfirmation(List<IMyrientEntry> downloadList)
    {
        ReportDownloadSummary(downloadList);
        
        Console.WriteLine();
        Console.Write("Continue? [Y/n]: ");

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

    private static FileSize CalculateMyrientEntriesSize(List<IMyrientEntry> myrientEntries)
    {
        decimal bytes = 0;
        
        foreach (var myrientEntry in myrientEntries)
        {
            switch (myrientEntry)
            {
                case MyrientDirectory myrientDirectory:
                    bytes += CalculateMyrientEntriesSize(myrientDirectory.Entries).Bytes;
                    break;
                
                case MyrientFile:
                    bytes += myrientEntry.Size.Bytes;
                    break;
            }
        }

        return new FileSize { Bytes = bytes };
    }

    private static ulong CalculateMyrientDirectoriesCount(List<IMyrientEntry> myrientEntries)
    {
        ulong count = 0;

        foreach (var myrientEntry in myrientEntries)
            if (myrientEntry is MyrientDirectory myrientDirectory)
                count += CalculateMyrientDirectoriesCount(myrientDirectory.Entries) + 1;

        return count;
    }

    private static ulong CalculateMyrientFilesCount(List<IMyrientEntry> myrientEntries)
    {
        ulong count = 0;

        foreach (var myrientEntry in myrientEntries)
        {
            switch (myrientEntry)
            {
                case MyrientDirectory myrientDirectory:
                    count += CalculateMyrientFilesCount(myrientDirectory.Entries);
                    break;
                
                case MyrientFile:
                    count += 1;
                    break;
            }
        }

        return count;
    }

    public static async Task StartDownloadMyrientFilesAsync(List<IMyrientEntry> myrientEntries, string myrientDirectoryPath, string myrientDownloadPath)
    {
        var progress = AnsiConsole.Progress()
            .Columns(
                new SpinnerColumn(),
                new TaskDescriptionColumn() { Alignment = Justify.Left },
                new ProgressBarColumn() { Width = 40},
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new TransferSpeedColumn());
            
        progress.RefreshRate = TimeSpan.FromSeconds(1);
            
        await progress.StartAsync(async ctx => await TryDownloadMyrientFilesAsync(myrientEntries, myrientEntries, myrientDirectoryPath, myrientDownloadPath, ctx));
    }

    private static async Task<bool> TryDownloadMyrientFilesAsync(List<IMyrientEntry> totalMyrientEntries, List<IMyrientEntry> currentMyrientEntries, string myrientDirectoryPath, string myrientDownloadPath, ProgressContext ctx)
    {
        var isDownloadSuccessful = true;
        
        foreach (var myrientEntry in currentMyrientEntries)
        {
            if (myrientEntry.IsDownloaded)
                continue;
            
            switch (myrientEntry)
            {
                case MyrientDirectory myrientDirectory:
                    var isMyrientDirectoryDownloaded = await TryDownloadMyrientFilesAsync(totalMyrientEntries, myrientDirectory.Entries, myrientDirectoryPath, myrientDownloadPath, ctx);

                    if (!isMyrientDirectoryDownloaded)
                        isDownloadSuccessful = false;
                    
                    myrientDirectory.IsDownloaded = isMyrientDirectoryDownloaded;
                    break;
                
                case MyrientFile myrientFile:
                    var isMyrientFileDownloaded = await TryDownloadMyrientFileAsync(myrientFile, myrientDownloadPath, ctx);

                    if (!isMyrientFileDownloaded)
                        isDownloadSuccessful = false;
                    
                    myrientFile.IsDownloaded = isMyrientFileDownloaded;
                    break;
            }
            
            SaveDownloadList(totalMyrientEntries, myrientDirectoryPath, myrientDownloadPath);
        }

        return isDownloadSuccessful;
    }
    
    private static async Task<bool> TryDownloadMyrientFileAsync(MyrientFile myrientFile, string myrientDownloadPath, ProgressContext ctx)
    {
        var isDownloadSuccessful = true;
        var task = ctx.AddTask($"{myrientFile.Name[..Math.Min(myrientFile.Name.Length, 37)].EscapeMarkup()}...");
        var downloadAttempts = 1;

        while (downloadAttempts < 4)
        {
            try
            {
                using var response = await _httpClient.GetAsync(myrientFile.Path.AbsoluteUri, HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                var destinationSubPathString = string.Join(
                    separator: "",
                    values: myrientFile.Path.Segments[2..].Select(WebUtility.UrlDecode));

                var destinationFilePath = Path.Combine(myrientDownloadPath, destinationSubPathString);
                var destinationDirectoryPath = Path.GetDirectoryName(destinationFilePath);

                Directory.CreateDirectory(destinationDirectoryPath!);

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1L;

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);

                var buffer = new Memory<byte>(new byte[8192]);
                int bytesRead;

                task.MaxValue = totalBytes;

                //var readAttempts = 1;
                //
                // while (readAttempts < 4)
                // {
                //     try
                //     {
                //         bytesRead = await stream.ReadAsync(buffer);
                //
                //         if (bytesRead <= 0)
                //             break;
                //     
                //         await fileStream.WriteAsync(buffer);
                //
                //         if (canReportProgress)
                //             task.Increment(bytesRead);
                //     }
                //     catch (Exception e)
                //     {
                //         if (readAttempts == 3)
                //             throw;
                //
                //         AnsiConsole.MarkupLine($"[yellow]Encountered a read error while downloading. Retrying chunk...[/]");
                //         readAttempts += 1;
                //     }
                // }
                
                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer);
                
                    if (canReportProgress)
                        task.Increment(bytesRead);
                }

                break;
            }
            catch (Exception exception)
            {
                switch (exception)
                {
                    case HttpRequestException httpRequestException:
                        AnsiConsole.MarkupLineInterpolated($"[red]Download request failed: [[{httpRequestException.StatusCode}]] {httpRequestException.Message}[/]");
                        break;
                    
                    case HttpIOException httpIOException:
                        AnsiConsole.MarkupLineInterpolated($"[red]A HttpIOException occurred. {httpIOException.Message}[/]");
                        break;
                    
                    default:
                        AnsiConsole.MarkupLineInterpolated($"[red]An exception occurred. {exception.Message}[/]");
                        break;
                }
                
                task.Value = 0;
                
                if (downloadAttempts == 3)
                {
                    task.StopTask();
                    isDownloadSuccessful = false;
                    downloadAttempts += 1;
                    AnsiConsole.MarkupLine($"[red]Download failed! Skipping file...[/]");
                }

                else
                {
                    downloadAttempts += 1;
                    AnsiConsole.MarkupLine($"[yellow]Retrying download (Attempt #{downloadAttempts})...[/]");
                }
            }
        }

        return isDownloadSuccessful;
    }
}