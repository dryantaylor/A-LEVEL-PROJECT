using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SDL2;

namespace PokemonBattleSimulator.GameStates
{
    public class TeamSelect : GameState
    {
        internal List<Dictionary<string, dynamic>> thisTeam, oppTeam;
        internal UI.ScrollableButtonsManager teams;
        internal Dictionary<string, List<Dictionary<string, dynamic>>> teamDict;

        private string FontPath = "./Assets/EnterServerInfo/SweetChild.ttf";
        private string InactiveButtonTexturePath = "./Assets/EnterServerInfo/button.png";
        private string ActiveButtonPath = "./Assets/EnterServerInfo/buttonActive.png";
        private string PressedButtonPath = "./Assets/EnterServerInfo/buttonPress.png";
        private SDL.SDL_Color Colour = new SDL.SDL_Color(){r=0,g=0,b=0};

        internal const int ButtonHeight = 100, ButtonWidth = 300;
        internal const string TeamLocation = ".\\..teams";

        internal bool hasSentTeamToServer = false;

        //this hides the base GameState RenderQueue which doesn't need the destroy after bool
        //this is a somewhat jank soloution to the problem of destroying the name textures but I don't have time to
        //refactor all my draw code
        private new List<(IntPtr texture, SDL.SDL_Rect sRect, SDL.SDL_Rect dRect, bool destroyAfter)> RenderQueue = new();
        public TeamSelect(IntPtr renderer, int screenWidth, int screenHeight) : base(renderer, screenWidth, screenHeight)
        {
            Init("./assets/TeamSelect", ("Background", "./Assets/Common/menu.jpg", false),("SelectTeamText", "/TeamSelectText.png",true), ("TeamBackground", ".\\Assets\\EnterServerInfo\\PopUp.png", false));
        }
        public override void Init(string textureBasePath, params (string name, string filePath, bool isOnDefualtPath)[] FilePaths)
        {
            base.Init(textureBasePath, FilePaths);

            //get the .team files in the TeamLocation folder and make each a button in the scrollable buttons
            teamDict  = new Dictionary<string, List<Dictionary<string, dynamic>>>();
            List<UI.Button> buttons= new List<UI.Button>();

            var Font = SDL_ttf.TTF_OpenFont(FontPath, 50);
            foreach (string file in Directory.GetFiles(TeamLocation, "*.team"))
            {
                teamDict[file] = JsonConvert.DeserializeObject<List<Dictionary<string, dynamic>>>(File.ReadAllText(file));
                IntPtr active = SDL_image.IMG_LoadTexture(Renderer, ActiveButtonPath);
                IntPtr inactive = SDL_image.IMG_LoadTexture(Renderer, InactiveButtonTexturePath);
                IntPtr pressed = SDL_image.IMG_LoadTexture(Renderer, PressedButtonPath);
                var text_surface = SDL_ttf.TTF_RenderText_Blended(Font, file.Split("\\").Last().Split(".")[0], Colour);
                IntPtr Text = SDL.SDL_CreateTextureFromSurface(Renderer, text_surface); 
                SDL.SDL_QueryTexture(Text, out _, out _, out int textW, out int textH);
                buttons.Add(new UI.Button(inactive, active, pressed, Text, new SDL.SDL_Rect() { w=ButtonWidth, h=ButtonHeight}, 
                    new SDL.SDL_Rect() { x = (ButtonWidth - textW) / 2, y = (ButtonHeight - textH) / 2, w = textW, h = textH }, file));
            }
            SDL_ttf.TTF_CloseFont(Font);
            teams = new UI.ScrollableButtonsManager(10, 80, ScreenHeight - 120, ButtonHeight, 20, UI.ManagerModes.MODE_UP_TO_DOWN, buttons.ToArray(), 250);
            teams.SetActive(true);
            
        }

        public override void Update(uint deltaTime)
        {
            NetworkUpdate();
            teams.Update(deltaTime);

            //control logic here
            if (hasSentTeamToServer) 
            { 
                if (teams.PressedButton == -1)
                {
                    
                    Program.StateManager.ClearStates(new LoadingScreen(Renderer, ScreenWidth, ScreenHeight,thisTeam,oppTeam));
                    //load new scene since button press Animation has finished
                }
            }
            else if (teams.PressedButton > -1)
            {
                thisTeam = teamDict[teams.PressedButtonObject.Value.ToString()];
                //team has been selected so send this team file to the server
                Program.GameServerNetworkManager.SendMessage(Encoding.UTF8.GetBytes(File.ReadAllText(teams.PressedButtonObject.Value.ToString())));
                hasSentTeamToServer = true;
            }


            //Draw Logic.
            //Render Background;
            SDL.SDL_QueryTexture(TextureDict["Background"], out _, out _, out int w, out int h);
            RenderQueue.Add((TextureDict["Background"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect() { w = ScreenWidth, h = ScreenHeight }, false)); ;
            SDL.SDL_QueryTexture(TextureDict["SelectTeamText"], out _, out _, out w, out h);
            // RenderQueue.Add((TextureDict["SelectTeamText"], new SDL.SDL_Rect() {x=0,y=0, w= w,h= h}, new SDL.SDL_Rect() { x= 20, y= 10, w= w, h = h}, false));

            RenderQueue.AddRange(ConvertDraw(teams.Draw()));
                
            
            for (int index = 0; index < 6; index++)
            {
                RenderQueue.AddRange(DrawPokemonDisplay(index, teamDict[teams.ActiveButtonObject.Value.ToString()], 350, 30, 550, 110, 4));
            }
            //display contents of team here
            
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
        
        private List<(IntPtr texture, SDL.SDL_Rect SRect, SDL.SDL_Rect DRect, bool destroyAfter)> DrawPokemonDisplay(int index, List<Dictionary<string, dynamic>> teamJson, int x0, int y0, int w, int h, int spacing)
        {
            var renderQueue = new List<(IntPtr texture, SDL.SDL_Rect sRect, SDL.SDL_Rect dRect, bool destroyAfter)> { };
            //logic here
            int ybase = y0 + (spacing + h) * index;
            SDL.SDL_QueryTexture(TextureDict["TeamBackground"], out _, out _, out int textureW, out int textureH);
            renderQueue.Add((TextureDict["TeamBackground"], new SDL.SDL_Rect() { w = textureW, h = textureH }, new SDL.SDL_Rect() { x = x0, y = ybase, w = w, h = h }, false)) ;
            
            if (index < teamJson.Count)
            {
                var font = SDL_ttf.TTF_OpenFont(FontPath, 25);
                //render name
                string display_name = teamJson[index]["Nickname"] != "" ? teamJson[index]["Nickname"] : teamJson[index]["Name"];
                //create surface of the name text
                var name_surface = SDL_ttf.TTF_RenderText_Blended(font,display_name, new SDL.SDL_Color() { r=0,g=0,b=0,a=255});
                //turn surface to texture
                var name_texture = SDL.SDL_CreateTextureFromSurface(Renderer, name_surface);
                //destroy the surface to free up memory and prevent a memory leak
                SDL.SDL_FreeSurface(name_surface);
                //no idea why i have to specify a type for my discards here, however leaving this out creates an error
                SDL.SDL_QueryTexture(name_texture, out uint _, out int _, out textureW, out textureH);
                        renderQueue.Add((name_texture, new SDL.SDL_Rect() { w = textureW, h = textureH }, new SDL.SDL_Rect() { x = x0 + 70, y = ybase + 10, h = textureH, w = textureW }, true));

                //close font and name texture to prevent memory leak
                SDL_ttf.TTF_CloseFont(font);
            }
            return renderQueue;
        }

        public override void Draw()
        {
            int x = 0;
            foreach (var (texture, sRect, dRect, destroyAfter) in RenderQueue)
            {
                var tsRect = sRect;
                var tdRect = dRect;
                SDL.SDL_RenderCopy(Renderer, texture, ref tsRect, ref tdRect);
                if (destroyAfter)
                {
                    SDL.SDL_DestroyTexture(texture); 
                }
                x++;
            }
            RenderQueue.Clear();
        }
        public override void Close()
        {
            base.Close();

        }

        private List<(IntPtr texture, SDL.SDL_Rect sRect, SDL.SDL_Rect dRect, bool destroyTexture)> ConvertDraw(List<(IntPtr texture, SDL.SDL_Rect sRect, SDL.SDL_Rect dRect)> draw)
        {
            //turns the 3 item tuple entries into 4 item tuple with the destroy texture object at the end
            return draw.Select(a => new { a.Item1, a.Item2, a.Item3 })
                       .AsEnumerable().Select(c => (c.Item1, c.Item2, c.Item3, false)).ToList();

            }
        }
}
