using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Unleasharp;

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

    private BlockingCollection<T>  _queue           = new BlockingCollection<T>();
    private List<Thread>           _threads         = new List<Thread>();
    private Thread                 _feederThread;
    private Action<Threadnut<T>>   _feeder;
    private Action<T>              _callback;

    public Threadnut() { }

    public Threadnut<T> WithThreadCount(int threadCount) {
        // Please, don't be an idiot
        if (threadCount < 1) {
            return this;
        }

        this.ThreadCount = threadCount;

        return this;
    }

    public Threadnut<T> WithQueueLimit(long limit) {
        this.QueueLimit = limit;

        return this;
    }

    public Threadnut<T> WithEphemeral(bool ephemeral) {
        this.Ephemeral = ephemeral;

        return this;
    }

    public Threadnut<T> Cancel() {
        this.Cancelled = true;

        return this;
    }

    public Threadnut<T> WithCancellationToken(CancellationToken token) {
        this.CancellationToken = token;

        return this;
    }

    /// <summary>
    /// Configures a feeder action to retrieve and enqueue items to be processed by the threads
    /// </summary>
    /// <returns>The current <see cref="Threadnut{T}"/> instance, allowing for method chaining.</returns>
    public Threadnut<T> WithFeederAction(Action<Threadnut<T>> feeder) {
        this._feeder = feeder;

        return this;
    }

    public Threadnut<T> WithCallback(Action<T> callback) {
        this._callback = callback;

        return this;
    }

    public Threadnut<T> WithItems(List<T> items) {
        this._queue = new BlockingCollection<T>(new ConcurrentQueue<T>(items));

        return this;
    }

    public Threadnut<T> WithItems(T[] items) {
        this._queue = new BlockingCollection<T>(new ConcurrentQueue<T>(items));

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
            this._queue.CompleteAdding();
        }
        catch (Exception e) { }

        return this;
    }

    /// <summary>
    /// Adds an item to the queue and returns the current instance.
    /// </summary>
    /// <param name="queueItem">The item to add to the queue. Cannot be null.</param>
    /// <returns>The current instance of <see cref="Threadnut{T}"/> to allow method chaining.</returns>
    public Threadnut<T> Add(T queueItem) {
        this._queue.Add(queueItem);

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
        if (this._feeder != null) {
            this._feederThread = new Thread(() => {
                this._feeder.Invoke(this);

                this.FinalizeQueue();
            });
        }

        bool allThreadsInitialized = false;
        if (this._callback != null) {
            this._threads = new List<Thread>();

            for (int i = 0; i <  this.ThreadCount; i++) {
                this._threads.Add(new Thread(() => {
                    // Wait untill all threads have been initialized
                    while (!allThreadsInitialized) {
                        Thread.Sleep(10);
                    }

                    while (
                        !this.Cancelled
                        &&
                        !(this.CancellationToken == null || this.CancellationToken.IsCancellationRequested)
                        &&
                        !this.__QueueFinalized()
                    ) {
                        T queueItem = default(T);
                        if (this._queue.TryTake(out queueItem)) {
                            try {
                                this._callback(queueItem);
                            }
                            catch (Exception e) {
                                // Don't die here by exceptions inside the callback, just return the item to the queue
                                // Or maybe exceptions should be used to discard items... Damn, what should I do? :(
                                this.Add(queueItem);
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

        this._feederThread.Start();
        foreach (Thread queueThread in this._threads) {
            queueThread.Start();
        }
        allThreadsInitialized = true;

        return this;
    }

    private bool __QueueFinalized() {
        return this._queue.IsCompleted;
    }

    private bool __ThreadsFinalized() {
        bool threadsAlive = true;

        foreach (Thread queueThread in this._threads) {
            threadsAlive = threadsAlive && queueThread.IsAlive;
        }

        return !threadsAlive;
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
        return this.QueueLimit == 0 || this._queue.Count >= this.QueueLimit;
    }
}
