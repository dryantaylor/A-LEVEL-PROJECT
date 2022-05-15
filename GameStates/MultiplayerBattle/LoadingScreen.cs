using System;
using System.Collections.Generic;
using System.Text;
using SDL2;
using Newtonsoft.Json;

namespace PokemonBattleSimulator.GameStates
{
    public class LoadingScreen : GameState
    {
        private EngineFramework.Rendering.AnimationManager ThrobberManager;
        private EngineFramework.Rendering.AnimationManager TextManager;
        private EngineFramework.Rendering.AnimationManager BattleFadeManager;

        internal List<Dictionary<string, dynamic>> thisTeam, oppTeam;
        public LoadingScreen(IntPtr renderer, int screenWidth, int screenHeight, List<Dictionary<string, dynamic>> thisTeam, List<Dictionary<string, dynamic>> oppTeam = null) : base(renderer, screenWidth, screenHeight)
        {
            this.thisTeam = thisTeam;
            this.oppTeam = oppTeam;
            Init("./Assets/LoadingScreen", ("Background", "./Assets/Common/menu.jpg", false));
        }
        public override void Init(string textureBasePath, params (string name, string filePath, bool isOnDefualtPath)[] FilePaths)
        {
            base.Init(textureBasePath, FilePaths);
            //Load throbber
            ThrobberManager = new EngineFramework.Rendering.AnimationManager(Renderer);

            ThrobberManager.AddAnimation("loading", "./Assets/LoadingScreen/Throbber.anim");
            ThrobberManager.SetIdleAnimation("loading");
            ThrobberManager.SetActiveAnimation("loading");
            
            //Load "Loading" text
            TextManager = new EngineFramework.Rendering.AnimationManager(Renderer);
            
            TextManager.AddAnimation("loading", "./Assets/LoadingScreen/Text.anim");
            TextManager.SetIdleAnimation("loading");
            TextManager.SetActiveAnimation("loading");

            //Load EnterBattle Animation
            BattleFadeManager = new EngineFramework.Rendering.AnimationManager(Renderer);
            int[] timings = new int[24];
            string[] fileLocs = new string[24];
            for (int i = 0; i < 24; i++)
            {
                timings[i] = 50;
                fileLocs[i] = $"./Assets/LoadingScreen/Fade/{i.ToString(format: "D3")}.png";
            }
            BattleFadeManager.AddAnimation("transition", fileLocs, timings, doesLoop: false);
            BattleFadeManager.AddAnimation("idle", new string[] { "./Assets/LoadingScreen/Fade/023.png" }, new int[] { 1000 }, doesLoop: true);
            BattleFadeManager.SetActiveAnimation("transition");
            BattleFadeManager.SetIdleAnimation("idle");
        }
public override void Update(uint deltaTime)
{
    if (oppTeam != null)
    {
        //Once/if teacm has been received play the battle intro
        if (BattleFadeManager.GetActiveAnimationName() == "idle")
        {
            //MainBattle Goes here.
            return;
        }
        RenderQueue.Add((BattleFadeManager.GetNextFrame(Convert.ToInt32(deltaTime)),BattleFadeManager.GetSourceRect(),
            new SDL.SDL_Rect() { w= ScreenWidth, h=ScreenHeight}));
    }
    else
    {
        NetworkUpdate();

        //Loading if awaiting the other player to send their team
        RenderBackground();
                
        RenderQueue.Add((ThrobberManager.GetNextFrame(Convert.ToInt32(deltaTime)),
                        ThrobberManager.GetSourceRect(), new SDL.SDL_Rect() { x = 10, y = 600, w = 128, h = 128 }));

        RenderQueue.Add((TextManager.GetNextFrame(Convert.ToInt32(deltaTime)),
                        TextManager.GetSourceRect(), new SDL.SDL_Rect() { x = 135, y = 550, w = 262, h = 225 }));
    }
}
private void NetworkUpdate()
{
    (int? responseLength, byte[] response) = Program.GameServerNetworkManager.ReceiveMessage(50);
    if (responseLength != 0 && responseLength.HasValue)
    { 
        switch (response[0])
        {
            case 1:
                oppTeam = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(Encoding.UTF8.GetString(response[1..(int)responseLength]));
                        
                break;
        }

    }
}

        public override void Close()
        {
            base.Close();
            ThrobberManager.Close();
            TextManager.Close();
            BattleFadeManager.Close();
        }
    }
}
