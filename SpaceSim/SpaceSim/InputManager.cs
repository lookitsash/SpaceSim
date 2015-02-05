using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using BEPUphysics.Paths.PathFollowing;

namespace SpaceSim
{
    public class InputManager
    {
        KeyboardState previousKeyboardState = new KeyboardState();
        KeyboardState currentKeyboardState = new KeyboardState();
        Game game;

        const float DEFAULT_MOUSE_SMOOTHING_SENSITIVITY = 0.5f;
        const int MOUSE_SMOOTHING_CACHE_SIZE = 10;

        int savedMousePosX;
        int savedMousePosY;
        int mouseIndex;
        float mouseSmoothingSensitivity;
        Vector2[] mouseMovement;
        Vector2[] mouseSmoothingCache;
        Vector2 smoothedMouseMovement;
        MouseState currentMouseState;
        MouseState previousMouseState;
        bool Paused = false;
        ShipEntity playerEntity;

        public InputManager(Game game, ShipEntity playerEntity)
        {
            this.game = game;
            this.playerEntity = playerEntity;
            savedMousePosX = -1;
            savedMousePosY = -1;
            mouseSmoothingCache = new Vector2[MOUSE_SMOOTHING_CACHE_SIZE];
            mouseSmoothingSensitivity = DEFAULT_MOUSE_SMOOTHING_SENSITIVITY;
            mouseIndex = 0;
            mouseMovement = new Vector2[2];
            mouseMovement[0].X = 0.0f;
            mouseMovement[0].Y = 0.0f;
            mouseMovement[1].X = 0.0f;
            mouseMovement[1].Y = 0.0f;

            Rectangle clientBounds = game.Window.ClientBounds;
            Mouse.SetPosition(clientBounds.Width / 2, clientBounds.Height / 2);
        }

        /// <summary>
        /// Filters the mouse movement based on a weighted sum of mouse
        /// movement from previous frames.
        /// <para>
        /// For further details see:
        ///  Nettle, Paul "Smooth Mouse Filtering", flipCode's Ask Midnight column.
        ///  http://www.flipcode.com/cgi-bin/fcarticles.cgi?show=64462
        /// </para>
        /// </summary>
        /// <param name="x">Horizontal mouse distance from window center.</param>
        /// <param name="y">Vertical mouse distance from window center.</param>
        void PerformMouseFiltering(float x, float y)
        {
            // Shuffle all the entries in the cache.
            // Newer entries at the front. Older entries towards the back.
            for (int i = mouseSmoothingCache.Length - 1; i > 0; --i)
            {
                mouseSmoothingCache[i].X = mouseSmoothingCache[i - 1].X;
                mouseSmoothingCache[i].Y = mouseSmoothingCache[i - 1].Y;
            }

            // Store the current mouse movement entry at the front of cache.
            mouseSmoothingCache[0].X = x;
            mouseSmoothingCache[0].Y = y;

            float averageX = 0.0f;
            float averageY = 0.0f;
            float averageTotal = 0.0f;
            float currentWeight = 1.0f;

            // Filter the mouse movement with the rest of the cache entries.
            // Use a weighted average where newer entries have more effect than
            // older entries (towards the back of the cache).
            for (int i = 0; i < mouseSmoothingCache.Length; ++i)
            {
                averageX += mouseSmoothingCache[i].X * currentWeight;
                averageY += mouseSmoothingCache[i].Y * currentWeight;
                averageTotal += 1.0f * currentWeight;
                currentWeight *= mouseSmoothingSensitivity;
            }

            // Calculate the new smoothed mouse movement.
            smoothedMouseMovement.X = averageX / averageTotal;
            smoothedMouseMovement.Y = averageY / averageTotal;
        }

        /// <summary>
        /// Averages the mouse movement over a couple of frames to smooth out
        /// the mouse movement.
        /// </summary>
        /// <param name="x">Horizontal mouse distance from window center.</param>
        /// <param name="y">Vertical mouse distance from window center.</param>
        void PerformMouseSmoothing(float x, float y)
        {
            mouseMovement[mouseIndex].X = x;
            mouseMovement[mouseIndex].Y = y;

            smoothedMouseMovement.X = (mouseMovement[0].X + mouseMovement[1].X) * 0.5f;
            smoothedMouseMovement.Y = (mouseMovement[0].Y + mouseMovement[1].Y) * 0.5f;

            mouseIndex ^= 1;
            mouseMovement[mouseIndex].X = 0.0f;
            mouseMovement[mouseIndex].Y = 0.0f;
        }

        /// <summary>
        /// Resets all mouse states. This is called whenever the mouse input
        /// behavior switches from click-and-drag mode to real-time mode.
        /// </summary>
        void ResetMouse()
        {
            currentMouseState = Mouse.GetState();
            previousMouseState = currentMouseState;

            for (int i = 0; i < mouseMovement.Length; ++i)
                mouseMovement[i] = Vector2.Zero;

            for (int i = 0; i < mouseSmoothingCache.Length; ++i)
                mouseSmoothingCache[i] = Vector2.Zero;

            savedMousePosX = -1;
            savedMousePosY = -1;

            smoothedMouseMovement = Vector2.Zero;
            mouseIndex = 0;

            Rectangle clientBounds = game.Window.ClientBounds;

            int centerX = clientBounds.Width / 2;
            int centerY = clientBounds.Height / 2;
            int deltaX = centerX - currentMouseState.X;
            int deltaY = centerY - currentMouseState.Y;

            Mouse.SetPosition(centerX, centerY);
        }

        /// <summary>
        /// Determines which way the mouse wheel has been rolled.
        /// The returned value is in the range [-1,1].
        /// </summary>
        /// <returns>
        /// A positive value indicates that the mouse wheel has been rolled
        /// towards the player. A negative value indicates that the mouse
        /// wheel has been rolled away from the player.
        /// </returns>
        float GetMouseWheelDirection()
        {
            int currentWheelValue = currentMouseState.ScrollWheelValue;
            int previousWheelValue = previousMouseState.ScrollWheelValue;

            if (currentWheelValue > previousWheelValue)
                return 1.0f;
            else if (currentWheelValue < previousWheelValue)
                return -1.0f;
            else
                return 0.0f;
        }

        public void Update(GameTime gameTime)
        {
            previousKeyboardState = currentKeyboardState;
            previousMouseState = currentMouseState;

            currentKeyboardState = Keyboard.GetState();
            currentMouseState = Mouse.GetState();

            if (currentKeyboardState.IsKeyDown(Keys.Escape))
            {
                game.Exit();
            }

            if (previousKeyboardState.IsKeyUp(Keys.P) && (currentKeyboardState.IsKeyDown(Keys.P)))
            {
                Paused = !Paused;
                game.IsMouseVisible = Paused;
            }

            if (!Paused)
            {
                if (!Paused)
                {
                    Rectangle clientBounds = game.Window.ClientBounds;

                    int centerX = clientBounds.Width / 2;
                    int centerY = clientBounds.Height / 2;
                    int deltaX = centerX - currentMouseState.X;
                    int deltaY = centerY - currentMouseState.Y;

                    Mouse.SetPosition(centerX, centerY);

                    PerformMouseFiltering((float)deltaX, (float)deltaY);
                    PerformMouseSmoothing(smoothedMouseMovement.X, smoothedMouseMovement.Y);

                    float dx = smoothedMouseMovement.X;
                    float dy = smoothedMouseMovement.Y;

                    if (dx != 0) playerEntity.PhysicsEntity.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(playerEntity.PhysicsEntity.WorldTransform.Up, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(playerEntity.PhysicsEntity.WorldTransform.Up, dx * 0.01f), 1.0f);
                    if (dy != 0) playerEntity.PhysicsEntity.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(playerEntity.PhysicsEntity.WorldTransform.Left, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(playerEntity.PhysicsEntity.WorldTransform.Left, dy * -0.01f), 1.0f);

                    if (currentMouseState.LeftButton == ButtonState.Pressed) playerEntity.PhysicsEntity.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(playerEntity.PhysicsEntity.WorldTransform.Forward, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(playerEntity.PhysicsEntity.WorldTransform.Forward, -0.15f), 1.0f);
                    if (currentMouseState.RightButton == ButtonState.Pressed) playerEntity.PhysicsEntity.AngularVelocity += EntityRotator.GetAngularVelocity(BEPUutilities.Quaternion.CreateFromAxisAngle(playerEntity.PhysicsEntity.WorldTransform.Forward, 0), BEPUutilities.Quaternion.CreateFromAxisAngle(playerEntity.PhysicsEntity.WorldTransform.Forward, 0.15f), 1.0f);

                    float mouseWheelDirection = GetMouseWheelDirection();
                    if (mouseWheelDirection > 0) playerEntity.ShipSpeedIncrease();
                    else if (mouseWheelDirection < 0) playerEntity.ShipSpeedDecrease();
                    else if (currentMouseState.MiddleButton == ButtonState.Pressed) playerEntity.ShipSpeedZero();
                    playerEntity.PhysicsEntity.LinearVelocity = playerEntity.PhysicsEntity.WorldTransform.Forward * playerEntity.ShipSpeed;
                }
            }
        }
    }
}
