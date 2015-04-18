using System.Windows.Forms;
using System.Xml;

using LiveSplit.Model;
using LiveSplit.UI;
using LiveSplit.UI.Components;
using System.IO.Pipes;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace LiveSplit.BunnySplit
{
    class BunnySplitComponent : LogicComponent
    {
        private BunnySplitSettings Settings = new BunnySplitSettings();
        private const string PIPE_NAME = "BunnymodXT-Pipe";
        private NamedPipeClientStream Pipe = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.InOut);
        private CancellationTokenSource CTS = new CancellationTokenSource();
        private Thread PipeThread;
        private TimeSpan CurrentTime = new TimeSpan();
        public override string ComponentName { get { return "BunnySplit"; } }

        public BunnySplitComponent()
        {
            PipeThread = new Thread(PipeKeepaliveFunc);
            PipeThread.Start();
        }

        public override void Dispose()
        {
            CTS.Cancel();
            PipeThread.Join();
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return Settings;
        }

        public override void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            if (Pipe.IsConnected)
            {
                Debug.WriteLine("Trying to communicate through the pipe.");
                try
                {
                    var buf = new byte[17];
                    buf[0] = 0x01;
                    Pipe.Write(buf, 0, 1);

                    Debug.WriteLine("Wrote 0x01 to the pipe.");

                    if (Pipe.Read(buf, 0, 17) == 17)
                    {
                        if (buf[0] == 0x01)
                        {
                            var hours = BitConverter.ToInt32(buf, 1);
                            var minutes = BitConverter.ToInt32(buf, 5);
                            var seconds = BitConverter.ToInt32(buf, 9);
                            var milliseconds = BitConverter.ToInt32(buf, 13);
                            CurrentTime = new TimeSpan(hours / 24, hours % 24, minutes, seconds, milliseconds);
                            Debug.WriteLine("Received time: {0}:{1}:{2}.{3}.", hours, minutes, seconds, milliseconds);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Read less bytes than requested. The pipe has probably disconnected.");
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error communicating through the pipe: {0}", e.Message);
                }
            }

            state.IsGameTimePaused = true;
            state.SetGameTime(CurrentTime);
        }

        private void PipeKeepaliveFunc()
        {
            while (!CTS.IsCancellationRequested)
            {
                if (!Pipe.IsConnected)
                {
                    Debug.WriteLine("Connecting to the pipe.");
                    try
                    {
                        Pipe.Connect(1000);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
