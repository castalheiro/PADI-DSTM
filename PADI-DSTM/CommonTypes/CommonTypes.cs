﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTypes {
    public interface IClient {
    }

    public interface IPadInt {
    }

    public interface IServer {
        bool createPadInt(int uid);
        bool confirmPadInt(int uid);
        int readPadInt(int tid, int uid);
        bool writePadInt(int tid, int uid, int value);
        bool commit(int tid, List<int> usedPadInts);
        bool abort(int tid, List<int> usedPadInts);
    }

    public interface IMaster {
        int getNextTID();
        int registerServer(String address);
        Tuple<int, string> getPadIntServer(int uid);
        Tuple<int, string> registerPadInt(int uid);
    }

    public interface ILog {
        void log(String[] logs);
    }

    [Serializable]
    public abstract class IPadiException : System.Exception {
        public abstract string getMessage();



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

    public static class Logger {
        static ILog logServer = (ILog) Activator.GetObject(typeof(ILog), "tcp://localhost:7002/LogServer");

        public static void log(String[] args) {
            logServer.log(args);
        }

    }

}
