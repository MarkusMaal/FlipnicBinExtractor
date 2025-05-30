﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace FlipnicBinExtractor
{
    class Program
    {
        // Root function
        static int Main(string[] args)
        {
            // Print title
            Console.WriteLine("Flipnic Game Data Extractor And Repacker");
            // Get the executable name of this program
            string me = AppDomain.CurrentDomain.FriendlyName;

            // Quick help function
            if (string.Join("", args).Contains("/?"))
            {
                string help = "Help" +
                    "\n" +
                    string.Format("\nUsage: {0} [/e /c /f] [source] [destination] [directory tree file]", me) +
                    "\n" +
                    "\n/e          - Extract data from BIN file" +
                    "\n/ef         - Extract data from BIN file. Skip unpacking subfolders (subfolders are saved in original" +
                    "\n              format as [folder location]\\A)." +
                    "\n/es         - Extract data from a subfolder (A file)." +
                    "\n/l          - Display directory tree without extracting data" +
                    "\n/le         - Save directory tree to a file" +
                    "\n/c          - Create BIN file using files in a folder. Any subfolders containing files will be aliased" +
                    "\n              and the A file will be interpreted as a subfolder in TOC. OPTIONAL: File order can be" +
                    "\n              specified by directory tree file. This can help avoid crashes in certain cases. " +
                    "\n/f          - Create a subdirectory file, which you can use for repacking. Note that this is significantly slower," +
                    "\n              because each file is 1 byte addressed." +
                    "\n/lst        - Lists all streams within a .PSS file" +
                    "\n/est        - Separates all streams from a .PSS file" +
                    "\nsource      - The source file. If extracting, this must be a .BIN file. If you're repacking, make sure this points" +
                    "\n              to a folder." +
                    "\ndestination - The destination folder (when extracting) or file (when repacking)" +
                    "\n" +
                    "\nExamples:" +
                    "\n\n" +
                    string.Format("\n{0} /e STR.BIN STR", me) +
                    string.Format("\n{0} /f STR STR\\A", me) +
                    string.Format("\n{0} /l RES.BIN", me) +
                    string.Format("\n{0} /le RES.BIN RES.TXT", me) +
                    string.Format("\n{0} /lst SILVER_DROP.PSS", me) +
                    string.Format("\n{0} /est SHUKYAKUDEMO.PSS", me) +
                    string.Format("\n{0} /c RES RES.BIN RES.TXT", me) +
                    string.Format("\n{0} /c TUTO TUTO.BIN", me) + "\n";
                Console.Write(help);
                return 0;
            }
            // Simple error handling
            else if (args.Length == 0)
            {
                Console.WriteLine(string.Format("No command specified. To see all available commands, type \"{0} /?\".", me));
                return 4;
            }
            else if (((args[0].ToLower() != "/l") && (args[0].ToLower() != "/le") && (args[0].ToLower() != "/lst") && (args[0].ToLower() != "/est") && (args.Length < 3)))
            {
                Console.WriteLine("Not enough arguments specified!");
                return 5;
            }
            else if (((args.Length > 3) && (args[0].ToLower() != "/c")) || ((args.Length > 4) && (args[0].ToLower() == "/c")))
            {
                Console.WriteLine("Too many arguments specified!");
                return 6;
            }
            else
            {
                // Parse first argument
                switch (args[0].ToLower())
                {
                    case "/lst":
                        switch (ListPss(args[1]))
                        {
                            case 0:
                                Console.WriteLine("Command completed successfully.");
                                return 0;
                            default:
                                Console.WriteLine("Unknown error has occoured.");
                                return 999;
                        }
                    case "/est":
                        switch (ListPss(args[1], true))
                        {
                            case 0:
                                Console.WriteLine("Command completed successfully.");
                                return 0;
                            default:
                                Console.WriteLine("Unknown error has occoured.");
                                return 999;
                        }
                        return 0;
                    case "/e":
                        switch (ExtractBin(args[1], args[2]))
                        {
                            case 0:
                                Console.WriteLine("Command completed successfully.");
                                return 0;
                            case 1:
                                Console.WriteLine("Cannot continue. Must overwrite directory to extract!");
                                return 1;
                            default:
                                Console.WriteLine("Unknown error has occoured.");
                                return 999;
                        }
                    case "/ef":
                        switch (ExtractBin(args[1], args[2], false))
                        {
                            case 0:
                                Console.WriteLine("Command completed successfully.");
                                return 0;
                            case 1:
                                Console.WriteLine("Cannot continue. Must overwrite directory to extract!");
                                return 1;
                            default:
                                Console.WriteLine("Unknown error has occoured.");
                                return 999;
                        }
                    case "/c":
                        switch (RepackBin(args[1], args[2]))
                        {
                            case 0:
                                Console.WriteLine("Command completed successfully.");
                                return 0;
                            case 1:
                                Console.WriteLine("Cannot continue. Must overwrite existing BIN file to repack!");
                                return 1;
                            // Only applicable when the TOC file is used. In other situations, these errors shouldn't occour
                            case 30:
                                Console.WriteLine("Folder container file referenced missing for repack!");
                                return 30;
                            case 31:
                                Console.WriteLine("File references missing for repack!");
                                return 30;
                            default:
                                Console.WriteLine("Unknown error has occoured.");
                                return 999;
                        }
                    case "/l":
                        switch (ListBin(args[1]))
                        {
                            case 0:
                                Console.WriteLine("Command completed successfully.");
                                return 0;
                            case 1:
                                Console.WriteLine("Specified file does not exist.");
                                return 1;
                            default:
                                Console.WriteLine("Unknown error has occoured.");
                                return 999;
                        }
                    case "/le":
                        switch (ListBin(args[1], true))
                        {
                            case 0:
                                Console.WriteLine("Command completed successfully.");
                                return 0;
                            case 1:
                                Console.WriteLine("Specified file does not exist.");
                                return 1;
                            default:
                                Console.WriteLine("Unknown error has occoured.");
                                return 999;
                        }
                    case "/f":
                        switch (CreateFolder(args[1], args[2]))
                        {
                            case 0:
                                Console.WriteLine("Command completed successfully.");
                                return 0;
                            case 1:
                                Console.WriteLine("Cannot continue. Must overwrite file to create folder");
                                return 1;
                            default:
                                Console.WriteLine("Unknown error has occoured.");
                                return 999;
                        }
                        
                    default:
                        Console.WriteLine("The syntax of the requested command is incorrect.");
                        return 3;
                }
            }
        }

        // Full repack function
        // If you use folder packs, delete all files which you do not want to get
        // aliased by this function (a.k.a. all of the small files)
        static int RepackBin(string source, string destination)
        {
            if (File.Exists(destination))
            {
                Console.Write("Specified file already exists. Overwrite? [Y/N] ");
                ConsoleKey result = Console.ReadKey().Key;

                while (!((result == ConsoleKey.Y) || (result == ConsoleKey.N)))
                {
                    result = Console.ReadKey().Key;
                }
                Console.Write("\n");
                switch (result)
                {
                    case ConsoleKey.Y:
                        File.Delete(destination);
                        break;
                    case ConsoleKey.N:
                        return 1;
                }
            }
            Console.WriteLine("Analyzing files...");
            // To generate folder packs, use the /f switch (see CreateFolder function)
            Dictionary<string, long> folderpacks = new Dictionary<string, long>();

            // Aliases are files, which exist in subfolders, but aren't part of a
            // folder pack. The advantage is that these files are both faster to
            // write and faster to read by the game (reducing load times). The
            // disadvantage to being stored directly in root TOC is less efficient
            // space usage for smaller files (which is why folder packs exist).
            Dictionary<string, long> aliases = new Dictionary<string, long>();

            // Actual files, which exist in the root directory
            Dictionary<string, long> files = new Dictionary<string, long>();

            // Set cluster size. Do not change this, unless you know exactly what
            // you're doing! 2048 is the one that works and is strongly recommended!
            int clst = 2048;

            // variable for storing end of file byte
            long eof = 0;

            // variable for storing end of TOC data
            long eotoc = 64;

            // Gather information about filenames and sizes
            foreach (FileInfo fi in new DirectoryInfo(source).EnumerateFiles())
            {
                files[fi.Name.ToUpper()] = SetSizeOnBin(fi.Length, clst);
                eotoc += 64;
                eof += SetSizeOnBin(fi.Length, clst);
            }

            // Gather information about file aliases and folder packs
            // Store their sizes
            foreach (DirectoryInfo di in new DirectoryInfo(source).EnumerateDirectories())
            {
                foreach (FileInfo fi in new DirectoryInfo(di.FullName).EnumerateFiles())
                {
                    if (fi.Name == "A")
                    {
                        folderpacks[di.Name.ToUpper() + "\\"] = SetSizeOnBin(fi.Length, clst);
                        eotoc += 64;
                        eof += SetSizeOnBin(fi.Length, clst);
                    }
                    else
                    {
                        aliases[di.Name.ToUpper() + "\\" + fi.Name.ToUpper()] = SetSizeOnBin(fi.Length, clst);
                        eotoc += 64;
                        eof += SetSizeOnBin(fi.Length, clst);
                    }
                }
            }
            eotoc += 64;
            // add TOC size to calculate the actual end of file
            eof += eotoc;
            Console.WriteLine(string.Format("Ready to write {0} of data", UserFriendlyFileSize(eof)));

            // TOC constructor
            // Write speed 64 bytes per cycle (makes code easier to write)
            Console.WriteLine("Constructing TOC...");

            Encoding ascii = Encoding.ASCII;
            eotoc = SetSizeOnBin(eotoc, clst);
            // TOC header
            using (var stream = new FileStream(destination, FileMode.Append))
            {
                byte[] buffer = new byte[64];
                List<byte> bytes = new List<byte>();
                string filename = "*Top Of CD Data";
                bytes.AddRange(ascii.GetBytes(filename));
                for (int i = 0; i < 60 - filename.Length; i++)
                {
                    bytes.Add(0x00);
                }
                bytes.AddRange(BitConverter.GetBytes((uint)RoundUp(eotoc / 2048.0, 0)));
                buffer = bytes.ToArray();
                stream.Write(buffer, 0, buffer.Length);
            }

            long offset = eotoc;
            // File system

            // Folders and aliases
            foreach (KeyValuePair<string, long> kvp in folderpacks)
            {
                byte[] buffer = new byte[64];
                List<byte> bytes = new List<byte>();
                string filename = kvp.Key;
                bytes.AddRange(ascii.GetBytes(filename));
                for (int i = 0; i < 60 - filename.Length; i++)
                {
                    bytes.Add(0x00);
                }
                bytes.AddRange(BitConverter.GetBytes((uint)RoundUp(offset / 2048.0, 0)));
                AppendData(bytes.ToArray(), destination);
                offset += kvp.Value;
                foreach (KeyValuePair<string, long> skvp in aliases)
                {
                    if (skvp.Key.StartsWith(kvp.Key))
                    {
                        buffer = new byte[64];
                        bytes.Clear();
                        filename = skvp.Key;
                        bytes.AddRange(ascii.GetBytes(filename));
                        for (int i = 0; i < 60 - filename.Length; i++)
                        {
                            bytes.Add(0x00);
                        }
                        bytes.AddRange(BitConverter.GetBytes((uint)RoundUp(offset / 2048.0, 0)));
                        AppendData(bytes.ToArray(), destination);
                        offset += skvp.Value;
                    }
                }
            }

            // Files
            foreach (KeyValuePair<string, long> kvp in files)
            {
                byte[] buffer = new byte[64];
                List<byte> bytes = new List<byte>();
                string filename = kvp.Key;
                bytes.AddRange(ascii.GetBytes(filename));
                for (int i = 0; i < 60 - filename.Length; i++)
                {
                    bytes.Add(0x00);
                }
                bytes.AddRange(BitConverter.GetBytes((uint)RoundUp(offset / 2048.0, 0)));
                AppendData(bytes.ToArray(), destination);
                offset += kvp.Value;
            }

            // TOC footer
            using (var stream = new FileStream(destination, FileMode.Append))
            {
                byte[] buffer = new byte[64];
                List<byte> bytes = new List<byte>();
                string filename = "*End Of CD Data";
                bytes.AddRange(ascii.GetBytes(filename));
                for (int i = 0; i < 60 - filename.Length; i++)
                {
                    bytes.Add(0x00);
                }
                bytes.AddRange(BitConverter.GetBytes((uint)RoundUp(eof / 2048.0, 0))); ;
                buffer = bytes.ToArray();
                stream.Write(buffer, 0, buffer.Length);
            }
            Console.WriteLine("TOC written!");
            List<byte> nulls = new List<byte>();
            offset = new FileInfo(destination).Length;
            int pad = 0;
            while ((offset % clst > 0) || (offset < clst))
            {
                pad += 1;
                offset += 1;
            }
            if (pad > 0)
            {
                Console.WriteLine(string.Format("Appending {0} null bytes...", pad));
            }
            AppendData(new byte[pad], destination);
            Console.WriteLine("Writing subfolders...");
            foreach (KeyValuePair<string, long> kvp in folderpacks)
            {
                Console.WriteLine(string.Format("\tWriting folder {0} ({1})", kvp.Key, UserFriendlyFileSize(kvp.Value)));
                using (Stream src = File.OpenRead(source + "/" + kvp.Key + "/A"))
                {
                    byte[] buffer = new byte[2048];
                    while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        AppendData(buffer, destination);
                    }
                }
                foreach (KeyValuePair<string, long> skvp in aliases)
                {
                    if (skvp.Key.StartsWith(kvp.Key))
                    {
                        Console.WriteLine(string.Format("\tWriting alias {0} ({1})", skvp.Key, UserFriendlyFileSize(skvp.Value)));
                        using (Stream src = File.OpenRead(source + "/" + skvp.Key))
                        {
                            byte[] buffer = new byte[2048];
                            while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                AppendData(buffer, destination);
                            }
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, long> kvp in files)
            {
                Console.WriteLine(string.Format("\tWriting file {0} ({1})", kvp.Key, UserFriendlyFileSize(kvp.Value)));
                using (Stream src = File.OpenRead(source + "/" + kvp.Key))
                {
                    byte[] buffer = new byte[2048];
                    while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        AppendData(buffer, destination);
                    }
                }
            }
            Console.WriteLine("Done!");
            return 0;

        }

        // RoundUp is a general function
        static double RoundUp(double number, int decimalPlaces)
        {
            return Math.Ceiling(number * Math.Pow(10, decimalPlaces)) / Math.Pow(10, decimalPlaces);
        }

        // AppendData is a general function
        // Appends certain amount of bytes to a file
        // The using statement is used so that the file is closed properly after the
        // write
        static void AppendData(byte[] bytes, string destination)
        {
            using (var stream = new FileStream(destination, FileMode.Append))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        // Convert bytes to a user friendly display, which can be displayed on the console log
        // The maximum size is in gibibytes, since that's the biggest unit used in the context
        // of PS2 game DVDs
        static string UserFriendlyFileSize(long bytes)
        {
            if (bytes >= Math.Pow(2, 30)) { return string.Format("{0} GiB", Math.Round(bytes / Math.Pow(2, 30), 2)); }
            else if (bytes >= Math.Pow(2, 20)) { return string.Format("{0} MiB", Math.Round(bytes / Math.Pow(2, 20), 2)); }
            else if (bytes >= Math.Pow(2, 10)) { return string.Format("{0} kiB", Math.Round(bytes / 1024.0, 2)); }
            else { return string.Format("{0} bytes", bytes); }
        }

        // This converts file sizes to align with cluster sizes.
        // Cluster size defines the minimum possible file size
        // and the number of bytes, which the memory in TOC is
        // addressed by. Taken from a MSDN social forums post.
        static long SetSizeOnBin(long length, int cluster)
        {
            return cluster * ((length + cluster - 1) / cluster);
        }

        // Create folder pack function
        static int CreateFolder(string source, string destination)
        {
            if (File.Exists(destination))
            {
                Console.Write("Specified file already exists. Overwrite? [Y/N] ");
                ConsoleKey result = Console.ReadKey().Key;
                while (!((result == ConsoleKey.Y) || (result == ConsoleKey.N)))
                {
                    result = Console.ReadKey().Key;
                }
                if (result == ConsoleKey.Y)
                {
                    Console.Write("\n");
                    File.Delete(destination);

                }
                else if (result == ConsoleKey.N)
                {
                    Console.Write("\n");
                    return 1;
                }
            }
            Console.WriteLine("Analyzing directory...");
            int toc_length = 0;
            long eof = 0;
            Dictionary<string, long> fs_entries = new Dictionary<string, long>();
            foreach (FileInfo fi in new DirectoryInfo(source).EnumerateFiles())
            {
                if (fi.Name != destination.Split('/')[^1])
                {
                    fs_entries[fi.Name] = fi.Length;
                    toc_length += 64;
                    eof += fi.Length;
                }
            }
            toc_length += 64;
            eof += toc_length;
            Console.WriteLine("Writing TOC data...");
            byte[] entry = new byte[64];
            long sum_of_bytes = 0;
            foreach (KeyValuePair<string, long> record in fs_entries)
            {
                Console.WriteLine(string.Format("\t{0}, offset = {1}", record.Key, sum_of_bytes.ToString("X")));
                List<byte> blist = new List<byte>();
                int nulllength = 60 - record.Key.Length;
                for (int i = 0; i < record.Key.Length; i++) {
                    blist.Add((byte)record.Key[i]);
                }
                for (int i = 0; i < nulllength; i++)
                {
                    blist.Add(0x00);
                }
                // use uint to make sure we get 4 bytes (a.k.a. 32 bits)
                blist.AddRange(BitConverter.GetBytes((uint)(toc_length + sum_of_bytes)));
                sum_of_bytes += record.Value;
                entry = blist.ToArray();
                using (var stream = new FileStream(destination, FileMode.Append))
                {
                    stream.Write(entry, 0, entry.Length);
                }
            }
            Console.WriteLine(string.Format("End Of Mem Data, offset = {0}", eof.ToString("X")));
            string eom = "*End Of Mem Data";
            List<byte> blist2 = new List<byte>();
            int nulllength2 = 60 - eom.Length;
            for (int i = 0; i < eom.Length; i++)
            {
                blist2.Add((byte)eom[i]);
            }
            for (int i = 0; i < nulllength2; i++)
            {
                blist2.Add(0x00);
            }
            blist2.AddRange(BitConverter.GetBytes((uint)eof));
            entry = blist2.ToArray();
            using (var stream = new FileStream(destination, FileMode.Append))
            {
                stream.Write(entry, 0, entry.Length);
            }
            Console.WriteLine("End of TOC, begin writing data...");
            foreach (KeyValuePair<string, long> file in fs_entries)
            {
                Console.WriteLine(string.Format("\tWriting {0}... ({1})", file.Key, UserFriendlyFileSize(eof)));
                using (Stream src = File.OpenRead(source + "/" + file.Key))
                {
                    byte[] buffer = new byte[2048];
                    int offset = 0;
                    while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        using (var stream = new FileStream(destination, FileMode.Append))
                        {
                            stream.Write(buffer, 0, offset);
                        }
                    }
                }
            }
            Console.WriteLine(string.Format("Finished writing data. Total size: {0}", UserFriendlyFileSize(eof)));
            return 0;
        }

        static Dictionary<string, long> GetSubEntries(string source)
        {
            Dictionary<string, long> fsentries = new Dictionary<string, long>();
            using (Stream src = File.OpenRead(source.Replace("\\", "/")))
            {
                byte[] buffer = new byte[64];
                int offset = 0;

                while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] cache = buffer;
                    string filename = "";

                    foreach (byte b in cache[..60])
                    {
                        if (b == 0x00)
                        {
                            continue;
                        }
                        filename += Encoding.ASCII.GetString(new[] { b });
                    }
                    if (filename == "*End Of Mem Data")
                    {
                        break;
                    }
                    byte[] bytes = cache[60..];
                    long byteoffset = (long)(BitConverter.ToInt32(bytes, 0));
                    string original_filename = filename;
                    Random r = new Random();
                    int i = 1;
                    while (fsentries.ContainsKey(filename))
                    {
                        filename = original_filename + "_" + i.ToString();
                        i++;
                    }
                    fsentries[filename] = byteoffset;
                }
            }
            return fsentries;
        }

        static Dictionary<string, long> GetFsEntries(string source)
        {
            Dictionary<string, long> fsentries = new Dictionary<string, long>();
            Dictionary<string, long> folders = new Dictionary<string, long>();
            using (Stream src = File.OpenRead(source))
            {
                byte[] buffer = new byte[64];
                string filename = "";
                int offset = 0;
                long loc = 0;
                long end_of_toc = 9999;
                bool intoc = true;
                List<byte> pointer = new List<byte>();
                while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] cache = new byte[buffer.Length];
                    Buffer.BlockCopy(buffer, 0, cache, 0, buffer.Length);
                    if (intoc)
                    {
                        filename = "";
                        if (loc == end_of_toc)
                        {
                            intoc = false;
                            continue;
                        }
                        pointer.Clear();
                        foreach (byte b in cache[..60])
                        {
                            if (b == 0x00)
                            {
                                continue;
                            }
                            filename += Encoding.ASCII.GetString(new[] { b });
                        }
                        byte[] bytes = cache[60..];
                        long byteoffset = (long)(BitConverter.ToInt32(bytes, 0)) * 2048;
                        if (filename == "*Top Of CD Data")
                        {
                            end_of_toc = byteoffset;
                            continue;
                        }
                        if (filename == "*End Of CD Data")
                        {
                            intoc = false;
                        }

                        if (filename.EndsWith("\\"))
                        {
                            fsentries[filename + "A"] = byteoffset;
                        }
                        else
                        {
                            fsentries[filename] = byteoffset;
                        }
                    }
                    loc += 64;
                }
            }
            return fsentries;
        }

        static void CopyStream(Stream destination, Stream source)
        {
            int count;
            byte[] buffer = new byte[1024];
            while ((count = source.Read(buffer, 0, buffer.Length)) > 0)
                destination.Write(buffer, 0, count);
        }


        private static void CutFile(string sourceFilePath, string destinationFilePath, long startPosition, long endPosition)
        {
            Console.WriteLine("Extracting " + (endPosition - startPosition).ToString() + " bytes as " + new FileInfo(destinationFilePath).Name.ToString() + "...");
            FileMode fm = FileMode.Create;
            using (FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open))
            {
                using (FileStream destinationStream = new FileStream(destinationFilePath + ".TEMP", fm))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    // Set the position to the starting position
                    sourceStream.Seek(startPosition, SeekOrigin.Begin);

                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (destinationStream.Position + bytesRead > endPosition - startPosition + 1)
                        {
                            // Ensure we don't write more bytes than needed
                            bytesRead = (int)(endPosition - startPosition + 1 - destinationStream.Position);
                        }

                        if (bytesRead > 0)
                        {
                            destinationStream.Write(buffer, 0, bytesRead);

                            if (destinationStream.Position >= endPosition - startPosition + 1)
                            {
                                // Reached the end position, exit the loop
                                break;
                            }
                        }
                    }
                }
            }
            FileStream fs1 = File.Open(destinationFilePath, FileMode.Append);
            FileStream fs2 = File.Open(destinationFilePath + ".TEMP", FileMode.Open);
            byte[] fs2Content = new byte[fs2.Length];
            fs2.Read(fs2Content, 0, (int)fs2.Length);
            fs1.Write(fs2Content, 0, (int)fs2.Length);
            fs1.Close();
            fs2.Close();

            File.Delete(destinationFilePath + ".TEMP");
        }

        static int ListPss(string filename, bool extract = false)
        {
            Console.WriteLine("Searching for video/audio streams...");
            IDictionary<string, long> streams = new Dictionary<string, long>();
            List<string> extractCommands = new List<string>();
            using (Stream src = File.OpenRead(filename))
            {
                byte[] buffer = new byte[16];

                int offset = 0;
                int seek = 0;
                while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    // audio stream
                    if ((buffer[0] == 0x49) && (buffer[1] == 0x4E) && (buffer[2] == 0x54) && (buffer[3] == 0x00))
                    {
                        byte[] idbytes = { buffer[4], buffer[5], buffer[6], buffer[7] };
                        byte[] sizebytes = { buffer[8], buffer[9], buffer[10], buffer[11] };
                        byte[] nextpointer = { buffer[12], buffer[13], buffer[14], buffer[15] };
                        int streamID = BitConverter.ToInt32(idbytes, 0);
                        int streamSize = BitConverter.ToInt32(sizebytes, 0);
                        int gotoPointer = BitConverter.ToInt32(nextpointer, 0);
                        bool exists = false;
                        foreach (string stream in streams.Keys)
                        {
                            if (stream == "Audio " + streamID.ToString())
                            {
                                exists = true;
                            }
                        }
                        if (!exists)
                        {
                            streams.Add(new KeyValuePair<string, long>("Audio " + streamID.ToString(), streamSize));
                        } else
                        {
                            streams["Audio " + streamID.ToString()] += streamSize;
                        }
                        if (extract)
                        {
                            long startRange = seek + 0x10;
                            long endRange = startRange + streamSize - 1;
                            extractCommands.Add(filename + "," + filename + ".INT" + streamID.ToString() + "," + startRange.ToString() + "," + endRange.ToString());
                        }
                        //Console.WriteLine("Audio " + streamID.ToString() + " at " + seek.ToString("X"));
                        seek += gotoPointer + 0x10;
                        src.Seek(seek, 0);
                        continue;
                    }
                    // video stream
                    else if ((buffer[0] == 0x49) && (buffer[1] == 0x50) && (buffer[2] == 0x55) && (buffer[3] == 0x00))
                    {
                        byte[] idbytes = { buffer[4], buffer[5], buffer[6], buffer[7] };
                        byte[] sizebytes = { buffer[8], buffer[9], buffer[10], buffer[11] };
                        byte[] nextpointer = { buffer[12], buffer[13], buffer[14], buffer[15] };
                        int streamID = BitConverter.ToInt32(idbytes, 0);
                        int streamSize = BitConverter.ToInt32(sizebytes, 0);
                        int gotoPointer = BitConverter.ToInt32(nextpointer, 0);
                        bool exists = false;
                        foreach (string stream in streams.Keys)
                        {
                            if (stream == "Video " + streamID.ToString())
                            {
                                exists = true;
                            }
                        }
                        if (!exists)
                        {
                            streams.Add(new KeyValuePair<string, long>("Video " + streamID.ToString(), streamSize));
                        }
                        else
                        {
                            streams["Video " + streamID.ToString()] += streamSize;
                        }
                        if (extract)
                        {
                            long startRange = seek + 0x10;
                            long endRange = startRange + streamSize - 1;
                            extractCommands.Add(filename + "," + filename + ".IPU" + streamID.ToString() + "," + startRange.ToString() + "," + endRange.ToString());
                        }
                        //Console.WriteLine("Video " + streamID.ToString() + " at " + seek.ToString("X"));
                        seek += gotoPointer + 0x10;
                        src.Seek(seek, 0);
                        continue;
                    }
                    // end of file
                    else if ((buffer[0] == 0x45) && (buffer[1] == 0x4E) && (buffer[2] == 0x44) && (buffer[3] == 0x00))
                    {
                        break;
                    }
                    seek += 16;
                }
            }
            if (extract)
            {
                Console.WriteLine("Starting extraction process...");
                foreach (string cmd in extractCommands)
                {
                    string[] args = cmd.Split(',');
                    CutFile(args[0], args[1], Convert.ToInt64(args[2]), Convert.ToInt64(args[3]));
                }
                Console.WriteLine("Finished extraction process!");
            }
            else
            {
                Console.WriteLine("The following streams have been found:");
                foreach (KeyValuePair<string, long> kvp in streams)
                {
                    Console.WriteLine(kvp.Key + ": " + kvp.Value + " bytes");
                }
            }
            return 0;
        }

        static int ListBin(string source, bool savefile = false)
        {
            string text_output = "";
            if (!File.Exists(source))
            {
                return 1;
            }
            if (savefile)
            {
                text_output = source[..^3] + "TXT";
                if (File.Exists(text_output)) { File.Delete(text_output); }
                string filekey = "Key:                  File types:\r\nS = Subfolder entry   BD  - Sample data          TM2 - Texture file    MID - MIDI sequence (music)      XML - Menu Layout (XML format)   SVAG - Sony ADPCM Compressed wave files\r\nR = Root file entry   HD  - Sample header data   ICO - Icon file       FPD - Flipnic Path Data          MLB - Menu Layout (Binary)       TXT  - Charset group data (Text format)\r\nF = Folder entry      MSG - In-game messages     COL - Color data      FPC - Camera Position Data       CSV - Menu Layout (CSV format)   FTL  - Charset group data (Binary)\r\nA - Alias             SST - Stage or save data   LP4 - 3D Model Data   LAY - 3D Model Layout on Stage   PSS - PlayStation Stream (FMV)   LIT  - Lighting data???\r\nSCC - Dummy file\r\n";
                File.AppendAllText(text_output, filekey);
            }
            Dictionary<string, long> folders = new Dictionary<string, long>();
            using (Stream src = File.OpenRead(source))
            {
                byte[] buffer = new byte[64];
                string filename = "";
                int offset = 0;
                long loc = 0;
                long end_of_toc = 9999;
                bool intoc = true;
                List<byte> pointer = new List<byte> ();
                bool insub = false;
                string folder = "";
                long folder_loc = 0;
                while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] cache = buffer;
                    if (intoc)
                    {
                        filename = "";
                        if (loc == end_of_toc)
                        {
                            intoc = false;
                            continue;
                        }
                        pointer.Clear();
                        foreach (byte b in cache[..60])
                        {
                            if (b == 0x00)
                            {
                                continue;
                            }
                            filename += Encoding.ASCII.GetString(new[] { b });
                        }
                        byte[] bytes = cache[60..];
                        long byteoffset = (long)(BitConverter.ToInt32(bytes, 0)) * 2048;
                        if (filename == "*Top Of CD Data")
                        {
                            end_of_toc = byteoffset;
                            continue;
                        }
                        if (filename == "*End Of CD Data")
                        {
                            intoc = false;
                            continue;
                        }
                        if (filename.EndsWith("\\"))
                        {
                            folders[filename] = byteoffset;
                        }
                        string echo = string.Format("\\{0} (offset: 0x{1})", filename, byteoffset.ToString("X"));
                        if (!savefile) { Console.WriteLine(echo); }
                        else {
                            string marker = "A ---> ";
                            if (filename.EndsWith("\\"))
                            {
                                marker = "F ---> ";
                            }
                            else if (!filename.Contains("\\"))
                            {
                                marker = "R ---> ";
                            }
                            File.AppendAllText(text_output, marker +  echo + "\r\n");
                        }
                    } else if (insub)
                    {
                        int i = cache.Length - 5;
                        while (cache[i] == 0)
                            --i;
                        byte[] name = cache[..(i+1)];
                        byte[] soff = cache[60..];
                        filename = Encoding.ASCII.GetString(name);
                        long byteoffset = (long)(BitConverter.ToUInt32(soff, 0)) + folder_loc;
                        if (filename == "*End Of Mem Data")
                        {
                            insub = false;
                        } else
                        {
                            string echo = string.Format("\\{0}{1} (offset: 0x{2})", folder, filename, byteoffset.ToString("X"));
                            if (!savefile) { Console.WriteLine(echo); }
                            else { File.AppendAllText(text_output, "S ---> " + echo + "\r\n"); }
                        }
                    }
                    else
                    {
                        foreach (KeyValuePair<string, long> kvp in folders)
                        {
                            if (kvp.Value == loc)
                            {
                                insub = true;
                                folder = kvp.Key;
                                folder_loc = kvp.Value;
                                int i = cache.Length - 5;
                                while (cache[i] == 0)
                                    --i;
                                byte[] name = cache[..(i + 1)];
                                byte[] soff = cache[60..];
                                filename = Encoding.ASCII.GetString(name);
                                long byteoffset = (long)(BitConverter.ToUInt32(soff, 0)) + kvp.Value;
                                string marker = "A ---> ";
                                if (intoc == false)
                                {
                                    marker = "S ---> ";
                                }
                                else if (filename.EndsWith("\\"))
                                {
                                    marker = "F ---> ";
                                }
                                else if (!filename.Contains("\\"))
                                {
                                    marker = "R ---> ";
                                }
                                string echo = string.Format("\\{0}{1} (offset: 0x{2})", kvp.Key, filename, byteoffset.ToString("X"));
                                if (!savefile) { Console.WriteLine(echo); }
                                else { File.AppendAllText(text_output, marker + echo + "\r\n"); }
                            }
                        }
                    }

                    loc += 64;
                }
            }
            return 0;
        }

        static int ExtractFolder(string source, string destination)
        {
            Console.WriteLine(string.Format("Extracting from subfolder at {0}\\", new DirectoryInfo(source).Name));

            Console.WriteLine("Interpreting subfolder TOC data...");
            Dictionary<string, long> fs_entries = GetSubEntries(destination + "\\A");
            using (Stream src = File.OpenRead(source))
            {
                byte[] buffer = new byte[1];
                int offset = 0;
                byte[] c2;
                List<byte> content = new List<byte>();
                Console.WriteLine("Loading subfolder to memory...");
                while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    content.AddRange(buffer);
                }
                c2 = content.ToArray<byte>();
                List<long> fs_values = new List<long>();
                List<string> fs_keys = new List<string>();
                foreach (KeyValuePair<string, long> kvp in fs_entries)
                {
                    fs_values.Add(kvp.Value);
                    fs_keys.Add(kvp.Key);
                }
                for (int i = 0; i < fs_entries.Count; i++)
                {
                    long start = fs_values[i];
                    long end = content.Count;
                    if (i < fs_values.Count - 1)
                    {
                        end = fs_values[i + 1];
                    }
                    try
                    {
                        byte[] entry = new byte[end - start];
                        Console.WriteLine("Extracting {0} ({1})", fs_keys[i], UserFriendlyFileSize(end - start));
                        try
                        {
                            Buffer.BlockCopy(c2, Convert.ToInt32(start), entry, 0, (int)(end - start));
                            File.WriteAllBytes(destination + "/" + fs_keys[i], entry);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    } catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                Console.WriteLine("Disposing variables...");
                c2 = null;
                content = null;
            }
            return 0;
        }

        static int ExtractBin(string source, string destination, bool extract_subfolder = true)
        {
            if (Directory.Exists(destination))
            {
                Console.Write("Specified folder already exists. Overwrite? [Y/N] ");
                ConsoleKey result = Console.ReadKey().Key;
                while (!((result == ConsoleKey.Y) || (result == ConsoleKey.N)))
                {
                    result = Console.ReadKey().Key;
                }
                if (result == ConsoleKey.Y)
                {
                    Console.Write("\n");

                } else if (result == ConsoleKey.N)
                {
                    Console.Write("\n");
                    return 1;
                }
            }
            Console.WriteLine("Preparing...");
            Console.WriteLine("Interpreting TOC data...");
            Dictionary<string, long> fs_entries = GetFsEntries(source);
            string write_to = "";
            using (Stream src = File.OpenRead(source))
            {
                byte[] buffer = new byte[2048];
                int offset = 0;
                ulong finish = 0;
                bool dnb = false;
                byte[] memory = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                List<string> afiles = new List<string>();
                string lastfile = "";
                List<byte> content = new List<byte>();
                Console.WriteLine("Loading file to memory...");
                while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    content.AddRange(buffer);
                }
                byte[] c2 = content.ToArray<byte>();
                for (long loc = 0; loc < content.Count; loc+=2048)
                {
                    byte[] entry = new byte[2048];
                    Buffer.BlockCopy(c2, Convert.ToInt32(loc), entry, 0, 2048);
                    if (!dnb)
                    {
                        if (lastfile.EndsWith("/A") && (extract_subfolder))
                        {
                            fs_entries.Remove(lastfile);
                            ExtractFolder(destination + "/" + lastfile, new FileInfo(destination + "/" + lastfile).DirectoryName);
                            File.Delete(destination + "/" + lastfile);
                            lastfile = "";
                        }
                        foreach (KeyValuePair<string, long> kvp in fs_entries)
                        {
                            if ((kvp.Value == loc) && (!kvp.Key.EndsWith("\\")))
                            {
                                ulong min = (ulong)new FileInfo(source).Length;
                                foreach (KeyValuePair<string, long> kvp2 in fs_entries)
                                {
                                    if ((kvp2.Value > kvp.Value) && ((ulong)kvp2.Value < min))
                                    {
                                        min = (ulong)kvp2.Value;
                                    }
                                }
                                if (!Directory.Exists(new FileInfo(destination + "/" + kvp.Key).DirectoryName))
                                {
                                    Console.WriteLine(string.Format("Creating folder: {0}", new FileInfo(destination + "/" + kvp.Key).DirectoryName));
                                    Directory.CreateDirectory(new FileInfo(destination + "/" + kvp.Key).DirectoryName);
                                }
                                finish = min;
                                lastfile = write_to;
                                if (!kvp.Key.EndsWith("\\A"))
                                {
                                    afiles.Add(kvp.Key);
                                    Console.WriteLine(string.Format("Extracting {0} ({1})", kvp.Key, UserFriendlyFileSize((long)finish - loc)));
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("Extracting {0} ({1})", kvp.Key[0..^1], UserFriendlyFileSize((long)finish - loc)));
                                }
                                write_to = kvp.Key.Replace("\\", "/");
                                CheckMissingDirs(kvp.Key, destination);
                                dnb = true;
                            }
                        }
                    }
                    lastfile = write_to;
                    if (dnb)
                    {
                        using (var stream = new FileStream(destination + "/" + write_to, FileMode.Append))
                        {
                            stream.Write(entry, 0, entry.Length);
                        }
                    }
                    if ((dnb) && ((ulong)loc >= finish - 2048))
                    {
                        dnb = false;
                    }
                }
                if (lastfile.EndsWith("\\A"))
                {
                    fs_entries.Remove(lastfile);
                    ExtractFolder(destination + "/" + lastfile.Replace("\\", "/"), new FileInfo(destination + "/" + lastfile.Replace("\\", "/")).DirectoryName);
                    File.Delete(destination + "/" + lastfile.Replace("\\", "/"));
                }
            }
            
            return 0;

        }

        private static void CheckMissingDirs(string dirname, string target)
        {
            if (!dirname.Contains("\\")) return;
            Directory.CreateDirectory(target + "/" + dirname.Split("\\")[0]);
        }
    }
}
