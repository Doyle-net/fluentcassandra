﻿/**
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements. See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership. The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License. You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied. See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
 * Contains some contributions under the Thrift Software License.
 * Please see doc/old-thrift-license.txt in the Thrift distribution for
 * details.
 */

using FluentCassandra.Thrift.Transport;
using System;
using System.Net.Sockets;

namespace FluentCassandra.Connections {

    /// <summary>
    /// Custom implementation of TStreamTransport, mimicking TSocket while adding connection timeout. 
    /// </summary>
    public class TSocketWithConnectTimeout : TStreamTransport
    {
        private TcpClient client = null;
        private readonly string host = null;
        private readonly int port = 0;
        private readonly int timeout = 0;

        public TSocketWithConnectTimeout(string host, int port, int timeout)
        {
            this.host = host;
            this.port = port;
            this.timeout = timeout;

            InitSocket();
        }

        private void InitSocket()
        {
            client = new TcpClient();
            client.ReceiveTimeout = client.SendTimeout = timeout;
            client.Client.NoDelay = true;
        }

        public override bool IsOpen
        {
            get
            {
                if(client == null)
                {
                    return false;
                }

                return client.Connected;
            }
        }

        public override void Open()
        {
            if(IsOpen)
            {
                throw new TTransportException(TTransportException.ExceptionType.AlreadyOpen, "Socket already connected");
            }

            if(String.IsNullOrEmpty(host))
            {
                throw new TTransportException(TTransportException.ExceptionType.NotOpen, "Cannot open null host");
            }

            if(port <= 0)
            {
                throw new TTransportException(TTransportException.ExceptionType.NotOpen, "Cannot open without port");
            }

            if(client == null)
            {
                InitSocket();
            }

            var connectTimeout = timeout;
            if(connectTimeout == 0)
            {
                connectTimeout = -1;
            }
            else if(connectTimeout > 0 && connectTimeout < 500)
            {
                connectTimeout = 500;
            }

            var ar = client.BeginConnect(host, port, null, null);
            if(ar.AsyncWaitHandle.WaitOne(connectTimeout)) {
                client.EndConnect(ar);
            } else {
                client.Close();
                throw new TimeoutException();
            }
            inputStream = client.GetStream();
            outputStream = client.GetStream();
        }

        public override void Close()
        {
            base.Close();
            if(client != null)
            {
                client.Close();
                client = null;
            }
        }

        #region " IDisposable Support "
        private bool _IsDisposed;

        // IDisposable
        protected override void Dispose(bool disposing)
        {
            if(!_IsDisposed) 
            {
                if(disposing)
                {
                    if(client != null)
                        ((IDisposable)client).Dispose();
                    base.Dispose(disposing);
                }
            }
            _IsDisposed = true;
        }
        #endregion
    }
}
