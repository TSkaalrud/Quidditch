using Boids;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snitch : MonoBehaviour
{

    private Rigidbody Rigidbody;
    public Transform spawn_Location;
    public Flock[] Flocks;
    public GameObject manager;

    private void Awake()
    {
        // Extract rigid body
        Rigidbody = GetComponent<Rigidbody>();
        // Give the bird a small push
        Rigidbody.velocity = transform.forward.normalized;



    }
    // Start is called before the first frame update
    void Start()
    {
        manager = GameObject.Find("SceneManager");
        //MainSceneManager settings = manager.GetComponent<MainSceneManager>();
        //Flocks[0] = settings.Flocks[0];
        //Flocks[1] = settings.Flocks[1];
        //Flocks[0] = GameObject.Find("Slytherin(Clone)");
        //Flocks[1] = GameObject.Find("Gryffindor(Clone)");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static explicit operator Snitch(GameObject v)
    {
        throw new NotImplementedException();
    }
}
