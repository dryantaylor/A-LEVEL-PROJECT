using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2;

namespace PokemonBattleSimulator.GameStates
{
    public class ServerHub : GameState
    {
        
        private UI.ScrollableButtonsManager Users;
        private string FontPath = "./Assets/EnterServerInfo/SweetChild.ttf";
        private string InactiveButtonTexturePath = "./Assets/EnterServerInfo/button.png";
        private string ActiveButtonPath = "./Assets/EnterServerInfo/buttonActive.png";
        private string PressedButtonPath = "./Assets/EnterServerInfo/buttonPress.png";
        private SDL.SDL_Color Colour;
        private int ButtonWidth = 300, ButtonHeight = 100;
        public ServerHub(IntPtr renderer, int screenWidth, int screenHeight) : base(renderer, screenWidth, screenHeight)
        {
            Init("./Assets/ServerHub/",("Background","./Assets/Common/menu.jpg",false));

            base.RenderWhilstPaused = true;
        }
        public override void Init(string textureBasePath, params (string name, string filePath, bool isOnDefualtPath)[] FilePaths)
        {
            base.Init(textureBasePath, FilePaths);
            
            
            var Font =SDL_ttf.TTF_OpenFont(FontPath,50);
            SDL.SDL_Color Colour = new SDL.SDL_Color() { r=0, g=0, b=0 };
            var initialUsers = new List<UI.Button> { };

            //loops through each user to create a button for them
            foreach (var user in Program.MainServerNetworkManager.Users)
            {
                IntPtr active = SDL_image.IMG_LoadTexture(Renderer,ActiveButtonPath);
                IntPtr inactive = SDL_image.IMG_LoadTexture(Renderer, InactiveButtonTexturePath);
                IntPtr pressed = SDL_image.IMG_LoadTexture(Renderer, PressedButtonPath);
                
                string uname = Encoding.UTF8.GetString(user);
                var text_surface = SDL_ttf.TTF_RenderText_Blended(Font, uname, Colour);
                IntPtr Text = SDL.SDL_CreateTextureFromSurface(Renderer, text_surface);
                SDL.SDL_FreeSurface(text_surface);
                SDL.SDL_QueryTexture(Text,out _, out _, out int textW, out int textH);
                initialUsers.Add(new UI.Button(inactive,active,pressed,Text,new SDL.SDL_Rect() {w=ButtonWidth, h = ButtonHeight },
                    //text goes in the centre of the button
                    new SDL.SDL_Rect() {x = (ButtonWidth - textW)/2,y = (ButtonHeight - textH)/2, w =textW ,h= textH}, user));
            }
            
            SDL_ttf.TTF_CloseFont(Font);
            Users = new UI.ScrollableButtonsManager(0, 0, ScreenHeight, ButtonHeight, 20, UI.ManagerModes.MODE_UP_TO_DOWN,initialUsers.ToArray());
            Users.SetActive(true);

            //SDL_ttf.TTF_CloseFont(Font);
        }
        public override void Update(uint deltaTime)
        {
            NetworkUpdate();   

            //UI logic
            Users.Update(deltaTime);

            //USER CONTROL LOGIC
            //if a button is pressed create a menu for it
            if (Users.PressedButton > -1)
            {
                Console.WriteLine("Button Pressed");
                Program.StateManager.AddState(new UserServerHubMenu(Renderer, ScreenWidth, ScreenHeight,Users.Buttons[Users.PressedButton].Value));
            }
            //rendering
            RenderBackground();
            RenderQueue.AddRange(Users.Draw());
        }

        public override void PausedUpdate(uint deltaTime)
        {
            base.PausedUpdate(deltaTime);
            NetworkUpdate();
            RenderBackground();
            RenderQueue.AddRange(Users.Draw());
        }
        public override void Resume()
        {
            base.Resume();
            Users.UnpressButton(Users.PressedButton);
        }

        private void NetworkUpdate()
        {
            //network logic
            (int? responseLength, byte[] response) = Program.MainServerNetworkManager.ReceiveMessage(50);
            if (responseLength != 0 && responseLength.HasValue)
            {
                Console.WriteLine($"message recieved : { (int)response[0]} , {Encoding.UTF8.GetString(response[1..(int)responseLength])}");
                //new data has been sent so handle it
                switch (response[0])
                {
                    case 2:
                        //new user has connected so handle this by getting the name and adding it to the users list
                        Program.MainServerNetworkManager.Users.Add(response[1..(int)(responseLength)]);
                        //Add users to Users Scrollable button Manager so that user can interact with it
                        var Font = SDL_ttf.TTF_OpenFont(FontPath, 50);
                        IntPtr active = SDL_image.IMG_LoadTexture(Renderer, ActiveButtonPath);
                        IntPtr inactive = SDL_image.IMG_LoadTexture(Renderer, InactiveButtonTexturePath);
                        IntPtr pressed = SDL_image.IMG_LoadTexture(Renderer, PressedButtonPath);
                        string uname = Encoding.UTF8.GetString(Program.MainServerNetworkManager.Users.Last());
                        IntPtr Text = SDL.SDL_CreateTextureFromSurface(Renderer, SDL_ttf.TTF_RenderText_Blended(Font, uname, Colour));
                        SDL.SDL_QueryTexture(Text, out _, out _, out int textW, out int textH);

                        Users.AddButton(new UI.Button(inactive, active, pressed, Text, new SDL.SDL_Rect() { w = ButtonWidth, h = ButtonHeight },
                                        new SDL.SDL_Rect() { x = (ButtonWidth - textW) / 2, y = (ButtonHeight - textH) / 2, w = textW, h = textH }, Program.MainServerNetworkManager.Users.Last()));
                        SDL_ttf.TTF_CloseFont(Font);
                        break;
                    case 3:
                        //user has disconnected so remove them from the list of users and list of sent and received and sent battle requests
                        int index = Program.MainServerNetworkManager.GetIndexInUsers(response[1..(int)(responseLength)]);
                        Program.MainServerNetworkManager.Users.RemoveAt(index);
                        Users.RemoveButton(index);

                        index = Program.MainServerNetworkManager.GetIndexInReceivedRequests(response[1..(int)(responseLength)]);
                        if (index != -1)
                        {
                            Program.MainServerNetworkManager.BattleRequestsRecieved.RemoveAt(index);
                        }
                        index = Program.MainServerNetworkManager.GetIndexInRequestsSent(response[1..(int)(responseLength)]);
                        if (index != -1)
                        {
                            Program.MainServerNetworkManager.BattleRequestsSent.RemoveAt(index);
                        }
                        break;
                    case 4:
                        Console.WriteLine("Battle request received");
                        Program.MainServerNetworkManager.BattleRequestsRecieved.Add(response[1..(int)(responseLength)]);
                        break;
                    case 5:
                        //battle request accepted so handle this
                        Program.GameServerNetworkManager =
                            new EngineFramework.Networking.GameServerNetworkManager(hostString: Program.MainServerNetworkManager.Host.ToString(),
                            port: BitConverter.ToInt32(response[1..5], 0), thisUname: Program.MainServerNetworkManager.Username,
                            password:response[5..21], opponentUname: response[21..(int)responseLength]);
                        Program.StateManager.ClearStates(new TeamSelect(Renderer, ScreenWidth, ScreenHeight));
                        break;
                    case 6:
                        //battle request denied so remove from sent battle requests
                        Program.MainServerNetworkManager.BattleRequestsSent.Remove(response[1..(int)(responseLength)]);
                        //TODO: Show pop up in UI
                        break;
                }
            }
        }

        public override void Close()
        {
            base.Close();
            Users.Close();
        }
    }
}
