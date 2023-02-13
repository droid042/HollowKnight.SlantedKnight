using Modding;
using System;
using System.Reflection;
using UnityEngine;

namespace Slanted
{
    public class SlantedKnight : Mod, ITogglableMod
    {
        private const float MAX_SLANT_ANGLE = 60;
        private bool hooksAttached = false;
        private bool initialised = false;
        private long lastUpdatedMillis = 0;
        private bool heroFacingRight = true;
        private int maxHealth = 0;
        private bool wasRotated = false;
        private Rigidbody2D rb2d = null;

        public override string GetVersion()
        {
            return "1.0.0.0";
        }

        public override void Initialize()
        {
            if (this.hooksAttached)
            {
                return;
            }
            ModHooks.HeroUpdateHook += this.CustomHeroUpdate;
            ModHooks.SetPlayerIntHook += this.CustomSetPlayerInt;
            this.hooksAttached = true;
        }

        public void Unload()
        {
            ModHooks.HeroUpdateHook -= this.CustomHeroUpdate;
            ModHooks.SetPlayerIntHook -= this.CustomSetPlayerInt;
        }

        private void CustomHeroUpdate()
        {
            if (!this.initialised)
            {
                this.SlantedInit();
                this.initialised = true;
            }
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (now - this.lastUpdatedMillis > 100)
            {
                this.SlantedHeroUpdate();
                this.lastUpdatedMillis = now;
            }
        }

        private int CustomSetPlayerInt(string name, int value)
        {
            if (name == "health" && this.wasRotated)
            {
                float percent = ((float)(value - 1)) / this.maxHealth * 100;
                float shiftedAngle = percent * MAX_SLANT_ANGLE * 2 / 100;
                float angle = shiftedAngle - 60;
                this.Log($"{value - 1} {percent} {shiftedAngle} {angle}");
                var hc = HeroController.instance;
                if (!hc.cState.facingRight)
                {
                    angle = -angle;
                }
                this.rb2d.SetRotation(angle);
            }
            else if (name == "maxHealth")
            {
                this.maxHealth = value - 1;
            }
            return value;
        }

        private void SlantedHeroUpdate()
        {
            var hc = HeroController.instance;
            if (hc.cState.facingRight != this.heroFacingRight
                   && hc.cState.onGround && !hc.cState.touchingWall)
            {
                this.rb2d.SetRotation(-this.rb2d.rotation);
                this.heroFacingRight = hc.cState.facingRight;
                if (!this.wasRotated)
                {
                    this.wasRotated = true;
                }
            }
        }


        private void SlantedInit()
        {
            var hc = HeroController.instance;
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var rb2dProp = hc.GetType().GetField("rb2d", flags);
            this.rb2d = (Rigidbody2D)rb2dProp.GetValue(hc);
            this.rb2d.SetRotation(SlantedKnight.MAX_SLANT_ANGLE);

        }
    }
}