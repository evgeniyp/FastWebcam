using System;
using System.Collections.Generic;
using System.Threading;

namespace FastWebCam
{
    public struct Command
    {
        public string Send;
        public string Expect;
        public Action Callback;
    }

    public class CommandQueue
    {
        private object _queueLocker;
        private Queue<Command> _queue;
        private Thread _thread;
        private volatile bool _stopRequest;
        private Action<string, string, Action> _onCommand;


        public CommandQueue(Action<string, string, Action> onCommand)
        {
            _queueLocker = new object();
            _queue = new Queue<Command>();

            _onCommand = onCommand;

            StartThread();
        }

        private void StartThread()
        {
            _thread = new Thread(ThreadFunc);
            _thread.IsBackground = false;
            _thread.Start();
        }

        private void ThreadFunc()
        {
            while (!_stopRequest)
            {
                Command currentCommand = new Command() { Send = string.Empty, Expect = string.Empty, Callback = null };
                lock (_queueLocker)
                {
                    if (_queue.Count > 0)
                    {
                        currentCommand = _queue.Dequeue();
                    }
                }

                if (!string.IsNullOrEmpty(currentCommand.Send) && _onCommand != null)
                {
                    _onCommand(currentCommand.Send, currentCommand.Expect, currentCommand.Callback);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        public void ClearQueue()
        {
            lock (_queueLocker)
            {
                _queue.Clear();
            }
        }

        public void Enqueue(string send, string expect, Action callback = null)
        {
            lock (_queueLocker)
            {
                _queue.Enqueue(new Command() { Send = send, Expect = expect, Callback = callback });
            }
        }

        public void Stop()
        {
            _stopRequest = true;
        }
    }
}
