using System.Diagnostics;
using Display;
using Engine.Display;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine
{
    public class StatsDisplay : Sprite
    {
        private static readonly float UPDATE_INTERVAL = 0.5f;
        private static readonly float B_TO_MB = 1.0f / (1024f * 1024f); // convert from bytes to MB
        private readonly TextField _values;
        private int _frameCount;
        private float _totalTime;
        private const uint _width = 90;
        private const uint _height = 35;
        
        public float Fps;
        public float Memory;
        public float GpuMemory;
        public int DrawCount = 0;
        private int _skipCount;

        /// <summary>
        /// Creates a new Statistics Box.
        /// </summary>
        public StatsDisplay() : base(_width, _height, new Rgba32(0f, 0f, 0f, 0.7f))
        {
            const string gpuLabel = "\ngpu memory:";
            const string labels = "frames/sec:\nstd memory:" + gpuLabel + "\ndraw calls:";
            var font = KestrelApp.FontRobotoMono.CreateFont(14);

            var labels1 = new TextField(font)
            {
                Width = _width - 2,
                Height = _height,
                Text = labels,
                HorizontalAlign = HorizontalAlignment.Left,
                X = 2
            };

            _values = new TextField(font)
            {
                Width = _width - 1,
                Height = _height,
                HorizontalAlign = HorizontalAlignment.Right
            };
            AddChild(labels1);
            AddChild(_values);

            AddedToStage += OnAddedToStage;
            RemovedFromStage += OnRemovedFromStage;
        }

        private void OnAddedToStage(DisplayObject target)
        {
            EnterFrameEvent += OnEnterFrame;
            _totalTime = _frameCount = _skipCount = 0;
            Update();
        }

        private void OnRemovedFromStage(DisplayObject target)
        {
            EnterFrameEvent -= OnEnterFrame;
        }

        private void OnEnterFrame(DisplayObject target, double passedTime)
        {
            _totalTime += (float) (passedTime / 1000);
            _frameCount++;
            if (_totalTime > UPDATE_INTERVAL)
            {
                Update();
                _frameCount = _skipCount = 0;
                _totalTime = 0;
            }
        }

        /// <summary>
        /// Updates the displayed values.
        /// </summary>
        public void Update()
        {
            //todo _background.Tint = _skipCount > (_frameCount / 2) ? (uint)0x003F00 : 0x0;
            Fps = _totalTime > 0 ? _frameCount / _totalTime : 0;
            Process currentProc = Process.GetCurrentProcess();
            Memory = currentProc.PrivateMemorySize64 * B_TO_MB;
            GpuMemory = GetGPUMemory();

            string fpsText = Fps < 100 ? Fps.ToString("N1") : Fps.ToString("N0");
            string memText = Memory < 100 ? Memory.ToString("N1") : Memory.ToString("N0");
            string gpuMemText = GpuMemory < 100 ? GpuMemory.ToString("N1") : GpuMemory.ToString("N0");
            string drwText = (_totalTime > 0 ? DrawCount - 2 : DrawCount).ToString(); // ignore self

            _values.Text = fpsText + "\n" + memText + "\n" +
                           (GpuMemory >= 0 ? gpuMemText + "\n" : "") + drwText;
        }

        /// <summary>
        /// Call this once in every frame that can skip rendering because nothing changed.
        /// </summary>
        public void MarkFrameAsSkipped()
        {
            _skipCount += 1;
        }

        /// <summary>
        /// Returns the currently used GPU memory in bytes. Might not work in all platforms!
        /// </summary>
        private int GetGPUMemory()
        {
            /*
            if (GLExtensions.DeviceSupportsOpenGLExtension("GL_NVX_gpu_memory_info"))
            {
                // this returns in Kb, Nvidia only extension
                int dedicated;
                Gl.Get(Gl.GPU_MEMORY_INFO_DEDICATED_VIDMEM_NVX, out dedicated);

                int available;
                Gl.Get(Gl.GPU_MEMORY_INFO_CURRENT_AVAILABLE_VIDMEM_NVX, out available);

                return (dedicated - available) / 1024;
            }
            */
            return 0;
        }
    }
}