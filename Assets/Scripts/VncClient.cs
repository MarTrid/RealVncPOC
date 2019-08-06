using System;
using System.Threading.Tasks;
using UnityEngine;
using RealVNC.VncSdk;

namespace VncViewerUnity
{
    public class VncClient : MonoBehaviour
    {
        private VncViewerSession session;
        
        private Task<VncLibraryThread> vncLibraryThreadTask;
        private VncLibraryThread vncLibraryThread { get { return vncLibraryThreadTask.Result; } }


        private void Start()
        {
            vncLibraryThreadTask = VncLibraryThread.Start();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                StartConnection();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                session?.Disconnect();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                session?.SendKeyDown(Library.UnicodeToKeysym(54), 54);
                session?.SendKeyUp(54);
                session?.SendKeyDown(Library.UnicodeToKeysym(54), 54);
                session?.SendKeyUp(54);
                session?.SendKeyDown(Library.UnicodeToKeysym(54), 54);
                session?.SendKeyUp(54);
                session?.SendKeyDown(Library.UnicodeToKeysym(54), 54);
                session?.SendKeyUp(54);
                session?.SendKeyDown(Library.UnicodeToKeysym(54), 54);
                session?.SendKeyUp(54);
                session?.SendKeyDown(Library.UnicodeToKeysym(54), 54);
                session?.SendKeyUp(54);
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector2Int? clickedPixel = GetClickedPointOnScreen();

                if (clickedPixel != null)
                {
                    session?.SendPointerEvent(clickedPixel.Value.x, clickedPixel.Value.y, Viewer.MouseButton.Left, false);
                }
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                Vector2Int? clickedPixel = GetClickedPointOnScreen();

                if (clickedPixel != null)
                {
                    session?.SendPointerEvent(clickedPixel.Value.x, clickedPixel.Value.y, Viewer.MouseButton.Zero, false);
                }
            }
        }

        private Vector2Int? GetClickedPointOnScreen()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector2 clickedPoint = hit.textureCoord;
                Vector2Int clickedPixel = new Vector2Int((int)(clickedPoint.x * 2560), (int)(clickedPoint.y * 1440));

                Debug.Log("Clicked screen at " + clickedPixel);

                return clickedPixel;
            }

            return null;
        }

        private void StartConnection()
        {
            VncViewerSession viewerSession;
            try
            {
                Debug.Log("Connecting...");
                
                // Now start the connection
                viewerSession = new VncViewerSession
                {
                    TcpAddress = "localhost",
                    TcpPort = 5900,
                    UsingCloud = false,
                    
                    FrameBufferHandler = GetComponent<FrameBufferHandler>(),

                    OnConnect = () => Debug.Log("Connected"),
                    OnDisconnect = (msg, flags) => OnDisconnect(msg),

                    // Put status messages on the status-label
                    OnNewStatus = (msg) => Debug.Log(msg),

                    CurrentCanvasSize = new Vector2Int(2560,1440)
                };
            }
            
            catch (Exception ex)
            {
                OnDisconnect(ex.Message);
                return;
            }
            
            // Start this viewer session on the library thread.
            vncLibraryThread.StartViewerSession(viewerSession);
            session = viewerSession;
        }
        
        private void OnDisconnect(string disconnectMessage = null)
        {
            // This method is called at the end of every viewer session.

            session = null;
            
            if (string.IsNullOrEmpty(disconnectMessage))
                disconnectMessage = "Disconnected";

            Debug.Log(disconnectMessage);
        }
    }
}
