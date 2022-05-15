using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonBattleSimulator.GameClasses
{
    public class Move
    {
        public string Name;
        public string Description;
        public int MaxPP; //number of uses
        public int PPRemaining;
        public int? Power; //not all moves will have a power so it is nullable
        public int Priority; //higher priority will go fast
        public string Type;
        public string Catagory; // "Physical"/ "Special" - determines if Atk/Def or SpAtk/SpDef gets used

        public Action<Pokemon,Pokemon> On_Move_Use; //functions shown in python code
        public Action<Pokemon,Pokemon> Before_Opponent_Move_Use;
        public Action<Pokemon,Pokemon> After_Opponent_Move_Use;
        public Action<Pokemon, Pokemon> On_End_Of_Turn;
        //engine must be created to allow a move to be executed
        public Microsoft.Scripting.Hosting.ScriptEngine PythonEngine = IronPython.Hosting.Python.CreateEngine();
        public (int lower, int upper)[] OnMoveUseRandoms, 
                                        BeforeOpponentMoveUseRandoms, 
                                        AfterOppoentMoveUseRandoms, 
                                        OnEndOfTurnRandoms;

        public Move(string MoveName, Trainer thisPlayer, Trainer oppPlayer)
        {
            var source = PythonEngine.CreateScriptSourceFromFile($"./.Content/Moves/{MoveName}.move");
            var compiled = source.Compile();
            //ADDING VARIABLES TO SCOPE
            var scope = PythonEngine.CreateScope();
            //Adding the variables I want the player to able to use will go here
            scope.SetVariable("Random", new Func<int>(thisPlayer.Random));
            scope.SetVariable("this_player", thisPlayer);
            scope.SetVariable("opp_player", oppPlayer);
            scope.SetVariable("calculate_damage", new Func<Pokemon, Pokemon, int, int>(CalculateDamage));
            compiled.Execute(scope);

            Name = scope.GetVariable<string>("Name");
            Type = scope.GetVariable<string>("Type");
            Priority = scope.GetVariable<int>("Priority");
            Catagory = scope.GetVariable<string>("Catagory");
            Power = scope.GetVariable<int?>("Power");
            MaxPP = scope.GetVariable<int>("PP");
            PPRemaining = MaxPP;
            Description = scope.GetVariable<string>("Description");

            On_Move_Use = scope.GetVariable<Action<Pokemon, Pokemon>>("on_move_use");
            Before_Opponent_Move_Use = scope.GetVariable<Action<Pokemon, Pokemon>>("before_opponent_move_use");
            After_Opponent_Move_Use = scope.GetVariable<Action<Pokemon, Pokemon>>("after_opponent_move_use");
            On_End_Of_Turn = scope.GetVariable<Action<Pokemon, Pokemon>>("on_end_of_turn");

            OnMoveUseRandoms = ToRandomTuples(scope.GetVariable("on_move_use_randoms"));
            BeforeOpponentMoveUseRandoms = ToRandomTuples(scope.GetVariable("before_target_move_use_randoms"));
            AfterOppoentMoveUseRandoms = ToRandomTuples(scope.GetVariable("after_target_move_use_randoms"));
            OnEndOfTurnRandoms = ToRandomTuples(scope.GetVariable("on_end_of_turn_randoms"));


            
        }

        public int CalculateDamage(Pokemon thisMon, Pokemon targetMon, int random)
        {
            float attack, defence;
            if (Catagory == "Physical") 
            {
                attack = thisMon.Stats["Atk"];
                defence = targetMon.Stats["Def"];
            }
            else
            {
                attack = thisMon.Stats["SpAtk"];
                defence = targetMon.Stats["SpDef"];
            }
            int critical = 1;
            //int random = new Random().Next(217, 256);
            float STAB = thisMon.Types.Contains(Type) ? 1.5f: 1 ;
            float typeEffectiveness = 1;
            foreach (string defType in targetMon.Types)
            {
                typeEffectiveness *= Program.TypeChart.GetDamageModifier(Type, defType);
            }
            return(int) (( ( (22 * (int)Power * (attack / defence) )/50)+2) * (random/255f) * STAB * typeEffectiveness);
        }

        private (int, int)[] ToRandomTuples(IronPython.Runtime.PythonList list)
        {
            List<(int, int)> l = new List<(int, int)>();

            foreach (IronPython.Runtime.PythonTuple tuple in list)
            {
                l.Add(((int)tuple[0], (int)tuple[1]));
            }
            return l.ToArray();
        }
    }
}
