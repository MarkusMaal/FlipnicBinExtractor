using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
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
                    string.Format("\nUsage: {0} [/e /c /f] [source] [destination]", me) +
                    "\n" +
                    "\n/e          - Extract data from BIN file" +
                    "\n/l          - Display directory tree without extracting data" +
                    "\n/c          - Create BIN file using files in a folder. Any subfolders containing files will be aliased and the A file will be interpreted as a subfolder in TOC." +
                    "\n/f          - Create a subdirectory file, which you can use for repacking. Note that this is significantly slower, because each file is 1 byte addressed." +
                    "\nsource      - The source file. If extracting, this must be a .BIN file. If you're repacking, make sure this points to a folder." +
                    "\ndestination - The destination folder (when extracting) or file (when repacking)" +
                    "\n" +
                    "\nExamples:" +
                    "\n\n" +
                    string.Format("\n{0} /e STR.BIN STR", me) +
                    string.Format("\n{0} /f STR STR\\A", me) +
                    string.Format("\n{0} /l RES.BIN", me) +
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
            else if ((args[0].ToLower() != "/l") && (args.Length < 3))
            {
                Console.WriteLine("Not enough arguments specified!");
                return 5;
            }
            else if (args.Length > 3)
            {
                Console.WriteLine("Too many arguments specified!");
                return 6;
            }
            else
            {
                // Parse first argument
                switch (args[0].ToLower())
                {
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
                    case "/c":
                        switch (RepackBin(args[1], args[2]))
                        {
                            case 0:
                                Console.WriteLine("Command completed successfully.");
                                return 0;
                            case 1:
                                Console.WriteLine("Cannot continue. Must overwrite existing BIN file to repack!");
                                return 1;
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
                    } else
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
                bytes.AddRange(BitConverter.GetBytes((uint)RoundUp(eof / 2048.0, 0)));;
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
                using (Stream src = File.OpenRead(source + "\\" + kvp.Key + "\\A"))
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
                        using (Stream src = File.OpenRead(source + "\\" + skvp.Key))
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
                using (Stream src = File.OpenRead(source + "\\" + kvp.Key))
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

        static double RoundUp(double number, int decimalPlaces)
        {
            return Math.Ceiling(number * Math.Pow(10, decimalPlaces)) / Math.Pow(10, decimalPlaces);
        }

        static void AppendData(byte[] bytes, string destination)
        {
            using (var stream = new FileStream(destination, FileMode.Append))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

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
                if (fi.Name != destination.Split('\\')[^1])
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
                using (Stream src = File.OpenRead(source + "\\" + file.Key))
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
            try
            {
                using (Stream src = File.OpenRead(source))
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
                        byte[] bytes = cache[60..];
                        long byteoffset = (long)(BitConverter.ToInt32(bytes, 0));
                        if (filename == "*End Of Mem Data")
                        {
                            break;
                        }

                        fsentries[filename] = byteoffset;

                    }
                }
            } catch { }
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
        static int ListBin(string source)
        {
            if (!File.Exists(source))
            {
                return 1;
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
                        Console.WriteLine(string.Format("\\{0} (offset: 0x{1})", filename, byteoffset.ToString("X")));
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
                            Console.WriteLine(string.Format("\\{0}{1} (offset: 0x{2})", folder, filename, byteoffset.ToString("X")));
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
                                Console.WriteLine(string.Format("\\{0}{1} (offset: 0x{2})", kvp.Key, filename, byteoffset.ToString("X")));
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

            string write_to = "";
            long loc = -1;
            using (Stream src = File.OpenRead(source))
            {
                byte[] buffer = new byte[1];
                int offset = 0;
                bool dnb = false;
                long finish = -1;
                while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] entry = buffer;
                    if (dnb)
                    {
                        if (loc < finish - 1)
                        {
                            using (var stream = new FileStream(destination + "\\" + write_to, FileMode.Append))
                            {
                                stream.Write(entry, 0, entry.Length);
                            }
                        } else
                        {
                            dnb = false;
                            foreach (KeyValuePair<string, long> fsentry in fs_entries)
                            {
                                if (fsentry.Value == loc + 1)
                                {
                                    dnb = true;
                                    write_to = fsentry.Key;
                                    long minimum = new FileInfo(source).Length;
                                    foreach (KeyValuePair<string, long> kvp in fs_entries)
                                    {
                                        if ((kvp.Value > fsentry.Value) && (kvp.Value < minimum))
                                        {
                                            minimum = kvp.Value - 1;
                                        }
                                    }
                                    finish = minimum;
                                    Console.WriteLine("Writing {0} ({1})", fsentry.Key, UserFriendlyFileSize(finish - loc));
                                }
                            }
                            
                        }
                        if (loc < finish - 2048)
                        {
                            buffer = new byte[2048];
                        } else
                        {
                            buffer = new byte[1];
                        }
                        loc += buffer.Length;
                        continue;
                    }
                    foreach (KeyValuePair<string, long> fsentry in fs_entries)
                    {
                        if (fsentry.Value == loc + 1)
                        {
                            dnb = true;
                            write_to = fsentry.Key;
                            long minimum = new FileInfo(source).Length;
                            foreach (KeyValuePair<string, long> kvp in fs_entries)
                            {
                                if ((kvp.Value > fsentry.Value) && (kvp.Value < minimum))
                                {
                                    minimum = kvp.Value;
                                }
                            }
                            finish = minimum - 1;
                            Console.WriteLine("Extracting {0} ({1})", fsentry.Key, UserFriendlyFileSize(finish - loc));
                            using (var stream = new FileStream(destination + "\\" + write_to, FileMode.Append))
                            {
                                stream.Write(entry, 0, entry.Length);
                            }
                        }
                    }
                    loc+=buffer.Length;
                }
            }
            return 0;
        }

        static int ExtractBin(string source, string destination)
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
            long loc = 0;
            using (Stream src = File.OpenRead(source))
            {
                byte[] buffer = new byte[2048];
                int offset = 0;
                long eof = 0;
                ulong finish = 0;
                bool dnb = false;
                byte[] memory = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                List<string> afiles = new List<string>();
                string lastfile = "";
                while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] entry = buffer;
                    if (!dnb)
                    {
                        if (lastfile.EndsWith("\\A"))
                        {
                            fs_entries.Remove(lastfile);
                            ExtractFolder(destination + "\\" + lastfile, new FileInfo(destination + "\\" + lastfile).DirectoryName);
                            File.Delete(destination + "\\" + lastfile);
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
                                if (!Directory.Exists(new FileInfo(destination + "\\" + kvp.Key).DirectoryName))
                                {
                                    Console.WriteLine(string.Format("Creating folder: {0}", new FileInfo(destination + "\\" + kvp.Key).DirectoryName));
                                    Directory.CreateDirectory(new FileInfo(destination + "\\" + kvp.Key).DirectoryName);
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
                                write_to = kvp.Key;
                                dnb = true;
                            }
                        }
                    }
                    lastfile = write_to;
                    if ((dnb) && ((ulong)loc >= finish))
                    {
                        dnb = false;
                    }
                    if (dnb)
                    {
                        using (var stream = new FileStream(destination + "\\" + write_to, FileMode.Append))
                        {
                            stream.Write(entry, 0, entry.Length);
                        }
                    }

                    loc += 2048;
                }
                if (lastfile.EndsWith("\\A"))
                {
                    fs_entries.Remove(lastfile);
                    ExtractFolder(destination + "\\" + lastfile, new FileInfo(destination + "\\" + lastfile).DirectoryName);
                    File.Delete(destination + "\\" + lastfile);
                    lastfile = "";
                }
            }
            
            return 0;

        }
    }
}
