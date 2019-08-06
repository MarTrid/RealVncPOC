using RealVNC.VncSdk;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VncViewerUnity
{
    /// <summary>
    /// Class to run a single thread/task that accesses the VNC library. This is because the thread can only have a single init and shutdown
    /// per process and needs to accessed on a single thread. This task will simply wait for a shutdown signal or
    /// another ViewerSession with which to make a connection attempt and run the connection via the session's Run() method.
    /// </summary>
    class VncLibraryThread : IDisposable
    {
        private readonly Task LibraryTask;
        private readonly TaskCompletionSource<VncLibraryThread> LibraryReady = new TaskCompletionSource<VncLibraryThread>();

        private readonly ManualResetEventSlim NewSession = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim ShouldStop = new ManualResetEventSlim(false);
        
        private VncViewerSession CurrentVncViewerSession;
        private static string libraryPath;

        // To enable direct TCP connectivity you need to copy the content of your add-on code here
        private const string DirectTcpAddOnCode = @"bm8IMo/fpGWC9bhvbGs3jGztKEFbraPnaXxObLoB33YWeQJOIWGP6CHUHGL+CxNtAzeDfaS9m9zuabmJlYOj1hSoXpDpfZNBfXb/1aH4dnM9IqgfYi6mxaE4fn/9f0KcpVvu8Iy8pGP9Nrt8FETNqQDyDPXBR6bc3sL0DEFYKa/h+cZG3R/kPbm8JPlQStNjsn5JwJ3luXyO/z07PZo10weJEPUEskGggQLlx8lUxk7qISQJgVFQY0XJG8rNExgc02l6ibL2PcISM9V6jqlKtLrRrjLXxfZTxGLVa1XL2FHI+2HMHaRJ0HfBiH/sNUgCVqXPAP6O44Cm6yy2Z9gJsnz994OpEGdUsA0MAoeuz6iVXuCX/43lz0oPgm/4bq7XbyxA4cjM/IPASFGX1N4SopaTyyYMcK5wbTaQCAC8A9uzzLZ/MUbcVmBDQJdOMaL+M9kY8yNLFFAh5wF8/RHt2SGpBb24+KRaY90wzMbeentDqXV7sKGDUeuadmkTUTfd3ii/mmmedpOhbXxUryX87lUiuV6Cbc2gW37yNp9LocKyQ14GS108Gp32fhAcHBbkmunAT2ftfuf0nuneyCHPDpTxkapLTNw7HhbScsziYrJB91GRHo3BQUK9rsKxH9Q93h2fpIZi5/kVFu0MT6+1uWIdEYAi7TV1+yOy1gSvywk=";

        /// <summary>
        /// Starts a new VncLibraryThread.
        /// </summary>
        /// <returns>A task that will return the VncLibraryThread once it is ready.</returns>
        public static Task<VncLibraryThread> Start()
        {
            return new VncLibraryThread().LibraryReady.Task;
        }

        private VncLibraryThread()
        {
            libraryPath = Application.dataPath + "/RealVnc";
            LibraryTask = Task.Run(() =>
            {
                if (LoadLibrary())
                {
                    Run();
                }
            });
        }

        /// <summary>
        /// Starts the viewer session. Not safe to call again until the caller
        /// has been notified that the session has ended.
        /// </summary>
        /// <param name="viewerSession">The viewer session to start.</param>
        public void StartViewerSession(VncViewerSession viewerSession)
        {
            CurrentVncViewerSession = viewerSession;
            NewSession.Set();
        }

        private void Run()
        {
            try
            {
                RunLoop();
            }
            catch (Exception e)
            {
                Console.WriteLine("SDK error: {0}: {1}", e.GetType().Name, e.Message);
#if DEBUG
                Console.WriteLine(e.StackTrace);
#endif
            }
            finally
            {
                LibraryShutdown();
            }
        }

        /// <summary>
        /// Loads the library and updates the LibraryReady task result.
        /// </summary>
        /// <returns><code>true</code> on success or <code>false</code> if an
        /// exception occurred.</returns>
        private bool LoadLibrary()
        {
            try
            {
                LibraryInit();
                LibraryReady.SetResult(this);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("SDK error: {0}: {1}", e.GetType().Name, e.Message);
#if DEBUG
                Console.WriteLine(e.StackTrace);
#endif
                LibraryReady.SetException(e);
                return false;
            }
        }

        private void RunLoop()
        {
            // Wait for a new viewer session, or to be told to stop.
            var handles = new[] { NewSession.WaitHandle, ShouldStop.WaitHandle };
            while (WaitHandle.WaitAny(handles) == 0)  // NewSession
            {
                // Reset the event, so that the UI may start a new session
                // as soon as it is notified of this session's disconnection.
                NewSession.Reset();

                //
                // Do all the work in here!
                //
                CurrentVncViewerSession.Run();
            }
        }

        private static void LibraryInit()
        {
            // Load the library.
            DynamicLoader.LoadLibrary(libraryPath);

            // Create a logger with outputs to sys.stderr
            RealVNC.VncSdk.Logger.CreateStderrLogger();

            // Create a file DataStore for storing persistent data for the viewer.
            // Ideally this would be created in a directory that only the viewer
            // user has access to.
            DataStore.CreateFileStore("dataStore.txt");

            // Now initialise the library proper.
            Library.Init();

            if (!string.IsNullOrEmpty(DirectTcpAddOnCode))
                Library.EnableAddOn(DirectTcpAddOnCode);
        }

        private static void LibraryShutdown()
        {
            Library.Shutdown();
        }

        /// <summary>
        /// Stops the library altogether. Should only be called when no viewer
        /// sessions are in progress.
        /// </summary>
        /// <remarks>
        /// Once called, no further connections can be made by this process.
        /// </remarks>
        public void StopLibrary()
        {
            ShouldStop.Set();

            // Stop any running VncViewerSession without a clean disconnection.
            EventLoop.Stop();

            LibraryTask.Wait();
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

            NewSession.Dispose();
            ShouldStop.Dispose();
        }
    }
}
