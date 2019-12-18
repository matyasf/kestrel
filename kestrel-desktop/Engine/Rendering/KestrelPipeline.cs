using System;
using System.IO;
using Veldrid;
using Veldrid.SPIRV;

namespace Engine.Rendering
{
    /// <summary>
    /// The GPU pipeline used for rendering.
    /// </summary>
    public class KestrelPipeline
    {
        public Pipeline Pipeline { get; private set; }
        public ResourceLayout TexLayout { get; private set; }
        public ResourceSet OrthoSet { get; private set; }
        public DeviceBuffer OrthoBuffer { get; private set; }
        
        public KestrelPipeline(GraphicsDevice gd)
        {
            ResourceFactory factory = gd.ResourceFactory;
            OrthoBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            var orthoLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("OrthographicProjection", ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            OrthoSet = factory.CreateResourceSet(new ResourceSetDescription(orthoLayout, OrthoBuffer));

            TexLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SpriteTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SpriteSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            Pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleAlphaBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleStrip,
                new ShaderSetDescription(
                    new[]
                    {
                        new VertexLayoutDescription(
                            QuadVertex.VertexSize,
                            1,
                            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                            new VertexElementDescription("Size", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                            new VertexElementDescription("Tint", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Byte4_Norm),
                            new VertexElementDescription("Rotation", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float1))
                    },
                    factory.CreateFromSpirv(
                        new ShaderDescription(ShaderStages.Vertex, LoadShaderBytes("sprite.vert.spv"), "main"),
                        new ShaderDescription(ShaderStages.Fragment, LoadShaderBytes("sprite.frag.spv"), "main"),
                        new CrossCompileOptions(false, false, new SpecializationConstant(0, false)))),
                new[] { orthoLayout, TexLayout },
                gd.MainSwapchain.Framebuffer.OutputDescription));
        }
        
        private static byte[] LoadShaderBytes(string name)
        {
            return File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Engine", "Assets", "Shaders", name));
        }
    }
}