using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Unleasharp {
    /// <summary>
    /// Multithread helper class, it should automatically manage threads to concurrently process item lists using a given callback
    /// </summary>
    /// <typeparam name="T">The type of each item to process concurrently in each thread</typeparam>
    public class Threadnut<T> {
        public  int                    ThreadCount       { get; private set; } = Environment.ProcessorCount;
        public  bool                   Ephemeral         { get; private set; } = true;
        public  bool                   Cancelled         { get; private set; } = false;
        public CancellationToken       CancellationToken { get; private set; }

        public long                    QueueLimit        { get; private set; }

        private BlockingCollection<T>  __Queue           = new BlockingCollection<T>();
        private List<Thread>           __Threads         = new List<Thread>();
        private Thread                 __FeederThread;
        private Action<Threadnut<T>>   __Feeder;
        private Action<T>              __Callback;

        public Threadnut() { }

        public Threadnut<T> WithThreadCount(int ThreadCount) {
            // Please, don't be an idiot
            if (ThreadCount < 1) {
                return this;
            }

            this.ThreadCount = ThreadCount;

            return this;
        }

        public Threadnut<T> WithQueueLimit(long Limit) {
            this.QueueLimit = Limit;

            return this;
        }

        public Threadnut<T> WithEphemeral(bool Ephemeral) {
            this.Ephemeral = Ephemeral;

            return this;
        }

        public Threadnut<T> Cancel() {
            this.Cancelled = true;

            return this;
        }

        public Threadnut<T> WithCancellationToken(CancellationToken Token) {
            this.CancellationToken = Token;

            return this;
        }

        /// <summary>
        /// Configures a feeder action to retrieve and enqueue items to be processed by the threads
        /// </summary>
        /// <returns>The current <see cref="Threadnut{T}"/> instance, allowing for method chaining.</returns>
        public Threadnut<T> WithFeederAction(Action<Threadnut<T>> Feeder) {
            this.__Feeder = Feeder;

            return this;
        }

        public Threadnut<T> WithCallback(Action<T> Callback) {
            this.__Callback = Callback;

            return this;
        }

        public Threadnut<T> WithItems(List<T> Items) {
            this.__Queue = new BlockingCollection<T>(new ConcurrentQueue<T>(Items));

            return this;
        }

        public Threadnut<T> WithItems(T[] Items) {
            this.__Queue = new BlockingCollection<T>(new ConcurrentQueue<T>(Items));

            return this;
        }

        /// <summary>
        /// Marks the queue as complete for adding new items and prevents further additions.
        /// </summary>
        /// <remarks>Once the queue is finalized, no new items can be added. Consumers can continue to
        /// retrieve  items already in the queue until it is empty. This method is typically called when no more  items
        /// are expected to be added to the queue.</remarks>
        /// <returns>The current instance of <see cref="Threadnut{T}"/>, allowing for method chaining.</returns>
        public Threadnut<T> FinalizeQueue() {
            try {
                this.__Queue.CompleteAdding();
            }
            catch (Exception e) { }

            return this;
        }

        /// <summary>
        /// Adds an item to the queue and returns the current instance.
        /// </summary>
        /// <param name="QueueItem">The item to add to the queue. Cannot be null.</param>
        /// <returns>The current instance of <see cref="Threadnut{T}"/> to allow method chaining.</returns>
        public Threadnut<T> Add(T QueueItem) {
            this.__Queue.Add(QueueItem);

            return this;
        }


        /// <summary>
        /// Initializes and starts the processing threads for the current <see cref="Threadnut{T}"/> instance.
        /// </summary>
        /// <remarks>This method sets up and starts the feeder thread and worker threads for processing
        /// items in the queue. The feeder thread is responsible for adding items to the queue, while the worker threads
        /// process the queued items using the callback function provided. The method ensures that all threads are
        /// properly initialized before processing begins.</remarks>
        /// <returns>The current <see cref="Threadnut{T}"/> instance, allowing for method chaining.</returns>
        public Threadnut<T> Bolt() {
            if (this.__Feeder != null) {
                this.__FeederThread = new Thread(() => {
                    this.__Feeder.Invoke(this);

                    this.FinalizeQueue();
                });
            }

            bool AllThreadsInitialized = false;
            if (this.__Callback != null) {
                this.__Threads = new List<Thread>();

                for (int i = 0; i <  this.ThreadCount; i++) {
                    this.__Threads.Add(new Thread(() => {
                        // Wait untill all threads have been initialized
                        while (!AllThreadsInitialized) {
                            Thread.Sleep(10);
                        }

                        while (
                            !this.Cancelled
                            &&
                            !(this.CancellationToken == null || this.CancellationToken.IsCancellationRequested)
                            &&
                            !this.__QueueFinalized()
                        ) {
                            T QueueItem = default(T);
                            if (this.__Queue.TryTake(out QueueItem)) {
                                try {
                                    this.__Callback(QueueItem);
                                }
                                catch (Exception e) {
                                    // Don't die here by exceptions inside the callback, just return the item to the queue
                                    // Or maybe exceptions should be used to discard items... Damn, what should I do? :(
                                    this.Add(QueueItem);
                                }
                            }
                            else {
                                // Give some rest to the thread
                                Thread.Sleep(10);
                            }
                        }
                    }));
                }
            }

            this.__FeederThread.Start();
            foreach (Thread QueueThread in this.__Threads) {
                QueueThread.Start();
            }
            AllThreadsInitialized = true;

            return this;
        }

        private bool __QueueFinalized() {
            return this.__Queue.IsCompleted;
        }

        private bool __ThreadsFinalized() {
            bool ThreadsAlive = true;

            foreach (Thread QueueThread in this.__Threads) {
                ThreadsAlive = ThreadsAlive && QueueThread.IsAlive;
            }

            return !ThreadsAlive;
        }

        /// <summary>
        /// Waits for all threads managed by this instance to complete execution.
        /// </summary>
        /// <remarks>This method blocks the calling thread until all threads managed by this instance have
        /// finalized. It is recommended to use this method when you need to ensure that all threads have completed
        /// before proceeding with further operations.</remarks>
        /// <returns>The current instance of <see cref="Threadnut{T}"/>, allowing for method chaining.</returns>
        public Threadnut<T> Wait() {
            // Wait for threads to finalize
            while (!this.__ThreadsFinalized()) {
                Thread.Sleep(10);
            }

            return this;
        }

        /// <summary>
        /// Determines whether the queue has reached its maximum allowed limit.
        /// </summary>
        /// <returns><see langword="true"/> if the queue limit is set to 0 (indicating no limit) or if the current  number of
        /// items in the queue is greater than or equal to the specified queue limit;  otherwise, <see langword="false"/>.</returns>
        public bool QueueReachedLimit() {
            return this.QueueLimit == 0 || this.__Queue.Count >= this.QueueLimit;
        }
    }
}
