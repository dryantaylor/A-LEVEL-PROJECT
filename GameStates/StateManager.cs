using System.Collections.Generic;

namespace PokemonBattleSimulator.GameStates
{
    public class StateManager
    {
        public List<GameState> StateQueue = new List<GameState>();
        public StateManager()
        {

        }
        public void AddState(GameState Newstate)
        {
            StateQueue.Insert(0, Newstate);
            StateQueue[1].Pause();
        }
        public void ClearStates(GameState NewState)
        {
            foreach (GameState state in StateQueue)
            {
                state.Close();
            }
            StateQueue.Clear();
            StateQueue.Add(NewState);
        }
        public void ReplaceActiveState(GameState Newstate)
        {
            StateQueue[0].Close();
            StateQueue[0] = Newstate;
        }
        public void PopState()
        {
            StateQueue[0].Close();
            StateQueue.RemoveAt(0);
            StateQueue[0].Resume();
        }
        public void Update(uint deltaTime)
        {
            StateQueue[0].Update(deltaTime);
            for (int i = StateQueue.Count - 1; i > 0; i--) //newest states should draw atop older ones
            {
                StateQueue[i].PausedUpdate(deltaTime);
            }
        }
        public void Draw()
        {
            if (StateQueue[0].RenderOthersWhilstActive) //if the active states allows paused states to also draw
            {
                for (int i = StateQueue.Count -1; i > 0; i--) //newest states should draw atop older ones
                {
                    if (StateQueue[i].RenderWhilstPaused) // draw state if it draws whilst paused, else don't
                    {
                        StateQueue[i].Draw();
                        
                    }
                }
            }
            StateQueue[0].Draw();
        }

        public void Close()
        {
            foreach (GameState state in StateQueue)
            {
                state.Close();
            }
        }
    }

    
}
