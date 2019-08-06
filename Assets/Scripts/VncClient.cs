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
            Debug.Log(session?.CurrentCanvasSize.Value);
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                StartConnection();
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                session?.StopSession();
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
                Vector2Int? mousePosition = GetMousePosition();

                if (mousePosition != null)
                {
                    session?.SendPointerEvent(mousePosition.Value.x, mousePosition.Value.y, Viewer.MouseButton.Left, false);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                Vector2Int? mousePosition = GetMousePosition();

                if (mousePosition != null)
                {
                    session?.SendPointerEvent(mousePosition.Value.x, mousePosition.Value.y, Viewer.MouseButton.Zero, false);
                }
            }
        }

        private Vector2Int? GetMousePosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector2 point = hit.textureCoord;
                Vector2Int pixel = new Vector2Int((int)(point.x * session.CurrentCanvasSize.Value.x), (int)(point.y * session.CurrentCanvasSize.Value.y));

                Debug.Log("Clicked screen at " + pixel);

                return pixel;
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
