using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;

public class ThreadedDataRequester : MonoBehaviour {
    //the Queues that hold the data for either meshes or heightMap and the appropriate callback
    //the reason we use a queue is for the former, as unity won't let you do stuff like alter meshes outside of the main thread.
    static Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

    //The threading works by passing in a method generateData, and a method to be done when that data has been generated
    public static void RequestData(Func<object> generateData, Action<object> callback) {
        //declare what function we want our thread to do
        ThreadStart threadStart = delegate {
            DataThread(generateData, callback);
        };

        //have it do the function specified above
        new Thread(threadStart).Start();
    }

    //This is what is happenning inside of each threads
    private static void DataThread(Func<object> generateData, Action<object> callback) {
        //generate the data
        object data = generateData();

        //makes sure that only one thread can access the Queue at once, as they are not thread-safe
        lock (dataQueue) {
            //add the Info and callback(what to do with the info) to the Queue
            dataQueue.Enqueue(new ThreadInfo(callback, data));
        }
    }

    void Update() {
        //if there's stuff in the Queue, take it out and execute the callback
        if (dataQueue.Count > 0) {
            for (int i = 0; i < dataQueue.Count; i++) {
                ThreadInfo threadInfo = dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    // holds either a meshData or HeightMap and the correct callback for that data
    struct ThreadInfo {
        public readonly Action<object> callback;
        public readonly object parameter;
        public ThreadInfo(Action<object> callback, object parameter) {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
