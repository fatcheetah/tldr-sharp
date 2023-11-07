using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System;
using System.Collections.Generic;
using System.Data;

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var dataLocation = $"{baseDirectory}pages.dat";

if (args.Any())
    switch (args[0])
    {
        case "-v":
        case "--version":
            Console.WriteLine("2311.07");
            return;
        case "-l":
        case "--list":
            DownloadPagesZipDeflateContents();
            ListCommands();
            break;
        case "-r":
        case "--random":
            DownloadPagesZipDeflateContents();
            GetRandomCommand();
            break;
        case "-h":
        case "--help":
            WriteHelp();
            break;
        default:
            DownloadPagesZipDeflateContents();
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
    ConsoleEx.WriteColor("# tldr-sharp\n\n", ConsoleColor.Yellow);
    Console.Write(
        """
        Display simple help pages for command-line tools from the tldr-pages project.
        More information: https://tldr.sh.
        
        - Print the tldr page for a specific command

        """);
    ConsoleEx.WriteColor("`tldr-sharp ", ConsoleColor.DarkBlue);
    ConsoleEx.WriteColor("<command>", ConsoleColor.Yellow);
    ConsoleEx.WriteColor("`\n\n", ConsoleColor.DarkBlue);
    Console.Write(
        """
        -v,  --version           Display Version
        -l,  --list              List all commands for current platform
        -r,  --random            Show a random command
        -h,  --help              Show this information
        """);
    Console.Write("\n");
}

void DownloadPagesZipDeflateContents()
{
    if (File.Exists(dataLocation)) return;

    try
    {
        const string url = "https://github.com/tldr-pages/tldr/archive/refs/tags/v2.0.zip";
        Console.WriteLine($"Downloading pages from {url} - file size: 5.8M");
        var client = new HttpClient();
        var httpStream = client.GetStreamAsync(url).Result;

        using var archive = new ZipArchive(httpStream, ZipArchiveMode.Read);
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, false);

        var osName = Environment.OSVersion.Platform switch
        {
            PlatformID.Unix => "linux",
            PlatformID.MacOSX => "osx",
            PlatformID.Other => "windows",
            _ => "windows",
        };

        var commonEntries = archive.Entries.Where(e => e.FullName.StartsWith($"tldr-2.0{Path.DirectorySeparatorChar}pages{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}"));
        var osEntries = archive.Entries.Where(e => e.FullName.StartsWith($"tldr-2.0{Path.DirectorySeparatorChar}pages{Path.DirectorySeparatorChar}common{Path.DirectorySeparatorChar}"));
        var entries = commonEntries.Concat(osEntries);

        foreach (var entry in entries)
        {
            var match = entry.FullName.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            var keyName = entry.FullName[match..].Replace(".md", string.Empty);

            using var streamReader = new StreamReader(entry.Open());
            string contents = streamReader.ReadToEnd();

            writer.Write(keyName);
            writer.Write(contents);
        }

        // compress contents to binary file
        using var compressStream = File.Open(dataLocation, FileMode.Create);
        using var compresser = new BrotliStream(compressStream, compressionLevel: CompressionLevel.Fastest);

        memoryStream.Position = 0;                  // move position from end to start
        memoryStream.CopyTo(compresser);            // compress the memory stream
        compresser.Dispose();                       // close the compresser
        archive.Dispose();                          // close the archive
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

void GetCommand(string commandName)
{
    using var dataFile = File.Open(dataLocation, FileMode.Open);
    using var decompressor = new BrotliStream(dataFile, CompressionMode.Decompress);
    using var reader = new BinaryReader(decompressor, encoding: Encoding.UTF8, false);

    try
    {
        while (true)
        {
            var key = reader.ReadString();
            var value = reader.ReadString();

            if (key == commandName)
            {
                WriteContentOfFile(value);
                return;
            }
        }
    }
    catch (EndOfStreamException)
    {
        return;
    }
}

void ListCommands()
{
    using var dataFile = File.Open(dataLocation, FileMode.Open);
    using var decompressor = new BrotliStream(dataFile, CompressionMode.Decompress);
    using var reader = new BinaryReader(decompressor, encoding: Encoding.UTF8, false);

    var list = new SortedSet<string>();
    try
    {
        while (true)
        {
            var cmd = reader.ReadString();
            list.Add(cmd);

            reader.ReadString();
        }
    }
    catch (EndOfStreamException)
    {
        return;
    }
    finally
    {
        foreach (var cmd in list)
        {
            ConsoleEx.WriteColor($"{cmd}, ", ConsoleColor.Yellow);
        }
        Console.Write("\n");
    }
}

void GetRandomCommand()
{
    using var dataFile = File.Open(dataLocation, FileMode.Open);
    using var decompressor = new BrotliStream(dataFile, CompressionMode.Decompress);
    using var reader = new BinaryReader(decompressor, encoding: Encoding.UTF8, false);

    var list = new List<(string key, string value)>();
    try
    {
        while (true)
        {
            var key = reader.ReadString();
            var value = reader.ReadString();
            list.Add((key, value));
        }
    }
    catch (EndOfStreamException)
    {
        return;
    }
    finally
    {
        var index = new Random().Next(list.Count);
        var command = list[index];
        WriteContentOfFile(command.value);
    }
}

void WriteContentOfFile(string value)
{
    var bytes = Encoding.UTF8.GetBytes(value);
    var firstLine = true;
    var inCodeBlock = false;
    var count = 0;
    for (int i = 0; i < value.Length; i++)
    {
        var character = value[i];
        switch (character)
        {
            case (char)10:
                firstLine = false;
                ConsoleEx.Write(character);
                break;
            case (char)58: // :
                ConsoleEx.Write(character);
                if (value[i + 1] == (char)10) i++; 
                break;
            case (char)60: // <
            case (char)62: // >
                ConsoleEx.WriteColor(character, ConsoleColor.Red);
                continue;
            case (char)96: // `
                inCodeBlock ^= true;
                ConsoleEx.WriteColor(character, ConsoleColor.DarkBlue);
                break;
            case (char)123: // {
            case (char)125: // }
                continue;
            default:
                if (firstLine)
                {
                    ConsoleEx.WriteColor(character, ConsoleColor.Yellow);
                    break;
                }
                if (inCodeBlock)
                {
                    ConsoleEx.WriteColor(character, ConsoleColor.DarkBlue);
                    break;
                }
                ConsoleEx.Write(character);
                break;
        }
    }
}

public static class ConsoleEx
{
    public static void Write(int write)
    {
        Console.Write((char)write);
    }

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