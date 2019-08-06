using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    public static MainThreadDispatcher Instance;
    private Queue<Action> actionQueue = new Queue<Action>();
        
    private void Start()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        while (actionQueue.Count > 0)
        {
            actionQueue.Dequeue()();
        }
    }

    public void Invoke(Action action)
    {
        actionQueue.Enqueue(action);
    }
    
    
}
