using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace SpaceSim
{
    public class GameModel
    {
        // To store instance transform matrices in a vertex buffer, we use this custom
        // vertex type which encodes 4x4 matrices as a set of four Vector4 values.
        public static VertexDeclaration InstanceVertexDeclaration = new VertexDeclaration
        (
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
            new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
            new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
            new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3)
        );

        public Model Model;
        public Matrix[] ModelBones, InstanceTransforms;
        public GraphicsDevice GraphicsDevice;

        private DynamicVertexBuffer _InstanceVertexBuffer = null;
        public DynamicVertexBuffer InstanceVertexBuffer
        {
            get
            {
                // If we have more instances than room in our vertex buffer, grow it to the neccessary size.
                if ((_InstanceVertexBuffer == null) || (InstanceTransforms.Length > _InstanceVertexBuffer.VertexCount))
                {
                    if (_InstanceVertexBuffer != null)
                        _InstanceVertexBuffer.Dispose();

                    _InstanceVertexBuffer = new DynamicVertexBuffer(GraphicsDevice, InstanceVertexDeclaration, InstanceTransforms.Length, BufferUsage.WriteOnly);
                }

                // Transfer the latest instance transform matrices into the instanceVertexBuffer.
                _InstanceVertexBuffer.SetData(InstanceTransforms, 0, InstanceTransforms.Length, SetDataOptions.Discard);

                return _InstanceVertexBuffer;
            }
        }

        public GameModel(GraphicsDevice graphicsDevice, ContentManager content, string modelPath)
        {
            GraphicsDevice = graphicsDevice;
            Model = content.Load<Model>(modelPath);
            ModelBones = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(ModelBones);
            //Model.Meshes[0].MeshParts[0].Effect.T
            //Model.Meshes[0].MeshParts[0].
        }

        public void SetInstanceTransforms(List<GameEntity> entities)
        {
            Array.Resize(ref InstanceTransforms, entities.Count);
            for (int i = 0; i < entities.Count; i++)
            {
                InstanceTransforms[i] = entities[i].World;
            }
        }
    }
}
