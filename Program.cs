using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
namespace FlipnicBinExtractor
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Flipnic Game Data Extractor And Repacker");
            string me = AppDomain.CurrentDomain.FriendlyName;

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

        static int RepackBin(string source, string destination)
        {
            return 0;
        }

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
                Console.WriteLine(string.Format("\tWriting {0}... ({1} KiB)", file.Key, file.Value / 1024));
                using (Stream src = File.OpenRead(source + "\\" + file.Key))
                {
                    byte[] buffer = new byte[1];
                    int offset = 0;
                    while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        using (var stream = new FileStream(destination, FileMode.Append))
                        {
                            stream.Write(buffer, 0, buffer.Length);
                        }
                    }
                }
            }
            Console.WriteLine(string.Format("Finished writing data. Total size: {0} KiB", eof / 1024));
            return 0;
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
            Dictionary<string, long> fs_entries = new Dictionary<string, long>();
            Dictionary<string, long> subfolders = new Dictionary<string, long>();
            Console.WriteLine("Interpreting TOC data...");
            bool read_toc = true;
            string write_to = "";
            bool subdirectory = false;
            bool scan_mem_toc = false;
            long folder = 0;
            long end_folder = 0;
            string prefix = "";
            long loc = 0;
            using (Stream src = File.OpenRead(source))
            {
                byte[] buffer = new byte[64];
                int offset = 0;
                long eof = 0;
                long finish = 0;
                bool dnb = false;
                byte[] memory = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


                while ((offset = src.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] entry = buffer;
                    if (loc >= finish)
                    {
                        dnb = false;
                    }
                    if (loc == end_folder)
                    {
                        subdirectory = false;
                        folder = 0;
                        end_folder = 0;
                        dnb = false;
                    }
                    if (dnb)
                    {
                        using (var stream = new FileStream(destination + "\\" + write_to, FileMode.Append))
                        {
                            stream.Write(entry, 0, entry.Length);
                        }
                    }
                    if (read_toc)
                    {
                        string filename = "";
                        long pointer = 0;
                        for (int i = 0; i < entry.Length; i++)
                        {
                            if (i < 60)
                            {
                                if (entry[i] == 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    filename = filename + Encoding.ASCII.GetString(new[] { entry[i] });
                                }
                            }
                            else
                            {
                                ushort bit = entry[i];
                                // convert to hex and back to int, because it's mathematically much easier
                                switch (i)
                                {
                                    case 60:
                                        pointer += (long)bit;
                                        break;
                                    case 61:
                                        pointer += long.Parse(bit.ToString("X") + "00", System.Globalization.NumberStyles.HexNumber);
                                        break;
                                    case 62:
                                        pointer += long.Parse(bit.ToString("X") + "0000", System.Globalization.NumberStyles.HexNumber);
                                        break;
                                    case 63:
                                        pointer += long.Parse(bit.ToString("X") + "000000", System.Globalization.NumberStyles.HexNumber);
                                        break;
                                }
                            }
                        }
                        pointer *= (long)2048;
                        if (filename == "*End Of CD Data")
                        {
                            eof = pointer;
                            Console.WriteLine("Finished reading main TOC data!");
                            Console.WriteLine("Creating directories...");
                            Directory.CreateDirectory(destination);
                            foreach (KeyValuePair<string, long> kvp in subfolders)
                            {
                                Directory.CreateDirectory(destination + "\\" + kvp.Key);
                            }
                            read_toc = false;
                            continue;
                        }
                        else if (!filename.EndsWith("\\"))
                        {
                            Console.WriteLine(string.Format("File: {0} at {1}", filename, pointer));
                            fs_entries[filename] = pointer;
                        }
                        else
                        {
                            Console.WriteLine(string.Format("Subdirectory: {0} at {1}", filename, pointer));
                            subfolders[filename] = pointer;
                        }
                        if (fs_entries.Count == 1)
                        {
                            if (filename == "*Top Of CD Data")
                            {
                                Console.WriteLine("TOC identified!");
                            }
                        }
                    }
                    else if (scan_mem_toc)
                    {
                        string filename = "";
                        long pointer = 0;
                        for (int i = 0; i < entry.Length; i++)
                        {
                            if (i < 60)
                            {
                                if (entry[i] != 0)
                                {
                                    filename += Encoding.ASCII.GetString(new[] { entry[i] });
                                }
                            }
                            else
                            {
                                ushort bit = entry[i];
                                // convert to hex and back to int, because it's mathematically much easier
                                switch (i)
                                {
                                    case 60:
                                        pointer += (long)bit;
                                        break;
                                    case 61:
                                        pointer += long.Parse(bit.ToString("X") + "00", System.Globalization.NumberStyles.HexNumber);
                                        break;
                                    case 62:
                                        pointer += long.Parse(bit.ToString("X") + "0000", System.Globalization.NumberStyles.HexNumber);
                                        break;
                                    case 63:
                                        pointer += long.Parse(bit.ToString("X") + "000000", System.Globalization.NumberStyles.HexNumber);
                                        break;
                                }
                            }
                        }
                        pointer = folder + pointer - 64;
                        if (filename == "*End Of Mem Data")
                        {
                            end_folder = pointer;
                            Console.WriteLine(string.Format("End of subdirectory at {0}", end_folder));
                            scan_mem_toc = false;
                            dnb = false;
                        } else
                        {
                            Console.WriteLine(string.Format("{0}\\{1} at {2}", prefix, filename, pointer));
                            fs_entries[(prefix + "\\" + filename).Replace("\\\\", "\\")] = pointer;
                        }
                    }
                    else
                    {
                        if (!dnb)
                        {
                            bool nextisfinish = false;
                            foreach (KeyValuePair<string, long> kvp in fs_entries)
                            {
                                if (kvp.Key == "*Top Of CD Data")
                                {
                                    continue;
                                }
                                else
                                {
                                    if (nextisfinish)
                                    {
                                        finish = kvp.Value - 1;
                                        foreach (KeyValuePair<string, long> subfldr in subfolders)
                                        {
                                            if ((subfldr.Value < finish) && (subfldr.Value > loc))
                                            {
                                                finish = subfldr.Value - 1;
                                                break;
                                            }
                                        }
                                        foreach (KeyValuePair<string, long> kvp2 in fs_entries)
                                        {
                                            if ((kvp2.Value < finish) && (kvp2.Value > loc))
                                            {
                                                finish = kvp2.Value - 1;
                                                break;
                                            }
                                        }
                                        double sizeinkb = Math.Round((double)(finish - loc) / 1024.0, 2);
                                        if (sizeinkb < 0)
                                        {
                                            sizeinkb = Math.Round((double)(eof - loc) / 1024.0, 2);
                                            finish = eof;
                                        }
                                        Console.WriteLine(string.Format("Extracting {0} ({1} KiB)", write_to, sizeinkb));
                                        nextisfinish = false;
                                        dnb = true;
                                        if (File.Exists(destination + "\\" + write_to))
                                        {
                                            int i = 1;
                                            string copy = destination + "\\" + write_to + " (" + i.ToString() + ")";
                                            while (File.Exists(copy))
                                            {
                                                i++; copy = destination + "\\" + write_to + " (" + i.ToString() + ")";
                                            }
                                            File.Move(destination + "\\" + write_to, destination + "\\" + write_to + " (" + i.ToString() + ")");
                                        }

                                        using (var stream = new FileStream(destination + "\\" + write_to, FileMode.Create))
                                        {
                                            stream.Write(memory, 0, memory.Length);
                                        }
                                        if (memory != entry)
                                        {
                                            using (var stream = new FileStream(destination + "\\" + write_to, FileMode.Append))
                                            {
                                                stream.Write(entry, 0, entry.Length);
                                            }
                                        }
                                        break;
                                    }
                                    if (loc == kvp.Value)
                                    {
                                        write_to = kvp.Key;
                                        nextisfinish = true;
                                    }
                                }
                            }
                            foreach (KeyValuePair<string, long> kvp in subfolders)
                            {
                                if (loc == kvp.Value)
                                {
                                    write_to = kvp.Key;
                                    subdirectory = true;
                                    scan_mem_toc = true;
                                    prefix = kvp.Key;
                                    folder = kvp.Value;
                                    Console.WriteLine(string.Format("Subdirectory: {0}", kvp.Key));
                                    break;
                                }
                            }
                        }
                    }
                    loc += 64;
                    memory = entry;
                }
            }
            
            return 0;

        }
    }
}
