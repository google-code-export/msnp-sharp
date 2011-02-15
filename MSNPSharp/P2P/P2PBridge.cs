#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp.P2P
{
    #region P2PMessageEventArgs

    /// <summary>
    /// 
    /// </summary>
    public class P2PMessageEventArgs : EventArgs
    {
        private P2PMessage p2pMessage;
        public P2PMessage P2PMessage
        {
            get
            {
                return p2pMessage;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p2pMessage"></param>
        public P2PMessageEventArgs(P2PMessage p2pMessage)
        {
            this.p2pMessage = p2pMessage;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class P2PMessageSessionEventArgs : P2PMessageEventArgs
    {
        private P2PSession p2pSession;
        public P2PSession P2PSession
        {
            get
            {
                return p2pSession;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p2pMessage"></param>
        /// <param name="p2pSession"></param>
        public P2PMessageSessionEventArgs(P2PMessage p2pMessage, P2PSession p2pSession)
            : base(p2pMessage)
        {
            this.p2pSession = p2pSession;
        }
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    public abstract class P2PBridge : IDisposable
    {
        #region Events & Fields

        public event EventHandler<EventArgs> BridgeOpened;
        public event EventHandler<EventArgs> BridgeSynced;
        public event EventHandler<EventArgs> BridgeClosed;
        public event EventHandler<P2PMessageSessionEventArgs> BridgeSent;

        private static uint bridgeCount = 0;
        protected internal uint syncIdentifier = 0;
        protected internal uint localTrackerId = 0;

        protected uint bridgeID = ++bridgeCount;
        protected int queueSize = 0;
        protected Dictionary<P2PSession, P2PSendQueue> sendQueues = new Dictionary<P2PSession, P2PSendQueue>();
        protected Dictionary<P2PSession, P2PSendList> sendingQueues = new Dictionary<P2PSession, P2PSendList>();
        protected List<P2PSession> stoppedSessions = new List<P2PSession>();

        #endregion

        #region Properties

        public abstract bool IsOpen
        {
            get;
        }

        public abstract int MaxDataSize
        {
            get;
        }

        public abstract Contact Remote
        {
            get;
        }

        public virtual bool Synced
        {
            get
            {
                return (0 != syncIdentifier);
            }
        }

        protected internal virtual uint SyncId
        {
            get
            {
                return syncIdentifier;
            }
            set
            {
                syncIdentifier = value;

                if (0 != value)
                {
                    OnBridgeSynced(EventArgs.Empty);
                }
            }
        }

        public virtual Dictionary<P2PSession, P2PSendQueue> SendQueues
        {
            get
            {
                return sendQueues;
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queueSize"></param>
        protected P2PBridge(int queueSize)
        {
            this.queueSize = queueSize;
            this.localTrackerId = (uint)new Random().Next(5000, int.MaxValue);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PBridge {0} created", this.ToString()), GetType().Name);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Dispose()
        {
            sendQueues.Clear();
            sendingQueues.Clear();
            stoppedSessions.Clear();

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
               String.Format("P2PBridge {0} disposed", this.ToString()), GetType().Name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public virtual bool SuitableFor(P2PSession session)
        {
            Contact remote = Remote;

            return (session != null) && (remote != null) && (session.Remote.IsSibling(remote));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public virtual bool Ready(P2PSession session)
        {
            if (queueSize == 0)
                return IsOpen && (!stoppedSessions.Contains(session));

            if (!sendingQueues.ContainsKey(session))
                return IsOpen && SuitableFor(session) && (!stoppedSessions.Contains(session));

            return IsOpen && (sendingQueues[session].Count < queueSize) && (!stoppedSessions.Contains(session));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="remote"></param>
        /// <param name="remoteGuid"></param>
        /// <param name="msg"></param>
        /// <param name="ackHandler"></param>
        public virtual void Send(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg, AckHandler ackHandler)
        {
            if (remote == null)
                throw new ArgumentNullException("remote");

            P2PMessage[] msgs = SetSequenceNumberAndRegisterAck(session, remote, msg, ackHandler);

            if (session == null)
            {
                if (!IsOpen)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "Send called with no session on a closed bridge", GetType().Name);

                    return;
                }

                // Bypass queueing
                foreach (P2PMessage m in msgs)
                {
                    SendOnePacket(null, remote, remoteGuid, m);
                }

                return;
            }

            if (!SuitableFor(session))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "Send called with a session this bridge is not suitable for", GetType().Name);
                return;
            }

            if (!sendQueues.ContainsKey(session))
                sendQueues[session] = new P2PSendQueue();

            foreach (P2PMessage m in msgs)
                sendQueues[session].Enqueue(remote, remoteGuid, m);

            ProcessSendQueues();
        }

        private P2PMessage[] SetSequenceNumberAndRegisterAck(P2PSession session, Contact remote, P2PMessage p2pMessage, AckHandler ackHandler)
        {
            if (p2pMessage.Header.Identifier == 0)
            {
                if (p2pMessage.Version == P2PVersion.P2PV1)
                {
                    p2pMessage.Header.Identifier = ++localTrackerId;
                }
                else if (p2pMessage.Version == P2PVersion.P2PV2)
                {
                    p2pMessage.V2Header.Identifier = localTrackerId;
                }
            }

            if (p2pMessage.Version == P2PVersion.P2PV1 && p2pMessage.V1Header.AckSessionId == 0)
            {
                p2pMessage.V1Header.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);
            }

            P2PMessage[] msgs = p2pMessage.SplitMessage(MaxDataSize);

            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                // Correct local sequence no
                P2PMessage lastMsg = msgs[msgs.Length - 1];
                localTrackerId = lastMsg.V2Header.Identifier + lastMsg.V2Header.MessageSize;
            }

            if (ackHandler != null)
            {
                P2PMessage firstMessage = msgs[0];
                remote.NSMessageHandler.P2PHandler.RegisterAckHandler(firstMessage, ackHandler);
            }

            if (session != null)
            {
                session.LocalIdentifier = localTrackerId;
            }

            return msgs;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void ProcessSendQueues()
        {
            foreach (KeyValuePair<P2PSession, P2PSendQueue> pair in sendQueues)
            {
                lock (pair.Key)
                {
                    while (Ready(pair.Key) && (pair.Value.Count > 0))
                    {
                        P2PSendItem item = pair.Value.Dequeue();

                        if (!sendingQueues.ContainsKey(pair.Key))
                            sendingQueues.Add(pair.Key, new P2PSendList());

                        sendingQueues[pair.Key].Add(item);

                        SendOnePacket(pair.Key, item.Remote, item.RemoteGuid, item.P2PMessage);
                    }
                }
            }

            bool moreQueued = false;
            foreach (KeyValuePair<P2PSession, P2PSendQueue> pair in sendQueues)
            {
                if (pair.Value.Count > 0)
                {
                    moreQueued = true;
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                        String.Format("Queue holds {0} messages for session {1}", pair.Value.Count, pair.Key.SessionId), GetType().Name);
                }
            }

            if (!moreQueued)
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Queues are all empty", GetType().Name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="remote"></param>
        /// <param name="remoteGuid"></param>
        /// <param name="msg"></param>
        protected abstract void SendOnePacket(P2PSession session, Contact remote, Guid remoteGuid, P2PMessage msg);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public virtual void StopSending(P2PSession session)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PBridge {0} stop sending for {1}", this.ToString(), session.SessionId), GetType().Name);

            if (!stoppedSessions.Contains(session))
                stoppedSessions.Add(session);
            else
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Session is already in stopped list", GetType().Name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public virtual void ResumeSending(P2PSession session)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PBridge {0} resume sending for {1}", this.ToString(), session.SessionId), GetType().Name);

            if (stoppedSessions.Contains(session))
            {
                stoppedSessions.Remove(session);
                ProcessSendQueues();
            }
            else
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Session not present in stopped list", GetType().Name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="newBridge"></param>
        public virtual void MigrateQueue(P2PSession session, P2PBridge newBridge)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
               String.Format("P2PBridge {0} migrating session {1} queue to new bridge {2}",
               this.ToString(), session.SessionId, (newBridge != null) ? newBridge.ToString() : "null"), GetType().Name);

            P2PSendQueue newQueue = new P2PSendQueue();

            if (sendingQueues.ContainsKey(session))
            {
                if (newBridge != null)
                {
                    foreach (P2PSendItem item in sendingQueues[session])
                        newQueue.Enqueue(item);
                }

                sendingQueues.Remove(session);
            }

            if (sendQueues.ContainsKey(session))
            {
                if (newBridge != null)
                {
                    while (sendQueues[session].Count > 0)
                        newQueue.Enqueue(sendQueues[session].Dequeue());
                }

                sendQueues.Remove(session);
            }

            if (stoppedSessions.Contains(session))
                stoppedSessions.Remove(session);

            if (newBridge != null)
                newBridge.AddQueue(session, newQueue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="queue"></param>
        public virtual void AddQueue(P2PSession session, P2PSendQueue queue)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
               String.Format("P2PBridge {0} received queue for session {1}", this.ToString(), session.SessionId), GetType().Name);

            if (sendQueues.ContainsKey(session))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                    "A queue is already present for this session, merging the queues", GetType().Name);

                while (queue.Count > 0)
                    sendQueues[session].Enqueue(queue.Dequeue());
            }
            else
                sendQueues[session] = queue;

            ProcessSendQueues();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBridgeOpened(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PBridge {0} opened", this.ToString()), GetType().Name);

            if (BridgeOpened != null)
                BridgeOpened(this, e);

            ProcessSendQueues();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected internal virtual void OnBridgeSynced(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("{0} synced, sync id: {1}", this.ToString(), this.syncIdentifier), GetType().Name);

            if (BridgeSynced != null)
                BridgeSynced(this, e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBridgeClosed(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("P2PBridge {0} closed", this.ToString()), GetType().Name);

            if (BridgeClosed != null)
                BridgeClosed(this, e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnBridgeSent(P2PMessageSessionEventArgs e)
        {
            if (e.P2PMessage.Header.Identifier != 0)
            {
                this.localTrackerId = (e.P2PMessage.Version == P2PVersion.P2PV1)
                    ? e.P2PMessage.Header.Identifier + 1 : e.P2PMessage.Header.Identifier + e.P2PMessage.Header.MessageSize;
            }

            P2PSession session = e.P2PSession;

            if ((session != null) && sendingQueues.ContainsKey(session))
            {
                if (sendingQueues[session].Contains(e.P2PMessage))
                {
                    sendingQueues[session].Remove(e.P2PMessage);
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                        "Sent message not present in sending queue", GetType().Name);
                }
            }

            if (BridgeSent != null)
                BridgeSent(this, e);

            ProcessSendQueues();
        }

        public override string ToString()
        {
            return String.Format("{0}:{1}", bridgeID, GetType().Name);
        }
    }
};
