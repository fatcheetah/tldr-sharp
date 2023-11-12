using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System;

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var dataLocation = $"{baseDirectory}tldr-sharp.dat";

if (!args.Any())
{
    WriteHelp();
    return;
}

var arguments = args.Select(arg => arg.ToLower()).ToList();
var cmdBuilder = new StringBuilder();
var platform = string.Empty;

for (var i = 0; i < arguments.Count; i++)
{
    var arg = arguments[i];

    switch (arg)
    {
        case "-h" or "--help":
            WriteHelp();
            return;
        case "-V" or "--version":
            WriteVersion();
            return;
        case "-l" or "--list":
            DownloadPagesZipDeflateContents();
            ListCommands();
            return;
        case "-r" or "--random":
            DownloadPagesZipDeflateContents();
            GetRandomCommand();
            return;
        case "-p" or "--platform" when i + 1 == arguments.Count:
            Console.WriteLine("Could not find a platform specified");
            Console.WriteLine("Look at --help for more information");
            return;
        case "-p" or "--platform":
        {
            platform = arguments[i + 1] switch
            {
                "osx" or "macos" => "osx",
                "windows" or "win" => "windows",
                "linux" or "unix" => "linux",
                _ => platform
            };

            i++;
            break;
        }

        default:
            cmdBuilder.Append($"-{arg}");
            break;
    }
}

if (cmdBuilder.Length != 0)
{
    DownloadPagesZipDeflateContents();
    var command = cmdBuilder.ToString()[1..];

    if (platform != string.Empty)
    {
        GetCommand(command, platform);
    }
    else
    {
        GetCommand(command);
    }

    return;
}

WriteHelp();
return;

void WriteVersion()
{
    Console.WriteLine(
        """
        tldr-sharp 2311.12
        client spec 2.0
        """
    );
}

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
    ConsoleEx.WriteColor("command", ConsoleColor.Yellow);
    ConsoleEx.WriteColor("`\n\n", ConsoleColor.DarkBlue);
    Console.WriteLine(
        """
        -V,  --version           Display Version
        -l,  --list              List all commands for current platform
        -r,  --random            Show a random command
        -p,  --platform          Specify platform pages to be used [linux, osx, windows]
        -h,  --help              Show this information
        """);
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

        var osPath = Environment.OSVersion.Platform switch
        {
            PlatformID.Unix => "linux",
            PlatformID.MacOSX => "osx",
            PlatformID.Other => "windows",
            _ => "linux",
        };

        var pagePath = $"tldr-2.0{Path.DirectorySeparatorChar}pages{Path.DirectorySeparatorChar}";
        var commonEntries = archive.Entries
            .Where(e => e.FullName.StartsWith($"{pagePath}common{Path.DirectorySeparatorChar}"));
        var linuxEntries = archive.Entries
            .Where(e => e.FullName.StartsWith($"{pagePath}linux{Path.DirectorySeparatorChar}"));
        var osxEntries = archive.Entries
            .Where(e => e.FullName.StartsWith($"{pagePath}osx{Path.DirectorySeparatorChar}"));
        var windowsEntries = archive.Entries
            .Where(e => e.FullName.StartsWith($"{pagePath}windows{Path.DirectorySeparatorChar}"));

        // fix the order of this relevant to current OS in use
        var entries = commonEntries
            .Concat(linuxEntries).Concat(osxEntries).Concat(windowsEntries)
            .Select(e => new {
                    Zip = e.Open(),
                    Command = e.FullName[(e.FullName.LastIndexOf(Path.DirectorySeparatorChar) + 1)..].Replace(".md", string.Empty),
                    Platform = e.FullName[..(e.FullName.LastIndexOf(Path.DirectorySeparatorChar))][15..]
            })
            .OrderByDescending(e => e.Platform == osPath)
            .ThenByDescending(e => e.Platform == "common");

        var keys = new StringBuilder();
        var keyPosition = 0;

        foreach (var entry in entries)
        {
            using var streamReader = new StreamReader(entry.Zip);
            string contents = streamReader.ReadToEnd();
            
            keys.Append($"{entry.Command} {entry.Platform} {keyPosition},");
            writer.Write(contents);

            keyPosition++;
        }

        using var memoryHeadersStream = new MemoryStream();
        using var writerHeaders = new BinaryWriter(memoryHeadersStream, Encoding.UTF8);
        writerHeaders.Write(keys.ToString());

        memoryStream.Position = 0;
        memoryStream.CopyTo(memoryHeadersStream);
        memoryStream.Dispose();

        using var compressStream = File.Open(dataLocation, FileMode.Create);
        using var compressor = new BrotliStream(compressStream, compressionLevel: CompressionLevel.Optimal);

        memoryHeadersStream.Position = 0;
        memoryHeadersStream.CopyTo(compressor);

        compressor.Dispose();
        archive.Dispose();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

void GetCommand(string commandName, string platformName = "")
{
    using var dataFile = File.Open(dataLocation, FileMode.Open);
    using var decompressor = new BrotliStream(dataFile, CompressionMode.Decompress);
    using var reader = new BinaryReader(decompressor, encoding: Encoding.UTF8, false);

    try
    {
        var headerItems = reader.ReadString()
            .Split(",", StringSplitOptions.RemoveEmptyEntries)
            .Select(ci => new
            {
                Command = ci.Split()[0],
                Platform = ci.Split()[1],
                ToSkip = ci.Split()[2],
            })
            .ToList();

        var items = headerItems.Where(hi => hi.Command == commandName);

        if (!string.IsNullOrEmpty(platformName)) 
        {
            items = items.Where(hi => hi.Platform == platform);
        }

        if (!items.Any()) 
        {
            ConsoleEx.WriteColor($"{commandName} ", ConsoleColor.Yellow);
            Console.Write("not found \n");
            return;
        }

        var position = int.Parse(items.First().ToSkip);
        for (var i = 0; i < position; i++)
        {
            reader.ReadString();
        }

        var contents = reader.ReadString();
        WriteContentOfFile(contents);

        if (items.Count() > 1 && string.IsNullOrEmpty(platformName)) 
        { 
            Console.Write("\n");
            items.ToList()
                .ForEach(i => { ConsoleEx.WriteColor($"[i] page found for platform {i.Platform} \n", ConsoleColor.DarkMagenta); });
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

    try
    {
        var commandIndexes = reader.ReadString()
            .Split(",", StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(ci => ci)
            .Select(ci => new
            {
                Command = ci.Split()[0]
            })
            .ToList();

        var commandBuilder = new StringBuilder();
        commandIndexes.ForEach(cmd => { commandBuilder.Append($"{cmd.Command},"); });

        ConsoleEx.WriteColor(commandBuilder.ToString(), ConsoleColor.Yellow);
        Console.Write("\n");
    }
    catch (EndOfStreamException)
    {
        return;
    }
}

void GetRandomCommand()
{
    using var dataFile = File.Open(dataLocation, FileMode.Open);
    using var decompressor = new BrotliStream(dataFile, CompressionMode.Decompress);
    using var reader = new BinaryReader(decompressor, encoding: Encoding.UTF8, false);

    try
    {
        var headerItems = reader.ReadString()
            .Split(",", StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(ci => ci)
            .Select(ci => new
            {
                ToSkip = ci.Split()[2]
            })
            .ToList();

        var index = new Random().Next(headerItems.Count);
        var command = headerItems[index];
        var position = int.Parse(command.ToSkip);

        for (var i = 0; i < position; i++)
        {
            reader.ReadString();
        }

        var contents = reader.ReadString();
        WriteContentOfFile(contents);
    }
    catch (EndOfStreamException)
    {
        return;
    }
}

void WriteContentOfFile(string value)
{
    var bytes = Encoding.UTF8.GetBytes(value);
    var firstLine = true;
    var inCodeBlock = false;
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
