using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System;

var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
var pageLocation = $"{baseDirectory}{Path.DirectorySeparatorChar}tldr-2.0{Path.DirectorySeparatorChar}pages";

if (args.Any())
    switch (args[0])
    {
        case "-v":
        case "--version":
            Console.WriteLine("2311.06");
            return;
        case "-l":
        case "--list":
            CheckDownloadPagesZip();
            GetListOfPlatformCommands();
            break;
        case "-la":
        case "--list-all":
            CheckDownloadPagesZip();
            GetListOfCommands();
            break;
        case "-r":
        case "--random":
            CheckDownloadPagesZip();
            GetRandomCommand();
            break;
        case "-b":
        case "--binary":
            CheckDownloadPagesZip();
            StorePagesAsBinary();
            break;
        case "-h":
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
        -la, --list-all          List all commands for any platform
        -r,  --random            Show a random command
        -h,  --help              Show this information
        """);
    Console.Write("\n");
}

void GetCommand(string command)
{
    BuildSearchCommandNames(command);
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
        // TODO: only get pages archive 8mb to 1.5mb
        archive.ExtractToDirectory(baseDirectory);
        archive.Dispose();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

void StorePagesAsBinary()
{
    if (!File.Exists("binarypages.dat"))
    {
        var common = Directory.EnumerateFiles($"{pageLocation}{Path.DirectorySeparatorChar}common", "*.md", SearchOption.TopDirectoryOnly).ToList();

        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, false);

        common.ForEach(path =>
        {
            // get page values and make index name
            var match = path.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            var keyName = path[match..].Replace(".md", string.Empty);

            // contents of page
            using var filestream = new FileInfo(path).Open(FileMode.Open, FileAccess.Read);
            using var streamReader = new StreamReader(filestream);
            string contents = streamReader.ReadToEnd();

            // write to binary
            writer.Write(keyName); // string
            writer.Write(contents); // string
        });

        // compress contents to binary file
        using var compressStream = File.Open("binarypages.dat", FileMode.Create);
        using var compresser = new BrotliStream(compressStream, compressionLevel: CompressionLevel.Fastest);

        memoryStream.Position = 0;                  // move position from end to start
        memoryStream.CopyTo(compresser);            // copy contents to the compressStream
        compresser.Close();                         // close the compressStream
    }

    // access contents of binary file
    using var compressFile = File.Open("binarypages.dat", FileMode.Open);
    using var decompressor = new BrotliStream(compressFile, CompressionMode.Decompress);
    using var reader = new BinaryReader(decompressor, encoding: Encoding.UTF8, false);

    // return tuples into returned index
    var returnedIndex = new Dictionary<string, string>();
    while (true)
    {
        try
        {
            returnedIndex.Add(
                    reader.ReadString(),
                    reader.ReadString()
                    );

            if (returnedIndex.ContainsKey("zsh"))
            {
                Console.WriteLine(returnedIndex["zsh"]);
                return;
            }
        }
        catch
        {
            break;
        }
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
}

void WriteContentOfFile(string filePath)
{
    using var filestream = new FileInfo(filePath).Open(FileMode.Open, FileAccess.Read);
    using var streamReader = new StreamReader(filestream);

    var firstLine = true;
    var inCodeBlock = false;
    while (filestream.CanRead)
    {
        var readByte = streamReader.Read();

        switch (readByte)
        {
            case -1: // EOF
                streamReader.Close();
                filestream.Close();
                break;
            case 10:
                firstLine = false;
                ConsoleEx.Write(readByte);
                break;
            case 58: // :
                ConsoleEx.Write(readByte);
                if (streamReader.Peek() == 10)
                {
                    streamReader.Read();
                }
                break;
            case 60: // <
            case 62: // >
                ConsoleEx.WriteColor(readByte, ConsoleColor.Red);
                continue;
            case 96: // `
                inCodeBlock ^= true;
                ConsoleEx.WriteColor(readByte, ConsoleColor.DarkBlue);
                break;
            case 123: // {
            case 125: // }
                continue;
            default:
                if (firstLine)
                {
                    ConsoleEx.WriteColor(readByte, ConsoleColor.Yellow);
                    break;
                }
                if (inCodeBlock)
                {
                    ConsoleEx.WriteColor(readByte, ConsoleColor.DarkBlue);
                    break;
                }
                ConsoleEx.Write(readByte);
                break;
        }
    }

    Console.Write("\n");
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