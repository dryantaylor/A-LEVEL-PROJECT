using System;
using SDL2;

namespace PokemonBattleSimulator.GameStates
{
    public class StartMenu : GameState
    {
        private bool isTransitioning = false;
        private uint transitionTime = 2000;
        private EngineFramework.Rendering.AnimationManager Logo;
        public StartMenu(IntPtr renderer, int screenWidth, int screenHeight) : base(renderer, screenWidth, screenHeight)
        {
            Init("./Assets/MainMenu/", ("Background", "Background.png",true), ("LogoStatic", "LogoStatic.png",true));
        }
        public override void Init(string textureBasePath, params (string name, string filePath,bool isOnDefualtPath)[] FilePaths)
        {
            base.Init(textureBasePath, FilePaths);
            Logo = new EngineFramework.Rendering.AnimationManager(Renderer);
            Logo.AddAnimation("Idle", new IntPtr[] { TextureDict["LogoStatic"] }, new int[] {100}, null, true);
            Logo.SetActiveAnimation("Idle");
            Logo.SetIdleAnimation("Idle");
            //Logo.AddAnimation("FadeOut", new IntPtr[] { }, new int[] { });
            var font = SDL_ttf.TTF_OpenFont(textureBasePath+"SweetChild.ttf", 30);
            TextureDict.Add("PressA",
                SDL.SDL_CreateTextureFromSurface(
                    Renderer,SDL_ttf.TTF_RenderText_Solid(font,"Press A  to Begin", 
                    new SDL.SDL_Color() { r = 255,g=255,b=255})));
            
            SDL_ttf.TTF_CloseFont(font);
            font = IntPtr.Zero;
        }
    public override void Update(uint deltaTime)
        {
            RenderBackground();

            if (!isTransitioning)
            {
                //logic
                //within files other than program
                if (Program.InputManager.InputMappings["A"])
                {
                    //play pressed Sound
                    isTransitioning = true;
                }
                //rendering
                
                RenderQueue.Add((Logo.GetNextFrame((int)deltaTime), Logo.GetSourceRect(), new SDL.SDL_Rect() { x = 411, y = 100, w = 458, h = 224 }));
                SDL.SDL_QueryTexture(TextureDict["PressA"], out _, out _, out int w, out int h);
                RenderQueue.Add((TextureDict["PressA"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect() { x = 520, y = 300, w = w, h = h }));
            }
            else
            {
                
                if (deltaTime >= transitionTime)
                {
                    this.Close();
                    //Here is where the next state will be created 
                    Program.StateManager.ClearStates(new EnterServerInfo(Renderer, Program.ScreenWidth, Program.ScreenHeight));
                    return;
                }
                transitionTime -= deltaTime;
                //rendering
                RenderQueue.Add((Logo.GetNextFrame((int)deltaTime), Logo.GetSourceRect(), new SDL.SDL_Rect() { x = 411, y = 100, w = 458, h = 224 }));
               
            }
        }
        
        public override void Close()
        {
            base.Close();
            Logo.Close();
        }
    }
}
