using SDL2;
using System;
using System.Collections.Generic;

namespace PokemonBattleSimulator.GameStates
{
    public abstract class GameState
    {
        public bool IsPaused;
        public bool RenderOthersWhilstActive = false;
        public bool RenderWhilstPaused = false;
        internal IntPtr Renderer;
        internal List<(IntPtr texture, SDL.SDL_Rect sRect, SDL.SDL_Rect dRect)> RenderQueue = new ();
        internal Dictionary<string, IntPtr> TextureDict = new Dictionary<string, IntPtr>();
        internal int ScreenWidth, ScreenHeight;
        public GameState(IntPtr renderer,int screenWidth,int screenHeight)
        {
            Renderer = renderer;
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
        }
        //used when class is first loaded
        public virtual void Init(string textureBasePath,params (string name, string filePath,bool isOnDefualtPath)[] FilePaths) 
        {
            foreach (var (name, filePath,isOnDefualtPath) in FilePaths)
            {
                if (isOnDefualtPath)
                {
                    TextureDict[name] = SDL_image.IMG_LoadTexture(Renderer, textureBasePath + filePath);
                }
                else
                {
                    TextureDict[name] = SDL_image.IMG_LoadTexture(Renderer, filePath);
                }
            }
        }

        //called whenever the state is paused, responsible for opening the pause menu
        public virtual void Pause()
        {
            IsPaused = true;
        }

        //called whenever pause state is exited
        public virtual void Resume()
        {
            IsPaused = false;
        }
        
        //called whenever state is not paused once per frame
        public abstract void Update(uint deltaTime);

        //called whenever state is paused once per frame
        public virtual void PausedUpdate(uint deltaTime) { }

        //called once per frame if is_current_rendering flag is true
        public virtual void Draw()
        {
            foreach (var (texture, sRect, dRect) in RenderQueue)
            {
                var tsRect = sRect;
                var tdRect = dRect;
                SDL.SDL_RenderCopy(Renderer, texture, ref tsRect, ref tdRect);
            }
            RenderQueue.Clear(); //clear so that the screen refreshes each frame
        }

        //called once when gamestate is being closed, should handle cleanup of all objects
        public virtual void Close()
        {
            foreach (IntPtr t in TextureDict.Values)
            {
                SDL.SDL_DestroyTexture(t);
            }
        }

        internal void RenderBackground()
        {
            var fullscreenRect = new SDL.SDL_Rect() { x = 0, y = 0, w = ScreenWidth, h = ScreenHeight };
            SDL.SDL_QueryTexture(TextureDict["Background"], out _, out _, out int w, out int h);
            RenderQueue.Add((TextureDict["Background"], new SDL.SDL_Rect() { w = w, h = h }, fullscreenRect));
        }

       
    }
}
