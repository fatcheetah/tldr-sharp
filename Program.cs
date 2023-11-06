﻿using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System;

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var pageLocation = $"{baseDirectory}{Path.DirectorySeparatorChar}tldr-2.0{Path.DirectorySeparatorChar}pages";

if (args.Any())
    switch (args[0])
    {
        case "--version":
            Console.WriteLine("2311.06");
            return;
        case "--list":
            CheckDownloadPagesZip();
            GetListOfPlatformCommands();
            break;
        case "--list-all":
            CheckDownloadPagesZip();
            GetListOfCommands();
            break;
        case "--random":
            CheckDownloadPagesZip();
            GetRandomCommand();
            break;
        case "--help":
            WriteHelp();
            break;
        default:
            CheckDownloadPagesZip();
            GetCommand(args[0]);
            break;
    }
else
{
    WriteHelp();
}

return;

void WriteHelp()
{

    Console.Write(
        """
        tldr-sharp
        
        Display simple help pages for command-line tools from the tldr-pages project.
        More information: https://tldr.sh.
        
        - Print the tldr page for a specific command
        `tldr-sharp
        """);
    ConsoleEx.WriteColor(" <command>` \n\n", ConsoleColor.Yellow);
    Console.Write(
        """
        --version           Display Version
        --list              List all commands for current platform
        --list-all          List all commands for any platform
        --random            Show a random command
        --help              Show this information
        """);
    Console.Write("\n");
}


void GetCommand(string command)
{
    BuildSearchCommandNames(command);
    return;
}

void CheckDownloadPagesZip()
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

void BuildSearchCommandNames(string commandName)
{
    var commandSearch = $"{commandName}.md";

    try
    {
        var commonPath = $"{pageLocation}{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}{commandSearch}";
        WriteContentOfFile(commonPath);
        return;
    }
    catch
    {
    }

    try
    {
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Unix:
                var linuxPath = $"{pageLocation}{Path.DirectorySeparatorChar}linux{Path.DirectorySeparatorChar}{commandSearch}";
                WriteContentOfFile(linuxPath);
                break;
            case PlatformID.MacOSX:
                var osxPath = $"{pageLocation}{Path.DirectorySeparatorChar}osx{Path.DirectorySeparatorChar}{commandSearch}";
                WriteContentOfFile(osxPath);
                break;
            case PlatformID.Other:
            default:
                var winPath = $"{pageLocation}{Path.DirectorySeparatorChar}windows{Path.DirectorySeparatorChar}{commandSearch}";
                WriteContentOfFile(winPath);
                break;
        }
        return;
    }
    catch
    {
    }

    ConsoleEx.WriteColor($"{commandName} ", ConsoleColor.Yellow);
    Console.WriteLine("not found");
}

void GetListOfPlatformCommands()
{
    var common = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}common", "*.md", SearchOption.TopDirectoryOnly);

    IEnumerable<string> os;
    switch (Environment.OSVersion.Platform)
    {
        case PlatformID.Unix:
            os = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}linux", "*.md", SearchOption.TopDirectoryOnly);
            break;
        case PlatformID.MacOSX:
            os = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}osx", "*.md", SearchOption.TopDirectoryOnly);
            break;
        case PlatformID.Other:
        default:
            os = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}windows", "*.md", SearchOption.TopDirectoryOnly);
            break;
    }

    var commandList = common.Concat(os).OrderBy(cl => cl);
    foreach (var path in commandList)
    {
        var match = path.LastIndexOf(Path.DirectorySeparatorChar) + 1;
        var result = path[match..].Replace(".md", string.Empty);
        ConsoleEx.WriteColor($"{result}, ", ConsoleColor.Yellow);
    }
    Console.Write("\n");
}

void GetListOfCommands()
{
    var common = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}common", "*.md", SearchOption.TopDirectoryOnly);
    var linux = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}linux", "*.md", SearchOption.TopDirectoryOnly);
    var osx = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}osx", "*.md", SearchOption.TopDirectoryOnly);
    var windows = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}windows", "*.md", SearchOption.TopDirectoryOnly);
    var commandList = common.Concat(linux).Concat(osx).Concat(windows).OrderBy(cl => cl);

    foreach (var path in commandList)
    {
        var match = path.LastIndexOf(Path.DirectorySeparatorChar) + 1;
        var result = path[match..].Replace(".md", string.Empty);
        ConsoleEx.WriteColor($"{result}, ", ConsoleColor.Yellow);
    }
    Console.Write("\n");
}

void GetRandomCommand()
{
    var common = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}common", "*.md", SearchOption.TopDirectoryOnly);
    var linux = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}linux", "*.md", SearchOption.TopDirectoryOnly);
    var osx = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}osx", "*.md", SearchOption.TopDirectoryOnly);
    var windows = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}windows", "*.md", SearchOption.TopDirectoryOnly);
    var commandList = common.Concat(linux).Concat(osx).Concat(windows).ToList();

    var totalCommands = commandList.Count();
    var random = new Random();
    var randomIndex = random.Next(totalCommands);

    var path = commandList[randomIndex];

    var match = path.LastIndexOf(Path.DirectorySeparatorChar) + 1;
    var result = path[match..].Replace(".md", string.Empty);
    WriteContentOfFile(path);
    Console.Write("\n");
}

void WriteContentOfFile(string filePath)
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
                ConsoleEx.ColorToggle(ConsoleColor.Yellow);
                Console.Write((char)readByte);
                break;
            case 60: // <
            case 62: // >
                ConsoleEx.WriteColor(readByte, ConsoleColor.Red);
                continue;
            case 96: // `
                ConsoleEx.ColorToggle(ConsoleColor.DarkBlue);
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

public static class ConsoleEx
{
    public static void WriteColor(int write, ConsoleColor foregroundColor)
    {
        Console.ForegroundColor = foregroundColor;
        Console.Write((char)write);
        Console.ResetColor();
    }

    public static void WriteColor(string write, ConsoleColor foregroundColor)
    {
        Console.ForegroundColor = foregroundColor;
        Console.Write(write);
        Console.ResetColor();
    }

    public static void ColorToggle(ConsoleColor foregroundColor)
    {
        if (Console.ForegroundColor == foregroundColor)
        {
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = foregroundColor;
    }
}