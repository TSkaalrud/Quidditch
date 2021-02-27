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
    public float min_velocity;
    public float max_velocity;
    

    private void Awake()
    {
        // Extract rigid body
        Rigidbody = GetComponent<Rigidbody>();
        // Give the bird a small push
        Rigidbody.velocity = transform.forward.normalized;

        manager = GameObject.Find("SceneManager");



    }
    // Start is called before the first frame update
    void Start()
    {

        //MainSceneManager settings = manager.GetComponent<MainSceneManager>();
        //Flocks[0] = settings.Flocks[0];
        //Flocks[1] = settings.Flocks[1];
        //Flocks[0] = GameObject.Find("Slytherin(Clone)");
        //Flocks[1] = GameObject.Find("Gryffindor(Clone)");
    }

    public void initialize()
    {
        
        MainSceneManager script = manager.GetComponent<MainSceneManager>();
        float Gmax = script.Flocks[0].Max_Velocity_mean;
        float Smax = script.Flocks[1].Max_Velocity_mean;
        max_velocity = Gmax + Smax;

        min_velocity = script.Settings.MinSpeed;// manager.GetComponent<FlockSettingScriptable>().MinSpeed;
    }


    // Update is called once per frame
    void Update()
    {
        // Initialize the new velocity
        Vector3 acceleration = Vector3.zero;

        // Calculate forces applied to stay inside the borders
        acceleration += 2 * BorderForces();

        acceleration += EscapeForces();

        acceleration += CenteringForce() / 2;

        acceleration += ComputeCollisionAvoidanceForce() * 10f;

        //random force
        acceleration += RandomForce();

        // Compute the new velocity
        Vector3 velocity = Rigidbody.velocity;
        velocity += acceleration * Time.deltaTime;

        // Ensure the velocity remains within the accepted range
        velocity = velocity.normalized * Mathf.Clamp(velocity.magnitude,
            min_velocity, max_velocity);

        // Apply velocity
        Rigidbody.velocity = velocity;

        // Update rotation
        transform.forward = Rigidbody.velocity.normalized;
    }



    Vector3 BorderForces()
    {
        // Initialize seperation force
        Vector3 force = Vector3.zero;

        //fetch for border objects
        MainSceneManager script = manager.GetComponent<MainSceneManager>();

        //repel away from each of the 6 borders with force increasing with proximity.
        force.y += 1 / (transform.position.y - script.Borders[0].transform.position.y);
        force.y += 1 / (transform.position.y - script.Borders[1].transform.position.y);

        force.x += 1 / (transform.position.x - script.Borders[2].transform.position.x);
        force.x += 1 / (transform.position.x - script.Borders[3].transform.position.x);

        force.z += 1 / (transform.position.z - script.Borders[4].transform.position.z);
        force.z += 1 / (transform.position.z - script.Borders[5].transform.position.z);
        
        return force;
    }


    private Vector3 EscapeForces()
    {
        Vector3 force = Vector3.zero;

        //fetch for team flocks
        MainSceneManager script = manager.GetComponent<MainSceneManager>();

        force += transform.position - script.Flocks[0].CenterPosition;
        force += transform.position - script.Flocks[1].CenterPosition;

        force = force.normalized;// * ((script.Flocks[0].Max_Velocity_mean + script.Flocks[1].Max_Velocity_mean) / 2);

        return force;
    }

    private Vector3 CenteringForce()
    {
        Vector3 force = Vector3.zero;

        MainSceneManager script = manager.GetComponent<MainSceneManager>();

        //force.x += 1 / (script.Spawn_Snitch.position.x - transform.position.x);
        //force.y += 1 / (script.Spawn_Snitch.position.y - transform.position.y);
        //force.z += 1 / (script.Spawn_Snitch.position.z - transform.position.z);
        force += script.Spawn_Snitch.position - transform.position;

        return force;

    }

    private Vector3 RandomForce()
    {
        Vector3 force = Vector3.zero;

        System.Random r = new System.Random();
        force.x = (float)r.NextDouble();
        force.y = (float)r.NextDouble();
        force.z = (float)r.NextDouble();

        return force.normalized;
    }

    private void OnTriggerEnter(Collider other)
    {
        print("Snitch hit " + other);
        if (other.tag == "Player")
        {
            MainSceneManager scene = manager.GetComponent<MainSceneManager>();

            GameObject player = other.gameObject;
            Bird script = (Bird) player.GetComponent<Bird>();

            //score points based on other's team and team's potential streak
            if (script.Flock.name == "Slytherin(Clone)")
            {
                if (scene.last_Scored == "Slytherin")
                {
                    script.Flock.score = script.Flock.score + 2;
                }
                else
                {
                    script.Flock.score = script.Flock.score + 1;
                    scene.last_Scored = "Slytherin";
                }
            }
            else if(script.Flock.name == "Gryffindor(Clone)")
            {
                if (scene.last_Scored == "Gryffindor")
                {
                    script.Flock.score = script.Flock.score + 2;
                }
                else
                {
                    script.Flock.score = script.Flock.score + 1;
                    scene.last_Scored = "Gryffindor";
                }
            }
            //"respawn" the snitch
            this.transform.position = scene.Spawn_Snitch.position;
        }
    }

    /// <summary>
    /// Computes the force that helps avoid collision.
    /// </summary>
    private Vector3 ComputeCollisionAvoidanceForce()
    {
        // Check if heading to collision
        if (!Physics.SphereCast(transform.position,
            2,
            transform.forward,
            out RaycastHit hitInfo,
            2))
            return Vector3.zero;

        // Compute force
        return transform.position - hitInfo.point;
    }

    public static explicit operator Snitch(GameObject v)
    {
        throw new NotImplementedException();
    }
}
