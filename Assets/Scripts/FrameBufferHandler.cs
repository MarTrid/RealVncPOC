using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VncViewerUnity
{
    public class FrameBufferHandler : MonoBehaviour, IVncFramebufferCallback
    {
        private Material screenMaterial;
        private BufferHolder bufferHolder = new BufferHolder();
        private object bufferLock = new object();
        private bool beBlank = true;
        private bool isDirty = false;

        private void Start()
        {
            Debug.Log("Called start");
            screenMaterial = GetComponent<MeshRenderer>().material;
        }

        /*
        private void Update()
        {
            Debug.Log("Updating screen");
            Debug.LogFormat("BufferHolder: {0}, Buffer: {1}", bufferHolder, bufferHolder == null ? null : bufferHolder.Buffer);


            lock (bufferLock)
            {
                if (bufferHolder != null && bufferHolder.Buffer != null)
                {
                    Debug.Log("But not calling this");
                    List<Color32> pixels = new List<Color32>();
                    Texture2D texture = new Texture2D(2560, 1440);
                    for (int i = 0; i < bufferHolder.Buffer.Length; i = i + 4)
                    {
                        pixels.Add(new Color32(bufferHolder.Buffer[i], bufferHolder.Buffer[i + 1], bufferHolder.Buffer[i + 2], bufferHolder.Buffer[i + 3]));
                    }

                    texture.SetPixels32(pixels.ToArray());

                    screenMaterial.mainTexture = texture;
                    isDirty = false;
                }
            }
        }
        */

        public void OnFrameBufferResized(int width, int height, int stride, byte[] buffer, bool resizeWindow)
        {
            Debug.Log("resized");
            //Debug.Log(buffer.Length);
            lock (bufferLock)
            {
                if (bufferHolder != null)
                {
                    if (buffer != null)
                    {
                        bufferHolder.ResizeBuffer(width, height, stride, buffer);
                        beBlank = false;
                    }
                    else
                    {
                        beBlank = true;
                    }

                    // And redraw
                    //BeginInvoke(new Action(() => { Invalidate(); }));
                }

                //isDirty = true;
            }
        }

        public void OnFrameBufferUpdated(Rect rc)
        {
            Debug.Log("Frame buffer updated.");
            Debug.Log(bufferHolder.Buffer.Length);
            /*
            // Invalidate on the GUI thread
            BeginInvoke(new Action(() =>
            {
                Invalidate(new Rectangle((int)rc.Left, (int)rc.Top, (int)rc.Width, (int)rc.Height));
                Update();
            }));
            */
            //isDirty = true;
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

            Canvas.LoadRawTextureData(pointer, width * height);

            
            //var pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
            Texture2D.allowThreadedTextureCreation = true;
            Canvas = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, false, true, pointer);


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
