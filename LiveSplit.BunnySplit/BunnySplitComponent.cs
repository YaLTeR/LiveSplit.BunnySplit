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
        private TimerModel Model;
        public override string ComponentName { get { return "BunnySplit"; } }

        public BunnySplitComponent(LiveSplitState state)
        {
            Model = new TimerModel();
            Model.CurrentState = state;
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
                    Pipe.WriteByte(0x01);
                    Debug.WriteLine("Wrote 0x01 to the pipe.");

                    int len = Pipe.ReadByte();
                    if (len >= 17)
                    {
                        var buf = new byte[len];
                        if (Pipe.Read(buf, 0, len) == len)
                        {
                            var hours = BitConverter.ToInt32(buf, 0);
                            var minutes = BitConverter.ToInt32(buf, 4);
                            var seconds = BitConverter.ToInt32(buf, 8);
                            var milliseconds = BitConverter.ToInt32(buf, 12);
                            CurrentTime = new TimeSpan(hours / 24, hours % 24, minutes, seconds, milliseconds);
                            Debug.WriteLine("Received time: {0}:{1}:{2}.{3}.", hours, minutes, seconds, milliseconds);

                            // 0x00 = paused;
                            // 0x01 = running.
                            switch (buf[16])
                            {
                                case 0x00:
                                    if (state.CurrentPhase == TimerPhase.Running)
                                        Model.Pause();
                                    break;
                                case 0x01:
                                    if (state.CurrentPhase == TimerPhase.Paused)
                                        Model.Pause();
                                    else if (state.CurrentPhase == TimerPhase.NotRunning)
                                        Model.Start();
                                    break;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Read less bytes than requested.");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Len is < 17!");
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
