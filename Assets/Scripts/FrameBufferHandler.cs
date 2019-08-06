using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using RealVNC.VncSdk;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace VncViewerUnity
{
    public class FrameBufferHandler : MonoBehaviour, IVncFramebufferCallback
    {
        private Material screenMaterial;
        private BufferHolder bufferHolder = new BufferHolder();
        private readonly object bufferLock = new object();
        private Texture2D canvas;
        private byte[] testData;

        [SerializeField]
        private Texture2D testTexture;

        private void Start()
        {
            Debug.Log("Called start");
            screenMaterial = GetComponent<MeshRenderer>().material;
            Texture2D.allowThreadedTextureCreation = true;
            canvas = new Texture2D(2560, 1440, TextureFormat.BGRA32, false);
            screenMaterial.mainTexture = canvas;


            testData = new byte[2560 * 1440 * 4];

            for (int i = 0; i < testData.Length; i = i + 4)
            {
                if (i < testData.Length / 2)
                {
                    testData[i] = 255;
                    testData[i + 1] = 127;
                    testData[i + 2] = 0;
                    testData[i + 3] = 0;
                }
                else
                {
                    testData[i] = 127;
                    testData[i + 1] = 255;
                    testData[i + 2] = 0;
                    testData[i + 3] = 0;
                }
            }
        }

        private void Update()
        {
            lock (bufferLock)
            {
                if (bufferHolder != null && bufferHolder.Buffer != null)
                {
                    canvas.LoadRawTextureData(bufferHolder.Buffer);
                    canvas.Apply();
                    Debug.Log("Screen updated.");
                }
            }
        }

        public void OnFrameBufferResized(int width, int height, int stride, byte[] buffer, bool resizeWindow)
        {
            Debug.LogFormat("Resized to {0}x{1}", width, height);

            lock (bufferLock)
            {
                if (bufferHolder != null)
                {
                    if (buffer != null)
                    {
                        bufferHolder.ResizeBuffer(width, height, stride, buffer);
                    }

                    // And redraw
                    //BeginInvoke(new Action(() => { Invalidate(); }));
                }
            }
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
            Debug.Log("Resizing buffer");
            Buffer = buffer;
            if (PinnedBuffer.IsAllocated)
            {
                PinnedBuffer.Free();
            }

            PinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr pointer = PinnedBuffer.AddrOfPinnedObject();
            

            //var pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
            //Canvas = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, false, true, pointer);

            // Create the canvas bitmap using the pointer provided from the SDK
            //Canvas = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, false, true, pointer);
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
            if (!disposing) return;
            
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