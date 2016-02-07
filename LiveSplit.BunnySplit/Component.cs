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
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LiveSplit.BunnySplit
{
    class Component : LogicComponent
    {
        public override string ComponentName => "BunnySplit";

        private const string PIPE_NAME = "BunnymodXT-BunnySplit";
        private enum MessageType : byte
        {
            Time = 0x00,
            Event = 0x04
        }
        private enum EventType : byte
        {
            GameEnd = 0x00,
            MapChange = 0x01,
            TimerReset = 0x02,
            TimerStart = 0x03
        }

        private interface IEvent
        {
            TimeSpan Time { get; }
        }
        private struct GameEndEvent : IEvent
        {
            public TimeSpan Time { get; set; }
        }
        private struct MapChangeEvent : IEvent
        {
            public TimeSpan Time { get; set; }
            public string Map { get; set; }
        }
        private struct TimerResetEvent : IEvent
        {
            public TimeSpan Time { get; set; }
        }
        private struct TimerStartEvent : IEvent
        {
            public TimeSpan Time { get; set; }
        }

        private ComponentSettings settings = new ComponentSettings();
        private NamedPipeClientStream pipe = new NamedPipeClientStream(
            ".",
            PIPE_NAME,
            PipeAccessRights.ReadData | PipeAccessRights.WriteAttributes,
            PipeOptions.None,
            System.Security.Principal.TokenImpersonationLevel.None,
            System.IO.HandleInheritability.None);
        private CancellationTokenSource cts = new CancellationTokenSource();
        private Thread pipeThread;
        private TimerModel model;
        private TimeSpan currentTime = new TimeSpan();
        private object currentTimeLock = new object();
        private List<IEvent> events = new List<IEvent>();
        private object eventsLock = new object();
        private HashSet<string> visitedMaps = new HashSet<string>();

        private DateTime lastTime = DateTime.Now;

        public Component(LiveSplitState state)
        {
            state.OnStart += OnStart;
            model = new TimerModel() { CurrentState = state };
            model.InitializeGameTime();
            pipeThread = new Thread(PipeThreadFunc);
            pipeThread.Start();
        }

        public override void Dispose()
        {
            cts.Cancel();
            pipeThread.Join();
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return settings.GetSettings(document);
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            return settings;
        }

        public override void SetSettings(XmlNode settings)
        {
            this.settings.SetSettings(settings);
        }

        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            state.IsGameTimePaused = true;
            
            bool start = false;
            lock (eventsLock)
            {
                foreach (var ev in events)
                {
                    if (ev is GameEndEvent)
                    {
                        if (state.CurrentPhase == TimerPhase.Running && settings.ShouldSplitOnGameEnd())
                        {
                            state.SetGameTime(ev.Time);
                            model.Split();
                        }
                    }
                    else if (ev is MapChangeEvent)
                    {
                        var e = (MapChangeEvent)ev;
                        if (visitedMaps.Add(e.Map) && settings.ShouldSplitOn(e.Map))
                        {
                            state.SetGameTime(e.Time);
                            model.Split();
                        }
                    }
                    else if (ev is TimerResetEvent)
                    {
                        if (settings.IsAutoResetEnabled())
                        {
                            state.SetGameTime(ev.Time);
                            model.Reset();
                        }
                    }
                    else if (ev is TimerStartEvent)
                    {
                        if (settings.IsAutoStartEnabled())
                        {
                            state.SetGameTime(ev.Time);
                            start = true;
                        }
                    }
                }
                events.Clear();
            }
            if (start)
                model.Start();

            TimeSpan curTime;
            lock (currentTimeLock)
            {
                curTime = currentTime;
            }
            state.SetGameTime(curTime);
        }

        private void OnStart(object sender, EventArgs e)
        {
            lock (eventsLock)
            {
                events.Clear();
            }

            visitedMaps.Clear();
        }

        private void ParseMessage(byte[] buf)
        {
            switch (buf[1])
            {
                case (byte)MessageType.Time:
                    {
                        var hours = BitConverter.ToInt32(buf, 2);
                        var minutes = BitConverter.ToInt32(buf, 6);
                        var seconds = BitConverter.ToInt32(buf, 10);
                        var milliseconds = BitConverter.ToInt32(buf, 14);
                        //Debug.WriteLine("Received time: {0}:{1}:{2}.{3}.", hours, minutes, seconds, milliseconds);

                        lock (currentTimeLock)
                        {
                            currentTime = new TimeSpan(hours / 24, hours % 24, minutes, seconds, milliseconds);
                        }
                    }
                    break;

                case (byte)MessageType.Event:
                    {
                        var eventType = buf[2];
                        var hours = BitConverter.ToInt32(buf, 3);
                        var minutes = BitConverter.ToInt32(buf, 7);
                        var seconds = BitConverter.ToInt32(buf, 11);
                        var milliseconds = BitConverter.ToInt32(buf, 15);
                        var time = new TimeSpan(hours / 24, hours % 24, minutes, seconds, milliseconds);

                        switch (eventType)
                        {
                            case (byte)EventType.GameEnd:
                                {
                                    var ev = new GameEndEvent() { Time = time };
                                    lock (eventsLock)
                                    {
                                        events.Add(ev);
                                    }
                                    Debug.WriteLine("Received a game end event: {0}:{1}:{2}.{3}.", hours, minutes, seconds, milliseconds);
                                }
                                break;

                            case (byte)EventType.MapChange:
                                {
                                    var len = BitConverter.ToInt32(buf, 19);
                                    string map = System.Text.Encoding.ASCII.GetString(buf, 23, len);

                                    var ev = new MapChangeEvent { Time = time, Map = map };
                                    lock (eventsLock)
                                    {
                                        events.Add(ev);
                                    }
                                    Debug.WriteLine("Received a map change event: {0}:{1}:{2}.{3}; {4}.", hours, minutes, seconds, milliseconds, map);
                                }
                                break;

                            case (byte)EventType.TimerReset:
                                {
                                    var ev = new TimerResetEvent() { Time = time };
                                    lock (eventsLock)
                                    {
                                        events.Add(ev);
                                    }
                                    Debug.WriteLine("Received a timer reset event: {0}:{1}:{2}.{3}.", hours, minutes, seconds, milliseconds);
                                }
                                break;

                            case (byte)EventType.TimerStart:
                                {
                                    var ev = new TimerStartEvent() { Time = time };
                                    lock (eventsLock)
                                    {
                                        events.Add(ev);
                                    }
                                    Debug.WriteLine("Received a timer start event: {0}:{1}:{2}.{3}.", hours, minutes, seconds, milliseconds);
                                }
                                break;

                            default:
                                Debug.WriteLine("Received an unknown event type: " + buf[2]);
                                break;
                        }
                    }
                    break;

                default:
                    Debug.WriteLine("Received an unknown message type: " + buf[1]);
                    break;
            }
        }

        private void PipeThreadFunc()
        {
            while (!cts.IsCancellationRequested)
            {
                if (!pipe.IsConnected)
                {
                    Debug.WriteLine("Connecting to the pipe.");
                    try
                    {
                        pipe.Connect(0);
                        Debug.WriteLine("Connected to the pipe. Readmode: " + pipe.ReadMode);
                        pipe.ReadMode = PipeTransmissionMode.Message;
                        Debug.WriteLine("Set the message read mode.");
                    }
                    catch (Exception e) when (e is TimeoutException || e is IOException)
                    {
                        Debug.WriteLine("Idling for 1 second.");
                        cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                        continue;
                    }
                }

                // Connected to the pipe.
                try
                {
                    var buf = new byte[256];
                    var task = pipe.ReadAsync(buf, 0, 256, cts.Token);
                    task.Wait();

                    if (task.Result == 0)
                    {
                        // The pipe was closed.
                        Debug.WriteLine("Pipe end of stream reached.");
                        continue;
                    }

                    if (buf[0] != task.Result)
                    {
                        Debug.WriteLine("Received an incorrect number of bytes (" + task.Result + ", expected " + buf[0] + ").");
                        continue;
                    }

                    ParseMessage(buf);
                }
                catch (AggregateException e)
                {
                    foreach (var ex in e.InnerExceptions)
                    {
                        if (ex is TaskCanceledException)
                        {
                            return;
                        }
                    }

                    Debug.WriteLine("Error reading from the pipe:");
                    foreach (var ex in e.InnerExceptions)
                    {
                        Debug.WriteLine("- " + ex.GetType().Name + ": " + ex.Message);
                    }
                    Debug.WriteLine("Idling for 1 second.");
                    cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    continue;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Error reading from the pipe: " + e.Message);
                    Debug.WriteLine("Idling for 1 second.");
                    cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    continue;
                }
            }
        }
    }
}
