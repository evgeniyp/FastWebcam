﻿using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace FastWebCam
{
    public class SerialPortWrapper
    {
        public event Action<Exception> OnException;
        public event Action<string> OnData;

        private Encoding _encoding;
        private string _delimiter;
        private byte[] _serialPortBuffer = new byte[65536];
        private Thread _serialPortReader;
        private SerialPort _serialPort;

        private object _receiveStringLock = new object();
        private string _receiveString;

        public SerialPortWrapper(Encoding encoding, string delimiter = "\r\n")
        {
            _receiveString = "";
            _encoding = encoding;
            _delimiter = delimiter;
        }

        public bool Open(string portName)
        {
            _receiveString = "";

            _serialPort = new SerialPort(portName);
            _serialPort.ReadTimeout = 1000;
            _serialPort.Encoding = _encoding;
            _serialPort.BaudRate = 115200;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.DataBits = 8;
            _serialPort.Handshake = Handshake.None;

            if (!_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Open();
                }
                catch (Exception e)
                {
                    if (OnException != null) { OnException(e); }
                    return false;
                }
            }

            _serialPortReader = new Thread(ReaderThreadFunc);
            _serialPortReader.Start();

            return true;
        }

        public void Send(string s)
        {
            var bytes = _encoding.GetBytes(s);

            try
            {
                _serialPort.Write(bytes, 0, bytes.Length);
            }
            catch (Exception e)
            {
                if (OnException != null) { OnException(e); }
            }
        }

        private void ReaderThreadFunc()
        {
            try
            {
                while (true)
                {
                    var bytesToRead = _serialPort.BytesToRead;
                    if (bytesToRead > 0)
                    {
                        int bytesRead = _serialPort.Read(_serialPortBuffer, 0, Math.Min(_serialPortBuffer.Length, bytesToRead));
                        string s = _encoding.GetString(_serialPortBuffer, 0, bytesRead);

                        lock (_receiveStringLock)
                        {
                            _receiveString += s;

                            int delimiterPosition;
                            while ((delimiterPosition = _receiveString.IndexOf(_delimiter)) > -1)
                            {
                                string line = _receiveString.Substring(0, delimiterPosition);
                                _receiveString = _receiveString.Substring(delimiterPosition + _delimiter.Length);

                                if (line.Length > 0 && OnData != null)
                                {
                                    OnData(line);
                                }
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                if (OnException != null) { OnException(e); }
            }
        }

        public void Close()
        {
            if (_serialPort != null)
            {
                _serialPortReader.Abort();
                if (_serialPort.IsOpen) { _serialPort.Close(); }
            }
        }
    }
}
