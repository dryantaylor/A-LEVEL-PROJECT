using System;
using SDL2;

namespace PokemonBattleSimulator.GameStates
{
    public class BattleMonSelect: GameState
    {
        private GameClasses.Trainer ThisPlayer, OppPlayer;
        private UI.ScrollableButtonsManager SelectButton;
        public BattleMonSelect(IntPtr renderer, int screenWidth,int screenHeight, GameClasses.Trainer thisPlayer, GameClasses.Trainer oppPlayer): base(renderer, screenWidth, screenHeight)
        {
            ThisPlayer = thisPlayer;
            OppPlayer = oppPlayer;
            //Whilst this would be false, setting it as such causes a massive memory leak
            base.RenderOthersWhilstActive = true;
            Init("./Assets/BattleSwap", ("Background", "./Assets/Common/menu.jpg", false));
            
        }
        public override void Init(string textureBasePath, params (string name, string filePath, bool isOnDefualtPath)[] FilePaths)
        {
            base.Init(textureBasePath, FilePaths);
            UI.Button[] buttons = new UI.Button[6];
            IntPtr font = SDL_ttf.TTF_OpenFont("./Assets/BattleFight/Pirulen.ttf",16);
            for (int i = 0; i < 6; i++)
            {
                IntPtr inactiveTexture = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOption.png");
                IntPtr activeTexture = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOptionActive.png");
                IntPtr pressedTexture = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOption.png");
                IntPtr textTexture;
                int val;
                if (ThisPlayer.Team[i] != null)
                {
                    IntPtr textSurface = SDL_ttf.TTF_RenderText_Blended(font, ThisPlayer.Team[i].Name, new SDL.SDL_Color() { r=255,g=255,b=255});
                    textTexture = SDL.SDL_CreateTextureFromSurface(Renderer, textSurface);
                    SDL.SDL_FreeSurface(textSurface);
                    val = i;
                }
                else
                {
                    textTexture = IntPtr.Zero;
                    val = -1;
                }
                SDL.SDL_QueryTexture(textTexture, out _, out _, out int w, out int h);
                buttons[i] = new UI.Button(inactiveTexture, activeTexture, pressedTexture, textTexture,
                    new SDL.SDL_Rect() { w = 300, h = 100 }, new SDL.SDL_Rect() { x = 10, y = 15, w = w, h = h }, val);

            }
            SDL_ttf.TTF_CloseFont(font);
            SelectButton = new UI.ScrollableButtonsManager(0, 0, 720, 100, 15, UI.ManagerModes.MODE_UP_TO_DOWN, buttons, 500);
            SelectButton.SetActive(true);
        }

        public override void Update(uint deltaTime)
        {
            SelectButton.Update(deltaTime);
            //LOGIC
           if (SelectButton.PressedButton > -1 && (int)SelectButton.PressedButtonObject.Value != -1)
           {
                ThisPlayer.SelectedAction = ("switching", (int)SelectButton.PressedButtonObject.Value);
                Program.StateManager.PopState();
           }
           else if (Program.InputManager.InputMappings["B"])
            {
                Program.StateManager.PopState();
            }

            //RENDERING
            RenderBackground();
            RenderQueue.AddRange(SelectButton.Draw());
        }

        public override void Close()
        {
            base.Close();
            SelectButton.Close();
        }
    }
}
