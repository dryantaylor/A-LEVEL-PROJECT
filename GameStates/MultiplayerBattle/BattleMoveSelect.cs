using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SDL2;

namespace PokemonBattleSimulator.GameStates
{
    public class BattleMoveSelect: GameState
    {
        private UI.ScrollableButtonsManager MoveSelect;
        private GameClasses.Trainer ThisPlayer;
        private GameClasses.Trainer OppPlayer;
        public BattleMoveSelect(IntPtr renderer, int screenWidth, int screenHeight, GameClasses.Trainer thisPlayer, GameClasses.Trainer oppPlayer): base(renderer,screenWidth, screenHeight)
        {

            base.RenderOthersWhilstActive = true; 
            ThisPlayer = thisPlayer;
            OppPlayer = oppPlayer;
            Init("./Assets/BattleFight");
        }
        public override void Init(string textureBasePath,params (string name, string filePath, bool isOnDefualtPath)[] FilePaths)
        {
            base.Init(textureBasePath,FilePaths);
            var buttons = new UI.Button[4];
            var NameFont = SDL_ttf.TTF_OpenFont("./Assets/BattleFight/Pirulen.ttf", 25);
            var PpFont = SDL_ttf.TTF_OpenFont(  "./Assets/BattleFight/Pirulen.ttf", 15);
            for (int i = 0; i < 4; i++)
            {
                var move = ThisPlayer.ActivePokemonObject.Moves[i];
                int[] colourRBG = Program.TypeChart.GetTypeColour(move.Type);

                IntPtr InactiveTexture = SDL_image.IMG_LoadTexture(Renderer, "./Assets/BattleFight/Move.png");
                IntPtr ActiveTexture = SDL_image.IMG_LoadTexture(Renderer, "./Assets/BattleFight/MoveActive.png");
                IntPtr PressedTexture = SDL_image.IMG_LoadTexture(Renderer, "./Assets/BattleFight/Move.png");
                SDL.SDL_QueryTexture(InactiveTexture, out uint format, out _, out int w, out int h);
                //TODO: EDIT THIS TO DIRECTLY INTERACT WITH PIXEL DATA POSSIBLY USING C++
                SDL.SDL_SetTextureColorMod(InactiveTexture, (byte)colourRBG[0],(byte) colourRBG[1],(byte) colourRBG[2]);
                SDL.SDL_SetTextureColorMod(ActiveTexture, (byte)colourRBG[0], (byte)colourRBG[1], (byte)colourRBG[2]);
                SDL.SDL_SetTextureColorMod(PressedTexture, (byte)colourRBG[0], (byte)colourRBG[1], (byte)colourRBG[2]);

                //DRAW DIFFERENT TEXTURES TO THE MOVE TEXT
                var TextSurface = SDL.SDL_CreateRGBSurface(0,300,100,32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
                //create Move name rendering
                var textNameSurface = SDL_ttf.TTF_RenderText_Blended(NameFont, move.Name, new SDL.SDL_Color() { r = 255, g = 255, b = 255 });
                var rect = new SDL.SDL_Rect() {x=20, y= 13, w = 300, h = 100 };
                SDL.SDL_BlitSurface(textNameSurface, IntPtr.Zero, TextSurface, ref rect);
                
                IntPtr PpSurface = SDL_ttf.TTF_RenderText_Blended(PpFont, $"{move.PPRemaining}/{move.MaxPP}", new SDL.SDL_Color() { r = 255, g = 255, b = 255 });
                var dstRect = new SDL.SDL_Rect() { x = 50, y = 50, w = 300, h = 100 };
                SDL.SDL_Surface surface = Marshal.PtrToStructure<SDL.SDL_Surface>(PpSurface);

                rect = new SDL.SDL_Rect() { x = 0, y = 0, w = surface.w, h = surface.h };
                SDL.SDL_BlitSurface(PpSurface, ref rect, TextSurface, ref dstRect);
                var TextTexture = SDL.SDL_CreateTextureFromSurface(Renderer, TextSurface);
                SDL.SDL_FreeSurface(TextSurface);
                SDL.SDL_FreeSurface(textNameSurface);
                SDL.SDL_FreeSurface(PpSurface);
                
                buttons[i] = new UI.Button(InactiveTexture,ActiveTexture, PressedTexture,TextTexture,
                    new SDL.SDL_Rect() { w=300,h=100},new SDL.SDL_Rect() { w=w, h=h});
            }
            MoveSelect = new UI.ScrollableButtonsManager(950, 280, 440, 100, 10, UI.ManagerModes.MODE_UP_TO_DOWN,buttons,500);
            MoveSelect.SetActive(true);
        }
        public override void Update(uint deltaTime)
        {
            MoveSelect.Update(deltaTime);
            RenderQueue.AddRange(MoveSelect.Draw());
            if (MoveSelect.PressedButton > -1)
            {
                ThisPlayer.SelectedAction = ("move", MoveSelect.PressedButton);
                Program.StateManager.PopState();
            }

        }

        public override void Close()
        {
            base.Close();
            MoveSelect.Close();
        }
    }
}
