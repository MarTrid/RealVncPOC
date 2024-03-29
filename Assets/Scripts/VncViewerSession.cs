using RealVNC.VncSdk;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VncViewerUnity
{
    public class VncViewerSession
    {
        public string TcpAddress { get; set; }
        public int TcpPort { get; set; }

        public Vector2Int? CurrentCanvasSize { get; set; }
        public IVncFramebufferCallback FrameBufferHandler { get; set; }
        private static VncViewerSession RunningSession;

        public Action OnConnect { get; set; }
        public Action<string, Viewer.DisconnectFlags> OnDisconnect { get; set; }
        public Action<string> OnNewStatus { get; set; }

        public bool AnnotationEnabled { get; set; }

        // A Viewer property that is only valid within a running session.
        private Viewer Viewer { get; set; }

        // A FrameBuffer property that is only valid within a running session.
        private FrameBuffer FrameBuffer { get; set; }

        // Callback properties that are only valid within a running session.
        private Viewer.ConnectionCallback ConnectionCallback { get; set; }
        private Viewer.FramebufferCallback FramebufferCallback { get; set; }
        private Viewer.ServerEventCallback ServerEventCallback { get; set; }
        private AnnotationManager.Callback AnnotationManagerCallback { get; set; }

        // Final result of a session, the disconnection reason and flags.
        private string DisconnectReason = "Stopped";
        private Viewer.DisconnectFlags DisconnectFlags = Viewer.DisconnectFlags.Zero;

        private double ServerAspectRatio;

        /// <summary>
        /// The main routine in which a connection is made and maintained.
        /// This function will only return once the EventLoop is stopped,
        /// which happens when the connection ends.
        /// </summary>
        public void Run()
        {
            try
            {
                // Keep these SDK objects available all the time the session is running.
                Viewer = new Viewer();
                FrameBuffer = new FrameBuffer(Viewer);

                SetUpCallbacks(Viewer);

                // Begin the connection to the Server.

                if (CurrentCanvasSize == null) CurrentCanvasSize = new Vector2Int(1920, 1080);

                UpdateFrameBufferToCanvasSize();

                // Make a Direct TCP connecticon.
                NewStatus($"Connecting to host address: {TcpAddress} port: {TcpPort}");
                using (DirectTcpConnector tcpConnector = new DirectTcpConnector())
                {
                    tcpConnector.Connect(TcpAddress, TcpPort, Viewer.GetConnectionHandler());
                }
                
                // Run the SDK's event loop.  This will return when any thread
                // calls EventLoop.Stop(), allowing this ViewerSession to stop.
                RunningSession = this;
                EventLoop.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine("SDK error: {0}: {1}", e.GetType().Name, e.Message);

                // Handle the implied disconnect (or failed to connect) and show the exception message as a status
                DisconnectReason = e.GetType().Name + ": " + e.Message;
            }
            finally
            {
                RunningSession = null;

                // Dispose of the SDK objects.
                FrameBuffer?.Dispose();
                FrameBuffer = null;

                // If the viewer is still connected, this drops the connection.
                Viewer?.Dispose();
                Viewer = null;

                // Notify that the session is finished and another may be enqueued.
                ReportDisconnection();
            }
        }

        private void ReportDisconnection()
        {
            // Draw a blank control - show we're disconnected (omit this call to keep the last image on screen)
            FrameBufferHandler.OnFrameBufferResized(0, 0, 0, null, false);

            // And pass on to the caller's disconnect routine.
            OnDisconnect.Invoke(DisconnectReason, DisconnectFlags);
        }

        private void SetUpCallbacks(Viewer viewer)
        {
            // Define callbacks to handle session events.

            // Callbacks should be defined as class members, not locals,
            // to prevent them being garbage collected while in use.

            ConnectionCallback = new Viewer.ConnectionCallback(
                connected:
                    (vwr) => OnConnect?.Invoke(),
                disconnected:
                    (vwr, reason, df) =>
                    {
                        if (RunningSession != this)
                            return;  // Already stopped, don't overwrite reason.

                        // On disconnection, record the reason and stop the event loop.
                        // This will cause the EventLoop.Run() call in Run() to return.
                        DisconnectReason = reason;
                        DisconnectFlags = df;
                        EventLoop.Stop();
                    });

            FramebufferCallback = new Viewer.FramebufferCallback(
                serverFbSizeChanged: OnServerFbSizeChanged,
                viewerFbUpdated: OnViewerFbUpdated);

            ServerEventCallback = new Viewer.ServerEventCallback(
                serverClipboardTextChanged: null,
                serverFriendlyNameChanged: OnNameChange);

            AnnotationManagerCallback = new AnnotationManager.Callback(
                availabilityChanged:
                    (am, isAvailable) =>
                    {
                        if (!isAvailable)
                        {
                            NewStatus("Annotation unavailable");
                            AnnotationEnabled = false;
                        }
                    });

            // Set the callbacks on the viewer.
            viewer.SetConnectionCallback(ConnectionCallback);
            viewer.SetFramebufferCallback(FramebufferCallback);
            viewer.SetServerEventCallback(ServerEventCallback);

            // AnnotationManagerCallback is set when annotation is enabled.
        }
        
        private bool UpdateFrameBufferToCanvasSize(Vector2Int? newSize = null, bool requestWindowResize = false)
        {
            if (newSize != null && newSize.Value != Vector2Int.zero)
                CurrentCanvasSize = newSize;

            Vector2Int size = CurrentCanvasSize ?? Vector2Int.zero;

            if (size != Vector2Int.zero)
            {
                if (ServerAspectRatio > 0)
                {
                    // Keep aspect ratio of the source screen and display within the dimensions of our canvas
                    int inferredH = (int)(size.x / ServerAspectRatio);
                    int inferredW = (int)(size.y * ServerAspectRatio);

                    // Go with the smallest size: keep within our canvas / window
                    if (inferredH < size.x)
                        size.y = inferredH;
                    else
                        size.x = inferredW;
                }

                UpdateFrameBufferSize(size.x, size.y, requestWindowResize);
                return true;
            }

            return false;
        }

        private void UpdateFrameBufferSize(int width, int height, bool requestWindowResize = false)
        {
            width = Math.Max(width, 10);
            height = Math.Max(height, 10);
            FrameBuffer.SetBuffer(width, height);
            FrameBufferHandler.OnFrameBufferResized(width, height, width, FrameBuffer.Buffer, requestWindowResize);
        }

        private void OnServerFbSizeChanged(Viewer viewer, int w, int h)
        {
            // The Server screen size has changed, so we signal the window to
            // resize to match its aspect ratio.
            
            ServerAspectRatio = w / (double)h;
            w = viewer.GetViewerFbWidth();
            h = (int)(w / ServerAspectRatio);
            
            FrameBuffer.SetBuffer(w, h);

            // Before we pass it onto the frame-buffer-handler check we have correctly applied the resize to our
            if (!UpdateFrameBufferToCanvasSize())
                FrameBufferHandler.OnFrameBufferResized(w, h, w, FrameBuffer.Buffer, true);
        }

        private void OnViewerFbUpdated(Viewer viewer, int x, int y, int w, int h)
        {
            // The Server has sent fresh pixel data, so we redraw the specified part of the form
            FrameBufferHandler.OnFrameBufferUpdated(new Rect(x, y, w, h));
        }

        /// <summary>
        /// Display a status message to the user
        /// </summary>
        private void NewStatus(string statusMessage)
        {
            Console.WriteLine(statusMessage);

            OnNewStatus?.Invoke(statusMessage);
        }

        /// <summary>
        /// Process a change in the server's friendly name
        /// </summary>
        private void OnNameChange(Viewer vwr, string newName)
        {
            // Display name
            NewStatus($"Server name change: {newName}");
        }

        #region Cross-thread calls

        // Runs an action within the session. Safe to call from any thread.
        private void RunInSession(Action action)
        {
            // The action will be run by the library within EventLoop.Run(),
            // so there will always be a running session when the action runs.
            EventLoop.RunOnLoop(() =>
            {
                // If the session is no longer running, ignore the action.
                if (RunningSession == this)
                    action.Invoke();
            });
        }

        /// <summary>
        /// Stops the session's event loop. Safe to call from any thread.
        /// </summary>
        /// <remarks>
        /// Runs the action on the library thread as part of this session.
        /// </remarks>
        public void StopSession()
        {
            RunInSession(EventLoop.Stop);
        }

        /// <summary>
        /// Starts to cleanly disconnect this session.
        /// Safe to call from any thread.
        /// </summary>
        /// <remarks>
        /// Runs the action on the library thread as part of this session.
        /// </remarks>
        public void Disconnect()
        {
            RunInSession(() => Viewer.Disconnect());
        }

        /// <summary>
        /// Calls Viewer.SendKeyDown() from any thread.
        /// </summary>
        /// <remarks>
        /// Runs the action on the library thread as part of this session.
        /// </remarks>
        public void SendKeyDown(int keysym, int keyCode)
        {
            RunInSession(() => Viewer.SendKeyDown(keysym, keyCode));
        }

        /// <summary>
        /// Calls Viewer.SendKeyUp() from any thread.
        /// </summary>
        /// <remarks>
        /// Runs the action on the library thread as part of this session.
        /// </remarks>
        public void SendKeyUp(int keyCode)
        {
            RunInSession(() => Viewer.SendKeyUp(keyCode));
        }

        /// <summary>
        /// Calls Viewer.SendScrollEvent() from any thread.
        /// </summary>
        /// <remarks>
        /// Runs the action on the library thread as part of this session.
        /// </remarks>
        public void SendScrollEvent(int delta, Viewer.MouseWheel axis)
        {
            RunInSession(() => Viewer.SendScrollEvent(delta, axis));
        }

        /// <summary>
        /// Calls Viewer.SendPointerEvent() from any thread.
        /// </summary>
        /// <remarks>
        /// Runs the action on the library thread as part of this session.
        /// </remarks>
        public void SendPointerEvent(int x, int y, Viewer.MouseButton buttonState, bool rel)
        {
            RunInSession(() => Viewer.SendPointerEvent(x, y, buttonState, rel));
        }

        /// <summary>
        /// Resizes the frame buffer. Safe to call from any thread.
        /// </summary>
        /// <remarks>
        /// Runs the action on the library thread as part of this session.
        /// </remarks>
        public void ResizeFrameBuffer(Vector2Int newSize)
        {
            RunInSession(() => UpdateFrameBufferToCanvasSize(newSize));
        }
        #endregion
    }

    public interface IVncFramebufferCallback
    {
        void OnFrameBufferResized(int width, int height, int stride, byte[] buffer, bool resizeWindow);
        void OnFrameBufferUpdated(Rect rc);
    }


    class FrameBuffer : IDisposable
    {
        private readonly Viewer viewer;
        private GCHandle pinnedBuffer;

        public byte[] Buffer { get; private set; }

        public FrameBuffer(Viewer viewer)
        {
            this.viewer = viewer;
        }

        public void SetBuffer(int width, int height)
        {
            if (pinnedBuffer.IsAllocated)
                pinnedBuffer.Free();
            Buffer = new byte[width * height * 4];
            pinnedBuffer = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            viewer.SetViewerFb(Buffer, PixelFormat.Rgb888(), width, height, width);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (pinnedBuffer.IsAllocated)
                pinnedBuffer.Free();
        }
    }
}
