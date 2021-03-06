﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace CommonTypes {

    /// <summary>
    /// Remote PadInt server interface
    /// </summary>
    public interface IServer {
        void ImAlive();
        void CreatePrimaryServer(string backupAddress, Dictionary<int, IPadInt> padInts, bool changeAddress);
        void CreateBackupServer(string primaryAddress, Dictionary<int, IPadInt> padInts, bool changeAddress);
        bool CreatePadInt(int uid);
        bool ConfirmPadInt(int uid);
        int ReadPadInt(int tid, int uid);
        bool WritePadInt(int tid, int uid, int value);
        bool Commit(int tid, List<int> usedPadInts);
        bool Abort(int tid, List<int> usedPadInts);
        bool Status();
        bool Freeze();
        bool Fail();
        void MovePadInts(List<int> padInts, string receiverAddress);
        void ReceivePadInts(Dictionary<int, IPadInt> receivedPadInts);
        void RemovePadInts(List<int> receivedPadInts);
    }

    public interface IServerMachine {
        bool RestartServer();
    }

    /// <summary>
    /// Remote Master server interface
    /// </summary>
    public interface IMaster {
        int GetNextTID();
        Tuple<int, string> RegisterServer(String address);
        Tuple<int, string> GetPadIntServer(int uid);
        Tuple<int, string> RegisterPadInt(int uid);
        bool Status();
        void UpdateServerAddress(int id, string address);
    }

    /// <summary>
    /// Remote PadInt interface
    /// </summary>
    public interface IPadInt {
        //Nothing to do here
    }

    /// <summary>
    /// Remote PadInt Exception interface
    /// </summary>
    [Serializable]
    public abstract class IPadiException : System.Exception {
        public abstract string GetMessage();

        public IPadiException() {
        }

        public IPadiException(System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
            : base(info, context) {
        }

        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) {
            base.GetObjectData(info, context);
        }
    }

    /// <summary>
    /// Remote Log server interface
    /// </summary>
    public interface ILog {
        void log(String[] logs);
    }

    /// <summary>
    /// Logger class which redirects log messages to LogServer
    /// </summary>
    public static class Logger {

        /// <summary>
        /// The log message
        /// </summary>
        private static string message = DateTime.Now + " ";
        /// <summary>
        /// Predicate that defines if debug mode is one
        /// </summary>
        private static bool debugOn = true;
        /// <summary>
        /// Predicate that defines where the log messages are printed
        /// </summary>
        private static bool isLocal = true;

        /// <summary>
        /// Redirects the log message according to predicates
        /// </summary>
        /// <param name="args">The log message arguments</param>
        public static void Log(String[] args) {
            message = "";
            if (debugOn) {
                if (isLocal) {
                    foreach (String s in args) {
                        message += s + " ";
                    }
                    Console.WriteLine(message);
                }
                else {
                    TcpChannel channel = new TcpChannel();
                    ChannelServices.RegisterChannel(channel, false);
                    ILog logServer = (ILog)Activator.GetObject(typeof(ILog), "tcp://localhost:7002/LogServer");
                    logServer.log(args);
                }
            }
        }
    }
}
