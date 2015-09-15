using System;

namespace FastWebCam
{
    public class RequestResponse
    {
        private const int DEVICE_MIN_CARET = 0;
        private const int DEVICE_MAX_CARET = 4899;
        private const int DEVICE_MIN_TABLE = 0;
        private const int DEVICE_MAX_TABLE = 2999;

        public void DeadBeef(out string send, out string expect)
        {
            send = "DEADBEEF";
            expect = "Unknown command";
        }

        public void Move(int caret, int table, out string send, out string expect)
        {
            caret = Math.Min(caret, DEVICE_MAX_CARET);
            caret = Math.Max(caret, 0);

            table = Math.Min(table, DEVICE_MAX_TABLE);
            table = Math.Max(table, 0);

            send = String.Format("Y{0} X{1}", caret, table);
            expect = "READY";
        }

        public void Calibrate(out string send, out string expect)
        {
            send = "G28";
            expect = "CALIBRATED=YES";
        }

        public void Reset(out string send, out string expect)
        {
            send = "/X";
            expect = null;
        }

        public void PowerOff(out string send, out string expect)
        {
            send = "M18";
            expect = null;
        }
    }
}