using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.ImageSharp;

namespace Engine
{
    /// <summary>
    /// This class manages the loading, storing and retrieving of images used in your app.
    /// </summary>
    public class ImageManager
    {
        private readonly Dictionary<string, (ResourceSet, TextureView)> _loadedImages = 
            new Dictionary<string, (ResourceSet, TextureView)>();

        /// <summary>
        /// Loads an stores an image from the disk.
        /// </summary>
        /// <param name="imageName">the image filename</param>
        public (ResourceSet ret, TextureView view) Load(string imageName)
        {
            if (!_loadedImages.TryGetValue(imageName, out (ResourceSet, TextureView) ret))
            {
                GraphicsDevice gd = KestrelApp.DefaultGraphicsDevice;
                var texPath = Path.Combine(AppContext.BaseDirectory, "Assets", imageName);
                var imTex = new ImageSharpTexture(texPath, false);
                var tex = imTex.CreateDeviceTexture(gd, gd.ResourceFactory);
                var view = gd.ResourceFactory.CreateTextureView(tex);
                ResourceSet set = gd.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                    KestrelApp.KestrelPipeline.TexLayout,
                    view,
                    gd.PointSampler)); 
                ret = (set, view);
                _loadedImages.Add(imageName, ret);
            }
            return ret;
        }
        
        /// <summary>
        /// Creates a texture with the given color.
        /// This is not optimal, because it will not be batchable with anything, you should
        /// rather create textures from parts of texture atlases.
        /// </summary>
        public unsafe ResourceSet CreateColoredTexture(uint width, uint height, Rgba32 color)
        {
            GraphicsDevice gd = KestrelApp.DefaultGraphicsDevice;
            var texture = gd.ResourceFactory.CreateTexture(
                TextureDescription.Texture2D(width, height, 1, 1,
                    PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            var textureView = gd.ResourceFactory.CreateTextureView(texture);
            var resSet = gd.ResourceFactory.CreateResourceSet(
                new ResourceSetDescription(
                    KestrelApp.KestrelPipeline.TexLayout,
                    textureView,
                    gd.PointSampler));
            uint size = width * height;
            Rgba32[] arr = new Rgba32[size];
            for (int i = 0; i < size; i++)
            {
                arr[i] = color;
            }
            Span<Rgba32> span = new Span<Rgba32>(arr);
            fixed (void* data = &MemoryMarshal.GetReference(span))
            {
                KestrelApp.DefaultGraphicsDevice.UpdateTexture(
                    texture,
                    (IntPtr)data, size * 4, 0, 0, 0, 
                    width, height, 1, 0, 0);
            }
            return resSet;
        }
    }
}