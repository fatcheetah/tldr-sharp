﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net.Http;
using System.IO.Compression;

if (args.Length == 0)
{
    Console.Write("Usage: tldr-sharp");
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write(" <command> \n");
    Console.ResetColor();
    Console.WriteLine("> tldr-sharp curl \n");
    return;
}

var commandArgument = args[0];

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var pageLocation = $"{baseDirectory}/tldr-main/pages";

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
        // grab the zip from the tldr-pages app
        var client = new HttpClient();
        var zipStream = client.GetStreamAsync("https://tldr.inbrowser.app/tldr-pages.zip").Result;

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(baseDirectory);
    }
    catch(Exception ex)
    {
        Console.WriteLine(ex);
    }
}

void ParseFileNamesToCommandNameIndex(out Dictionary<string, string> commandIndex)
{
    commandIndex = new Dictionary<string, string>();
    var commandRegex = new Regex(@"\/([^\/]+)\.md$");
    var list = Directory.EnumerateFiles(pageLocation, "*.md", SearchOption.AllDirectories);
    foreach (var path in list)
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
