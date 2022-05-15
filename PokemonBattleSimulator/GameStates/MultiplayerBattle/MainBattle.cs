using System;
using System.Collections.Generic;
using System.Linq;
using SDL2;

namespace PokemonBattleSimulator.GameStates
{
    public class MainBattle : GameState
    {
        private UI.ScrollableButtonsManager Options;
        private string State;
        private GameClasses.Trainer thisPlayer;
        private GameClasses.Trainer oppPlayer;
        private GameClasses.HealthUI thisHealthUI;
        private GameClasses.HealthUI oppHealthUI;

        private UI.TextRenderBox TextMessageBox;

        private GameClasses.Trainer firstMove;
        private GameClasses.Trainer lastMove;
        private byte TurnAnimationStage = 0;

        public byte FirstPkmnOnRandomSelect = 0;

        //I have to make the same changes to the rendering here as I did in the TeamSelect Game class.
        //Upon Reflection I should have designed the rendering system like this in all States
        private new List<(IntPtr texture, SDL.SDL_Rect sRect, SDL.SDL_Rect dRect, bool destroyAfter)> RenderQueue = new();
        public MainBattle(IntPtr renderer, int screenWidth, int screenHeight, List<Dictionary<string, dynamic>> thisTeam, List<Dictionary<string, dynamic>> oppTeam) : base(renderer, screenWidth, screenHeight)
        {
            Init("./Assets/MainBattle", ("Background","/BattleBackground.png", true));
            //Console.WriteLine($"team length: {thisTeam.Count}");
            thisPlayer = new GameClasses.Trainer();
            oppPlayer = new GameClasses.Trainer(oppTeam,renderer, thisPlayer);
            thisPlayer.Init(thisTeam, renderer, oppPlayer);
            thisHealthUI = new GameClasses.HealthUI(Renderer,thisPlayer.ActivePokemonObject,0,400);
            oppHealthUI = new GameClasses.HealthUI(Renderer, oppPlayer.ActivePokemonObject, 950, 40);


            TextMessageBox = new UI.TextRenderBox(Renderer,
                SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/TextBox.png"),
                new SDL.SDL_Rect() { x=0,y=500,w=1280, h=220},
                SDL_ttf.TTF_OpenFont("./Assets/BattleFight/Pirulen.ttf",20),
                new SDL.SDL_Rect() { x = 110, y = 50, w = 900 }, new SDL.SDL_Color());
            base.RenderWhilstPaused = true;
        }

        public override void Init(string textureBasePath, params (string name, string filePath, bool isOnDefualtPath)[] FilePaths)
        {
            
            base.Init(textureBasePath, FilePaths);
            //Load the turn options buttons
            UI.Button[] buttonArray = new UI.Button[3];
            //load the fight Button
            IntPtr inactiveButton = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOption.png");
            IntPtr activeButton = SDL_image.IMG_LoadTexture(Renderer,"./Assets/MainBattle/FightOptionActive.png");
            IntPtr pressedButton = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOptionActive.png");
            buttonArray[0] = new UI.Button(inactiveButton,activeButton, pressedButton, IntPtr.Zero,new SDL.SDL_Rect() { w=300,h=100}, new SDL.SDL_Rect(), "Fight");
            //load the switch button
             inactiveButton = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOption.png");
             activeButton = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOptionActive.png");
             pressedButton = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOptionActive.png");
            buttonArray[1] = new UI.Button(inactiveButton, activeButton, pressedButton, IntPtr.Zero, new SDL.SDL_Rect() { w = 300, h = 100 }, new SDL.SDL_Rect(),"Switch");

            //load the Forfeit button
            inactiveButton = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOption.png");
            activeButton = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOptionActive.png");
            pressedButton = SDL_image.IMG_LoadTexture(Renderer, "./Assets/MainBattle/FightOptionActive.png");
            buttonArray[2] = new UI.Button(inactiveButton, activeButton, pressedButton, IntPtr.Zero, new SDL.SDL_Rect() { w = 300, h = 100 }, new SDL.SDL_Rect(),"Forfeit");

            Options = new UI.ScrollableButtonsManager(950, 390,330,100, 10, UI.ManagerModes.MODE_UP_TO_DOWN, buttonArray, 100);

            State = "TurnSelect";
            Options.SetActive(true);
        }
        public override void Update(uint deltaTime)
        {
            //NETWORK UPDATE
            (int? responseLength, byte[] response) = Program.GameServerNetworkManager.ReceiveMessage(50);
            if (responseLength != 0 && responseLength.HasValue)
            {
                if (response[0] == 255 && responseLength == 2)
                {
                    FirstPkmnOnRandomSelect = response[1];
                }
                else if (State == "OppTurnSelect")
                {
                    oppPlayer.GetActionOverNetwork(response);
                    State = "MoveResoloution";
                }
            }

            //User update
            switch (State)
            {
                case "TurnSelect":
                    TurnSelectUpdate(deltaTime);
                    break;
                case "OppTurnSelect":
                    OppTurnSelectUpdate(deltaTime);
                    break;
                case "MoveResoloution":
                    MoveResoloutionUpdate(deltaTime);
                    break;
                case "MoveAnimation":
                    MoveAnimationUpdate(deltaTime);
                    break;
                case "SwapAnimation":
                    SwapAnimationUpdate(deltaTime);
                    break;
            }
           
        }
        public override void PausedUpdate(uint deltaTime)
        {
            SDL.SDL_QueryTexture(TextureDict["Background"], out _, out _, out int w, out int h);
            RenderQueue.Add((TextureDict["Background"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect { w = ScreenWidth, h = ScreenHeight }, false));
            
            //render player active Pokemon
            RenderQueue.Add((thisPlayer.ActivePokemonObject.BackSprite,
                thisPlayer.ActivePokemonObject.BackRect,
                new SDL.SDL_Rect() { x = 230, y = 355, w = 256, h = 256 }, false));

            RenderQueue.AddRange(thisHealthUI.Draw());
            //Render Opponent Pokemon
            RenderQueue.Add((oppPlayer.ActivePokemonObject.FrontSprite,
                oppPlayer.ActivePokemonObject.FrontRect,
                new SDL.SDL_Rect() { x = 750, y = 120, w = 256, h = 256 }, false));
            //Render Oppenent Health Information
            RenderQueue.AddRange(oppHealthUI.Draw());
        }
        private void TurnSelectUpdate(uint deltaTime)
        {
            //LOGIC
            Options.Update(deltaTime);
            if (Options.PressedButton > -1)
            {
                
                switch ((string)Options.PressedButtonObject.Value)
                {
                    case "Fight":
                        Program.StateManager.AddState(new BattleMoveSelect(Renderer, ScreenWidth, ScreenHeight, thisPlayer, oppPlayer));
                        break;
                    case "Switch":
                        Program.StateManager.AddState(new BattleMonSelect(Renderer, ScreenWidth, ScreenHeight, thisPlayer, oppPlayer));
                        break;
                    case "Forfeit":
                        break;
                }
                Options.UnpressButton(Options.PressedButton);
            }
            
            if (thisPlayer.SelectedAction.Index != -1)
            {
                State = "OppTurnSelect";
                TextMessageBox.SetString(new string[] { "Awaiting Opponent's selection"});
                //Generate Random numbers needed in the move code
                thisPlayer.GenerateMoveRandoms();
                byte[] turnMessage = new byte[] {};
                switch (thisPlayer.SelectedAction.ActionType.ToLower())
                {
                    case "move":
                        turnMessage = thisPlayer.GenerateMoveNetworkMessage();
                        break;
                    case "switching":
                        turnMessage = thisPlayer.GenerateSwapNetworkMessage();
                        break;
                }
                Program.GameServerNetworkManager.SendMessage(turnMessage);
            }
            //RENDERING
            SDL.SDL_QueryTexture(TextureDict["Background"], out _, out _, out int w, out int h);
            RenderQueue.Add((TextureDict["Background"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect { w = ScreenWidth, h = ScreenHeight }, false));
            
            //render player active Pokemon
            RenderQueue.Add((thisPlayer.ActivePokemonObject.BackSprite,
                thisPlayer.ActivePokemonObject.BackRect,
                new SDL.SDL_Rect() { x = 230, y = 355, w = 256, h = 256 }, false));
            //Render player health information
            RenderQueue.AddRange(thisHealthUI.Draw());

            //Render Opponent Pokemon
            RenderQueue.Add((oppPlayer.ActivePokemonObject.FrontSprite, 
                oppPlayer.ActivePokemonObject.FrontRect,
                new SDL.SDL_Rect() { x=750, y=120, w= 256, h = 256}, false));

            //Render Oppenent Health Information
            RenderQueue.AddRange(oppHealthUI.Draw());
            RenderQueue.AddRange(ConvertDraw(Options.Draw()));
        }
        private void OppTurnSelectUpdate(uint deltaTime)
        {

            //RENDERING
            SDL.SDL_QueryTexture(TextureDict["Background"], out _, out _, out int w, out int h);
            RenderQueue.Add((TextureDict["Background"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect { w = ScreenWidth, h = ScreenHeight }, false));

            //render player active Pokemon
            RenderQueue.Add((thisPlayer.ActivePokemonObject.BackSprite,
                thisPlayer.ActivePokemonObject.BackRect,
                new SDL.SDL_Rect() { x = 230, y = 355, w = 256, h = 256 }, false));
            //Render player health information
            RenderQueue.AddRange(thisHealthUI.Draw());

            //Render Opponent Pokemon
            RenderQueue.Add((oppPlayer.ActivePokemonObject.FrontSprite,
                oppPlayer.ActivePokemonObject.FrontRect,
                new SDL.SDL_Rect() { x = 750, y = 120, w = 256, h = 256 }, false));

            //Render Oppenent Health Information
            RenderQueue.AddRange(oppHealthUI.Draw());
            RenderQueue.AddRange(ConvertDraw(TextMessageBox.Draw()));
            
        }

        private void MoveResoloutionUpdate(uint deltaTime)
        {
            //swapping
            if (thisPlayer.SelectedAction.ActionType == "switching" && oppPlayer.SelectedAction.ActionType == "move")
            {
                firstMove = thisPlayer;
                lastMove = oppPlayer;
            }
            else if (oppPlayer.SelectedAction.ActionType == "switching" && oppPlayer.SelectedAction.ActionType == "move")
            {
                firstMove = oppPlayer;
                lastMove = thisPlayer;
            }
            //MOVE PRIORITY
            else if (thisPlayer.SelectedAction.ActionType == "move" && oppPlayer.SelectedAction.ActionType == "move") {
                GameClasses.Move thisMove = thisPlayer.ActivePokemonObject.Moves[thisPlayer.SelectedAction.Index];
                var oppMove = oppPlayer.ActivePokemonObject.Moves[oppPlayer.SelectedAction.Index];
                if (thisMove.Priority > oppMove.Priority)
                {
                    firstMove = thisPlayer;
                    lastMove = oppPlayer;
                }
                else if (oppMove.Priority > thisMove.Priority)
                {
                    firstMove = oppPlayer;
                    lastMove = thisPlayer;
                }
                else
                {

                    Program.GameServerNetworkManager.SendMessage(new byte[] { 255 });
                    if (FirstPkmnOnRandomSelect == 1)
                    {
                        firstMove = thisPlayer;
                        lastMove = oppPlayer;
                    }
                    else
                    {
                        firstMove = oppPlayer;
                        lastMove = thisPlayer;
                    }
                }
            }
            //SPEED
            else if (thisPlayer.ActivePokemonObject.Stats["Spd"] > oppPlayer.ActivePokemonObject.Stats["Spd"])
            {
                firstMove = thisPlayer;
                lastMove = oppPlayer;
            }
            else if (thisPlayer.ActivePokemonObject.Stats["Spd"] < oppPlayer.ActivePokemonObject.Stats["Spd"])
            {
                firstMove = oppPlayer;
                lastMove = thisPlayer;
            }
            else
            {
                Program.GameServerNetworkManager.SendMessage(new byte[]{255});
                if (FirstPkmnOnRandomSelect == 1)
                {
                    firstMove = thisPlayer;
                    lastMove = oppPlayer;
                }
                else
                {
                    firstMove = oppPlayer;
                    lastMove = thisPlayer;
                }
            }


            //resets selected move
            //thisPlayer.SelectedAction = ("", -1);
            //oppPlayer.SelectedAction = ("", -1);
            if (firstMove.SelectedAction.ActionType == "move")
            {
                State = "MoveAnimation";
            }
            else if (firstMove.SelectedAction.ActionType == "switching")
            {
                State = "SwapAnimation";
            }
            
            //RENDERING
            SDL.SDL_QueryTexture(TextureDict["Background"], out _, out _, out int w, out int h);
            RenderQueue.Add((TextureDict["Background"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect { w = ScreenWidth, h = ScreenHeight }, false));

            //render player active Pokemon
            RenderQueue.Add((thisPlayer.ActivePokemonObject.BackSprite,
                thisPlayer.ActivePokemonObject.BackRect,
                new SDL.SDL_Rect() { x = 230, y = 355, w = 256, h = 256 }, false));
            //Render player health information
            RenderQueue.AddRange(thisHealthUI.Draw());

            //Render Opponent Pokemon
            RenderQueue.Add((oppPlayer.ActivePokemonObject.FrontSprite,
                oppPlayer.ActivePokemonObject.FrontRect,
                new SDL.SDL_Rect() { x = 750, y = 120, w = 256, h = 256 }, false));

            //Render Oppenent Health Information#
            RenderQueue.AddRange(oppHealthUI.Draw());
            RenderQueue.AddRange(ConvertDraw(TextMessageBox.Draw()));
        }

        private void MoveAnimationUpdate(uint deltaTime)
        {
            //Logic
            switch (TurnAnimationStage)
            {
                case 0: //Set text for first player
                    List<string> textString;
                    if (firstMove == thisPlayer)
                    {
                        textString = new List<string> { $"Your {firstMove.ActivePokemonObject.Name} used {firstMove.ActivePokemonObject.Moves[firstMove.SelectedAction.Index].Name}" };
                        
                    }
                    else 
                    {
                        textString = new List<string> { $"The enemy {firstMove.ActivePokemonObject.Name} used {firstMove.ActivePokemonObject.Moves[firstMove.SelectedAction.Index].Name}" };
                    }

                    float typeEffectiveness = 1;
                    foreach (string defType in lastMove.ActivePokemonObject.Types)
                    {
                        typeEffectiveness *= Program.TypeChart.GetDamageModifier(firstMove.ActivePokemonObject.Moves[firstMove.SelectedAction.Index].Type, defType);
                    }
                    if (typeEffectiveness == 0)
                    {
                        textString.Add("But it had no effect");
                    }
                    else if (typeEffectiveness < 1)
                    {
                        textString.Add("But it was not very effective");
                    }
                    else if (typeEffectiveness > 1)
                    {
                        textString.Add("And it was super effective!!");
                    }
                    TextMessageBox.SetString(textString.ToArray());
                    TurnAnimationStage = 1;
                    break;
                case 1: //input handling for first player text box
                    TextMessageBox.pressTimeCurr += deltaTime;
                    if (Program.InputManager.InputMappings["A"] && TextMessageBox.pressTimeCurr >= TextMessageBox.pressDelay)
                    {
                        TextMessageBox.NextString();
                    }
                    if (TextMessageBox.GetCurrText() == null)
                    {
                        TurnAnimationStage = 2;
                        TextMessageBox.pressTimeCurr = 0;
                    }
                    break;
                case 2: //callkng functions for move
                    lastMove.ActiveFunc = "before_opponent_move_use";
                    lastMove.ActivePokemonObject.Moves[lastMove.SelectedAction.Index].Before_Opponent_Move_Use(lastMove.ActivePokemonObject,
                        firstMove.ActivePokemonObject);

                    firstMove.ActiveFunc = "on_move_use";
                    firstMove.ActivePokemonObject.Moves[firstMove.SelectedAction.Index].On_Move_Use(firstMove.ActivePokemonObject,lastMove.ActivePokemonObject);
                    lastMove.ActiveFunc = "after_opponent_move_use";
                    lastMove.ActivePokemonObject.Moves[lastMove.SelectedAction.Index].After_Opponent_Move_Use(lastMove.ActivePokemonObject, firstMove.ActivePokemonObject);
                    TurnAnimationStage = 3;
                    if (lastMove.SelectedAction.ActionType == "switching")
                    {
                        State = "SwitchingAnimation";
                    }
                    break;
                case 3: //Text for second player
                    var text = new List<string>();
                    if (lastMove== thisPlayer)
                    {
                        text = new List<string> { $"Your {lastMove.ActivePokemonObject.Name} used {lastMove.ActivePokemonObject.Moves[lastMove.SelectedAction.Index].Name}" };

                    }
                    else
                    {
                        text = new List<string> { $"The enemy {lastMove.ActivePokemonObject.Name} used {lastMove.ActivePokemonObject.Moves[lastMove.SelectedAction.Index].Name}" };
                    }

                    typeEffectiveness = 1;
                    foreach (string defType in firstMove.ActivePokemonObject.Types)
                    {
                        typeEffectiveness *= Program.TypeChart.GetDamageModifier(lastMove.ActivePokemonObject.Moves[firstMove.SelectedAction.Index].Type, defType);
                    }
                    if (typeEffectiveness == 0)
                    {
                        text.Add("But it had no effect");
                    }
                    else if (typeEffectiveness < 1)
                    {
                        text.Add("But it was not very effective");
                    }
                    else if (typeEffectiveness > 1)
                    {
                        text.Add("And it was super effective!!");
                    }
                    TextMessageBox.SetString(text.ToArray());
                    
                    TurnAnimationStage = 4;
                    break;
                case 4: //input for secondf player
                    
                    TextMessageBox.pressTimeCurr += deltaTime;
                    if (Program.InputManager.InputMappings["A"] && TextMessageBox.pressTimeCurr >= TextMessageBox.pressDelay)
                    {
                        TextMessageBox.NextString();
                    }
                    if (TextMessageBox.GetCurrText() == null)
                    {
                        TextMessageBox.pressTimeCurr = 0;
                        TurnAnimationStage = 5;
                        
                    }
                    break;
                case 5: //function for second player
                    firstMove.ActiveFunc = "before_opponent_move_use";
                    firstMove.ActivePokemonObject.Moves[firstMove.SelectedAction.Index].Before_Opponent_Move_Use(firstMove.ActivePokemonObject,
                        lastMove.ActivePokemonObject);

                    lastMove.ActiveFunc = "on_move_use";
                    lastMove.ActivePokemonObject.Moves[lastMove.SelectedAction.Index].On_Move_Use(lastMove.ActivePokemonObject, firstMove.ActivePokemonObject);
                    firstMove.ActiveFunc = "after_opponent_move_use";
                    firstMove.ActivePokemonObject.Moves[firstMove.SelectedAction.Index].After_Opponent_Move_Use(firstMove.ActivePokemonObject, lastMove.ActivePokemonObject);
                    TurnAnimationStage = 6;
                    break;
                case 6: //turn end for both players
                    if (firstMove.SelectedAction.ActionType == "move")
                    {
                        firstMove.ActiveFunc = "on_turn_end";
                        firstMove.ActivePokemonObject.Moves[firstMove.SelectedAction.Index].On_End_Of_Turn(firstMove.ActivePokemonObject,
                            lastMove.ActivePokemonObject);
                    }
                    if (lastMove.SelectedAction.ActionType == "move")
                    {
                        lastMove.ActiveFunc = "on_turn_end";
                        lastMove.ActivePokemonObject.Moves[lastMove.SelectedAction.Index].On_End_Of_Turn(lastMove.ActivePokemonObject,
                            firstMove.ActivePokemonObject);
                    }
                    firstMove.SelectedAction = ("", -1);
                    lastMove.SelectedAction = ("", -1);
                    State = "TurnSelect";
                    TurnAnimationStage = 0;
                    break;
            }
            //RENDERING
            //THIS WILL ALWAYS BE RENDERED
            SDL.SDL_QueryTexture(TextureDict["Background"], out _, out _, out int w, out int h);
            RenderQueue.Add((TextureDict["Background"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect { w = ScreenWidth, h = ScreenHeight }, false));

            //render player active Pokemon
            RenderQueue.Add((thisPlayer.ActivePokemonObject.BackSprite,
                thisPlayer.ActivePokemonObject.BackRect,
                new SDL.SDL_Rect() { x = 230, y = 355, w = 256, h = 256 }, false));
            //Render player health information
            RenderQueue.AddRange(thisHealthUI.Draw());

            //Render Opponent Pokemon
            RenderQueue.Add((oppPlayer.ActivePokemonObject.FrontSprite,
                oppPlayer.ActivePokemonObject.FrontRect,
                new SDL.SDL_Rect() { x = 750, y = 120, w = 256, h = 256 }, false));

            //Render Oppenent Health Information#
            RenderQueue.AddRange(oppHealthUI.Draw());
            if (TurnAnimationStage == 0 || TurnAnimationStage == 1 || TurnAnimationStage == 3 || TurnAnimationStage == 4) 
            {
                RenderQueue.AddRange(ConvertDraw(TextMessageBox.Draw()));
            }
            
        }

        private void SwapAnimationUpdate(uint deltaTime)
        {
            //LOGIC
            switch (TurnAnimationStage)
            {
                case 0: //text for the first user is set
                    List<string> textString;
                    if (firstMove == thisPlayer)
                    {
                        textString = new List<string> { $"You swapped from {firstMove.ActivePokemonObject.Name} to {firstMove.Team[firstMove.SelectedAction.Index].Name}" };

                    }
                    else
                    {
                        textString = new List<string> { $"The enemy swapped from {firstMove.ActivePokemonObject.Name} to {firstMove.Team[firstMove.SelectedAction.Index].Name}" };
                    }
                    TextMessageBox.SetString(textString.ToArray());
                    TurnAnimationStage = 1;
                    break;
                case 1: //await input
                    TextMessageBox.pressTimeCurr += deltaTime;
                    if (Program.InputManager.InputMappings["A"] && TextMessageBox.pressTimeCurr >= TextMessageBox.pressDelay)
                    {
                        TextMessageBox.NextString();
                    }
                    if (TextMessageBox.GetCurrText() == null)
                    {
                        TextMessageBox.pressTimeCurr = 0;
                        TurnAnimationStage = 2;

                    }
                    break;
                case 2: //swap first moves pokemon, check to see if state must be changed
                    //if i had more time i would set it so that the array is rearranged so the active pokemon
                    //is always at the top
                    firstMove.ActivePokemon = firstMove.SelectedAction.Index;
                    if (lastMove.SelectedAction.ActionType == "move")
                    {
                        State = "MoveAnimation";
                    }
                    TurnAnimationStage = 3;
                    if (firstMove == thisPlayer)
                    {
                        thisHealthUI.ChangeTrackingPokemon(firstMove.ActivePokemonObject);
                    }
                    else if (firstMove == oppPlayer)
                    {
                        oppHealthUI.ChangeTrackingPokemon(firstMove.ActivePokemonObject);
                    }
                    break;
                case 3: //text for second move
                    List<string> text;
                    if (firstMove == thisPlayer)
                    {
                        text = new List<string> { $"You swapped from {lastMove.ActivePokemonObject.Name} to {lastMove.Team[lastMove.SelectedAction.Index].Name}" };

                    }
                    else
                    {
                        text = new List<string> { $"The enemy swapped from {lastMove.ActivePokemonObject.Name} to {lastMove.Team[lastMove.SelectedAction.Index].Name}" };
                    }
                    TextMessageBox.SetString(text.ToArray());
                    TurnAnimationStage = 4;
                    break;
                case 4: //input
                    TextMessageBox.pressTimeCurr += deltaTime;
                    if (Program.InputManager.InputMappings["A"] && TextMessageBox.pressTimeCurr >= TextMessageBox.pressDelay)
                    {
                        TextMessageBox.NextString();
                    }
                    if (TextMessageBox.GetCurrText() == null)
                    {
                        TextMessageBox.pressTimeCurr = 0;
                        TurnAnimationStage = 5;

                    }
                    break;
                case 5: //update health tracking and swap last move
                    lastMove.ActivePokemon = lastMove.SelectedAction.Index;
                    State = "MoveAnimation"; //move to MoveAnimation
                    TurnAnimationStage = 6;
                    if (lastMove == thisPlayer)
                    {
                        thisHealthUI.ChangeTrackingPokemon(lastMove.ActivePokemonObject);
                    }
                    else if (lastMove == oppPlayer)
                    {
                        oppHealthUI.ChangeTrackingPokemon(lastMove.ActivePokemonObject);
                    }
                    break;
            }
            //RENDERING
            SDL.SDL_QueryTexture(TextureDict["Background"], out _, out _, out int w, out int h);
            RenderQueue.Add((TextureDict["Background"], new SDL.SDL_Rect() { w = w, h = h }, new SDL.SDL_Rect { w = ScreenWidth, h = ScreenHeight }, false));

            //render player active Pokemon
            RenderQueue.Add((thisPlayer.ActivePokemonObject.BackSprite,
                thisPlayer.ActivePokemonObject.BackRect,
                new SDL.SDL_Rect() { x = 230, y = 355, w = 256, h = 256 }, false));
            //Render player health information
            RenderQueue.AddRange(thisHealthUI.Draw());

            //Render Opponent Pokemon
            RenderQueue.Add((oppPlayer.ActivePokemonObject.FrontSprite,
                oppPlayer.ActivePokemonObject.FrontRect,
                new SDL.SDL_Rect() { x = 750, y = 120, w = 256, h = 256 }, false));

            //Render Oppenent Health Information#
            RenderQueue.AddRange(oppHealthUI.Draw());

            if (TurnAnimationStage == 0 || TurnAnimationStage == 1 || TurnAnimationStage == 3 || TurnAnimationStage == 4)
            {
                RenderQueue.AddRange(ConvertDraw(TextMessageBox.Draw()));
            }
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

        private List<(IntPtr texture, SDL.SDL_Rect sRect, SDL.SDL_Rect dRect, bool destroyTexture)> ConvertDraw(List<(IntPtr texture, SDL.SDL_Rect sRect, SDL.SDL_Rect dRect)> draw)
        {
            //turns the 3 item tuple entries into 4 item tuple with the destroy texture object at the end
            return draw.Select(a => new { a.Item1, a.Item2, a.Item3 })
                .AsEnumerable().Select(c => (c.Item1, c.Item2, c.Item3, false)).ToList();

        }
    }
}
