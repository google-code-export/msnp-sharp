#region Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2008, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
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
using System.Collections;
using System.Diagnostics;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;
    using System.Collections.Generic;

    /// <summary>
    /// Used in events where a P2PMessageSession object is created, or in another way affected.
    /// </summary>
    public class P2PSessionAffectedEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private P2PMessageSession session;

        /// <summary>
        /// The affected session
        /// </summary>
        public P2PMessageSession Session
        {
            get
            {
                return session;
            }
            set
            {
                session = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="affectedSession"></param>
        public P2PSessionAffectedEventArgs(P2PMessageSession affectedSession)
        {
            session = affectedSession;
        }
    }

    /// <summary>
    /// Handles incoming P2P messages from the switchboardserver.
    /// </summary>
    public partial class P2PHandler : IMessageHandler
    {
        /// <summary>
        /// </summary>
        private ArrayList messageSessions = new ArrayList();

        /// <summary>
        /// A list of all current p2p message sessions. Multiple threads can access this resource so make sure to lock this.
        /// </summary>
        public ArrayList MessageSessions
        {
            get
            {
                return messageSessions;
            }
        }


        /// <summary>
        /// Aborts and cleans up all running messagesessions and their transfersessions.
        /// </summary>
        public void ClearMessageSessions()
        {
            lock (MessageSessions)
            {
                foreach (P2PMessageSession session in MessageSessions)
                {
                    session.AbortAllTransfers();
                }
            }
            MessageSessions.Clear();
        }

        /// <summary>
        /// </summary>
        private NSMessageHandler nsMessageHandler;

        /// <summary>
        /// The nameserver handler. This object is used to request new switchboard sessions.
        /// </summary>
        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
            set
            {
                nsMessageHandler = value;
                nsMessageHandler.SBCreated += new EventHandler<SBCreatedEventArgs>(nsMessageHandler_SBCreated);
                nsMessageHandler.ContactOffline += new EventHandler<ContactEventArgs>(nsMessageHandler_ContactOffline);
            }
        }

        /// <summary>
        /// </summary>
        private List<SBMessageHandler> switchboardSessions = new List<SBMessageHandler>(0);

        /// <summary>
        /// A collection of all available switchboard sessions
        /// </summary>
        internal List<SBMessageHandler> SwitchboardSessions
        {
            get
            {
                return switchboardSessions;
            }
        }

        /// <summary>
        /// Occurs when a P2P session is created.
        /// </summary>
        public event EventHandler<P2PSessionAffectedEventArgs> SessionCreated;

        /// <summary>
        /// Occurs when a P2P session is closed.
        /// </summary>
        public event EventHandler<P2PSessionAffectedEventArgs> SessionClosed;

        /// <summary>
        /// Fires the SessionCreated event.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnSessionCreated(P2PMessageSession session)
        {
            if (SessionCreated != null)
                SessionCreated(this, new P2PSessionAffectedEventArgs(session));
        }

        /// <summary>
        /// Fires the SessionClosed event.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnSessionClosed(P2PMessageSession session)
        {
            if (SessionClosed != null)
                SessionClosed(this, new P2PSessionAffectedEventArgs(session));
        }

        /// <summary>
        /// Gets a reference to a p2p message session with the specified remote contact.
        /// In case a session does not exist a new session will be created and returned.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <returns></returns>
        public virtual P2PMessageSession GetSession(string localContact, string remoteContact)
        {
            // check for existing session
            P2PMessageSession existingSession = GetSessionFromRemote(remoteContact);
            if (existingSession != null)
                return existingSession;

            // no session available, create a new session
            P2PMessageSession newSession = CreateSessionFromLocal(localContact, remoteContact);
            lock (MessageSessions)
                MessageSessions.Add(newSession);

            // fire event
            OnSessionCreated(newSession);

            return newSession;
        }

        /// <summary>
        /// Creates a p2p session. The session is at the moment of return pure fictive; no actual messages
        /// have been sent to the remote client. The session will use the P2PHandler's messageprocessor as it's default messageprocessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <returns></returns>
        protected virtual P2PMessageSession CreateSessionFromLocal(string localContact, string remoteContact)
        {
            P2PMessageSession session = Factory.CreateP2PMessageSession();

            // set the parameters
            session.RemoteContact = remoteContact;
            session.LocalContact = localContact;
            session.MessageProcessor = MessageProcessor;

            session.ProcessorInvalid += new EventHandler<EventArgs>(session_ProcessorInvalid);

            // generate a local base identifier.
            session.LocalBaseIdentifier = (uint)((new Random()).Next(10000, int.MaxValue));

            // uses -1 because the first message must be the localbaseidentifier and the identifier
            // is automatically increased
            session.LocalIdentifier = (uint)session.LocalBaseIdentifier;// - (ulong)session.LocalInitialCorrection);//session.LocalBaseIdentifier - 4;

            return session;
        }

        /// <summary>
        /// Gets a switchboard session with the specified remote contact present in the session. Null is returned if no such session is found.
        /// </summary>
        /// <param name="remoteContact"></param>
        /// <returns></returns>
        protected SBMessageHandler GetSwitchboardSession(string remoteContact)
        {
            foreach (SBMessageHandler handler in switchboardSessions)
            {
                if (handler.Contacts.Count == 1 && handler.Contacts.ContainsKey(remoteContact) && handler.IsSessionEstablished)
                    return handler;
            }
            return null;
        }

        /// <summary>
        /// Gets the p2p message session for which the remote identifier equals the identifier passed as a parameter.
        /// This is typically called when an incoming message is processed.
        /// </summary>
        /// <param name="remoteContact">The identifier used by the remote client</param>
        /// <returns></returns>
        protected P2PMessageSession GetSessionFromRemote(string remoteContact)
        {
            lock (messageSessions)
            {
                foreach (P2PMessageSession session in messageSessions)
                {
                    if (session.RemoteContact == remoteContact)
                        return session;
                }
            }
            return null;
        }


        /// <summary>
        /// Creates a session based on a message received from the remote client.
        /// </summary>
        /// <param name="receivedMessage"></param>
        /// <returns></returns>
        protected P2PMessageSession CreateSessionFromRemote(P2PMessage receivedMessage)
        {
            P2PMessageSession session = Factory.CreateP2PMessageSession();

            // generate a local base identifier. After the acknowledgement the locals client begins at
            // identifier - 4 as identifiers in the following messages. (Weird)
            session.LocalBaseIdentifier = (uint)((new Random()).Next(10000, int.MaxValue));
            session.LocalIdentifier = (uint)(session.LocalBaseIdentifier);// - (ulong)session.LocalInitialCorrection);

            session.ProcessorInvalid += new EventHandler<EventArgs>(session_ProcessorInvalid);

            // setup the remote identifier
            session.RemoteBaseIdentifier = receivedMessage.Identifier;
            session.RemoteIdentifier = receivedMessage.Identifier;

            return session;
        }

        /// <summary>
        /// After the first acknowledgement we must set the identifier of the remote client.
        /// </summary>
        /// <param name="receivedMessage"></param>
        protected P2PMessageSession SetSessionIdentifiersAfterAck(P2PMessage receivedMessage)
        {
            P2PMessageSession session = GetSessionFromLocal(receivedMessage.AckSessionId);

            if (session == null)
                throw new MSNPSharpException("P2PHandler: an acknowledgement for the creation of a P2P session was received, but no local created session could be found with the specified identifier.");

            // set the remote identifiers. 
            session.RemoteBaseIdentifier = receivedMessage.Identifier;
            session.RemoteIdentifier = (uint)(receivedMessage.Identifier);// - (ulong)session.RemoteInitialCorrection);

            return session;
        }

        /// <summary>
        /// Gets the p2p message session for which the local identifier equals the identifier passed as a parameter.
        /// This is typically called when a message is created.
        /// </summary>
        /// <param name="identifier">The identifier used by the remote client</param>
        /// <returns></returns>
        protected P2PMessageSession GetSessionFromLocal(uint identifier)
        {
            lock (messageSessions)
            {
                foreach (P2PMessageSession session in messageSessions)
                {
                    if (session.LocalIdentifier == identifier)
                        return session;
                }
            }
            return null;
        }

        /// <summary>
        /// Protected constructor.
        /// </summary>
        protected P2PHandler()
        {

        }

        #region IMessageHandler Members
        private IMessageProcessor messageProcessor;
        /// <summary>
        /// The message processor that will send the created P2P messages to the remote contact.
        /// </summary>
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return messageProcessor;
            }
            set
            {
                messageProcessor = value;
            }
        }

        

        #endregion

        /// <summary>
        /// Requests a new switchboard processor.
        /// </summary>
        /// <remarks>
        /// This is done by delegating the request to the nameserver handler. The supplied contact is also direct invited to the newly created switchboard session.
        /// </remarks>
        /// <param name="remoteContact"></param>
        protected virtual SBMessageHandler RequestSwitchboard(string remoteContact)
        {

            if (nsMessageHandler.ContactList.HasContact(remoteContact, ClientType.PassportMember))
            {
                SBMessageHandler handler = null;

                handler = Factory.CreateSwitchboardHandler();
                if (NSMessageHandler == null)
                    throw new MSNPSharpException("P2PHandler could not request a new switchboard session because the NSMessageHandler property is null.");

                NSMessageHandler.RequestSwitchboard(handler, this);
                handler.NSMessageHandler = NSMessageHandler;

                handler.Invite(((Contact)NSMessageHandler.ContactList[remoteContact, ClientType.PassportMember]).Mail);
                return handler;
            }
            return null;
        }

        /// <summary>
        /// Add a switchboard handler to the list of switchboard sessions to send messages to.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void AddSwitchboardSession(SBMessageHandler session)
        {
            lock (SwitchboardSessions)
            {
                if (SwitchboardSessions.Contains(session) == false)
                    SwitchboardSessions.Add(session);
            }
        }

        /// <summary>
        /// Removes a switchboard handler from the list of switchboard sessions to send messages to.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void RemoveSwitchboardSession(SBMessageHandler session)
        {
            int remainCount = 0;
            lock (SwitchboardSessions)
            {
                SwitchboardSessions.Remove(session);
                remainCount = SwitchboardSessions.Count;
            }
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "A " + session.GetType().ToString() + " has been removed.");
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "There is/are " + remainCount.ToString() + " switchboard(s) remain(s) unclosed.");
        }

        /// <summary>
        /// Registers events of the new switchboard in order to act on these.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nsMessageHandler_SBCreated(object sender, SBCreatedEventArgs e)
        {

            SBMessageHandler sbHandler = e.Switchboard;
            sbHandler.MessageProcessor.RegisterHandler(this);

            // act on these events to ensure messages are properly sent to the right switchboards
            e.Switchboard.ContactJoined += new EventHandler<ContactEventArgs>(Switchboard_ContactJoined);
            e.Switchboard.ContactLeft += new EventHandler<ContactEventArgs>(Switchboard_ContactLeft);
            e.Switchboard.SessionClosed += new EventHandler<EventArgs>(Switchboard_SessionClosed);
            // keep track of this switchboard
            AddSwitchboardSession(sbHandler);
        }

        /// <summary>
        /// Removes the messageprocessor from the specified messagesession, because it is invalid.
        /// </summary>
        protected virtual void RemoveSessionProcessor(P2PMessageSession session)
        {
            session.MessageProcessor = null;

            return;
        }

        /// <summary>
        /// Sets the specified messageprocessor as the default messageprocessor for the message session.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="processor"></param>
        protected virtual void AddSessionProcessor(P2PMessageSession session, IMessageProcessor processor)
        {
            System.Threading.Thread.CurrentThread.Join(1000);
            session.MessageProcessor = processor;

            return;
        }

        /// <summary>
        /// Updates the internal switchboard collection to reflect the changes.
        /// </summary>
        /// <remarks>
        /// Conversations with more than one contact are not found suitable for p2p transfers.
        /// If multiple contacts are present, then any message sessions associated with the switchboard are unplugged.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Switchboard_ContactJoined(object sender, ContactEventArgs e)
        {
            SBMessageHandler handler = (SBMessageHandler)sender;
#if MSNC12
            if (handler.Contacts.Count > 2) //MSNP18: owner in the switchboard, so there're 2 contacts.
#else
            if (handler.Contacts.Count > 1)
#endif
            {
                // in a conversation with multiple contacts we don't want to send p2p messages.
                foreach (P2PMessageSession session in messageSessions)
                {
                    if (session.MessageProcessor == handler.MessageProcessor)
                    {
                        // set this to null to prevent sending messages to this processor.
                        // it will invalidate the processor when trying to send messages and thus
                        // force the p2p handler to create a new switchboard
                        RemoveSessionProcessor(session);
                    }
                }
            }

#if MSNC12
            if (handler.Contacts.Count == 2)  //MSNP18: owner in the switchboard, so there're 2 contacts.
#else
            if (handler.Contacts.Count == 1)
#endif
            {
                P2PMessageSession session = GetSessionFromRemote(e.Contact.Mail);

                // check whether the session exists and is valid
                // if so, we set this switchboard as new output gateway for the message session.
                if (session != null && session.ProcessorValid == false)
                {
                    AddSessionProcessor(session, handler.MessageProcessor);
                }
            }
        }

        /// <summary>
        /// Removes the switchboard processor from the corresponding p2p message session, if necessary.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Switchboard_ContactLeft(object sender, ContactEventArgs e)
        {
            SBMessageHandler handler = (SBMessageHandler)sender;
            P2PMessageSession session = GetSessionFromRemote(e.Contact.Mail);
            if (session != null && session.MessageProcessor == handler.MessageProcessor)
            {
                // invalidate the processor
                RemoveSessionProcessor(session);
            }
        }

        /// <summary>
        /// Cleans up p2p resources associated with the offline contact.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nsMessageHandler_ContactOffline(object sender, ContactEventArgs e)
        {
            // destroy all message sessions that dealt with this contact
            P2PMessageSession session = GetSessionFromRemote(e.Contact.Mail);
            if (session != null)
            {
                CloseMessageSession(session);
            }
        }

        /// <summary>
        /// Closes a message session.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void CloseMessageSession(P2PMessageSession session)
        {
            // make sure there are no sessions and references left
            session.AbortAllTransfers();
            session.CleanUp();

            // call the event
            OnSessionClosed(session);

            // and remove the session from the list
            lock (messageSessions)
            {
                messageSessions.Remove(session);
            }
        }

        /// <summary>
        /// Requests a new switchboard session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void session_ProcessorInvalid(object sender, EventArgs e)
        {
            P2PMessageSession session = (P2PMessageSession)sender;

            // create a new switchboard to fill the hole
            SBMessageHandler sbHandler = GetSwitchboardSession(session.RemoteContact);

            // if the contact is offline, there is no need to request a new switchboard. close the session.
            if ((NSMessageHandler.ContactList[session.RemoteContact, ClientType.PassportMember]).Status == PresenceStatus.Offline)
            {
                CloseMessageSession(session);
                return;
            }

            // check whether the switchboard handler is valid and has a valid processor.
            // if that is the case, use that processor. Otherwise request a new session.
            if (sbHandler == null || sbHandler.MessageProcessor == null ||
                ((SocketMessageProcessor)sbHandler.MessageProcessor).Connected == false ||
                (sbHandler.Contacts.ContainsKey(session.RemoteContact) == false))
            {
                RequestSwitchboard(session.RemoteContact);
            }
            else
            {
                session.MessageProcessor = sbHandler.MessageProcessor;
            }
        }

        /// <summary>
        /// Removes the session from the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Switchboard_SessionClosed(object sender, EventArgs e)
        {
            RemoveSwitchboardSession((SBMessageHandler)sender);
        }


    }
};