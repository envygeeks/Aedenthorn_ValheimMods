﻿using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ServerRewards
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {


        private static string GetPackageItems(PackageInfo package, PlayerInfo player)
        {
            int choiceAdd = 0;
            bool chose = false;
            List<string> output = new List<string>();
            foreach (string item in package.items)
            {
                Dbgl($"Checking {item}");
                int choice = UnityEngine.Random.Range(1, 100);

                string[] infos = item.Split(',');
                ItemInfo info = new ItemInfo()
                {
                    name = infos[0],
                    amount = infos[1],
                    chance = infos[2],
                    type = infos[3]
                };

                if(info.type.ToLower() == "choice")
                {
                    if (chose)
                        continue;
                    choiceAdd += int.Parse(info.chance);
                    Dbgl($"Checking choice {choiceAdd} > {choice}");
                    if (choice > choiceAdd)
                        continue;
                    chose = true;
                    Dbgl($"Won choice");
                }
                else
                {
                    int chance = UnityEngine.Random.Range(1, 100);
                    Dbgl($"Checking chance {info.chance} > {chance}");
                    if (chance > int.Parse(info.chance))
                        continue;
                    Dbgl($"Won chance");
                }

                int amount = 0;
                if (info.amount.Contains("-"))
                {
                    var a = info.amount.Split('-');
                    amount = UnityEngine.Random.Range(int.Parse(a[0]), int.Parse(a[1]));
                }
                else
                    amount = int.Parse(info.amount);

                if (amount > 0)
                    output.Add(info.name + "," + amount);


                Dbgl($"Added {amount} {info.name}");
            }
            return string.Join(";", output);
        }

        private static List<PackageInfo> GetStorePackagesFromString(string storeInventory)
        {
            List<PackageInfo> packages = new List<PackageInfo>();
            foreach(string package in storeInventory.Split(';'))
            {
                string[] info = package.Split(',');
                packages.Add(new PackageInfo()
                {
                    id = info[0],
                    name = info[1],
                    type = info[2],
                    price = int.Parse(info[3])
                });
            }
            return packages;
        }

        private static List<PackageInfo> GetAllPackages()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards", "StoreInfo");
            if (!Directory.Exists(path))
            {
                Dbgl("Missing store info");
                return null;
            }
            List<PackageInfo> packages = new List<PackageInfo>();
            foreach(string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                PackageInfo pi = JsonUtility.FromJson<PackageInfo>(json);
                packages.Add(pi);
            }
            return packages;
        }

        private static PackageInfo GetPackage(string packageID)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards", "StoreInfo");
            if (!Directory.Exists(path))
            {
                Dbgl("Missing store info");
                return null;
            }
            foreach(string file in Directory.GetFiles(path, "*.json"))
            {
                string json = File.ReadAllText(file);
                PackageInfo pi = JsonUtility.FromJson<PackageInfo>(json);
                if (pi.id == packageID)
                    return pi;
            }
            return null;
        }


        private static string GetStoreInventoryString(PlayerInfo player)
        {
            List<string> packages = new List<string>();
            foreach(PackageInfo pi in GetAllPackages())
            {
                if (CanBuyPackage(ref player, pi, false, true, out string result))
                    packages.Add(pi.StoreString());
            }
            return string.Join(";", packages);
        }

        private static int GetUserCurrency(string steamID)
        {
            PlayerInfo playerInfo = GetPlayerInfo(steamID);
            if(playerInfo == null)
                Dbgl("Player info is null");

            return playerInfo != null ? playerInfo.currency : -1;
        }
        private static void AddNewPlayerInfo(ZNetPeer peer)
        {
            var steamID = (peer.m_socket as ZSteamSocket).GetPeerID();

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards");
            if (!Directory.Exists(path))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(path);
            }
            string infoPath = Path.Combine(path, "PlayerInfo");
            if (!Directory.Exists(infoPath))
            {
                Directory.CreateDirectory(infoPath);
            }
            var info = new PlayerInfo()
            {
                id = steamID.m_SteamID,
                currency = playerStartCurrency.Value
            };
            string json = JsonUtility.ToJson(info);
            string file = Path.Combine(infoPath, steamID + ".json");
            File.WriteAllText(file, json);
        }
        private static PlayerInfo GetPlayerInfo(string steamID)
        {

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards", "PlayerInfo", steamID + ".json");

            if (!File.Exists(path))
            {
                Dbgl("Player file not found");
                return null;
            }

            string infoJson = File.ReadAllText(path);
            PlayerInfo playerInfo = JsonUtility.FromJson<PlayerInfo>(infoJson);
            return playerInfo;
        }
        private static string GetSteamID(string idOrName)
        {
            if(Regex.IsMatch(idOrName, @"[^0-9]"))
            {
                var peer = ZNet.instance.GetConnectedPeers().FirstOrDefault(p => p.m_playerName == idOrName);
                idOrName = (peer.m_socket as ZSteamSocket).GetPeerID().ToString();
            }
            return idOrName;
        }
        private static List<string> GetAllPlayerIDs()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards");
            if (!Directory.Exists(path))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(path);
            }
            string infoPath = Path.Combine(path, "PlayerInfo");
            if (!Directory.Exists(infoPath))
            {
                Directory.CreateDirectory(infoPath);
            }
            var output = new List<string>();
            foreach(string file in Directory.GetFiles(infoPath, "*.json"))
            {
                output.Add(Path.GetFileNameWithoutExtension(file));
            }
            return output;
        }

        private static void WritePlayerData(PlayerInfo playerInfo)
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "ServerRewards", "PlayerInfo", playerInfo.id + ".json");
            string infoJson = JsonUtility.ToJson(playerInfo);
            File.WriteAllText(path, infoJson);
        }
        private static bool CanBuyPackage(ref PlayerInfo player, PackageInfo package, bool checkCurrency, bool checkLimit, out string result)
        {
            result = null;
            if (checkCurrency && player.currency < package.price)
            {
                result = $"Player doesn't have enough currency {player.currency}, price {package.price}";
                return false;
            }
            for(int i = 0; i < player.packages.Count; i++)
            {
                string[] info = player.packages[i].Split(',');
                if(info[0] == package.id)
                {
                    if(checkLimit && package.limit > 0 && int.Parse(info[1]) >= package.limit)
                    {
                        result = $"Player has bought more than the limit for this package.";
                        return false;
                    }
                    player.packages[i] = package.id + "," + (int.Parse(info[1]) + 1);
                    result = "Player can buy this package.";
                }

            }
            if (result == null)
            {
                player.packages.Add(package.id + ",1");
                result = $"Player can buy this package.";
            }
            return true;
        }

        private static bool AdjustCurrency(string steamID, int amount)
        {
            var peerList = ZNet.instance.GetConnectedPeers();
            foreach (var peer in peerList)
            {
                if (steamID == "all" || (peer.m_socket as ZSteamSocket).GetPeerID().ToString() == steamID)
                {
                    var playerInfo = GetPlayerInfo((peer.m_socket as ZSteamSocket).GetPeerID().ToString());
                    if (playerInfo == null)
                    {
                        playerInfo = new PlayerInfo()
                        {
                            id = (peer.m_socket as ZSteamSocket).GetPeerID().m_SteamID,
                        };
                    }
                    playerInfo.currency += amount;
                    WritePlayerData(playerInfo);
                    if (steamID != "all")
                        return true;
                }
            }
            return steamID == "all";
        }
        private static bool SetCurrency(string steamID, int amount)
        {
            var peerList = ZNet.instance.GetConnectedPeers();
            foreach (var peer in peerList)
            {
                if (steamID == "all" || (peer.m_socket as ZSteamSocket).GetPeerID().ToString() == steamID)
                {
                    var playerInfo = GetPlayerInfo((peer.m_socket as ZSteamSocket).GetPeerID().ToString());
                    if (playerInfo == null)
                    {
                        playerInfo = new PlayerInfo()
                        {
                            id = (peer.m_socket as ZSteamSocket).GetPeerID().m_SteamID,
                        };
                    }
                    playerInfo.currency = amount;
                    WritePlayerData(playerInfo);
                    if (steamID != "all")
                        return true;
                }
            }
            return steamID == "all";
        }

        private static string GivePackage(string steamID, string packageID)
        {
            PlayerInfo player = GetPlayerInfo(steamID);
            if (player == null)
                return "User not found!";
            PackageInfo pi = GetPackage(packageID);
            if (pi == null)
                return "Package not found!";

            var peer = ZNet.instance.GetConnectedPeers().Find(p => (p.m_socket as ZSteamSocket).GetPeerID().ToString() == steamID);
            if(peer == null)
                return "User not online!";

            JsonCommand sendCommand = new JsonCommand()
            {
                command = "PurchaseResult",
                currency = player.currency,
                items = GetPackageItems(pi, player)
            };
            peer.m_rpc.Invoke("SendServerRewardsJSON", new object[] { JsonUtility.ToJson(sendCommand) });
            return null;
        }

        private static void PlayEffects()
        {
            EffectList effects = new EffectList();
            List<EffectList.EffectData> effectList = new List<EffectList.EffectData>();
            for (int i = 0; i < Player.m_localPlayer.m_deathEffects.m_effectPrefabs.Length; i++)
            {
                    effectList.Add(Player.m_localPlayer.m_deathEffects.m_effectPrefabs[i]);
            }
            effects.m_effectPrefabs = effectList.ToArray();
            effects.Create(Player.m_localPlayer.transform.position, Player.m_localPlayer.transform.rotation, Player.m_localPlayer.transform, 1f);
        }
    }
}
