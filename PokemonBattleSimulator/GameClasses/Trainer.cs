using System;
using System.Collections.Generic;

namespace PokemonBattleSimulator.GameClasses
{
    public class Trainer
    {
        public Pokemon[] Team;
        public Pokemon ActivePokemonObject { get { return Team[ActivePokemon]; } private set { } }
        public int ActivePokemon = 0;
        public (string ActionType, int Index) SelectedAction = ("",-1);
        public Queue<int> OnMoveUseRandoms { get; private set; }
        public Queue<int> BeforeTargetMoveUseRandoms { get; private set; }
        public Queue<int> AfterTargetMoveUseRandoms { get; private set; }
        public Queue<int> OnEndOfTurnRandoms { get; private set; }
        public string ActiveFunc = "";

        public Trainer()
        {

        }
        public Trainer(List<Dictionary<string, dynamic>> team,IntPtr renderer, Trainer oppPlayer)
        {
            Init(team, renderer, oppPlayer);
            
        }
        public void Init(List<Dictionary<string, dynamic>> team, IntPtr renderer, Trainer oppPlayer)
        {
            Team = new Pokemon[6];
            //load pokemon from disk
            OnMoveUseRandoms = new Queue<int>();
            BeforeTargetMoveUseRandoms = new Queue<int>();
            AfterTargetMoveUseRandoms = new Queue<int>();
            OnEndOfTurnRandoms = new Queue<int>();

            for (int i = 0; i < team.Count; i++)
            {
                Team[i] = new Pokemon(team[i], renderer, this, oppPlayer);
            }
            
        }
        public int GetNumAlivePokemon()
        {
            int num = 0;
            foreach (Pokemon mon in Team)
            {
                if (mon.CurrHealth > 0)
                {
                    num++;
                }
            }
            return num;
        }

        public bool GetActionOverNetwork(byte[] data)
        {
            switch (data[0])
            {
                case 2:
                    SelectedAction.ActionType = "switching";
                    SelectedAction.Index = BitConverter.ToInt32(data.AsSpan()[1..5]);
                    return true;
                case 3:
                    SelectedAction.ActionType = "move";
                    //As span is used to prevent uncessary data copying (suggestion by VS2022)
                    SelectedAction.Index = BitConverter.ToInt32(data.AsSpan()[1..5]);

                    //Fill the Random Queues with data
                    OnMoveUseRandoms = new Queue<int>();
                    int readHead = 5;
                    int start = 5;
                    while (readHead < ActivePokemonObject.Moves[SelectedAction.Index].OnMoveUseRandoms.Length * 4 + start)
                    {
                        OnMoveUseRandoms.Enqueue(BitConverter.ToInt32(data.AsSpan()[readHead..(readHead+4)]));
                        readHead += 4;
                    }
                    return true;
                default: return false;
            }
        }

        public int Random()
        {
            return ActiveFunc switch
            {
                "on_move_use" => OnMoveUseRandoms.Dequeue(),
                "before_opponent_move_use" => BeforeTargetMoveUseRandoms.Dequeue(),
                "after_opponent_move_use" => AfterTargetMoveUseRandoms.Dequeue(),
                "on_end_of_turn" => OnEndOfTurnRandoms.Dequeue(),
                _ => -1,
            };
        }

        public void GenerateMoveRandoms()
        {
            //List<(int lower, int upper)> randomRanges = new List<(int lower, int upper)>();
            //get the needed randoms from the move
            var selectedMove = ActivePokemonObject.Moves[SelectedAction.Index];
            var rand = new Random();

            //OnMoveUseRandoms
            OnMoveUseRandoms = new Queue<int>();
            foreach ((int lower,int upper) r in selectedMove.OnMoveUseRandoms)
            {
                OnMoveUseRandoms.Enqueue(r.lower);
                //OnMoveUseRandoms.Enqueue(rand.Next(r.lower, r.upper));
            }
            //BeforeOpponentTurnRandoms
            BeforeTargetMoveUseRandoms = new Queue<int>();
            foreach ((int lower, int upper) r in selectedMove.BeforeOpponentMoveUseRandoms)
            {
                BeforeTargetMoveUseRandoms.Enqueue(rand.Next(r.lower, r.upper));
            }
            //after turn
            AfterTargetMoveUseRandoms = new Queue<int> { };
            foreach ((int lower, int upper) r in selectedMove.AfterOppoentMoveUseRandoms)
            {
                AfterTargetMoveUseRandoms.Enqueue(rand.Next(r.lower, r.upper));
            }
            //end of turn
            OnEndOfTurnRandoms = new Queue<int>();
            foreach ((int lower, int upper) r in selectedMove.OnEndOfTurnRandoms)
            {
                OnEndOfTurnRandoms.Enqueue(rand.Next(r.lower, r.upper));
            }
        }   

        public byte[] GenerateMoveNetworkMessage()
        {
            byte[] moveNameBytes = BitConverter.GetBytes(SelectedAction.Index);
            byte[] moveRandomBytes = new byte[(OnMoveUseRandoms.Count + BeforeTargetMoveUseRandoms.Count + AfterTargetMoveUseRandoms.Count + OnEndOfTurnRandoms.Count )* 4];
            int Writehead = 0;
            Buffer.BlockCopy(OnMoveUseRandoms.ToArray(), 0, moveRandomBytes, Writehead, OnMoveUseRandoms.Count);
            Writehead += OnMoveUseRandoms.Count;
            Buffer.BlockCopy(BeforeTargetMoveUseRandoms.ToArray(), 0, moveRandomBytes, Writehead, BeforeTargetMoveUseRandoms.Count);
            Writehead += BeforeTargetMoveUseRandoms.Count;
            Buffer.BlockCopy(AfterTargetMoveUseRandoms.ToArray(),0,moveRandomBytes,Writehead, AfterTargetMoveUseRandoms.Count);
            Writehead += AfterTargetMoveUseRandoms.Count;
            Buffer.BlockCopy(OnEndOfTurnRandoms.ToArray(),0,moveRandomBytes,Writehead,OnEndOfTurnRandoms.Count);

            var turnMessage = new byte[1 + moveNameBytes.Length + moveRandomBytes.Length];
            turnMessage[0] = 3;
            Buffer.BlockCopy(moveNameBytes, 0, turnMessage, 1, moveNameBytes.Length);
            Buffer.BlockCopy(moveRandomBytes, 0, turnMessage, 1 + moveNameBytes.Length, moveRandomBytes.Length);

            return turnMessage;
        }

        public byte[] GenerateSwapNetworkMessage()
        {
            byte[] turnMessage = new byte[5];
            turnMessage[0] = 2;
            Buffer.BlockCopy(BitConverter.GetBytes(SelectedAction.Index), 0, turnMessage, 1, 4);
            return turnMessage;
        }
    }
}
