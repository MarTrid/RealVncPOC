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
           
            /*
            DynamicLoader.LoadLibrary("D:/Repos/VncTest/Assets/RealVnc/");
            Library.Init();
            Library.EnableAddOn("bm8IMo/fpGWC9bhvbGs3jGztKEFbraPnaXxObLoB33YWeQJOIWGP6CHUHGL+CxNtAzeDfaS9m9zuabmJlYOj1hSoXpDpfZNBfXb/1aH4dnM9IqgfYi6mxaE4fn/9f0KcpVvu8Iy8pGP9Nrt8FETNqQDyDPXBR6bc3sL0DEFYKa/h+cZG3R/kPbm8JPlQStNjsn5JwJ3luXyO/z07PZo10weJEPUEskGggQLlx8lUxk7qISQJgVFQY0XJG8rNExgc02l6ibL2PcISM9V6jqlKtLrRrjLXxfZTxGLVa1XL2FHI+2HMHaRJ0HfBiH/sNUgCVqXPAP6O44Cm6yy2Z9gJsnz994OpEGdUsA0MAoeuz6iVXuCX/43lz0oPgm/4bq7XbyxA4cjM/IPASFGX1N4SopaTyyYMcK5wbTaQCAC8A9uzzLZ/MUbcVmBDQJdOMaL+M9kY8yNLFFAh5wF8/RHt2SGpBb24+KRaY90wzMbeentDqXV7sKGDUeuadmkTUTfd3ii/mmmedpOhbXxUryX87lUiuV6Cbc2gW37yNp9LocKyQ14GS108Gp32fhAcHBbkmunAT2ftfuf0nuneyCHPDpTxkapLTNw7HhbScsziYrJB91GRHo3BQUK9rsKxH9Q93h2fpIZi5/kVFu0MT6+1uWIdEYAi7TV1+yOy1gSvywk=");
            */
            vncLibraryThreadTask = VncLibraryThread.Start();
            

            /*
                         
            
                        VncViewerSession session = new VncViewerSession()
                        {
                            AnnotationEnabled = false,
                            TcpAddress = "10.0.75.1",
                            TcpPort = 5900,
                            UsingCloud = false,
                            OnConnect = new Action(delegate
                            {
                                Debug.Log("connected");
                            }),
                            FrameBufferHandler = new FrameBufferHandler(),
                            OnDisconnect = delegate(string s, Viewer.DisconnectFlags flags)
                            {
                                Debug.Log(s + " " + flags);
                            }
                        };
            
                        session.Run();*/

            /*
            viewer = new Viewer();
            
            Viewer.AuthenticationCallback callback = new Viewer.AuthenticationCallback();
            callback.RequestUserCredentials = (targetViewer, user, password) => 
            {
                viewer.SendAuthenticationResponse(true, null, "666666");
            };
            viewer.SetAuthenticationCallback(callback);
            
            DirectTcpConnector connector = new DirectTcpConnector();
    
            connector.Connect("10.0.75.1", 5900, viewer.GetConnectionHandler());
            */
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
        }
        
        private void StartConnection()
        {
            VncViewerSession viewerSession;
            try
            {
                //ButtonConnect.IsEnabled = false;

                Debug.Log("Connecting...");

                //StartAnnotation.IsChecked = false; // Default to off
                //KeyboardControl.IsChecked = true; // So we can input code

                // Now start the connection
                viewerSession = new VncViewerSession
                {
                    //LocalCloudAddress = ConnectSettings.LocalCloudAddress,
                    //LocalCloudPassword = ConnectSettings.LocalCloudPassword,
                    //PeerCloudAddress = ConnectSettings.PeerCloudAddress,
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

                //EventMap = new VncWinformEventMap(viewerSession, VncViewerControl);
/*
                // Give the viewer session user-input events
                if (KeyboardControl.IsChecked == true)
                    EventMap.RegisterKeyboardControls(true);

                if (MouseControl.IsChecked == true)
                    EventMap.RegisterMouseControls(true);

                ManageConnectControls(true);

                // Give the child-control the focus of keyboard
                VncViewerControl.Focus();
                */
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

            /*
            if (EventMap != null)
            {
                EventMap.RegisterKeyboardControls(false);
                EventMap.RegisterMouseControls(false);
                EventMap = null;
            }
*/
            if (string.IsNullOrEmpty(disconnectMessage))
                disconnectMessage = "Disconnected";

            Debug.Log(disconnectMessage);

            /*
            ManageConnectControls(false);
*/
            // If we are closing the main window, continue to do so.
            /*
            if (IsClosing)
                Close();
                */
        }
    }
}
