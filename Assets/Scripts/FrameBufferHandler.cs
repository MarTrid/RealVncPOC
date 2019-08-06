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
using UnityEngine.Experimental.PlayerLoop;
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
        }

        private void UpdateDisplay(RectInt rect)
        {
            lock (bufferLock)
            {
                if (bufferHolder != null && bufferHolder.Buffer != null)
                {
                    if (bufferHolder.Buffer.Length != canvas.width * canvas.height * 4)
                    {
                        Debug.LogWarningFormat("buffer size does not match screen size.");
                        return;
                    }
                    
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

                    if (width == 0 || height == 0)
                    {
                        return;
                    }
                    
                    MainThreadDispatcher.Instance.Invoke(() =>
                    {
                        canvas.Resize(width, height);
                        UpdateDisplay(new RectInt(0, 0, width, height));
                    });
                }
            }
        }

        public void OnFrameBufferUpdated(Rect rc)
        {
            if (rc.width == 0 || rc.height == 0)
            {
                return;
            }
            
            Debug.LogFormat("Updating display at {0}", rc);
            MainThreadDispatcher.Instance.Invoke(() =>
            {
                UpdateDisplay(new RectInt((int)rc.xMin, (int)rc.yMin, (int)rc.width, (int)rc.height));
            });
        }
    }

    /// <summary>
    /// Our buffer container that looks after the low-level memory and resizing of the buffer
    /// </summary>
    public class BufferHolder : IDisposable
    {
        public byte[] Buffer { get; private set; }
        private GCHandle PinnedBuffer;
        public IntPtr BufferPointer { get; private set; }
        
        public void ResizeBuffer(int width, int height, int stride, byte[] buffer)
        {
            Buffer = buffer;
            if (PinnedBuffer.IsAllocated)
            {
                PinnedBuffer.Free();
            }

            PinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            BufferPointer = PinnedBuffer.AddrOfPinnedObject();
            

            //var pixelFormat = System.Drawing.Imaging.PixelFormat.Format32bppRgb;
            //Canvas = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, false, true, pointer);

            // Create the canvas bitmap using the pointer provided from the SDK
            //Canvas = Texture2D.CreateExternalTexture(width, height, TextureFormat.RGBA32, false, true, pointer);
            //Canvas = new Bitmap(width, height, stride * 4, pixelFormat, pointer);
            // Convert data to color32[]
            // set data to canvas
        }

        public bool IsValid => PinnedBuffer.IsAllocated;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (PinnedBuffer.IsAllocated)
            {
                PinnedBuffer.Free();
            }
        }
    }
}