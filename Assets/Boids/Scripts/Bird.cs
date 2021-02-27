using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Boids
{
    public class Bird : MonoBehaviour
    {

        #region Initialization

        /// <summary>
        /// Executes once on start.
        /// </summary>
        private void Awake()
        {
            // Extract rigid body
            Rigidbody = GetComponent<Rigidbody>();

        }

        /// <summary>
        /// Initializes the bird.
        /// </summary>
        public void Initialize(Flock flock, GameObject man)
        {
            // Give the bird a small push
            Rigidbody.velocity = transform.forward.normalized * flock.FlockSettings.MinSpeed;

            // Reference the flock this bird belongs to
            Flock = flock;
            manager = man;
            Cur_Exhaustion = 0;
            conscious = true;
            script = manager.GetComponent<MainSceneManager>();

            /*
            Weight = SampleValue(flock.Weight_mean, flock.Weight_std);
            Max_Velocity = SampleValue(flock.Max_Velocity_mean, flock.Max_Velocity_std);
            Aggressiveness = SampleValue(flock.Aggressiveness_mean, flock.Aggressiveness_std);
            Max_Exhaustion = SampleValue(flock.Max_Exhaustion_mean, flock.Max_Exhaustion_std);
            */
        }

        #endregion

        #region Fields/Properties

        /// <summary>
        /// References the rigidbody attached to this object.
        /// </summary>
        private Rigidbody Rigidbody;

        /// <summary>
        /// The flock this bird belongs to.
        /// </summary>
        public Flock Flock;


        public float Weight;
        public float Max_Velocity;
        public float Aggressiveness;
        public float Max_Exhaustion;
        public float Cur_Exhaustion;
        public GameObject manager;
        public MainSceneManager script;
        private bool tired;
        private bool conscious;

        #endregion

        #region Methods

        /// <summary>
        /// Continuous update the speed and rotation of the bird.
        /// </summary>
        private void Update()
        {
            if (!tired && conscious)
            {
                // Initialize the new velocity
                Vector3 acceleration = Vector3.zero;

                //constant to counteract the drastic effect of weight
                float acceleration_mult = 63f / Weight; //the minimum weight

                //clamped between 0.5 and 1, players speed depends on their proportion of exhaustion
                float exhaustion_factor = Mathf.Clamp((Max_Exhaustion - Cur_Exhaustion) / Max_Exhaustion, 0.5f, 1f);

                // Compute cohesion
                acceleration += NormalizeSteeringForce(ComputeCohisionForce())
                    * Flock.FlockSettings.CohesionForceWeight;

                // Compute seperation
                acceleration += NormalizeSteeringForce(ComputeSeperationForce())
                    * Flock.FlockSettings.SeperationForceWeight;

                // Compute alignment
                acceleration += NormalizeSteeringForce(ComputeAlignmentForce())
                    * Flock.FlockSettings.AlignmentForceWeight;

                // Compute collision avoidance
                acceleration += NormalizeSteeringForce(ComputeCollisionAvoidanceForce())
                    * Flock.FlockSettings.CollisionAvoidanceForceWeight;

                //Compute snitch chase force
                acceleration += NormalizeSteeringForce(ComputeSnitchForce())
                    * 2;

                //Compute border avoidance forces
                acceleration += NormalizeSteeringForce(BorderForces())
                    * 3;

                // Compute the new velocity
                Vector3 velocity = Rigidbody.velocity;
                velocity += acceleration * acceleration_mult * exhaustion_factor * Time.deltaTime;

                // Ensure the velocity remains within the accepted range
                velocity = velocity.normalized * Mathf.Clamp(velocity.magnitude,
                    Flock.FlockSettings.MinSpeed, Max_Velocity);

                // Apply velocity
                Rigidbody.velocity = velocity;

                // Update rotation
                transform.forward = Rigidbody.velocity.normalized;

                //gain exhaustion based on time
                Cur_Exhaustion += 2.5f * Time.deltaTime;
                if (Cur_Exhaustion >= Max_Exhaustion*.75)
                    tired = true;
            }
            else if (tired && conscious) 
            {
                Rigidbody.velocity = Rigidbody.velocity * 4 / 5 * Time.deltaTime;
                Cur_Exhaustion -= 10 * Time.deltaTime;
                if (Cur_Exhaustion <= 0)
                {
                    Cur_Exhaustion = 0;
                    tired = false;
                }
            }
            else if(!conscious && (Cur_Exhaustion > 0))
            {
                if (this.Rigidbody.useGravity == false)
                {
                    this.Rigidbody.useGravity = true;
                }
                Cur_Exhaustion -= 5 * Time.deltaTime;

            }
            else if(!conscious && (Cur_Exhaustion <= 0))
            {
                conscious = true;
                Cur_Exhaustion = 0;
                tired = false;
                this.Rigidbody.useGravity = false;
                this.transform.position = Flock.Starting_Pos.position;
            }

        }

        /// <summary>
        /// Normalizes the steering force and clamps it.
        /// </summary>
        private Vector3 NormalizeSteeringForce(Vector3 force)
        {
            return force.normalized * Mathf.Clamp(force.magnitude, 0, Flock.FlockSettings.MaxSteerForce);
        }

        /// <summary>
        /// Computes the cohision force that will pull the bird back to the center of the flock.
        /// </summary>
        private Vector3 ComputeCohisionForce()
        {
            // Check if this is the only bird in the flock
            if (Flock.Birds.Count == 1)
                return Vector3.zero;

            // Check if we are using the center of the flock
            if (Flock.FlockSettings.UseCenterForCohesion)
            {
                // Get current center of the flock
                Vector3 center = Flock.CenterPosition;

                // Get rid of this bird's position from the center
                float newCenterX = center.x * Flock.Birds.Count - transform.localPosition.x;
                float newCenterY = center.y * Flock.Birds.Count - transform.localPosition.y;
                float newCenterZ = center.z * Flock.Birds.Count - transform.localPosition.z;
                Vector3 newCenter = new Vector3(newCenterX, newCenterY, newCenterZ) / (Flock.Birds.Count - 1);

                // Compute force
                return newCenter - transform.localPosition;
            }

            // Else, use the center of the neighbor birds
            float centerX = 0, centerY = 0, centerZ = 0;
            int count = 0;
            foreach (Bird bird in Flock.Birds)
            {
                if (bird == this
                    || (bird.transform.position - transform.position).magnitude > Flock.FlockSettings.CohesionRadiusThreshold)
                    continue;

                centerX += bird.transform.localPosition.x;
                centerY += bird.transform.localPosition.y;
                centerZ += bird.transform.localPosition.z;
                count++;
            }

            // Compute force
            return count == 0 
                ? Vector3.zero 
                : new Vector3(centerX, centerY, centerZ) / count;
        }

        /// <summary>
        /// Computes the seperation force that ensures a safe distance is kept between the birds.
        /// </summary>
        private Vector3 ComputeSeperationForce()
        {
            // Initialize seperation force
            Vector3 force = Vector3.zero;

            // Find nearby birds
            foreach (Bird bird in Flock.Birds)
            {
                if (bird == this
                    || (bird.transform.position - transform.position).magnitude > Flock.FlockSettings.SeperationRadiusThreshold)
                    continue;

                // Repel away
                force += transform.position - bird.transform.position;
            }

            return force;
        }

        /// <summary>
        /// Computes the alignment force that aligns this bird with nearby birds.
        /// </summary>
        private Vector3 ComputeAlignmentForce()
        {
            // Initialize alignment force
            Vector3 force = Vector3.zero;

            // Find nearby birds
            foreach (Bird bird in Flock.Birds)
            {
                if (bird == this
                    || (bird.transform.position - transform.position).magnitude > Flock.FlockSettings.AlignmentRadiusThreshold)
                    continue;

                force += bird.transform.forward;
            }

            return force;
        }

        public static explicit operator Bird(GameObject v)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Computes the force that helps avoid collision.
        /// </summary>
        private Vector3 ComputeCollisionAvoidanceForce()
        {
            // Check if heading to collision
            if (!Physics.SphereCast(transform.position,
                Flock.FlockSettings.CollisionAvoidanceRadiusThreshold, 
                transform.forward, 
                out RaycastHit hitInfo,
                Flock.FlockSettings.CollisionAvoidanceRadiusThreshold))
                return Vector3.zero;

            // Compute force
            if (hitInfo.collider.gameObject.name != "snitch(Clone)")
            {
                // Compute force
                return transform.position - hitInfo.point;
            }
            return Vector3.zero;
        }

        private Vector3 ComputeSnitchForce()
        {
            //init
            Vector3 force = Vector3.zero;

            //fetch for reference to snitch
            MainSceneManager snitch = manager.GetComponent<MainSceneManager>();

            //add force towards snitch
            force += snitch.Snit.transform.position - this.transform.position;


            return force;
        }

        Vector3 BorderForces()
        {
            // Initialize seperation force
            Vector3 force = Vector3.zero;

            //repel away from each of the 6 borders with force increasing with proximity.
            force.y += 1 / (transform.position.y - script.Borders[0].transform.position.y);
            force.y += 2 / (transform.position.y - script.Borders[1].transform.position.y);

            force.x += 1 / (transform.position.x - script.Borders[2].transform.position.x);
            force.x += 2 / (transform.position.x - script.Borders[3].transform.position.x);

            force.z += 1 / (transform.position.z - script.Borders[4].transform.position.z);
            force.z += 1 / (transform.position.z - script.Borders[5].transform.position.z);

            return force;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Player")
            {
                GameObject collidedWith = collision.gameObject;
                Bird script = (Bird)collidedWith.GetComponent<Bird>();
                System.Random r = new System.Random();

                if (this.Flock.name == script.Flock.name)
                {
                    
                    if (r.NextDouble() < 0.025)
                    {
                        conscious = false;
                        print("Friendly fire!");

                    }
                }
                else
                {
                    double Me = Aggressiveness * (r.NextDouble() * (1.2 + 0.8) + 0.8) * (1-(Cur_Exhaustion/Max_Exhaustion));
                    double them = script.Aggressiveness * (r.NextDouble() * (1.2 + 0.8) + 0.8) * (1 - (script.Cur_Exhaustion / script.Max_Exhaustion));
                    print("Players are duking it out!");

                    if (Me > them)
                    {
                        script.conscious = false;
                    } else
                    {
                        conscious = false;
                    }
                }
            } else
            {
                conscious = false;
                print("Player struck terrain, like a dunce");
            }
        }



        #endregion

    }
}
