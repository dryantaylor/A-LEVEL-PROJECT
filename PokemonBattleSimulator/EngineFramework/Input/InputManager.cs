using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SDL2;

namespace PokemonBattleSimulator.EngineFramework.Input
{
    public class SDLInitException : Exception
    {
        public SDLInitException()
        {
        }
        public SDLInitException(string message) : base(message)
        {
        }
        public SDLInitException(string message, Exception inner) : base(message, inner) { }
    }
    public class InputManager
    {

        private readonly IntPtr _joystick = IntPtr.Zero;
        private readonly IntPtr _keyboardPointer = IntPtr.Zero;
        private readonly int _numkeys = 0;
        private readonly byte[] _keysDown;
        public bool IsInputActive = true;
        public Action UpdateInput { get; }

        private Dictionary<SDL.SDL_Scancode, string> _keyboardBindings;

        //-ve value of the axis bellow the -ve of the deadzone sets the 1st value in the string array to true, +ve value above the deadzone sets the 2nd value to true
        // (and the other to false), if neither than both set to false
        private Dictionary<SDL.SDL_GameControllerAxis, string[]> _controllerAxisBindings;
        private Dictionary<SDL.SDL_GameControllerButton, string> _controllerButtonBindings;
        private const short Deadzone = 10000;

        public Dictionary<string, bool> InputMappings { get; } = new()
        {
            {"up", false}, {"down", false}, {"left", false}, {"right", false},
            {"A", false}, {"B", false}, {"X", false}, {"Y", false}, {"L", false}, {"R", false}
        };
        public Dictionary<string, bool> SpecialKeys = new Dictionary<string, bool> { { "backspace", false }, { "enter", false } , { "esc",false} };
        private Dictionary<string, bool> SpecialKeysEligbleForTrue = new Dictionary<string, bool> { { "backspace", true }, { "enter", true }, {"esc", true} };

        public InputManager(IntPtr joystick)
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_GAMECONTROLLER) < 0) throw new SDLInitException(SDL.SDL_GetError());
            if (SDL.SDL_GameControllerEventState(SDL.SDL_ENABLE) < 0) throw new SDLInitException(SDL.SDL_GetError());
            _joystick = joystick;
            UpdateInput = UpdateController;
            _controllerAxisBindings = new Dictionary<SDL.SDL_GameControllerAxis, string[]>
            {
                {SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX, new[] {"left", "right"}},
                {SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY, new[] {"up", "down"}}
            };
            _controllerButtonBindings = new Dictionary<SDL.SDL_GameControllerButton, string>
            {
                {SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A, "A"},
                {SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B, "B"},
                {SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X, "X"},
                {SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y, "Y"},
                {SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER, "L"},
                {SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER, "R"}
            };
            _keyboardPointer = SDL.SDL_GetKeyboardState(out _numkeys);
            _keysDown = new byte[_numkeys];
        }
        public InputManager()
        {
            UpdateInput = UpdateKeyboard;
            _keyboardPointer = SDL.SDL_GetKeyboardState(out _numkeys);
            _keysDown = new byte[_numkeys];
            //TODO: read these in from a settings file
            _keyboardBindings =  new Dictionary<SDL.SDL_Scancode, string>()
            {
                {SDL.SDL_Scancode.SDL_SCANCODE_UP, "up"}, {SDL.SDL_Scancode.SDL_SCANCODE_DOWN, "down"}, {SDL.SDL_Scancode.SDL_SCANCODE_LEFT, "left"},
                {SDL.SDL_Scancode.SDL_SCANCODE_RIGHT, "right"},
                {SDL.SDL_Scancode.SDL_SCANCODE_Z, "A"}, {SDL.SDL_Scancode.SDL_SCANCODE_X, "B"}, {SDL.SDL_Scancode.SDL_SCANCODE_A, "X"},
                {SDL.SDL_Scancode.SDL_SCANCODE_S, "Y"}, {SDL.SDL_Scancode.SDL_SCANCODE_Q, "L"}, {SDL.SDL_Scancode.SDL_SCANCODE_W, "R"}
            };
        }

        private void UpdateController()
        {
            SDL.SDL_GameControllerUpdate();
            foreach (var (button, value) in _controllerButtonBindings)
            {
                InputMappings[value] = SDL.SDL_GameControllerGetButton(_joystick, button) == 1; //function returns 1 if pressed, 0 if not pressed
            }

            foreach (var (axis, values) in _controllerAxisBindings)
            {
                var axisPos = SDL.SDL_GameControllerGetAxis(_joystick, axis);
                switch (axisPos)
                {
                    case >= Deadzone:
                        InputMappings[values[1]] = true;
                        InputMappings[values[0]] = false;
                        break;
                    case <= -Deadzone:
                        InputMappings[values[0]] = true;
                        InputMappings[values[1]] = false;
                        break;
                    default:
                        InputMappings[values[0]] = false;
                        InputMappings[values[1]] = false;
                        break;
                }
            }
            Marshal.Copy(_keyboardPointer, _keysDown, 0, _numkeys);
            UpdateSpecialKeys();
        }
        
        private void UpdateKeyboard()
        {
            //https://stackoverflow.com/questions/63808884/sdl2-cs-getkeyboardstate-intptr-to-byte-array
            Marshal.Copy(_keyboardPointer, _keysDown, 0, _numkeys);
            foreach (var (key, value) in _keyboardBindings)
            {
                InputMappings[value] = _keysDown[(byte) key] == 1;
            }
            UpdateSpecialKeys();
        }

        private void UpdateSpecialKeys()
        {
            SpecialKeys["backspace"] = _keysDown[(byte)SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE] == 1;
            //this means on the first frame the key is pressed, it gets set to true
            //all frames after it will be false until the key is unpressed when it becomes EligableForTrueAgain
            //which means the first frame of the next press won't be flipped
            if (!SpecialKeysEligbleForTrue["backspace"]) 
            {
                if (SpecialKeys["backspace"] == true)
                {
                    SpecialKeys["backspace"] = false;
                }
                else if (SpecialKeys["backspace"] == false)
                {
                    SpecialKeysEligbleForTrue["backspace"] = true;
                }
            }
            else if (SpecialKeys["backspace"]){ SpecialKeysEligbleForTrue["backspace"] = false; }

            SpecialKeys["enter"] = _keysDown[(byte)SDL.SDL_Scancode.SDL_SCANCODE_RETURN] == 1;
            if (!SpecialKeysEligbleForTrue["enter"])
            {
                if (SpecialKeys["enter"] == true)
                {
                    SpecialKeys["enter"] = false;
                }
                else if (SpecialKeys["enter"] == false)
                {
                    SpecialKeysEligbleForTrue["enter"] = true;
                }
            }
            else if (SpecialKeys["enter"]) { SpecialKeysEligbleForTrue["enter"] = false; }
            
            SpecialKeys["esc"] = _keysDown[(byte)SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE] == 1;
            if (!SpecialKeysEligbleForTrue["esc"])
            {
                if (SpecialKeys["esc"] == true)
                {
                    SpecialKeys["esc"] = false;
                }
                else if (SpecialKeys["esc"] == false)
                {
                    SpecialKeysEligbleForTrue["esc"] = true;
                }
            }
            else if (SpecialKeys["esc"]) { SpecialKeysEligbleForTrue["esc"] = false; }


        }
        public bool UpdateBindings(Dictionary<SDL.SDL_GameControllerAxis, string[]> controllerAxisBindings, 
            Dictionary<SDL.SDL_GameControllerButton, string> controllerButtonBindings)
        {
            //check that the buttons/Axis contain all the required
            //all the buttons needed for my game
            var requiredButtons = new List<string> {"up", "down", "left", "right", "A","B","X","Y","L","R"};
            foreach (var bindings in controllerAxisBindings.Values)
            {
                if (bindings.Length == 2) //check each axis has exactly two values it controls e.g up and down
                {
                    if (requiredButtons.Contains(bindings[0]) && requiredButtons.Contains(bindings[1])) //check that these values are required ones
                    {
                        requiredButtons.Remove(bindings[0]);
                        requiredButtons.Remove(bindings[1]);
                    }
                    else
                    {
                        return false; //returns false if the bindings can't be updated
                    }
                }

                else
                {
                    return false;
                }
            }

            foreach (var binding in controllerButtonBindings.Values)
            {
                if (requiredButtons.Contains(binding)) //only binding names being used should be used
                {
                    requiredButtons.Remove(binding);
                }
                else
                {
                    return false;
                }
            }

            if (requiredButtons.Count != 0) //if all bindings have been accounted for
            {
                return false;
            }

            _controllerAxisBindings = controllerAxisBindings;
            _controllerButtonBindings = controllerButtonBindings;
            return true; //returns true since updating succeeded
        }

        public bool UpdateBindings(Dictionary<SDL.SDL_Scancode, string> keyboardBindings)
        {
            var requiredButtons = new List<string> { "up", "down", "left", "right", "A", "B", "X", "Y", "L", "R" };
            foreach (var binding in keyboardBindings.Values)
            {
                if (requiredButtons.Contains(binding))
                {
                    requiredButtons.Remove(binding);
                }
                else
                {
                    return false;
                }
            }

            if (requiredButtons.Count != 0)
            {
                return false;
            }

            _keyboardBindings = keyboardBindings;
            return true;
        }

        public void CleanUp()
        {
            //close the joystick if it is active
            if (_joystick == IntPtr.Zero) return;
            SDL.SDL_GameControllerClose(_joystick);
        }
    }

}