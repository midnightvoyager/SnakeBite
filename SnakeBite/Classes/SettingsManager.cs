﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SnakeBite
{
    public static class SettingsManager
    {
        public static bool DisableConflictCheck
        {
            get
            {
                return Properties.Settings.Default.DisableConflictCheck;
            }
            set
            {
                Properties.Settings.Default.DisableConflictCheck = value;
                Properties.Settings.Default.Save();
            }
        }

        public static List<string> GetModFpkFiles()
        {
            Settings settings = new Settings();
            settings.Load();
            List<string> fpkList = new List<string>();
            foreach (ModEntry mod in settings.ModEntries)
            {
                foreach (ModFpkEntry fpkFile in mod.ModFpkEntries)
                {
                    fpkList.Add(Tools.ToQarPath(fpkFile.FilePath));
                }
            }
            return fpkList;
        }

        public static List<string> GetModQarFiles(bool HideExtension = false)
        {
            Settings settings = new Settings();
            settings.Load();
            List<string> qarList = new List<string>();
            foreach (ModEntry mod in settings.ModEntries)
            {
                foreach (ModQarEntry qarFile in mod.ModQarEntries)
                {
                    string fileName;
                    if (HideExtension)
                    {
                        fileName = Tools.ToQarPath(qarFile.FilePath.Substring(0, qarFile.FilePath.IndexOf(".")));
                    }
                    else
                    {
                        fileName = Tools.ToQarPath(qarFile.FilePath);
                    }
                    qarList.Add(fileName);
                }
            }
            return qarList;
        }

        public static bool SettingsExist()
        {
            return File.Exists(ModManager.GameDir + "\\snakebite.xml");
        }

        public static void DeleteSettings()
        {
            File.Delete(ModManager.GameDir + "\\snakebite.xml");
        }

        public static void AddMod(ModEntry Mod)
        {
            Settings settings = new Settings();
            settings.Load();

            foreach (ModFpkEntry f in Mod.ModFpkEntries)
            {
                f.SourceType = FileSource.Mod;
                f.FpkFile = Tools.ToQarPath(f.FpkFile);
                f.FilePath = Tools.ToQarPath(f.FilePath);
            }

            foreach (ModQarEntry q in Mod.ModQarEntries)
            {
                q.SourceType = FileSource.Mod;
                q.FilePath = Tools.ToQarPath(q.FilePath);
            }

            settings.ModEntries.Add(Mod);
            settings.Save();
        }

        public static void RemoveMod(ModEntry Mod)
        {
            Settings settings = new Settings();
            settings.Load();
            ModEntry remMod = settings.ModEntries.Find(entry => entry.Name == Mod.Name);
            settings.ModEntries.Remove(remMod);
            settings.Save();
        }

        public static List<ModEntry> GetInstalledMods()
        {
            Settings settings = new Settings();
            settings.Load();
            return settings.ModEntries;
        }

        public static GameData GetGameData()
        {
            Settings settings = new Settings();
            settings.Load();
            return settings.GameData;
        }

        public static void SetGameData(GameData NewGameData)
        {
            Settings settings = new Settings();
            settings.Load();
            settings.GameData = NewGameData;
            settings.Save();
        }

        public static Version GetSettingsVersion()
        {
            Settings settings = new Settings();
            settings.Load();
            return settings.SbVersion.AsVersion();
        }

        public static void UpdateDatHash()
        {
            Settings settings = new Settings();
            settings.Load();

            // Hash 01.dat and update settings file
            string datHash = Tools.GetMd5Hash(ModManager.ZeroPath);
            settings.GameData.DatHash = datHash;

            settings.Save();
        }

        public static void ClearAllMods()
        {
            Settings settings = new Settings();
            settings.Load();
            settings.ModEntries = new List<ModEntry>();
            settings.Save();
        }

        internal static bool ValidateDatHash()
        {
            string datHash = Tools.GetMd5Hash(ModManager.ZeroPath);
            string hashOld = SettingsManager.GetGameData().DatHash;
            if (datHash != hashOld) return false;
            return true;
        }

        // Checks the saved InstallPath variable for the existence of MGSVTPP.exe
        public static bool ValidInstallPath
        {
            get
            {
                string installPath = Properties.Settings.Default.InstallPath;
                if (Directory.Exists(installPath))
                {
                    if (File.Exists(String.Format("{0}\\MGSVTPP.exe", installPath)))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }

    [XmlType("Settings")]
    public class Settings
    {
        [XmlElement("SbVersion")]
        public SerialVersion SbVersion { get; set; } = new SerialVersion();

        [XmlElement("GameData")]
        public GameData GameData { get; set; } = new GameData();

        [XmlArray("Mods")]
        public List<ModEntry> ModEntries { get; set; } = new List<ModEntry>();

        public void Save()
        {
            // Write settings to XML
            using (FileStream s = new FileStream(Path.Combine(ModManager.GameDir, "snakebite.xml"), FileMode.Create))
            {
                XmlSerializer x = new XmlSerializer(typeof(Settings), new[] { typeof(Settings) });
                foreach (ModEntry mod in ModEntries)
                {
                    mod.Description = mod.Description.Replace("\r\n", "\n");
                }
                SbVersion.Version = ModManager.GetSBVersion().ToString();
                x.Serialize(s, this);
            }
        }

        public void Load()
        {
            // Load settings from XML

            if (!File.Exists(ModManager.GameDir + "\\snakebite.xml"))
            {
                return;
            }

            using (FileStream s = new FileStream(Path.Combine(ModManager.GameDir, "snakebite.xml"), FileMode.Open))
            {
                XmlSerializer x = new XmlSerializer(typeof(Settings));
                Settings loaded = (Settings)x.Deserialize(s);
                GameData = loaded.GameData;
                ModEntries = loaded.ModEntries;
                SbVersion = loaded.SbVersion;
                foreach (ModEntry mod in ModEntries)
                {
                    mod.Description = mod.Description.Replace("\n", "\r\n");
                }
            }
            return;
        }
    }

    [XmlType("GameData")]
    public class GameData
    {
        public GameData()
        {
            GameQarEntries = new List<ModQarEntry>();
            GameFpkEntries = new List<ModFpkEntry>();
        }

        [XmlAttribute("DatHash")]
        public string DatHash { get; set; }

        [XmlArray("QarEntries")]
        public List<ModQarEntry> GameQarEntries { get; set; } = new List<ModQarEntry>();

        [XmlArray("FpkEntries")]
        public List<ModFpkEntry> GameFpkEntries { get; set; } = new List<ModFpkEntry>();
    }

    [XmlType("ModEntry")]
    public class ModEntry
    {
        public ModEntry()
        {
        }

        public ModEntry(string SourceFile)
        {
            ReadFromFile(SourceFile);
        }

        [XmlAttribute("Name")]
        public string Name { get; set; } = string.Empty;

        [XmlAttribute("Version")]
        public string Version { get; set; } = string.Empty;

        [XmlElement("MGSVersion")]
        public SerialVersion MGSVersion { get; set; } = new SerialVersion();

        [XmlElement("SBVersion")]
        public SerialVersion SBVersion { get; set; } = new SerialVersion();

        [XmlAttribute("Author")]
        public string Author { get; set; } = string.Empty;

        [XmlAttribute("Website")]
        public string Website { get; set; } = string.Empty;

        [XmlElement("Description")]
        public string Description { get; set; } = string.Empty;

        [XmlArray("QarEntries")]
        public List<ModQarEntry> ModQarEntries { get; set; } = new List<ModQarEntry>();

        [XmlArray("FpkEntries")]
        public List<ModFpkEntry> ModFpkEntries { get; set; } = new List<ModFpkEntry>();

        public void ReadFromFile(string Filename)
        {
            // Read mod metadata from xml

            if (!File.Exists(Filename)) return;

            XmlSerializer x = new XmlSerializer(typeof(ModEntry));
            StreamReader s = new StreamReader(Filename);
            System.Xml.XmlReader xr = System.Xml.XmlReader.Create(s);

            ModEntry loaded = (ModEntry)x.Deserialize(xr);

            Name = loaded.Name;
            Version = loaded.Version;
            MGSVersion = loaded.MGSVersion;
            SBVersion = loaded.SBVersion;
            Author = loaded.Author;
            Website = loaded.Website;
            Description = loaded.Description;

            ModQarEntries = loaded.ModQarEntries;
            ModFpkEntries = loaded.ModFpkEntries;

            s.Close();
        }

        public void SaveToFile(string Filename)
        {
            // Write mod metadata to XML

            if (File.Exists(Filename)) File.Delete(Filename);

            XmlSerializer x = new XmlSerializer(typeof(ModEntry), new[] { typeof(ModEntry) });
            StreamWriter s = new StreamWriter(Filename);
            x.Serialize(s, this);
            s.Close();
        }
    }

    public enum FileSource
    {
        System,
        Merged,
        Mod
    }

    [XmlType("QarEntry")]
    public class ModQarEntry
    {
        [XmlAttribute("Hash")]
        public ulong Hash { get; set; }

        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

        [XmlAttribute("Compressed")]
        public bool Compressed { get; set; }

        [XmlAttribute("ContentHash")]
        public string ContentHash { get; set; }

        [XmlAttribute("SourceType")]
        public FileSource SourceType { get; set; }

        [XmlAttribute("SourceName")]
        public string SourceName { get; set; }
    }

    [XmlType("FpkEntry")]
    public class ModFpkEntry
    {
        [XmlAttribute("FpkFile")]
        public string FpkFile { get; set; }

        [XmlAttribute("FilePath")]
        public string FilePath { get; set; }

        [XmlAttribute("ContentHash")]
        public string ContentHash { get; set; }

        [XmlAttribute("SourceType")]
        public FileSource SourceType { get; set; }

        [XmlAttribute("SourceName")]
        public string SourceName { get; set; }
    }
}