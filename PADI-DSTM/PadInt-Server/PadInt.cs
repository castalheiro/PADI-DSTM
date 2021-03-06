﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonTypes;
using System.Timers;
using System.Threading;

namespace PadIntServer {
    [Serializable]
    class PadInt : IPadInt {

        /// <summary>
        /// Constant used in the initialization of int variables
        /// </summary>
        private const int INITIALIZATION = -1;
        /// <summary>
        /// Constant used to represent the interval after which the
        ///  deadlock detection occurs (1000 = 1s)
        /// </summary>
        private const int DEADLOCK_INTERVAL = 10000;
        /// <summary>
        /// Constant used to represent a write lock
        /// </summary>
        private const bool WRITE_LOCK = true;
        /// <summary>
        /// PadInt identifier
        /// </summary>
        private int uid;
        /// <summary>
        /// PadInt's value during the transaction
        /// </summary>
        private int actualValue;
        /// <summary>
        /// PadInt's value in the begining of the transaction
        /// </summary>
        private int originalValue;
        /// <summary>
        /// Type of the attributed lock
        /// </summary>
        private bool lockType;
        /// <summary>
        /// Queue with transactions' tid with atributed read locks
        /// </summary>
        private List<int> readers;
        /// <summary>
        /// Transaction' tid with atributed write lock.
        /// Value INITIALIZATION means that there is no transaction,
        ///  identified by tid, writing. 
        /// </summary>
        private int writer;
        /// <summary>
        /// Queue with transactions' tid with pending read/write locks
        /// </summary>
        private List<bool> pendingTransactions;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="uid">PadInt identifier</param>
        internal PadInt(int uid) {

            this.uid = uid;
            this.actualValue = 0;
            this.originalValue = 0;

            this.readers = new List<int>();
            this.writer = INITIALIZATION;
            this.pendingTransactions = new List<bool>();
        }

        internal int Uid {
            get { return uid; }
            set { this.uid = value; }
        }

        internal int ActualValue {
            get { return actualValue; }
            set { this.actualValue = value; }
        }

        internal int OriginalValue {
            get { return originalValue; }
            set { this.originalValue = value; }
        }

        /// <summary>
        /// Tests if a transaction wants and can do a promotion
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <param name="requiredLockType">Type of the required lock</param>
        /// <returns>Returns true if is possible to do the promotion</returns>
        private bool WantPromotion(int tid, bool requiredLockType) {
            return lockType == !WRITE_LOCK && requiredLockType == WRITE_LOCK && readers.Contains(tid) && readers.Count == 1;
        }

        /// <summary>
        /// Test if it is possible to acquire a lock
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <param name="requiredLockType">Type of the required lock</param>
        /// <returns>Returns true if should not enter in AcquireLock loop</returns>
        private bool IsPossibleAcquire(int tid, bool requiredLockType) {
            return (writer == INITIALIZATION && readers.Count == 0) ||
                   (writer == INITIALIZATION && requiredLockType == !WRITE_LOCK) ||
                   WantPromotion(tid, requiredLockType);
        }

        /// <summary>
        /// Test if it is not possible to acquire a lock
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <param name="requiredLockType">Type of the required lock</param>
        /// <returns>Returns true if should enter in AcquireLock loop</returns>
        private bool IsNotPossibleAcquire(int tid, bool requiredLockType) {
            return (lockType == requiredLockType == WRITE_LOCK) || (lockType != requiredLockType && !WantPromotion(tid, requiredLockType));
        }

        /// <summary>
        /// Acquires a lock
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <param name="requiredLockType">Lock type</param>
        /// <returns>Returns true if successful</returns>
        internal bool AcquireLock(int tid, bool requiredLockType) {
            lock(this) {
                while(!IsPossibleAcquire(tid, requiredLockType) && IsNotPossibleAcquire(tid, requiredLockType)) {
                    bool res = Monitor.Wait(this, DEADLOCK_INTERVAL);
                    if(!res) {
                        readers.Remove(tid);
                        if(writer == tid) {
                            writer = INITIALIZATION;
                        }
                        throw new AbortException(tid, uid);
                    }
                }

                //updates the type of the lock
                lockType = requiredLockType;

                if(requiredLockType == !WRITE_LOCK) {
                    readers.Add(tid);
                } else {
                    if(requiredLockType == WRITE_LOCK) {
                        //promotion (Remove(tid) removes tid if it exists, otherwise don't remove anything)
                        readers.Remove(tid);

                        writer = tid;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Assigns to the transaction identified by tid
        ///  a read lock over the PadInt identified by uid,
        ///  as soon as possible.
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <returns>Returns true if successful</returns>
        internal bool GetReadLock(int tid) {
            Logger.Log(new String[] { "PadInt", "getReadLock" });

            if(readers.Contains(tid)) {
                return true;
            }
            return AcquireLock(tid, !WRITE_LOCK);
        }

        /// <summary>
        /// Returns true if the transaction identified by tid
        ///  has a write lock over the PadInt identified by uid
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <returns>bool</returns>
        internal bool HasWriteLock(int tid) {
            return writer == tid;
        }

        /// <summary>
        /// Assigns to the transaction identified by tid
        ///  a write lock over the PadInt identified by uid,
        ///  as soon as possible.
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <returns>Returns true if successful</returns>
        internal bool GetWriteLock(int tid) {
            Logger.Log(new String[] { "PadInt", "getWriteLock" });

            if(HasWriteLock(tid)) {
                return true;
            }
            return AcquireLock(tid, WRITE_LOCK);
        }

        /// <summary>
        /// Frees a read lock over the PadInt, identified by uid,
        ///  owned by a transaction identified by tid.
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <returns>Returns true if successful</returns>
        internal bool FreeReadLock(int tid) {
            Logger.Log(new String[] { "PadInt", "freeReadLock" });

            return readers.Remove(tid);
        }

        /// <summary>
        /// Frees a write lock over the PadInt, identified by uid,
        ///  owned by a transaction identified with tid.
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <returns>Returns true if successful</returns>
        internal bool FreeWriteLock(int tid) {
            Logger.Log(new String[] { "PadInt", "freeWriteLock" });

            /* "frees" writer variable */
            if(writer != INITIALIZATION) {
                writer = INITIALIZATION;
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Receives the identifier of a transaction and does a commit.
        /// 
        /// Free promotion variable, removes all read/write locks atributed
        ///  to this transaction and removes all possible pending locks.
        ///  
        /// Finally, updates PadInt's originalValue because we do a successful
        ///  commit.
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <returns>Returns true if successful</returns>
        internal bool Commit(int tid) {
            Logger.Log(new String[] { "PadInt", "commit" });

            lock(this) {
                bool commitSuccessful = true;

                FreeReadLock(tid);

                if(FreeWriteLock(tid)) {
                    OriginalValue = ActualValue;
                }

                Monitor.PulseAll(this);
                return commitSuccessful;
            }
        }

        /// <summary>
        /// Receives the identifier of a transaction and does an abort.
        /// 
        /// Free promotion variable, removes all read/write locks atributed
        ///  to this transaction and removes all possible pending locks.
        ///  
        /// Finally, does the rollback of the PadInt's value.
        /// </summary>
        /// <param name="tid">Transaction identifier</param>
        /// <returns>Returns true if successful</returns>
        internal bool Abort(int tid) {
            Logger.Log(new String[] { "PadInt", "abort" });

            lock(this) {
                FreeReadLock(tid);

                /* only if exists a write lock we do the rollback of the PadInt's value */
                if(FreeWriteLock(tid)) {
                    ActualValue = OriginalValue;
                }

                Monitor.PulseAll(this);
                return true;
            }
        }
    }
}
