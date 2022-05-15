using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2;

namespace PokemonBattleSimulator.GameClasses
{
    public class HealthUI
    {
        private IntPtr BoxUIImage;
        private SDL.SDL_Rect BoxSRect;
        private SDL.SDL_Rect BoxDRect;
        private IntPtr HealthBarGreen;
        private IntPtr HealthBarAmber;
        private IntPtr HealthBarRed;
        private SDL.SDL_Rect HealthBarSRect;
        private IntPtr HealthFont;
        private IntPtr HealthText;
        private IntPtr NameImage;
        private IntPtr NameFont;
        private SDL.SDL_Rect NameSRect;
        private IntPtr Renderer;
        public Pokemon TrackingPokemon { get; private set; }
        public HealthUI(IntPtr renderer, Pokemon trackingPokemon,int x, int y)
        {
            Renderer = renderer;
            BoxUIImage = SDL_image.IMG_LoadTexture(renderer, "./Assets/BattleFight/HealthBox.png");
            SDL.SDL_QueryTexture(BoxUIImage, out _, out _, out int w, out int h);
            BoxSRect = new SDL.SDL_Rect() { w = w, h = h };
            BoxDRect = new SDL.SDL_Rect() { x=x,y=y,w=w,h=h};
            TrackingPokemon = trackingPokemon;
            
            NameFont = SDL_ttf.TTF_OpenFont("./Assets/BattleFight/Pirulen.ttf", 20);
            var NameImageSurface = SDL_ttf.TTF_RenderText_Blended(NameFont, TrackingPokemon.Name, new SDL.SDL_Color() { r=0,g=0,b=0});
            NameImage = SDL.SDL_CreateTextureFromSurface(Renderer, NameImageSurface);
            SDL.SDL_FreeSurface(NameImageSurface);
            SDL.SDL_QueryTexture(NameImage, out _, out _, out int w_t, out int h_t);
            NameSRect = new SDL.SDL_Rect() { w = w_t, h = h_t };

            HealthBarGreen = SDL_image.IMG_LoadTexture(Renderer, "./Assets/BattleFight/HealthBarGreen.png");
            HealthBarAmber = SDL_image.IMG_LoadTexture(Renderer, "./Assets/BattleFight/HealthBarAmber.png");
            HealthBarRed   = SDL_image.IMG_LoadTexture(Renderer, "./Assets/BattleFight/HealthBarRed.png");
            SDL.SDL_QueryTexture(HealthBarGreen, out _, out _, out w, out h);
            HealthBarSRect = new SDL.SDL_Rect() { x = x, y = y + 44, w = w, h = h};
            
            HealthFont = SDL_ttf.TTF_OpenFont("./Assets/BattleFight/Pirulen.ttf", 18);
        }
        public List<(IntPtr texture, SDL.SDL_Rect sRect, SDL.SDL_Rect dRect, bool destroyTexture)> Draw()
        {
            var Queue = new List<(IntPtr texture, SDL.SDL_Rect sRect, SDL.SDL_Rect dRect, bool destroyTexture)> { };
            Queue.Add((BoxUIImage, BoxSRect, BoxDRect, false));
            //Decide Which Colour health bar to use based on health percentage
            float healthPercent = TrackingPokemon.CurrHealth /(float) TrackingPokemon.Stats["HP"];
            IntPtr HealthBar;
            switch (healthPercent)
            {
                case > 0.66f:
                    HealthBar = HealthBarGreen;
                    break;
                case > 0.33f:
                    HealthBar = HealthBarAmber;
                    break;
                default:
                    HealthBar = HealthBarRed;
                    break;
            }
            Queue.Add((HealthBar,
            new SDL.SDL_Rect() { x = 0, y = 0, w = (int)(HealthBarSRect.w * healthPercent), h = HealthBarSRect.h },
            new SDL.SDL_Rect() {x= HealthBarSRect.x, y= HealthBarSRect.y, w= (int)(280 * healthPercent), h=20 }, false));


            Queue.Add((NameImage, NameSRect, 
                new SDL.SDL_Rect() { x=BoxDRect.x + 10,y= BoxDRect.y+10,w=NameSRect.w, h= NameSRect.h},false));

            var HealthSurface = SDL_ttf.TTF_RenderText_Blended(HealthFont, $"{TrackingPokemon.CurrHealth}/{TrackingPokemon.Stats["HP"]}", new SDL.SDL_Color());
            HealthText = SDL.SDL_CreateTextureFromSurface(Renderer, HealthSurface);
            SDL.SDL_FreeSurface(HealthSurface);
            SDL.SDL_QueryTexture(HealthText, out _, out _, out int w, out int h);
            Queue.Add((HealthText, new SDL.SDL_Rect() { w=w,h=h}, 
                new SDL.SDL_Rect() { x = BoxDRect.x + 150, y = BoxDRect.y + 75, w=w, h=h}, true));
            return Queue;
        }
        public void ChangeTrackingPokemon(Pokemon NewTrackingTarget)
        {
            TrackingPokemon = NewTrackingTarget;
            var Namesurface = SDL_ttf.TTF_RenderText_Blended(NameFont, TrackingPokemon.Name, new SDL.SDL_Color());
            NameImage = SDL.SDL_CreateTextureFromSurface(Renderer, Namesurface);
            SDL.SDL_FreeSurface(Namesurface);
            SDL.SDL_QueryTexture(NameImage, out _, out _, out NameSRect.w, out NameSRect.h);

        }
    }
}
