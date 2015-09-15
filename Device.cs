using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FastWebCam
{
    public class Device
    {
        public event Action<string> OnStringSend;

        private RequestResponse _requestResponse;
        private CommandQueue _commandQueue;
        private object _expectingStringLocker;
        private string _expectingString;

        private bool _isCalibrated = false;
        private int _currentCaret = 0, _currentTable = 0;
        private const int MAX_CARET = 4899;
        private const int MAX_TABLE = 2999;

        public Device()
        {
            _expectingStringLocker = new object();
            _requestResponse = new RequestResponse();
            _commandQueue = new CommandQueue(OnCommand);
        }

        private void OnCommand(string send, string expect, Action callback)
        {
            if (OnStringSend != null) { OnStringSend(send); }

            if (!string.IsNullOrEmpty(expect))
            {
                lock (_expectingStringLocker)
                {
                    _expectingString = expect;
                }

                while (_expectingString != null)
                {
                    Thread.Sleep(1);
                }
            }

            if (callback != null)
            {
                callback();
            }
        }

        public void StringReceived(string s)
        {
            if (string.IsNullOrEmpty(s)) { return; }
            lock (_expectingStringLocker)
            {
                if (string.IsNullOrEmpty(_expectingString)) { return; }
                else if (s.Contains(_expectingString))
                {
                    _expectingString = null;
                }
            }
        }

        public void Move(int caret, int table)
        {
            caret = Math.Min(caret, MAX_CARET);
            caret = Math.Max(caret, 0);

            table = Math.Min(table, MAX_TABLE);
            table = Math.Max(table, 0);

            string send, expect;
            _requestResponse.Move(caret, table, out send, out expect);
            _commandQueue.Enqueue(send, expect, () =>
            {
                _currentCaret = caret;
                _currentTable = table;
            });
        }

        public void MoveByDelta(int deltaCaret, int deltaTable)
        {
            var caret = _currentCaret + deltaCaret;
            var table = _currentTable + deltaTable;
            Move(caret, table);
        }

        public void MoveByRatio(double caretRatio, double tableRatio)
        {
            var caret = (int)Math.Round(caretRatio * MAX_CARET);
            var table = (int)Math.Round(tableRatio * MAX_TABLE);
            Move(caret, table);
        }

        public void Calibrate()
        {
            string send, expect;
            _requestResponse.Calibrate(out send, out expect);
            _commandQueue.Enqueue(send, expect, () =>
            {
                _isCalibrated = true;
                _currentCaret = 0;
                _currentTable = 0;
            });
        }

        public void PowerOff()
        {
            string send, expect;
            _requestResponse.PowerOff(out send, out expect);
            _commandQueue.Enqueue(send, expect, () =>
            {
                _isCalibrated = false;
            });
        }

        public void Reset()
        {
            string send, expect;
            _requestResponse.Reset(out send, out expect);
            _commandQueue.ClearQueue();

            lock (_expectingStringLocker) { _expectingString = null; }

            _commandQueue.Enqueue(send, expect, () =>
            {
                _isCalibrated = false;
            });
        }

        public void DeadBeef()
        {
            string send, expect;
            _requestResponse.DeadBeef(out send, out expect);
            _commandQueue.Enqueue(send, expect);
        }

        public void StopQueueThread()
        {
            _commandQueue.Stop();
        }
    }
}
