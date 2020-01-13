using System.Collections.Generic;
using System.Numerics;
using Perlin.Display;
using Veldrid;

namespace Perlin.Rendering
{
    /// <summary>
    /// Internal class used in rendering.
    /// </summary>
    internal class BatchRenderer
    {
        private DeviceBuffer _vertexBuffer;
        private readonly List<RenderState> _renderQueue = new List<RenderState>();
        internal uint DrawCount;
        private readonly Stack<RenderState> _renderStates = new Stack<RenderState>();
        
        public BatchRenderer()
        {
            _vertexBuffer = PerlinApp.DefaultGraphicsDevice.ResourceFactory.CreateBuffer(
                new BufferDescription(1000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            _renderStates.Push(new RenderState());
        }

        /// <summary>
        /// Called when everything is added to the queue once per frame
        /// </summary>
        public void RenderQueue()
        {
            GraphicsDevice gd = PerlinApp.DefaultGraphicsDevice;
            float width = gd.MainSwapchain.Framebuffer.Width;
            float height = gd.MainSwapchain.Framebuffer.Height;
            gd.UpdateBuffer(
                PerlinApp.Pipeline.OrthoBuffer,
                0,
                Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1));

            EnsureVertexBufferSize((uint)_renderQueue.Count * QuadVertex.VertexSize);
            MappedResourceView<QuadVertex> writeMap = gd.Map<QuadVertex>(_vertexBuffer, MapMode.Write);
            for (int i = 0; i < _renderQueue.Count; i++)
            {
                writeMap[i] = _renderQueue[i].GetGpuVertex();
            }
            gd.Unmap(_vertexBuffer);
            var cl = PerlinApp.CommandList;
            cl.SetPipeline(PerlinApp.Pipeline.Pipeline);
            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetGraphicsResourceSet(0, PerlinApp.Pipeline.OrthoSet);
            DrawCount = 0;
            for (int i = 0; i < _renderQueue.Count;)
            {
                uint batchStart = (uint)i;
                ResourceSet rs = _renderQueue[i].ResSet;
                cl.SetGraphicsResourceSet(1, rs);
                // + textField needs here an extra UpdateBuffer call?
                // cl.UpdateBuffer(_textBuffer, 0, toDraw[0].GpuVertex);
                uint batchSize = 0;
                do
                {
                    i += 1;
                    batchSize += 1;
                }
                while (i < _renderQueue.Count && _renderQueue[i].ResSet == rs);
                DrawCount++;
                cl.Draw(4, batchSize, 0, batchStart);
            }
            _renderQueue.Clear();
        }
        
        private void EnsureVertexBufferSize(uint size)
        {
            if (_vertexBuffer.SizeInBytes < size)
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = PerlinApp.DefaultGraphicsDevice.ResourceFactory.CreateBuffer(
                    new BufferDescription(size, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            }
        }
        
        public void PushRenderState(DisplayObject displayObject)
        {
            var rs = new RenderState();
            //rs.CopyFrom(_renderStates.Peek());
            rs.ApplyNewState(_renderStates.Peek(), displayObject);
            _renderStates.Push(rs);
            
            if (!displayObject.GetBounds().IsEmpty())
            {
                if (rs.ResSet != null)
                {
                    _renderQueue.Add(rs);   
                }
            }
        }

        public void PopRenderState()
        {
            _renderStates.Pop();
        }
    }
}