using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PokemonBattleSimulator
{
    public class Program
    {
        //NETWORKS
        public static EngineFramework.Networking.MainServerNetworkManager MainServerNetworkManager;
        public static EngineFramework.Networking.GameServerNetworkManager GameServerNetworkManager;
        //screen information
        public static int ScreenWidth = 1280, ScreenHeight = 720;

        //Framework Input and StateManagment
        public static EngineFramework.Input.InputManager InputManager;
        public static GameStates.StateManager StateManager;
        public GameStates.GameState State;
        //Type Manager
        public static GameClasses.TypeChart TypeChart;
        public static bool running = true;

        //when a part of the code is using keyboard textinput it increments this by 1,
        //if greater than 0 than each frame if a character is inputed then the main loop stores this in KeyboardInput for that frame
        public static char KeyboardInput = '\0';
        public static int NumProcsGettingText = 0;
        private static bool TextEnabled = false;

        static void Main()
        {
            StateManager = new GameStates.StateManager();
            TypeChart = new GameClasses.TypeChart();
            
            SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING);
            SDL_ttf.TTF_Init();

            //Game Window Set up --------------------------------------------------------------------------------------
            var window = SDL.SDL_CreateWindow("Pokemon: Battle Simulator", SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED, ScreenWidth, ScreenHeight, 0);

            var renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            var icon = SDL_image.IMG_Load("Assets/Common/WindowIcon.png");
            SDL.SDL_SetWindowIcon(window,icon);
            SDL.SDL_FreeSurface(icon);

           //---------------------------------------------------------------------------------------------------------

            //controller vs keyboard input Manager init ---------------------------------------------------------------
            if (SDL.SDL_NumJoysticks() > 0)
            {
                InputManager = new EngineFramework.Input.InputManager(SDL.SDL_GameControllerOpen(0));
                Console.WriteLine("Controller connected");

            }
            else
            {
                //within program.cs Main()
                InputManager = new EngineFramework.Input.InputManager();
                Console.WriteLine("Keyboard connected");
            }
            //----------------------------------------------------------------------------------------------------------
            //set game to launch on the start menu
            StateManager.ClearStates(new GameStates.StartMenu(renderer, 1280, 720));
            string host = "127.0.0.1";
            int port = 65431;
            byte[] pword;
            byte[] uname;
            byte[] opnname;
            if (Console.ReadLine() == "1")
            {
                pword = new byte[] { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
                uname = Encoding.UTF8.GetBytes("dia");
                opnname = Encoding.UTF8.GetBytes("test");
            }
            else
            {
                
                pword = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
                uname = Encoding.UTF8.GetBytes("test");
                opnname = Encoding.UTF8.GetBytes("dia");
            }
            Program.GameServerNetworkManager = new EngineFramework.Networking.GameServerNetworkManager
                (
                host, port, uname,pword, opnname);
            StateManager.ClearStates(new GameStates.TeamSelect(renderer,1280,720));
            //find time between frames ---------------------------------------------------------------------------
            uint last_time = SDL.SDL_GetTicks();
            uint deltaTime;
            uint curr_time;
            //-----------------------------------------------------------------------------------------------------
            // text input ------------------------------------------------------------------------------------
            bool charChangedThisFrame = false;
            SDL.SDL_StopTextInput();
            while (running)
            {
                charChangedThisFrame = false;
                curr_time = SDL.SDL_GetTicks();
                deltaTime = curr_time - last_time;

                //if text isn't being collected but it is needed in one or more places in code
                if (!TextEnabled && NumProcsGettingText > 0) 
                { 
                    SDL.SDL_StartTextInput();
                    TextEnabled = true;
                }
                //if text is being collected but isn't needed anywhere in the program currently
                else if (TextEnabled && NumProcsGettingText < 1)
                { 
                    SDL.SDL_StopTextInput();
                    TextEnabled = false;
                }

                SDL.SDL_PumpEvents();
                while (SDL.SDL_PollEvent(out var e) != 0)
                {
                    switch (e.type)
                    {
                        case SDL.SDL_EventType.SDL_QUIT:
                            running = false;
                            break;
                        case SDL.SDL_EventType.SDL_TEXTINPUT:
                            unsafe //using pointers so unsafe block must be used
                            {
                                KeyboardInput = (char)e.text.text[0];
                            }
                            Console.WriteLine(KeyboardInput);
                            charChangedThisFrame = true;
                            break;
                    }

                }
                if (!charChangedThisFrame) 
                {
                    KeyboardInput = '\0'; // '\0' is the closest to null 
                }
                //updating
                InputManager.UpdateInput();
                StateManager.Update(deltaTime);
                //rendering
                SDL.SDL_RenderClear(renderer);
                StateManager.Draw();
                SDL.SDL_RenderPresent(renderer);
                //delta time code
                last_time = curr_time;
            }

            //code for clean up
            InputManager.CleanUp();
            StateManager.Close();
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_Quit();
        }

        public static List<byte[]> CalcCheckSums(params (string, string)[] folders)
        {
            var returnList = new List<byte[]>();
            foreach (var (folderpath, fileType) in folders)
            {
                var filePaths = Directory.GetFiles(folderpath, "*" + fileType);
                Array.Sort(filePaths);
                returnList.AddRange(from filePath in filePaths select File.ReadAllText(filePath,Encoding.UTF8)
                                    into SourceData select Encoding.UTF8.GetBytes(SourceData) 
                                    into tmpSource select new MD5CryptoServiceProvider().ComputeHash(tmpSource));
            }
            return returnList;
        }
    }

}

