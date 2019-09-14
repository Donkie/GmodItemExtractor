#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

#endregion

namespace GmodItemExtractor
{
    class Program
    {
        private static string _addonsPath;

        private static string _tempPath;

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
                                addonsPath = input;
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

        private static string GetAddonPath()
        {
            string[] addons = Directory.GetFiles(_addonsPath).Where(p => Path.GetExtension(p) == ".gma").ToArray();

            Pl("Here is a list of all workshop addons I found installed:");
            for (int i = 0; i < addons.Length; i++) Pl("({0}): {1}", i, Path.GetFileNameWithoutExtension(addons[i]));

            Pl("\nType in the left-hand-side number of the addon you're interested in.");

            string addonPath;
            while (true)
            {
                string input = Console.ReadLine();
                if (int.TryParse(input, out int output))
                    if (output >= 0 && output < addons.Length)
                    {
                        addonPath = addons[output];
                        break;
                    }

                Pl("Try again.");
            }

            Pl("{0} selected.\n", Path.GetFileNameWithoutExtension(addonPath));

            return addonPath;
        }

        private static void DirSearch(string path, List<string> filesFound)
        {
            foreach (string d in Directory.GetDirectories(path))
            {
                filesFound.AddRange(Directory.GetFiles(d));
                DirSearch(d, filesFound);
            }
        }

        public static string[] RecursiveFileScan(string path)
        {
            List<string> filesFound = new List<string>();
            DirSearch(path, filesFound);
            return filesFound.ToArray();
        }

        public static string PathSnip(string longpath, string basepath)
        {
            return longpath.Substring(basepath.Length + 1);
        }

        private static GMAFileHeader[] PickModels(GMAFile file)
        {
            GMAFileHeader[] files = file.Files;
            GMAFileHeader[] workshopModels = files.Where(f => Path.GetExtension(f.Path) == ".mdl").OrderBy(f => f.Path.ToLowerInvariant()).ToArray();

            Pl("Here is a list of all models I found in this addon:");
            for (int i = 0; i < workshopModels.Length; i++) Pl("({0}): {1}", i, workshopModels[i].Path.ToLowerInvariant());

            Pl("\nType in the left-hand-side numbers of the models you're interested in, comma separated, use dash for ranges (eg: 1,3,7 or 1,3,6-9).");

            List<GMAFileHeader> pickedModels = new List<GMAFileHeader>();
            while (true)
            {
                string input = Console.ReadLine();
                if (!string.IsNullOrEmpty(input))
                {
                    string[] inputSplit = input.Split(',');
                    foreach (string snumber in inputSplit)
                    {
                        MatchCollection regexMatches = Regex.Matches(snumber, @"(\d+)-(\d+)");

                        if (int.TryParse(snumber, out int output))
                        {
                            if (output < 0 || output >= workshopModels.Length)
                            {
                                Pl($"Invalid index \"{output}\".");
                                goto PickModelsTryAgain;
                            }

                            pickedModels.Add(workshopModels[output]);
                        }
                        else if (regexMatches.Count > 0)
                        {
                            Match m = regexMatches[0];
                            int start = int.Parse(m.Groups[1].Value);
                            int end = int.Parse(m.Groups[2].Value);
                            for (int i = start; i <= end; i++)
                            {
                                pickedModels.Add(workshopModels[i]);
                            }
                        }
                        else
                        {
                            Pl($"Malformed number \"{snumber}\".");
                            goto PickModelsTryAgain;
                        }
                    }

                    break;
                }

                PickModelsTryAgain:
                Pl("Try again.");
            }

            return pickedModels.Distinct(new GMAFileHeaderComparer()).ToArray();
        }

        private static void Main(string[] args)
        {
            while (true)
            {
                if (Debugger.IsAttached)
                    RunProgram();
                else
                    try
                    {
                        RunProgram();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                        break;
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

            GetAddonPath:
            string addonPath = GetAddonPath();

            Thread.Sleep(1000);

            Pl("Extracting GMA...");
            GMAFile gmaFile = new GMAFile(addonPath);

            PickModels:
            GMAFileHeader[] pickedModels = PickModels(gmaFile);
            Pl("Picked models:");
            foreach (GMAFileHeader f in pickedModels) Pl("{0}", f.Path);

            if (!YesNo("Is this correct?"))
                goto PickModels;

            List<GMAFileHeader> filesToKeep = new List<GMAFileHeader>();
            foreach (GMAFileHeader s in pickedModels)
            {
                MDLFiles files;
                try
                {
                    //Textures
                    files = MDLInfo.GetInfo(gmaFile.GetFileData(s.FileNumber));
                }
                catch (CRCMismatchException e)
                {
                    Pl(e.Message + " - ignoring");
                    continue;
                }

                //Model files
                GMAFileHeader[] modelFiles = gmaFile.GetModelFiles(s);
                filesToKeep.AddRange(modelFiles);

                foreach (string locmatpath in files.Paths)
                {
                    string matpath = Path.Combine("materials", locmatpath);

                    foreach (string vmtname in files.FileNames)
                    {
                        string vmtpath = CleanPath(Path.Combine(matpath, vmtname + ".vmt"));

                        GMAFileHeader? file = gmaFile.GetFileByPath(vmtpath);
                        if (!file.HasValue)
                            continue;

                        filesToKeep.Add(file.Value);

                        //Finds all VTF files in this VMT file, prepends absolute path, checks if it exists and adds to the list
                        try
                        {
                            string vmtFile = Encoding.UTF8.GetString(gmaFile.GetFileData(file.Value.FileNumber));
                            filesToKeep.AddRange(
                                gmaFile.GetFilesByPaths(
                                    VMTInfo.GetTextureFiles(vmtFile)
                                )
                            );
                        }
                        catch (CRCMismatchException e)
                        {
                            Pl(e.Message + " - ignoring");
                        }
                    }
                }
            }

            filesToKeep = filesToKeep.Distinct(new GMAFileHeaderComparer()).ToList();

            string addonName = Path.GetFileNameWithoutExtension(addonPath);
            string outputdir = Path.Combine(_addonsPath, $"gie_{addonName}");

            Pl("Necessary files:");
            foreach (GMAFileHeader file in filesToKeep)
            {
                Pl(file.Path);
                string topath = Path.Combine(outputdir, file.Path);
                string topathFolder = Path.GetDirectoryName(topath);
                if (!Directory.Exists(topathFolder))
                    Directory.CreateDirectory(topathFolder);

                try
                {
                    File.WriteAllBytes(topath, gmaFile.GetFileData(file.FileNumber));
                }
                catch (CRCMismatchException e)
                {
                    Pl(e.Message + " - ignoring");
                }
            }

            gmaFile.Dispose();
            gmaFile = null;

            Pl($"Files extracted to {outputdir}!");

            goto GetAddonPath;
        }
    }
}