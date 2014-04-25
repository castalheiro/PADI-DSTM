﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;

namespace PadIntServer {
    class FailedServer : ServerState {

        internal FailedServer(Server server)
            : base(server) {
            // Nothing to do here
        }

        /// <summary>
        /// Frailed servers do nothing when this method is called
        /// </summary>
        internal override void ImAlive() {
            //Nothing to do here
        }

        internal override bool createPadInt(int uid) {
            Logger.log(new String[] { "FailedServer", Server.ID.ToString(), "createPadInt", "uid ", uid.ToString() });

            throw new ServerDoesNotReplyException(Server.ID);
        }

        internal override bool confirmPadInt(int uid) {
            Logger.log(new String[] { "FailedServer", Server.ID.ToString(), "confirmPadInt ", "uid", uid.ToString() });

            throw new ServerDoesNotReplyException(Server.ID);
        }

        internal override int readPadInt(int tid, int uid) {
            Logger.log(new String[] { "FailedServer", Server.ID.ToString(), "readPadInt ", "tid", tid.ToString(), "uid", uid.ToString() });

            throw new ServerDoesNotReplyException(Server.ID);
        }

        internal override bool writePadInt(int tid, int uid, int value) {
            Logger.log(new String[] { "FailedServer", Server.ID.ToString(), " writePadInt ", "tid", tid.ToString(), "uid", uid.ToString(), "value", value.ToString() });

            throw new ServerDoesNotReplyException(Server.ID);
        }

        /// <summary>
        /// Commits a transaction on this server
        /// </summary>
        /// <param name="tid">transaction identifier</param>
        /// <param name="usedPadInts">Identifiers of PadInts involved</param>
        /// <returns>A predicate confirming the sucess of the operations</returns>
        internal override bool commit(int tid, List<int> usedPadInts) {
            Logger.log(new String[] { "FailedServer", Server.ID.ToString(), "commit", "tid", tid.ToString() });

            throw new ServerDoesNotReplyException(Server.ID);
        }

        /// <summary>
        /// Aborts a transaction on this server
        /// </summary>
        /// <param name="tid">transaction identifier</param>
        /// <param name="usedPadInts">Identifiers of PadInts involved</param>
        /// <returns>A predicate confirming the sucess of the operations</returns>
        internal override bool abort(int tid, List<int> usedPadInts) {
            Logger.log(new String[] { "FailedServer", Server.ID.ToString(), "abort", "tid", tid.ToString() });

            throw new ServerDoesNotReplyException(Server.ID);
        }
    }
}