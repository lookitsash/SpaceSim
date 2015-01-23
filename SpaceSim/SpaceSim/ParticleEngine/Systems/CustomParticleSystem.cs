#region File Description
//-----------------------------------------------------------------------------
// FireParticleSystem.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace ParticleEngine
{
    /// <summary>
    /// Custom particle system for creating a flame effect.
    /// </summary>
    class CustomParticleSystem : ParticleSystem
    {
        public CustomParticleSystem(Game game, ContentManager content)
            : base(game, content)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "Textures/smoke";

            settings.MaxParticles = 240;

            settings.Duration = TimeSpan.FromSeconds(0.5);

            settings.DurationRandomness = 1;

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 5;

            settings.MinVerticalVelocity = -5;
            settings.MaxVerticalVelocity = 5;

            // Set gravity upside down, so the flames will 'fall' upward.
            settings.Gravity = Vector3.Zero; // new Vector3(0, 15, 0);

            settings.MinColor = Color.Black; //new Color(255, 255, 255, 255);
            settings.MaxColor = Color.Black; //new Color(255, 255, 255, 255);

            settings.MinStartSize = 1;
            settings.MaxStartSize = 3;

            settings.MinEndSize = 1;
            settings.MaxEndSize = 3;

            // Use additive blending.
            //settings.BlendState = BlendState.Additive;
        }
    }
}
