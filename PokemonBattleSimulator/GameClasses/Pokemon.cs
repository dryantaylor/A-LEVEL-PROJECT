using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using SDL2;

namespace PokemonBattleSimulator.GameClasses
{
    public class Pokemon
    {
        public string Name; //DONE
        public string Gender;
        public Dictionary<string, int> Stats = new Dictionary<string, int> //DONE
        {
            {"HP", 0}, {"Atk", 0}, {"Def", 0}, {"SpAtk", 0}, {"SpDef", 0}, {"Spd", 0}
        };
        public int CurrHealth;
        public Dictionary<string, int> StatsBuffs = new Dictionary<string, int>  //DONE
        {
            {"HP", 0}, {"Atk", 0}, {"Def", 0}, {"SpAtk", 0}, {"SpDef", 0}, {"Spd", 0}, { "ACC", 0}, {"EVA",0}
        };
        public string[] Types; //DONE
        public IntPtr FrontSprite; //DONE
        public SDL.SDL_Rect FrontRect;
        public IntPtr BackSprite; //DONE
        public SDL.SDL_Rect BackRect;
        public Move[] Moves; //DONE

        public List<Action> NextTurnMoves;
        public (bool, string?, int?) IsInvinc;
        public Pokemon(Dictionary<string, dynamic> JsonParsed,IntPtr renderer,Trainer thisPlayer, Trainer oppPlayer)
        {
            //name is nickname if it has one else the species name
            Name = JsonParsed["Nickname"] != "" ? JsonParsed["Nickname"] : JsonParsed["Name"];
            
            Moves = new Move[4];
            for (int i = 0; i < JsonParsed["Moves"].Count; i++)
            {
                //need to add a toString at the end since JsonParsed value is dynamic
                Moves[i] = new Move(JsonParsed["Moves"][i].ToString(),thisPlayer,oppPlayer);
            }

            //LOAD THE POKEMON FILE OF THE POKEMON FOR STATS,TYPES AND SPRITES

            //OPEN THE POKEMON PROJECT FILE
            Dictionary<string, dynamic> ConfigJson = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(
                File.ReadAllText("./.Content/Config.pprj"));
            if (((ConfigJson["Pokemon"]).ContainsKey(JsonParsed["Name"].ToString())))
            {
                //accounting for the difference in the base path of the config file vs in this project
                string PokemonFileLoc = "./.Content/Pokemon/" + ConfigJson["Pokemon"][JsonParsed["Name"]];
                using (ZipArchive archive = ZipFile.OpenRead(PokemonFileLoc))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(".json"))
                        {
                            //open file and create a dictionary with it
                            var stream = entry.Open();
                            byte[] output = new byte[entry.Length];
                            stream.Read(output, 0, output.Length);
                            var InfoJson = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(Encoding.UTF8.GetString(output));
                            stream.Close();
                            Types = InfoJson["Types"].ToObject<string[]>();
                            //STAT CALCULATION
                            int LEVEL = 50;
                            foreach (string statName in Stats.Keys)
                            {
                                int BASE = InfoJson["Stats"][statName].ToObject<int>();
                                int IV = JsonParsed["EvIvs"][statName][1];//.ToObject<int>();
                                int EV = JsonParsed["EvIvs"][statName][0];//.ToObject<int>();
                                if (statName == "HP"){
                                    Stats["HP"] = ((2 * BASE + IV + EV / 4) * LEVEL) / 100 + LEVEL+ 5;
                                }
                                else
                                {
                                    Stats[statName] = ((2 * BASE + IV + EV/4)* LEVEL)/100 + 5;
                                }
                            }

                        }
                        else
                        {
                            IntPtr Surface;
                            unsafe 
                            {
                                //-------SPRITES---------
                                var stream = entry.Open();
                                IntPtr UnamangedMem = Marshal.AllocHGlobal((int)stream.Length);
                                while (stream.Position < stream.Length)
                                {
                                    Marshal.WriteByte(UnamangedMem, (int)stream.Position, Convert.ToByte(stream.ReadByte()));
                                }
                                IntPtr RW_ops = SDL.SDL_RWFromMem(UnamangedMem,(int) stream.Length);
                                Surface = SDL_image.IMG_Load_RW(RW_ops, 1);
                                stream.Close();
                                Marshal.FreeHGlobal(UnamangedMem);
                            }
                            if (entry.FullName.EndsWith("front.png"))
                            {
                                FrontSprite = SDL.SDL_CreateTextureFromSurface(renderer, Surface);
                                
                                SDL.SDL_QueryTexture(FrontSprite, out _, out _, out int w, out int h);
                                FrontRect = new SDL.SDL_Rect() { w = w, h = h };
                            }
                            else if (entry.FullName.EndsWith("back.png"))
                            {
                                BackSprite = SDL.SDL_CreateTextureFromSurface(renderer, Surface);
                                SDL.SDL_QueryTexture(BackSprite, out _, out _, out int w, out int h);
                                BackRect = new SDL.SDL_Rect() { w = w, h = h };
                            }
                            SDL.SDL_FreeSurface(Surface);
                        }
                        CurrHealth = Stats["HP"];
                    }
                }
            }
        }
        
        public void DealDamage(int damage)
        {
            CurrHealth = Math.Max(0, CurrHealth - damage);
        }
    }
}

