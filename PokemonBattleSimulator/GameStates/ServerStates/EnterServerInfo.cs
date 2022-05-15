using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SDL2;

namespace PokemonBattleSimulator.GameStates
{
    class EnterServerInfo : GameState
    {
        private UI.TextEnterBoxManager TextEnterBoxManager;
        private UI.Button EnterButton;
        private bool EnterHasBeenPressed;
        private bool DisplayError;
        private UI.PopUp ErrorPopUp;
        public EnterServerInfo(IntPtr renderer, int screenWidth, int screenHeight) : base(renderer, screenWidth, screenHeight) 
        {
            Init("./Assets/EnterServerInfo/", ("Background","./Assets/Common/menu.jpg",false),("TextInput","textInput.png",true));
        }
        public override void Init(string textureBasePath, params (string name, string filePath,bool isOnDefualtPath)[] FilePaths)
        {
            string ActiveBoxFilePath = textureBasePath+"textInputActive.png";
            string InactiveBoxFilePath = textureBasePath+"textInput.png";

            string InactiveButtonFilePath = textureBasePath+"button.png";
            string ActiveButtonFilePath = textureBasePath+"buttonActive.png";
            string PressedButtonFilePath = textureBasePath+"buttonpress.png";

            string ErrorBoxFilePath = textureBasePath + "PopUp.Png";
            

            SDL.SDL_Color Black = new SDL.SDL_Color { r = 0, g = 0, b = 0 };
            var white = new SDL.SDL_Color(){ r= 255,g= 255,b= 255 };

            base.Init(textureBasePath, FilePaths);

            var Font = SDL_ttf.TTF_OpenFont(textureBasePath + "SweetChild.ttf",30);
            TextureDict.Add("UserText",SDL.SDL_CreateTextureFromSurface(Renderer, SDL_ttf.TTF_RenderText_Solid(Font, "Username:", Black)));
            TextureDict.Add("IPText", SDL.SDL_CreateTextureFromSurface(Renderer, SDL_ttf.TTF_RenderText_Solid(Font, "IP:", Black)));
            TextureDict.Add("PortText", SDL.SDL_CreateTextureFromSurface(Renderer, SDL_ttf.TTF_RenderText_Solid(Font, "Port:", Black)));
            var JoinText = SDL.SDL_CreateTextureFromSurface(Renderer, SDL_ttf.TTF_RenderText_Solid(Font, "Join", Black));
            

            TextEnterBoxManager = new UI.TextEnterBoxManager(UI.ManagerModes.MODE_UP_TO_DOWN, Renderer);


            TextEnterBoxManager.AddTextBox(0,"Name",SDL_image.IMG_LoadTexture(Renderer, InactiveBoxFilePath),SDL_image.IMG_LoadTexture(Renderer, ActiveBoxFilePath),
                SDL_ttf.TTF_OpenFont(textureBasePath + "SweetChild.ttf", 35),Black,400,50,400,150);
            
            TextEnterBoxManager.AddTextBox(1, "IP", SDL_image.IMG_LoadTexture(Renderer, InactiveBoxFilePath), SDL_image.IMG_LoadTexture(Renderer, ActiveBoxFilePath),
                SDL_ttf.TTF_OpenFont(textureBasePath + "SweetChild.ttf", 35), Black, 400, 50, 400, 300,"127.0.0.1");
            TextEnterBoxManager.AddTextBox(2, "Port", SDL_image.IMG_LoadTexture(Renderer, InactiveBoxFilePath), SDL_image.IMG_LoadTexture(Renderer, ActiveBoxFilePath),
                SDL_ttf.TTF_OpenFont(textureBasePath + "SweetChild.ttf", 35), Black, 400, 50, 400, 450,"65430");

            var inactive = SDL_image.IMG_LoadTexture(Renderer,InactiveButtonFilePath);
            var active = SDL_image.IMG_LoadTexture(Renderer,ActiveButtonFilePath);
            var pressed = SDL_image.IMG_LoadTexture(Renderer, PressedButtonFilePath);
            SDL.SDL_QueryTexture(inactive, out _, out _, out int w, out int h);
            SDL.SDL_QueryTexture(JoinText, out _, out _, out int wt, out int ht);
            EnterButton = new UI.Button(inactive, active, pressed, JoinText, 
                new SDL.SDL_Rect() { x = 500, y = 550, w = (w/3) * 2, h = (h/3)*2 }, 
                new SDL.SDL_Rect() {x=550, y=570,w = wt,h = ht });

            
            ErrorPopUp = new UI.PopUp(Renderer, SDL_image.IMG_LoadTexture(Renderer, textureBasePath + "PopUp.png"), 
                                      inactive,active,pressed,SDL.SDL_CreateTextureFromSurface(Renderer, SDL_ttf.TTF_RenderText_Solid(Font,"Okay",Black)),
                                      "temp","temp",Font,white,Font,white,400,110,400,500);
            ErrorPopUp.DismisButton.Hover(true);
            ErrorPopUp.PressAllowedDelayTime = 500;

        }

        public override void Update(uint deltaTime)
        {

            /*IN FUTURE SOME OF THIS CODE COULD POSSIBLY BE OFLOADED TO A UI MANAGER*/
            //logic
            if (DisplayError) //if server rejection pop up is displaying logic goes here
            {
                ErrorPopUp.Update(deltaTime);
                if (ErrorPopUp.IsExited)
                {
                    
                    Program.StateManager.ClearStates(new EnterServerInfo(Renderer, ScreenWidth, ScreenHeight));
                    return;
                }
            }
            else if (!EnterHasBeenPressed) //Before attempting to connect to server
            {
                //THIS IS BODGED BUT WORKS AT MOVING TO ENTER BUTTON AFTER THE LAST TEXT ENTER BOX
                if (TextEnterBoxManager.IsActive)
                {
                    // if not entering text, at the last text box, and time between movements has expired, and input is moving down
                    if (!TextEnterBoxManager.IsTyping && TextEnterBoxManager.ActiveBoxPos == TextEnterBoxManager.NumBoxes - 1 && TextEnterBoxManager.RemainingMoveTime <= deltaTime
                    && Program.InputManager.InputMappings["down"])
                    {
                        //swap from text box to connect button
                        TextEnterBoxManager.SetActive(false);
                        EnterButton.Hover(true);
                    }
                    //handles entering text for the enter buttons
                    if (Program.InputManager.SpecialKeys["enter"] && !TextEnterBoxManager.IsTyping)
                    {
                        TextEnterBoxManager.IsTyping = true;
                        Program.NumProcsGettingText++;
                    }
                    //stops typing if esc is pressed
                    else if (Program.InputManager.SpecialKeys["esc"] && TextEnterBoxManager.IsTyping)
                    {
                        TextEnterBoxManager.IsTyping = false;
                        Program.NumProcsGettingText--;
                    }
                }
                else //if on the enter button
                {
                    //moves from enter to text boxes on up being pressed
                    if (Program.InputManager.InputMappings["up"] && !EnterButton.IsPressed)
                    {
                        TextEnterBoxManager.SetActive(true);
                        EnterButton.Hover(false);
                    }
                    //connects
                    else if (Program.InputManager.InputMappings["A"])
                    {
                        EnterButton.Press();
                        EnterHasBeenPressed = true;
                        //sanity check on input boxes
                        if (IPAddress.TryParse(TextEnterBoxManager.GetTextFromBox("IP"),out _) && int.TryParse(TextEnterBoxManager.GetTextFromBox("Port"),out _) && !string.IsNullOrWhiteSpace(TextEnterBoxManager.GetTextFromBox("Name")))
                        {
                            //attempts server connection
                            var hash = Program.CalcCheckSums(("./.content/Types/", ".type"));
                            var hashArray = new List<byte>();
                            foreach (byte[] ha in hash)
                            {
                                hashArray.AddRange(ha);
                            }
                            Program.MainServerNetworkManager = new EngineFramework.Networking.MainServerNetworkManager(TextEnterBoxManager.GetTextFromBox("IP"), 
                                int.Parse(TextEnterBoxManager.GetTextFromBox("Port")),
                                 hashArray.ToArray(), 
                                Encoding.UTF8.GetBytes(TextEnterBoxManager.GetTextFromBox("Name")));
                        }
                        else
                        {
                            //displays error here
                            DisplayError = true;
                            ErrorPopUp.UpdateMessage("Entered Data Error", "Either the port or IP is not valid");
                            
                        }
                    }
                }
            }
            else if (!EnterButton.IsPressed) //if enter button has been pressed and the press animation has finished.
            {
                if (Program.MainServerNetworkManager.IsSocketOpen)
                {
                    //move to next state
                    Program.StateManager.ClearStates(new ServerHub(Renderer, Program.ScreenWidth, Program.ScreenHeight));
                    return;
                }
                else
                {
                    //create an error message with either the exception or the servers response for why it didn't accept the connection
                    (Exception e, string response) = Program.MainServerNetworkManager.GetFailure();
                    DisplayError = true;
                    string error;
                    if (e == null) //if the server sends a reason why it isn't happy with the user then the network will return a reason as a string in response
                    {
                        error = response;
                    }
                    else 
                    {
                        if (e is ArgumentOutOfRangeException) //this is returned when the port is invalid
                        {
                            error = "Port is out of range of valid port values";
                        }
                        else if (e is SocketException) //returns when the server doesn't exist/refused the connection
                        {
                            error = "Server Refused connection";
                        }
                        else //this is for when I have no idea what's gone wrong as testing has not encountered any of these errors
                        {
                            error = "Uncaught Error please try again";
                        }
                    }
                    ErrorPopUp.UpdateMessage("Error Connecting to server", error);
                    EnterButton.UnpressEarly();

                }
            }

            TextEnterBoxManager.Update(deltaTime);
            EnterButton.Update(deltaTime);

            //rendering
            RenderBackground();

            
            SDL.SDL_QueryTexture(TextureDict["UserText"], out _, out _, out int w, out int h);
            RenderQueue.Add((TextureDict["UserText"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect() { x = 400, y = 115, w = w, h = h }));
            
            SDL.SDL_QueryTexture(TextureDict["IPText"], out _, out _, out w, out h);
            RenderQueue.Add((TextureDict["IPText"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect() { x = 400, y = 265, w = w, h = h }));

            SDL.SDL_QueryTexture(TextureDict["PortText"], out _, out _, out w, out h);
            RenderQueue.Add((TextureDict["PortText"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect() { x = 400, y = 415, w = w, h = h }));
            
            //Text Boxes
            RenderQueue.AddRange(TextEnterBoxManager.Draw());
            
            RenderQueue.AddRange(EnterButton.Draw());

            if (DisplayError) //display Pop up above
            {
                RenderQueue.AddRange(ErrorPopUp.Draw());
            }
            

        }
        public override void Close()
        {
            base.Close();
            EnterButton.Close();
            TextEnterBoxManager.Close();
            ErrorPopUp.Close();
        }
    }
}
