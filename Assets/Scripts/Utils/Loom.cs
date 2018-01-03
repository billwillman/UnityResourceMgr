using UnityEngine;  
using System.Collections;  
using System.Collections.Generic;  
using System;  
using System.Threading;  
using System.Linq;  
  
public class Loom : MonoBehaviour{  
	public static int maxThreads = 16;  
	public static int numThreads = 0;  
	  
	private static Loom _current;  
	private int _count;  

	public static Loom Current{  
		get{  
			Initialize();  
			return _current;  
		}  
	}  
	  
	void Awake(){  
		_current = this;  
		initialized = true;  
	}  
	  
	static bool initialized;  
	  
	static void Initialize(){  
		if (!initialized){  
		  
			if(!Application.isPlaying)  
				return;  
			initialized = true;  
			var g = new GameObject("Loom");
            DontDestroyOnLoad(g);
			_current = g.AddComponent<Loom>();  
		}  
	}  
	  
	private List<Action> _actions = new List<Action>();  

	public struct DelayedQueueItem{  
		public float time;  
		public Action action;  
	}  

	private List<DelayedQueueItem> _delayed = new  List<DelayedQueueItem>();  
  
	List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();  
	  
	public static void QueueOnMainThread(Action action){  
		QueueOnMainThread( action, 0f);  
	}  

	public static void QueueOnMainThread(Action action, float time){  
		if(time != 0){  
			lock(Current._delayed){  
				Current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = action});  
			}  
		}  
		else{  
			lock (Current._actions){  
				Current._actions.Add(action);  
			}  
		}  
	}  
	  
	public static Thread RunAsync(Action a){
		Initialize();
        while (numThreads >= maxThreads){
            Thread.Sleep(1);
        }
        Interlocked.Increment(ref numThreads);
        //ThreadPool.QueueUserWorkItem(RunAction, a);
        NsLib.Utils.ThreadPool.QueueUserWorkItem(RunAction, a);
        return null;
    }  
	  
	private static void RunAction(object action){  
		try{  
			((Action)action)();  
		}  
		catch(System.Exception e) {

#if DEBUG
            Debug.LogError(e.Message);
#endif

		}  
		finally{  
			Interlocked.Decrement(ref numThreads);  
		}  
	}  
	  
	void OnDisable(){  
		if (_current == this){  
			_current = null;  
		}  
	}  
  
	// Use this for initialization  
	void Start(){  
	  
	}  
	  
	List<Action> _currentActions = new List<Action>();  
	  
	// Update is called once per frame  
	void Update(){  
		lock (_actions){  
			_currentActions.Clear();
            // 优化GC
            if (_actions.Count > 0) {
                _currentActions.AddRange(_actions);
                _actions.Clear();
            }
		}  
		for (int i = 0; i < _currentActions.Count; ++i) {
            var a = _currentActions[i];
            a();  
		}  
		lock(_delayed){  
			_currentDelayed.Clear();
            // 优化GC
            if (_delayed.Count > 0) {
                _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));
                for (int i = 0; i < _currentDelayed.Count; ++i) {
                    var item = _currentDelayed[i];
                    _delayed.Remove(item);
                }
            }
		}  
		for (int i = 0; i < _currentDelayed.Count; ++i){
            var delayed = _currentDelayed[i];
            delayed.action();  
		}  
	}  
}

//Scale a mesh on a second thread  
//void ScaleMesh(Mesh mesh, float scale)
//{
//    //Get the vertices of a mesh  
//    var vertices = mesh.vertices;
//    //Run the action on a new thread  
//    Loom.RunAsync(() => {
//        //Loop through the vertices  
//        for (var i = 0; i < vertices.Length; i++)
//        {
//            //Scale the vertex  
//            vertices[i] = vertices[i] * scale;
//        }
//        //Run some code on the main thread  
//        //to update the mesh  
//        Loom.QueueOnMainThread(() => {
//            //Set the vertices  
//            mesh.vertices = vertices;
//            //Recalculate the bounds  
//            mesh.RecalculateBounds();
//        });

//    });
//}

