using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2;

namespace PokemonBattleSimulator.GameStates
{
    internal class UserServerHubMenu : GameState
    {
        private UI.ScrollableButtonsManager Options;
        private byte[] UserName;

        private string FontPath = "./Assets/EnterServerInfo/SweetChild.ttf";
        private string InactiveButtonTexturePath = "./Assets/EnterServerInfo/button.png";
        private string ActiveButtonPath = "./Assets/EnterServerInfo/buttonActive.png";
        private string PressedButtonPath = "./Assets/EnterServerInfo/buttonPress.png";
        private string PopUpFont = "./Assets/EnterServerInfo/SweetChild.ttf";
        private string PopUpBackgroundPath = "./Assets/EnterServerInfo/PopUp.Png";
        private int PopUpFontButtonTextSize = 20;
        private SDL.SDL_Color Colour;
        private int ButtonWidth = 300, ButtonHeight = 100;
        private UI.PopUp PopUp;
        internal UserServerHubMenu(IntPtr renderer, int screenWidth, int screenHeight, byte[] uname) : base(renderer, screenWidth, screenHeight)
        {
            //since this renders atop another Gamestate it needs to render the paused gamestates
            base.RenderOthersWhilstActive = true;
            UserName = uname;
            Init("");
        }
        public override void Init(string textureBasePath, params (string name, string filePath, bool isOnDefualtPath)[] FilePaths)
        {
            base.Init(textureBasePath, FilePaths);

            
            var Font = SDL_ttf.TTF_OpenFont(FontPath, 20);
            Colour = new SDL.SDL_Color() { r = 0, g = 0, b = 0 };

            UI.Button[] Buttons = new UI.Button[3];

            //check if invite has already been sent
            string text = "";
            //for some reason this doesn't work
            //BattleRequestsSent.Contains(UserName))
            //so i instead have to check if the item is in
            //the list through the get index methods
            if (Program.MainServerNetworkManager.GetIndexInRequestsSent(UserName) != -1)
            {
                text = "Invite Already Sent";
            }
            else
            {
                text = "Invite to Match";
            }
            IntPtr active = SDL_image.IMG_LoadTexture(Renderer, ActiveButtonPath);
            IntPtr inactive = SDL_image.IMG_LoadTexture(Renderer, InactiveButtonTexturePath);
            IntPtr pressed = SDL_image.IMG_LoadTexture(Renderer, PressedButtonPath);
            var surface = SDL_ttf.TTF_RenderText_Blended(Font, text, Colour);
            IntPtr Text = SDL.SDL_CreateTextureFromSurface(Renderer, surface);
            SDL.SDL_FreeSurface(surface);
            SDL.SDL_QueryTexture(Text, out _, out _, out int textW, out int textH);
            Buttons[0] = new UI.Button(inactive, active, pressed, Text, 
                new SDL.SDL_Rect() { w = ButtonWidth, h = ButtonHeight },
                
                new SDL.SDL_Rect() { x = (ButtonWidth - textW) / 2, 
                                     y = (ButtonHeight - textH) / 2, 
                                     w = textW, h = textH }, 
                                     text);
            // check if an invite has been sent

            if (Program.MainServerNetworkManager.GetIndexInReceivedRequests(UserName) != -1)
            {
                text = "Accept Battle Request";
            }
            else { text = "Awaiting Battle Request"; }
            active = SDL_image.IMG_LoadTexture(Renderer, ActiveButtonPath);
            inactive = SDL_image.IMG_LoadTexture(Renderer, InactiveButtonTexturePath);
            pressed = SDL_image.IMG_LoadTexture(Renderer, PressedButtonPath);
            Text = SDL.SDL_CreateTextureFromSurface(Renderer, SDL_ttf.TTF_RenderText_Blended(Font, text, Colour));
            SDL.SDL_QueryTexture(Text, out _, out _, out textW, out textH);
            Buttons[1] = new UI.Button(inactive, active, pressed, Text, new SDL.SDL_Rect() { w = ButtonWidth, h = ButtonHeight },
                new SDL.SDL_Rect() { x = (ButtonWidth - textW) / 2, y = (ButtonHeight - textH) / 2, w = textW, h = textH }, text);

            //back option
            text = "Back";
            active = SDL_image.IMG_LoadTexture(Renderer, ActiveButtonPath);
            inactive = SDL_image.IMG_LoadTexture(Renderer, InactiveButtonTexturePath);
            pressed = SDL_image.IMG_LoadTexture(Renderer, PressedButtonPath);
            Text = SDL.SDL_CreateTextureFromSurface(Renderer, SDL_ttf.TTF_RenderText_Blended(Font, text, Colour));
            SDL.SDL_QueryTexture(Text, out _, out _, out textW, out textH);
            Buttons[2] = new UI.Button(inactive, active, pressed, Text, new SDL.SDL_Rect() { w = ButtonWidth, h = ButtonHeight },
                new SDL.SDL_Rect() { x = (ButtonWidth - textW) / 2, y = (ButtonHeight - textH) / 2, w = textW, h = textH }, text);
            SDL_ttf.TTF_CloseFont(Font);

            Options = new UI.ScrollableButtonsManager(300, 100, ScreenHeight - 100, 100, 30, UI.ManagerModes.MODE_UP_TO_DOWN, Buttons, 1000);
            Options.SetActive(true);
        }
        public override void Update(uint deltaTime)
        {
            if (PopUp == null)
            {
                Options.Update(deltaTime);
                if (Options.PressedButton != -1)
                {
                    switch (Options.Buttons[Options.PressedButton].Value)
                    {
                        case "Back":
                            Program.StateManager.PopState();
                            return;
                        case "Invite to Match":
                            //send invite over network
                            Program.MainServerNetworkManager.SendBattleRequest(UserName);

                            IntPtr Font = SDL_ttf.TTF_OpenFont(PopUpFont, PopUpFontButtonTextSize);
                            var white = new SDL.SDL_Color() { r = 255, g = 255, b = 255 };
                            //creates a surface containing the text
                            var ButtonTextSurface = SDL_ttf.TTF_RenderText_Blended(Font, "Okay", new SDL.SDL_Color() { r = 0, b = 0, g = 0 });
                            PopUp = new UI.PopUp(Renderer, SDL_image.IMG_LoadTexture(Renderer, PopUpBackgroundPath),
                                 SDL_image.IMG_LoadTexture(Renderer, InactiveButtonTexturePath),
                                 SDL_image.IMG_LoadTexture(Renderer, ActiveButtonPath),
                                 SDL_image.IMG_LoadTexture(Renderer, PressedButtonPath),
                                 SDL.SDL_CreateTextureFromSurface(Renderer, ButtonTextSurface),
                                 "Invite Sent",
                                 $"Invite has been sent to {Encoding.UTF8.GetString(UserName)}",
                                 Font, white,
                                 Font, white,
                                 400, 110, 400, 500
                                 );
                            PopUp.PressAllowedDelayTime = 500;
                            PopUp.DismisButton.Hover(true);
                            break;
                        case "Accept Battle Request":
                            //Accept Invite Over network
                            Program.MainServerNetworkManager.AcceptBattleRequest(UserName);
                            //return back to main ServerHub GameState to handle transition to Game
                            Program.StateManager.PopState();
                            return;
                    }

                    if (Program.InputManager.InputMappings["B"])
                    {
                        Program.StateManager.PopState();
                        return;
                    }
                }
                RenderQueue.AddRange(Options.Draw());
            }
            else
            {
                PopUp.Update(deltaTime);
                RenderQueue.AddRange(Options.Draw());
                RenderQueue.AddRange(PopUp.Draw());
                if (PopUp.IsExited)
                {
                    PopUp.Close();
                    PopUp = null;
                    Close();
                    Program.StateManager.PopState();
                }
                

            }
            

        }

        public override void Close()
        {
            base.Close();
            Options.Close();
            if (PopUp != null)
            {
                PopUp.Close();
            }
        }
    }
}
