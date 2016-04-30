using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace GmodItemExtractor
{
    internal class Program
    {

        private static string _addonsPath;
        private static string _gmoshPath;

        public static void P(string s, params object[] param)
        {
            Console.Write(s, param);
        }

        public static void Pl(string s = "", params object[] param)
        {
            Console.WriteLine(s, param);
        }

        public static bool YesNo(string question)
        {
            while (true)
            {
                Pl("{0} (Y/N)", question);

                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                    continue;

                switch (input.ToLower()[0])
                {
                    case 'y':
                        return true;
                    case 'n':
                        return false;
                }
            }
        }

        public static string CleanPath(string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            path = path.Trim(' ', '"', ';', ',');
            return path;
        }

        private static string StartupAddonsPath()
        {
            string addonsPath = Properties.Settings.Default.addonspath;
            if (!Directory.Exists(addonsPath))
            {
                Pl("First off, you need to tell me the path to your garrysmod's addons folder. (C:\\etcetc\\garrysmod\\addons)");
                while (true)
                {
                    string input = Console.ReadLine();
                    if (input != null)
                    {
                        input = CleanPath(input);

                        Pl("Parsed path\n{0}", input);
                        if (YesNo("Is this correct?"))
                        {
                            if (Directory.Exists(input))
                            {
                                Properties.Settings.Default.addonspath = input;
                                Properties.Settings.Default.Save();
                                break;
                            }

                            Pl("Invalid directory!");
                        }
                    }

                    Pl("Try again.");
                }
            }
            else
            {
                Pl("Addons path set to\n{0}", addonsPath);
            }

            return addonsPath;
        }

        private static string StartupGmoshPath()
        {
            string gmoshPath = Properties.Settings.Default.gmoshpath;
            if (!File.Exists(gmoshPath))
            {
                Pl("This program depends on GMosh. Download here: https://github.com/FPtje/gmosh When downloaded, type in the link to the exe here. (C:\\Program Files\\gmosh\\bin\\gmosh.exe)");
                while (true)
                {
                    string input = Console.ReadLine();
                    if (input != null)
                    {
                        input = CleanPath(input);

                        Pl("Parsed path\n{0}", input);
                        if (YesNo("Is this correct?"))
                        {
                            if (File.Exists(input) && Path.GetFileName(input) == "gmosh.exe")
                            {
                                Properties.Settings.Default.gmoshpath = input;
                                Properties.Settings.Default.Save();
                                break;
                            }

                            Pl("Invalid file!");
                        }
                    }

                    Pl("Try again.");
                }
            }
            else
            {
                Pl("Gmosh path set to\n{0}", gmoshPath);
            }

            return gmoshPath;
        }

        private static string getAddonPath()
        {
            string[] addons = Directory.GetFiles(_addonsPath).Where(p => Path.GetExtension(p) == ".gma").ToArray();

            Pl("Here is a list of all workshop addons I found installed:");
            for (int i = 0; i < addons.Length; i++)
            {
                Pl("({0}): {1}", i, Path.GetFileNameWithoutExtension(addons[i]));
            }
            Pl("\nType in the left-hand-side number of the addon you're interested in.");

            string addonPath;
            while (true)
            {
                string input = Console.ReadLine();
                int output;
                if (int.TryParse(input, out output))
                {
                    if (output >= 0 && output < addons.Length)
                    {
                        addonPath = addons[output];
                        break;
                    }
                }
                Pl("Try again.");
            }
            Pl("{0} selected.\n", Path.GetFileNameWithoutExtension(addonPath));

            return addonPath;
        }

        public static Process GmoshCommand(params string[] param)
        {
            Process process = new Process
            {
                StartInfo =
                {
                    FileName = _gmoshPath,
                    Arguments = string.Join(" ", param),
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            
            StreamReader reader = process.StandardOutput;
            reader.ReadToEnd();

            process.WaitForExit();
            process.Close();

            return process;
        }

        private static List<string> _filesFound;
        private static void DirSearch(string path)
        {
            foreach (string d in Directory.GetDirectories(path))
            {
                foreach (string f in Directory.GetFiles(d))
                {
                    _filesFound.Add(f);
                }
                DirSearch(d);
            }
        }

        public static string[] RecursiveFileScan(string path)
        {
            _filesFound = new List<string>();
            DirSearch(path);
            return _filesFound.ToArray();
        }

        public static string PathSnip(string longpath, string basepath)
        {
            return longpath.Substring(basepath.Length + 1);
        }

        private static string[] PickModels()
        {
            string[] workshopFiles = RecursiveFileScan(_tempPath);
            string[] workshopModels = workshopFiles.Where(p => Path.GetExtension(p) == ".mdl").ToArray();
            Pl("Here is a list of all models I found in this addon:");
            for (int i = 0; i < workshopModels.Length; i++)
            {
                Pl("({0}): {1}", i, PathSnip(workshopModels[i], _tempPath));
            }
            Pl("\nType in the left-hand-side numbers of the models you're interested in, comma separated (eg: 1,3,7).");

            string[] pickedModels;
            while (true)
            {
                string input = Console.ReadLine();
                if (!string.IsNullOrEmpty(input))
                {
                    string[] inputSplit = input.Split(',');
                    pickedModels = new string[inputSplit.Length];
                    int i = 0;
                    foreach (string snumber in inputSplit)
                    {
                        int output;
                        if (int.TryParse(snumber, out output))
                        {
                            if (output < 0 || output >= workshopModels.Length)
                            {
                                Pl($"Invalid index \"{output}\".");
                                break;
                            }

                            pickedModels[i++] = workshopModels[output];
                        }
                        else
                        {
                            Pl($"Malformed number \"{snumber}\".");
                            break;
                        }
                    }

                    if (i == pickedModels.Length)
                        break;
                }
                Pl("Try again.");
            }

            return pickedModels;
        }

        private static string _tempPath;

        private static void Main(string[] args)
        {
            while (true)
            {
                if (Debugger.IsAttached)
                    RunProgram();
                else
                {
                    try
                    {
                        RunProgram();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        break;
                    }
                }
                if (YesNo("Do you want to quit?"))
                    break;
            }
        }
        private static void RunProgram()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), "gmoditemextractor");
            try
            {
                if (Directory.Exists(_tempPath))
                    Directory.Delete(_tempPath, true);
            }
            catch (IOException)
            {
            }

            Pl("Welcome to this tool made by Donkie.\nIt's designed to help you extract the correct model and materials related to said model from a Garrysmod workshop addon.");

            _addonsPath = StartupAddonsPath();
            Pl();
            _gmoshPath = StartupGmoshPath();
            Pl();
            
GetAddonPath:
            string addonPath = getAddonPath();

            Thread.Sleep(1000);

            Pl("Extracting GMA...");
            GmoshCommand($"-e \"{addonPath}\"", $"\"{_tempPath}\"");
            
            if (!Directory.Exists(_tempPath) || Directory.GetDirectories(_tempPath).Length == 0)
            {
                Pl("GMA Extraction failed. Try again.");
                goto GetAddonPath;
            }

PickModels:
            string[] pickedModels = PickModels();
            Pl("Picked models:");
            foreach (string s in pickedModels)
            {
                Pl("{0}", PathSnip(s, _tempPath));
            }
            if (!YesNo("Is this correct?"))
                goto PickModels;

            List<string> filesToKeep = new List<string>();
            foreach (string s in pickedModels)
            {
                //Model files
                string dirname = Path.GetDirectoryName(s);
                if (!string.IsNullOrEmpty(dirname))
                {
                    filesToKeep.AddRange(Directory.GetFiles(dirname, Path.GetFileNameWithoutExtension(s) + ".*"));
                }

                //Textures
                MDLFiles files = MDLInfo.GetInfo(s);

                foreach (string locmatpath in files.Paths)
                {
                    string matpath = Path.Combine(_tempPath, "materials", locmatpath);

                    if (!Directory.Exists(matpath))
                        continue;

                    foreach (string vmtname in files.FileNames)
                    {
                        string vmtpath = Path.Combine(matpath, vmtname + ".vmt");

                        if (!File.Exists(vmtpath))
                            continue;

                        filesToKeep.Add(vmtpath);

                        //Finds all VTF files in this VMT file, prepends absolute path, checks if it exists and adds to the list
                        filesToKeep.AddRange(VMTInfo.GetTextureFiles(File.ReadAllText(vmtpath)).Select(locvtfpath => Path.Combine(_tempPath, locvtfpath)).Where(File.Exists));
                    }
                }
            }

            //filesToKeep now contains a list of absolute paths to all necessary files. Can contain duplicates.
            Dictionary<string,bool> dict = new Dictionary<string, bool>();
            foreach (string s in filesToKeep)
                dict[s] = true;

            string addonName = Path.GetFileNameWithoutExtension(addonPath);
            string outputdir = Path.Combine(_addonsPath, $"gie_{addonName}");

            Pl("Necessary files:");
            foreach (KeyValuePair<string, bool> kv in dict)
            {
                string frompath = kv.Key;
                string relpath = PathSnip(frompath, _tempPath);
                string topath = Path.Combine(outputdir, relpath);
                string topath_folder = Path.GetDirectoryName(topath);
                Pl(relpath);

                if (!Directory.Exists(topath_folder))
                    Directory.CreateDirectory(topath_folder);

                File.Copy(frompath, topath, true);
            }

            Pl($"Files extracted to {outputdir}!");
            
        }
    }
}
