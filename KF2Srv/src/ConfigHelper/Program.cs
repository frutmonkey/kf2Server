using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.IO;
using System.Text;

namespace ConfigHelper
{
    public class Program
    {
        public static void Main(string[] args)
        {

            //arg 1 web address
            string configLoc = args[0].Trim();
            //arg 2 admin Access
            string addminAccess = args[1];

            string configRoot = args[2];
            //string configRoot = @"C:\Users\walter\Documents\KF2Stuff\KF2Srv\src\ConfigHelper\bin\Debug\netcoreapp1.0\kf2\kf2server\KFGame\Config\";
            //string configRoot = @".\kf2\kf2server\KFGame\Config\";

            string rawWebTxt;
            int workingIndex;

            if (configLoc.Substring(0, 4).Equals("HTTP", StringComparison.CurrentCultureIgnoreCase))
                using (var x = new HttpClient())
                {
                    rawWebTxt = x.GetStringAsync(configLoc).Result;
                }
            else
                rawWebTxt = File.ReadAllText(configLoc);

            var engine = new List<string>(File.ReadAllLines(Path.Combine(configRoot, "PCServer-KFEngine.ini")));
            var game = new List<string>(File.ReadAllLines(Path.Combine(configRoot, "PCServer-KFGame.ini")));
            var web = new List<string>(File.ReadAllLines(Path.Combine(configRoot, "KFWeb.ini")));

            var configArr = rawWebTxt.Split('\n');

            var workshopItems = new List<string>();
            var commonConfig = new Dictionary<string, string>();

            commonConfig.Add("AdminPassword", addminAccess);

            //pull out config
            for (int i = 0; i < configArr.Length; i++)
            {
                string line = configArr[i];
                if (line.StartsWith("["))
                {
                    i++;
                    for (; i < configArr.Length; i++)
                    {
                        string innerLine = configArr[i];
                        int trash;
                        if (IsSection(innerLine)) break;

                        switch (line)
                        {
                            case "[Maps]":
                                if (innerLine.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    int start = innerLine.LastIndexOf("?id") +4;
                                    int len = innerLine.LastIndexOf('&') - start;
                                    string cleanId = innerLine.Substring(start, len);
                                    if(int.TryParse(cleanId, out trash))
                                    workshopItems.Add(cleanId);
                                }
                                else if(int.TryParse(innerLine, out trash))
                                    workshopItems.Add(innerLine);
                                break;
                            case "[ServerCommon]":
                                if (innerLine.Count((x) => x == '=') == 1)
                                    commonConfig.Add(innerLine.Substring(0, innerLine.IndexOf('=')),
                                                     innerLine.Substring(innerLine.IndexOf('=') + 1));
                                break;
                            case "[Replace-KFGame]":
                                break;
                            case "[Replace-KFEngine]":
                                break;
                            default:
                                //var ex = new NotImplementedException($"Invalid Tag {line}");
                                Console.WriteLine($"Invalid Tag {line}");
                                break;
                                //throw ex;
                        }
                    }
                    i--;
                }
            }

            //set common config
            foreach(var pair in commonConfig)
            {
                string value = pair.Value;
                string option = pair.Key;
                switch(option)
                {
                    case "AdminPassword":
                        game[IndexOf("Engine.AccessControl", "AdminPassword", game)] = $"AdminPassword={value}";
                        break;
                    case "GamePassword":
                        game[IndexOf("Engine.AccessControl", "GamePassword", game)] = $"GamePassword={value}";
                        break;
                    case "GameLength":
                        game[IndexOf("KFGame.KFGameInfo", "GameLength", game)] = $"GamePassword={value}";
                        break;
                    case "BannerLink":
                        game[IndexOf("KFGame.KFGameInfo", "ServerMOTD", game)] = $"ServerMOTD={value}";
                        break;
                    case "WebsiteLink":
                        game[IndexOf("KFGame.KFGameInfo", "WebsiteLink", game)] = $"WebsiteLink={value}";
                        break;
                    case "ServerName":
                        game[IndexOf("Engine.GameReplicationInfo", "ServerName", game)] = $"ServerName={value}";
                        break;
                    case "ShortName":
                        game[IndexOf("Engine.GameReplicationInfo", "ShortName", game)] = $"ShortName={value}";
                        break;
                    case "WebAdmin":
                        game[IndexOf("IpDrv.WebServer", "bEnabled", web)] = $"bEnabled={value}";
                        break;
                    case "CanPause":
                        game[IndexOf("Engine.GameInfo", "bAdminCanPause", game)] = $"bAdminCanPause={value}";
                        break;
                    case "MaxPlayers":
                        game[IndexOf("Engine.GameInfo", "MaxPlayers", game)] = $"MaxPlayers={value}";
                        break;
                    default:
                        //var ex = new NotImplementedException($"Invalid Tag {line}");
                        Console.WriteLine($"Invalid Tag {option}");
                        break;
                        //throw ex;
                }
            }

            workingIndex = engine.IndexOf("[OnlineSubsystemSteamworks.KFWorkshopSteamworks]");
            engine.RemoveAt(workingIndex);
            //remove old workshop options
            while(workingIndex < engine.Count)
            {
                string line = engine[workingIndex];
                if (IsSection(line)) break;
                engine.RemoveAt(workingIndex);
            }

            //replace workshop options
            engine.Add("");
            engine.Add("[OnlineSubsystemSteamworks.KFWorkshopSteamworks]");
            foreach (string map in workshopItems)
                engine.Add($"ServerSubscribedWorkshopItems={map}");
            engine.Add("");


            //find mapps
            var foundMapps = new List<FileInfo>();
            ExpandAllMapps(new DirectoryInfo("."), foundMapps);

            var newMapps = new List<string>();
            foreach(var map in foundMapps)
            {
                string name = map.Name.Substring(0, map.Name.Length - 4);
                if (!game.Any((x) => x == $"[{name} KFMapSummary]"))
                {
                    newMapps.Add(name);

                    game.Add("");
                    game.Add($"[{name} KFMapSummary]");
                    game.Add($"MapName={name}");
                    game.Add("MapAssociation=2");
                    game.Add("ScreenshotPathName=UI_MapPreview_TEX.UI_MapPreview_Placeholder");
                }
            }

            //add any new maps to the vote menue
            workingIndex = IndexOf("KFGame.KFGameInfo", "GameMapCycles", game);
            //format like:
            //GameMapCycles=(Maps=("KF-Abyss","KF-Asgard","KF-BioticsLab"))
            var activeMapps = new StringBuilder(game[workingIndex]);
            activeMapps.Remove(0, 21);
            activeMapps.Remove(activeMapps.Length - 2, 2);
            //activeMapps = activeMapps..Substring(21, activeMapps.Length - 21 - 2);

            foreach (var map in newMapps)
            {
                activeMapps.Append(",\"");
                activeMapps.Append(map);
                activeMapps.Append('"');
            }
            activeMapps.Append("))");
            activeMapps.Insert(0, "GameMapCycles=(Maps=(");

            game[workingIndex] = activeMapps.ToString();

            //write out finished files
            File.WriteAllLines(Path.Combine(configRoot, "PCServer-KFEngine.ini"), engine);
            File.WriteAllLines(Path.Combine(configRoot, "PCServer-KFGame.ini"), game);
            File.WriteAllLines(Path.Combine(configRoot, "KFWeb.ini"), web);
        }//end main

        static void ExpandAllMapps(System.IO.DirectoryInfo root, List<System.IO.FileInfo> list)
        {
            foreach (DirectoryInfo dir in root.GetDirectories())
                ExpandAllMapps(dir, list);
            list.AddRange(root.GetFiles("*.kfm"));
            //list.AddRange(root.GetFiles("*.KFM"));
        }

        static int IndexOf(string section, string option, IList<string> file)
        {
            for(int i = 0; i < file.Count;i++)
            {
                string line = file[i];
                if (IsSection(line))
                    if (line.Substring(1, line.Length - 2) == section)
                    {
                        ++i;
                        for (; i < file.Count; i++)
                        {
                            if (IsSection(file[i])) return -1;
                            if (file[i].StartsWith($"{option}=")) return i;
                        }
                    }
            }
            return -1;
        }

        static bool IsSection(string text)
        {
            return text.StartsWith("[") && text.EndsWith("]");
        }
    }
}
