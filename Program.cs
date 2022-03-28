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
            Console.WriteLine("Flipnic Game Data Extractor 0.0");
            string me = System.AppDomain.CurrentDomain.FriendlyName;
            if (args.Length == 0)
            {
                Console.WriteLine(string.Format("No command specified. To see all available commands, type \"{0} /?\".", me));
                return 4;
            }
            else if (args.Length < 3)
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
                if (string.Join("", args).Contains("/?"))
                {
                    string help = "Help" +
                        "\n" +
                        string.Format("\nUsage: {0} [command] [source] [destination]", me) +
                        "\n" +
                        "\ncommand     - Use the /e switch for extracting, /c switch for repacking" +
                        "\nsource      - The source file. If extracting, this must be a .BIN file. If you're repacking, make sure this points to a folder" +
                        "\ndestination - The destination folder (when extracting) or file (when repacking)" +
                        "\n" +
                        "\nExamples:" +
                        "\n\n" +
                        string.Format("\n{0} /e STR.BIN STR", me) +
                        string.Format("\n{0} /c TUTO TUTO.BIN", me) + "\n";
                    Console.Write(help);
                    return 0;
                }
                else if (args[0] == "/e")
                {
                    switch (ExtractBin(args[1], args[2]))
                    {
                        case 0:
                            Console.WriteLine("Command completed successfully.");
                            return 0;
                        case 1:
                            Console.WriteLine("Cannot continue. Must overwrite directory to extract!");
                            return 1;
                    }
                }
                else if (args[0] == "/c")
                {
                    Console.WriteLine("Repacking is not available in this version of Flipnic Game Data Extractor.");
                    return 2;
                }
                else
                {
                    Console.WriteLine("The syntax of the requested command is incorrect.");
                    return 3;
                }
            }
            return 0;
        }

        static int ExtractBin(string source, string destination)
        {
            if (Directory.Exists(destination))
            {
                Console.Write("Specified folder already exists. Overwrite? [Y/N]");
                ConsoleKey result = Console.ReadKey().Key;
                while (!((result == ConsoleKey.Y) || (result == ConsoleKey.N)))
                {
                    result = Console.ReadKey().Key;
                }
                if (result == ConsoleKey.Y)
                {
                    Console.Write(" Y\n");

                } else if (result == ConsoleKey.N)
                {
                    Console.Write(" N\n");
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
                                        finish = kvp.Value;
                                        foreach (KeyValuePair<string, long> subfldr in subfolders)
                                        {
                                            if ((subfldr.Value < finish) && (subfldr.Value > loc))
                                            {
                                                finish = subfldr.Value;
                                                break;
                                            }
                                        }
                                        foreach (KeyValuePair<string, long> kvp2 in fs_entries)
                                        {
                                            if ((kvp2.Value < finish) && (kvp2.Value > loc))
                                            {
                                                finish = kvp2.Value;
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
