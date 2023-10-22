using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Http;
using System.IO.Compression;
using System.Linq;

if (args.Length == 0)
{
    Console.Write(
        """
        tldr-sharp
         
        Display simple help pages for command-line tools from the tldr-pages project.
        More information: https://tldr.sh.
         
        - Print the tldr page for a specific command
        `tldr
        """);
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write(" <command>` \n");
    Console.ResetColor();
    return;
}

var commandArgument = args[0];

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var pageLocation = $"{baseDirectory}/tldr-2.0/pages";

DownloadPopulatePagesFromZip();


ParseFileNamesToCommandNameIndex(out var commandIx);

if (commandIx.TryGetValue(commandArgument, out var value))
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
        var client = new HttpClient();

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(baseDirectory);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

void ParseFileNamesToCommandNameIndex(out Dictionary<string, string> commandIndex)
{
    commandIndex = new Dictionary<string, string>();
    var commandRegex = new Regex(@"\/([^\/]+)\.md$");

    var common = Directory.EnumerateFiles($"{pageLocation}/common", "*.md", SearchOption.AllDirectories);
    IEnumerable<string> os;

    switch (Environment.OSVersion.Platform)
    {
        case PlatformID.Unix:
            os = Directory.EnumerateFiles($"{pageLocation}/linux", "*.md", SearchOption.AllDirectories);
            break;
        case PlatformID.MacOSX:
            os = Directory.EnumerateFiles($"{pageLocation}/osx", "*.md", SearchOption.AllDirectories);
            break;
        case PlatformID.Other:
        default:
            os = Directory.EnumerateFiles($"{pageLocation}/windows", "*.md", SearchOption.AllDirectories);
            break;
    }

    var commandList = common.Concat(os);

    foreach (var path in commandList)
    {
        var match = commandRegex.Match(path);
        var result = match.Groups[1].Value;
        commandIndex.TryAdd(result, path);
    }
}

void GetContentOfFile(string filePath)
{
    using var filestream = new FileInfo(filePath).Open(FileMode.Open, FileAccess.Read);
    using var streamReader = new StreamReader(filestream);
    var builder = new StringBuilder();

    while (filestream.CanRead)
    {
        var readByte = streamReader.Read();

        if (readByte == 123 || readByte == 125) continue; // skip { }

        if (readByte == -1) // <EOF>
        {
            filestream.Close();
            streamReader.Close();
            break;
        }

        builder.Append((char)readByte);
    }

    Console.WriteLine(builder.ToString());
}