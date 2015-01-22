﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using ConversionHelper;

namespace SpaceSim
{
    public class Starfield : DrawableGameComponent
    {
        Texture2D texture;
        BasicEffect effect;
        VertexBuffer vertices;
        IndexBuffer indices;
        VertexDeclaration vertexDecl;

        int numStars;
        int width;

        Game game;

        public Starfield(Game game, int numStars)
            : base(game)
        {
            this.game = game;
            this.numStars = numStars;
            width = 4;
        }

        public override void Initialize()
        {
            base.Initialize();

            texture = game.Content.Load<Texture2D>("Textures/star-small"); // game.manager.Load<Texture2D>(@"Content\Textures\star_001");
            effect = new BasicEffect(game.GraphicsDevice);

            // vertex declaration required by renderer
            // contains two elements: Position(v3) and TextureCoordinate(v2)
            vertexDecl = new VertexDeclaration(new VertexElement[]
                {
                    new VertexElement(0,VertexElementFormat.Vector3,VertexElementUsage.Position,0),
                    new VertexElement(sizeof(float)*3,VertexElementFormat.Vector2,VertexElementUsage.TextureCoordinate,0)
                });

            // create a vertex buffer with 4 vertices per star
            vertices = new VertexBuffer(game.GraphicsDevice, typeof(VertexPositionTexture), 4 * numStars, BufferUsage.WriteOnly);
            VertexPositionTexture[] data = new VertexPositionTexture[4 * numStars];

            // create an index buffer with 6 indices per star
            indices = new IndexBuffer(game.GraphicsDevice, typeof(short), 6 * numStars, BufferUsage.WriteOnly);
            short[] ib = new short[6 * numStars];

            // create quad for each star
            for (int i = 0; i < numStars; i++)
            {
                Random rand = new Random();
                Vector3 pos = new Vector3(rand.Next(-1000, 1000), rand.Next(-1000, 1000), rand.Next(-1000, 1000));

                // create 4 vertices with texture info
                data[i * 4 + 0].Position = new Vector3(pos.X - width / 2, pos.Y - width / 2, pos.Z + width / 2);
                data[i * 4 + 0].TextureCoordinate.X = 1.0f;
                data[i * 4 + 0].TextureCoordinate.Y = 1.0f;
                data[i * 4 + 1].Position = new Vector3(pos.X - width / 2, pos.Y + width / 2, pos.Z + width / 2);
                data[i * 4 + 1].TextureCoordinate.X = 1.0f;
                data[i * 4 + 1].TextureCoordinate.Y = 0.0f;
                data[i * 4 + 2].Position = new Vector3(pos.X + width / 2, pos.Y + width / 2, pos.Z + width / 2);
                data[i * 4 + 2].TextureCoordinate.X = 0.0f;
                data[i * 4 + 2].TextureCoordinate.Y = 0.0f;
                data[i * 4 + 3].Position = new Vector3(pos.X + width / 2, pos.Y - width / 2, pos.Z + width / 2);
                data[i * 4 + 3].TextureCoordinate.X = 0.0f;
                data[i * 4 + 3].TextureCoordinate.Y = 1.0f;

                // add indices
                ib[0 + i * 6] = (short)(0 + i * 4);
                ib[1 + i * 6] = (short)(2 + i * 4);
                ib[2 + i * 6] = (short)(1 + i * 4);
                ib[3 + i * 6] = (short)(2 + i * 4);
                ib[4 + i * 6] = (short)(0 + i * 4);
                ib[5 + i * 6] = (short)(3 + i * 4);

            }
            vertices.SetData<VertexPositionTexture>(data);
            indices.SetData<short>(ib);

        }

        public override void Draw(GameTime gameTime)
        {
            // nothing to draw
            if (vertices == null)
                return;

            // set up effect params
            effect.World = Matrix.CreateBillboard(MathConverter.Convert(SpaceSimGame.entityShip.Position), SpaceSimGame.camera.Position, Vector3.Up, Vector3.Normalize(SpaceSimGame.camera.Position));
            effect.View = SpaceSimGame.camera.View;
            effect.Projection = SpaceSimGame.camera.Projection;
            effect.LightingEnabled = false;
            effect.TextureEnabled = true;

            // begin effects, set up graphics device
            //VertexBuffer vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexBuffer), vertices.Count, BufferUsage.WriteOnly);
            //vertexBuffer.SetData(vertices.ToArray()); ;
            //vb.V
            effect.Techniques[0].Passes[0].Apply();  //effect.Begin();
            //game.GraphicsDevice.VertexDeclaration = vertexDecl;
            game.GraphicsDevice.SetVertexBuffer(vertices); //game.GraphicsDevice.Vertices[0].SetSource(vertices, 0, vertexDecl.GetVertexStrideSize(0));
            game.GraphicsDevice.Indices = indices;
            //game.GraphicsDevice.RenderState.PointSpriteEnable = true;
             //game.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            
            BlendState prevBlendState = GraphicsDevice.BlendState;
            game.GraphicsDevice.BlendState = new BlendState() { AlphaSourceBlend = Blend.SourceAlpha, AlphaDestinationBlend = Blend.SourceAlpha };
            //game.GraphicsDevice.RenderState.DestinationBlend = Blend.SourceAlpha;
            //game.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            effect.Texture = texture;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();//pass.Begin();

                for (int i = 0; i < numStars; i++)
                {
                    game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, i * 4, 4, i * 6, 2);
                }
                //pass.End();
            }
            //effect.End();


            // reset render settings
            game.GraphicsDevice.BlendState = prevBlendState;
            //game.GraphicsDevice.RenderState.PointSpriteEnable = true;
            //game.GraphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
            //game.GraphicsDevice.RenderState.DestinationBlend = Blend.SourceAlpha;
            //game.GraphicsDevice.RenderState.AlphaBlendEnable = false;
            
            base.Draw(gameTime);
        }


    }
}
