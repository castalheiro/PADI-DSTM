﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections;


namespace PadIntServer {
    /// <summary>
    /// This class represents the PadInt server
    /// </summary>
    class Server : MarshalByRefObject, IServer, IDisposable {

        /// <summary>
        /// Constant used to represent non atributed server address
        /// </summary>
        private const string NO_SERVER_ADDRESS = "";
        /// <summary>
        /// Server's state
        /// </summary>
        private ServerState serverState;
        /// <summary>
        /// Server identifier
        /// </summary>
        private int identifier;
        /// <summary>
        /// Server address
        /// </summary>
        private string serverAddress;

        private ServerMachine serverMachine;

        public Server(string address, ServerMachine machine) {
            Address = address;
            this.serverMachine = machine;
            this.serverState = new FailedState(this);
        }

        internal int ID {
            set { this.identifier = value; }
            get { return identifier; }
        }

        internal string Address {
            set { this.serverAddress = value; }
            get { return serverAddress; }
        }

        internal ServerState State {
            set { this.serverState = value; }
            get { return this.serverState; }
        }

        public bool Init(int port) {
            Logger.Log(new String[] { "Server", ID.ToString(), "Init", " on port", port.ToString(), serverState.StateMsg });
            try {
                IMaster master = (IMaster)Activator.GetObject(typeof(IMaster), "tcp://localhost:8086/MasterServer");
                Tuple<int, string> info = master.RegisterServer(Address);
                ID = info.Item1;
                string primaryServerAddr = info.Item2;
                if (primaryServerAddr != NO_SERVER_ADDRESS) {
                    CreateBackupServer(primaryServerAddr, serverState.padIntDictionary, false);
                }
            }
            catch (ServerAlreadyExistsException) {
                throw;
            }
            return true;
        }

        /// <summary>
        /// Changes the server's role to primary role.
        /// </summary>
        /// <param name="backupAddress">Backup server address</param>
        /// <param name="id">Server identifier</param>
        /// <param name="padInts">Structure that maps UID to PadInt</param>
        public void CreatePrimaryServer(string backupAddress, Dictionary<int, IPadInt> padInts, bool changeAddress) {
            Logger.Log(new String[] { "Server", ID.ToString(), "createPrimaryServer", "backupAddress ", backupAddress, "id ", ID.ToString(), "padInts ", padInts.Count.ToString(), serverState.StateMsg });
            serverState = new PrimaryServer(this, backupAddress, padInts);
            if (changeAddress) {
                serverMachine.getNewPort();
            }
        }

        /// <summary>
        /// Changes the server's role to backup role.
        /// </summary>
        /// <param name="primaryAddress">Primary server address</param>
        /// <param name="id">Server identifier</param>
        /// <param name="padInts">Structure that maps UID to PadInt</param>
        public void CreateBackupServer(string primaryAddress, Dictionary<int, IPadInt> padInts, bool changeAddress) {
            Logger.Log(new String[] { "Server", ID.ToString(), "createBackupServer", "primaryAddress ", primaryAddress, "id ", ID.ToString(), "padInts ", padInts.Count.ToString(), serverState.StateMsg });
            if (changeAddress) {
                serverMachine.getNewPort();
            }
            serverState = new BackupServer(this, primaryAddress, padInts);
        }

        public void ImAlive() {
            serverState.ImAlive();
        }

        public bool CreatePadInt(int uid) {
            try {
                return serverState.CreatePadInt(uid);
            }
            catch (PadIntAlreadyExistsException) {
                throw;
            }
            catch (ServerDoesNotReplyException) {
                throw;
            }
        }

        public bool ConfirmPadInt(int uid) {
            Logger.Log(new String[] { "Server", ID.ToString(), "confirmPadInt ", "uid", uid.ToString(), serverState.StateMsg });
            try {
                return serverState.ConfirmPadInt(uid);
            }
            catch (PadIntNotFoundException) {
                throw;
            }
            catch (ServerDoesNotReplyException) {
                throw;
            }
        }

        /* Returns the value of the PadInt when the transaction
         *  has the read/write lock.
         * Throw an exception if PadInt not found. 
         */
        public int ReadPadInt(int tid, int uid) {
            Logger.Log(new String[] { "Server", ID.ToString(), "readPadInt ", "tid", tid.ToString(), "uid", uid.ToString(), serverState.StateMsg });

            try {
                return serverState.ReadPadInt(tid, uid);
            }
            catch (PadIntNotFoundException) {
                throw;
            }
            catch (AbortException) {
                throw;
            }
            catch (ServerDoesNotReplyException) {
                throw;
            }
        }

        public bool WritePadInt(int tid, int uid, int value) {
            Logger.Log(new String[] { "Server ", ID.ToString(), " writePadInt ", "tid", tid.ToString(), "uid", uid.ToString(), "value", value.ToString(), serverState.StateMsg });

            try {
                return serverState.WritePadInt(tid, uid, value);
            }
            catch (PadIntNotFoundException) {
                throw;
            }
            catch (AbortException) {
                throw;
            }
            catch (ServerDoesNotReplyException) {
                throw;
            }
        }

        /// <summary>
        /// Commits a transaction on this server
        /// </summary>
        /// <param name="tid">transaction identifier</param>
        /// <param name="usedPadInts">Identifiers of PadInts involved</param>
        /// <returns>A predicate confirming the sucess of the operations</returns>
        public bool Commit(int tid, List<int> usedPadInts) {
            Logger.Log(new String[] { "Server", ID.ToString(), "commit", "tid", tid.ToString(), serverState.StateMsg });

            try {
                return serverState.Commit(tid, usedPadInts);
            }
            catch (PadIntNotFoundException) {
                throw;
            }
            catch (ServerDoesNotReplyException) {
                throw;
            }
        }

        /// <summary>
        /// Aborts a transaction on this server
        /// </summary>
        /// <param name="tid">transaction identifier</param>
        /// <param name="usedPadInts">Identifiers of PadInts involved</param>
        /// <returns>A predicate confirming the sucess of the operations</returns>
        public bool Abort(int tid, List<int> usedPadInts) {
            Logger.Log(new String[] { "Server", ID.ToString(), "abort", "tid", tid.ToString(), serverState.StateMsg });

            try {
                return serverState.Abort(tid, usedPadInts);
            }
            catch (PadIntNotFoundException) {
                throw;
            }
            catch (ServerDoesNotReplyException) {
                throw;
            }
        }

        public bool Freeze() {
            State.imAliveTimer.Stop();
            //State.imAliveTimer.Close();
            serverState = new FrozeState(this);
            Logger.Log(new String[] { "Server", "Freeze", serverState.StateMsg });
            return true;
        }

        public bool Fail() {
            State.imAliveTimer.Stop();
            State.imAliveTimer.Close();
            serverState = new FailedState(this);
            Logger.Log(new String[] { "Server", "Fail", serverState.StateMsg });
            serverMachine.KillServer();
            return true;
        }

        public bool Status() {
            serverState.Status();
            return true;
        }

        public void MovePadInts(List<int> padInts, string receiverAddress) {
            Logger.Log(new String[] { "Server", "MovePadInts", serverState.StateMsg });
            serverState.MovePadInts(padInts, receiverAddress);
        }

        public void ReceivePadInts(Dictionary<int, IPadInt> receivedPadInts) {
            Logger.Log(new String[] { "Server", "ReceivePadInts", serverState.StateMsg });
            serverState.ReceivePadInts(receivedPadInts);
        }

        public void RemovePadInts(List<int> receivedPadInts) {
            Logger.Log(new String[] { "Server", "RemovePadInts", serverState.StateMsg });
            serverState.RemovePadInts(receivedPadInts);
        }

        public void Dispose() {
            serverState.Dispose();
        }

        public override object InitializeLifetimeService() {
            return null;
        }
    }
}
