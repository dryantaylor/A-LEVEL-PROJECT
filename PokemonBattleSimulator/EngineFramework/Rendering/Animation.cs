using System;
using SDL2;

namespace PokemonBattleSimulator.EngineFramework.Rendering
{
    public class Animation
    {
        public int[] Timings;
        public IntPtr[] Frames;
        public bool DoesLoop;
        public SDL.SDL_Rect sRect;

        public Animation(string[] frames, int[] timings, bool doesLoop, IntPtr renderer, SDL.SDL_Rect? srect = null)
        {
            Frames = new IntPtr[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                Frames[i] = SDL_image.IMG_LoadTexture(renderer, frames[i]);
            }
            if (srect != null)
            {
                sRect = (SDL.SDL_Rect)srect;
            }
            else
            {
                SDL.SDL_QueryTexture(Frames[0], out _, out _, out int w, out int h);
                sRect = new SDL.SDL_Rect() { w = w, h = h };
            }
            Timings = timings;
            DoesLoop = doesLoop;
        }

        public Animation(IntPtr[] frames, int[] timings, bool doesLoop, SDL.SDL_Rect? srect = null)
        {
            Timings = timings;
            Frames = frames;
            DoesLoop = doesLoop;
            if (srect != null)
            {
                sRect = (SDL.SDL_Rect)srect;
            }
            else
            {
                SDL.SDL_QueryTexture(Frames[0], out _, out _, out int w, out int h);
                sRect = new SDL.SDL_Rect() { w = w, h = h };
            }
        }

        public void Close()
        {
            foreach (var frame in Frames)
            {
                SDL.SDL_DestroyTexture(frame);
            }

        }
    }
}