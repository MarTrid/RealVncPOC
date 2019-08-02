using System;
using System.Runtime.InteropServices;
using Boo.Lang;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace VncViewerUnity
{
    public class FrameBufferHandler : MonoBehaviour, IVncFramebufferCallback
    {
        private Material screenMaterial;
        private BufferHolder bufferHolder = new BufferHolder();

        private void Start()
        {
            screenMaterial = GetComponent<MeshRenderer>().material;
        }

        public void OnFrameBufferResized(int width, int height, int stride, byte[] buffer, bool resizeWindow)
        {
            Debug.Log("Frame buffer resized.");

            Texture2D canvasTexture = new Texture2D(width, height, GraphicsFormat.R8G8B8_UInt, TextureCreationFlags.None);
            canvasTexture.width = width;
            canvasTexture.height = height;
            List<Color32> pixels = new List<Color32>();
            for (int i = 0; i < width * height; i = i + 4)
            {
                pixels.Add(new Color32(buffer[i], buffer[i + 1], buffer[i + 2], buffer[i + 3]));
            }
            
            canvasTexture.SetPixels32(pixels.ToArray());
        }

        public void OnFrameBufferUpdated(Rect rc)
        {
            Debug.Log("Frame buffer updated.");
            /*
            // Invalidate on the GUI thread
            BeginInvoke(new Action(() =>
            {
                Invalidate(new Rectangle((int)rc.Left, (int)rc.Top, (int)rc.Width, (int)rc.Height));
                Update();
            }));
            */
        }
    }
    
    /// <summary>
    /// Our buffer container that looks after the low-level memory and resizing of the buffer
    /// </summary>
    public class BufferHolder : IDisposable
    {
        public byte[] Buffer { get; private set; }
        private GCHandle PinnedBuffer;
        private Texture2D Canvas;

        public void ResizeBuffer(int width, int height, int stride, byte[] buffer)
        {
            this.Buffer = buffer;
            if (PinnedBuffer.IsAllocated)
            {
                PinnedBuffer.Free();
            }
            PinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr pointer = PinnedBuffer.AddrOfPinnedObject();

            //var pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppRgb;

            // Create the canvas bitmap using the pointer provided from the SDK
            Canvas = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, false, true, pointer);
            //Canvas = new Bitmap(width, height, stride * 4, pixelFormat, pointer);
            // Convert data to color32[]
            // set data to canvas
            
        }

        public bool IsValid => Canvas != null;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (Canvas != null)
            {
                Canvas = null;
            }

            if (PinnedBuffer.IsAllocated)
            {
                PinnedBuffer.Free();
            }
        }
    }
}
