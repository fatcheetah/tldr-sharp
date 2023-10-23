using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System;

if (args.Length == 0)
{
    Console.Write(
        """
        tldr-sharp
         
        Display simple help pages for command-line tools from the tldr-pages project.
        More information: https://tldr.sh.
         
        - Print the tldr page for a specific command
        `tldr-sharp
        """);
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write(" <command>` \n");
    Console.ResetColor();
    return;
}

var commandArgument = args[0];

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var pageLocation = $"{baseDirectory}{Path.DirectorySeparatorChar}tldr-2.0{Path.DirectorySeparatorChar}pages";

DownloadPopulatePagesFromZip();

var commandIndex = ParseFileNamesToCommandNameIndex();

if (commandIndex.TryGetValue(commandArgument, out var value))
{
    GetContentOfFile(value);
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($"{commandArgument} ");
    Console.ResetColor();
    Console.Write("page not found \n");
}

return;

void DownloadPopulatePagesFromZip()
{
    if (Directory.Exists(pageLocation)) return;

    try
    {
        const string url = "https://github.com/tldr-pages/tldr/archive/refs/tags/v2.0.zip";
        Console.WriteLine($"Downloading pages from {url} - file size: 5.8M");
        var client = new HttpClient();
        var zipStream = client.GetStreamAsync(url).Result;

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(baseDirectory);
        archive.Dispose();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

Dictionary<string, string> ParseFileNamesToCommandNameIndex()
{
    var index = new Dictionary<string, string>();
    var common = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}common", "*.md", SearchOption.AllDirectories);
    IEnumerable<string> os;

    switch (Environment.OSVersion.Platform)
    {
        case PlatformID.Unix:
            os = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}linux", "*.md", SearchOption.AllDirectories);
            break;
        case PlatformID.MacOSX:
            os = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}osx", "*.md", SearchOption.AllDirectories);
            break;
        case PlatformID.Other:
        default:
            os = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}windows", "*.md", SearchOption.AllDirectories);
            break;
    }

    var commandList = common.Concat(os);

    foreach (var path in commandList)
    {
        var match = path.LastIndexOf(Path.DirectorySeparatorChar) + 1;
        var result = path[match..].Replace(".md", string.Empty);
        index.TryAdd(result, path);
    }

    return index;
}

void GetContentOfFile(string filePath)
{
    using var filestream = new FileInfo(filePath).Open(FileMode.Open, FileAccess.Read);
    using var streamReader = new StreamReader(filestream);

    while (filestream.CanRead)
    {
        var readByte = streamReader.Read();

        switch (readByte)
        {
            case -1: // EOF
                streamReader.Close();
                filestream.Close();
                break;
            case 35: // #
                ConsoleColorToggle(ConsoleColor.Yellow);
                Console.Write((char)readByte);
                break;
            case 60: // <
            case 62: // >
                ConsoleWriteAsColor(ConsoleColor.Red, readByte);
                continue;
            case 96: // `
                ConsoleColorToggle(ConsoleColor.DarkBlue);
                Console.Write((char)readByte);
                break;
            case 123: // {
            case 125: // }
                continue;
            default:
                Console.Write((char)readByte);
                break;
        }
    }

    Console.Write("\n");
}

void ConsoleWriteAsColor(ConsoleColor foregroundColor, int charCode)
{
    Console.ForegroundColor = foregroundColor;
    Console.Write((char)charCode);
    Console.ResetColor();
}

void ConsoleColorToggle(ConsoleColor foregroundColor)
{
    if (Console.ForegroundColor == foregroundColor)
    {
        Console.ResetColor();
        return;
    }

    Console.ForegroundColor = foregroundColor;
}