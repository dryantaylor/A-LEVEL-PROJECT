using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace PokemonBattleSimulator.GameClasses
{
    public class TypeChart
    {
        private string[] types; //Array of the types, their index in the array == their index in the effictiveness matrix
        private float[,] chart; //Matrix of attacking types and their effectiveness on defending types
                                //First int is the attacking type, second is defending type
        private int[][] colours;
        public TypeChart()
        {
            string folderLocation = "./.content/"; //base path of user generated content

            string configLocation = folderLocation+"config.tprj";
            var configtext = File.ReadAllText(configLocation); //text of type config
            
            //dictionary of all types and their file locations
            Dictionary<string, string> typeFileLocDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(configtext)["types"];
            

            types = Enumerable.Repeat("", typeFileLocDict.Count).ToArray(); //array of all type names
            int i = 0;

            foreach (var k in typeFileLocDict.Keys)
            {
                types[i] = k;
                //since the base for the location in the tprj file is where ./ == folderlocation/ it must be replaced now
                typeFileLocDict[k] = typeFileLocDict[k].Replace(".\\",folderLocation);
                i++;
            }

            chart = new float[types.Length,types.Length];
            colours = new int[types.Length][];
            for (int defendingIndex = 0; defendingIndex < types.Length; defendingIndex++)
            {
                var json = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>
                    (File.ReadAllText(typeFileLocDict[types[defendingIndex]]));

                (string[] resistances, string[] vulns, string[] immune) = 
                    (json["resistances"].ToObject<string[]>(), 
                    json["vulnrabilities"].ToObject<string[]>(), 
                    json["immunities"].ToObject<string[]>());
                
                int[] colour = json["colour"].ToObject<int[]>();
                colours[defendingIndex] = colour;
                
                for (int attackingIndex = 0; attackingIndex < types.Length; attackingIndex++)
                {
                    //get the string name from the index
                    switch (types[attackingIndex])
                    {
                        // if the attacking type is in that array
                        case var s when resistances.Contains(s):
                            chart[attackingIndex,defendingIndex] = 0.5f;
                            break;
                        case var s when vulns.Contains(s):
                            chart[attackingIndex, defendingIndex] = 2f;
                            break;
                        case var s when immune.Contains(s):
                            chart[attackingIndex, defendingIndex] = 0f;
                            break;
                        default:
                            chart[attackingIndex, defendingIndex] = 1f;
                            break;
                    }
                }
            
            }
        }

        public float GetDamageModifier(string attackingType, string defendingType)
        {
            int atkIndx = Array.IndexOf(types, attackingType);
            int defIndx = Array.IndexOf(types, defendingType);
            return chart[atkIndx, defIndx];

        }

        public int[] GetTypeColour(string type)
        {
            int index = Array.IndexOf(types, type);
            return colours[index];
        }
    }
}
