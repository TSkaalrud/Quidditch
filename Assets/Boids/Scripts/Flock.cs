using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Boids
{
    public class Flock : MonoBehaviour
    {

        #region Initialization

        /// <summary>
        /// Executes once on awake.
        /// </summary>
        private void Awake()
        {
            if (_FlockSettings == null)
                _FlockSettings = ScriptableObject.CreateInstance<FlockSettingScriptable>();

            //if (_FlockSettings.NumberOfBirdsToGenerateOnAwake > 0)
            //    Initialize(_FlockSettings.NumberOfBirdsToGenerateOnAwake);
            manager = GameObject.Find("SceneManager");
            MainSceneManager settings = manager.GetComponent<MainSceneManager>();
            Initialize(settings.Settings.NumberOfBirdsToGenerateOnAwake);
            Starting_Pos = this.transform;
            score = 0;
        }

        /// <summary>
        /// Generates the birds in the flock.
        /// </summary>
        /// <param name="numberOfBirds">The number of birds to be generated in this flock.</param>
        public void Initialize(int numberOfBirds)
        {
            // Clear any existing bird
            Clear();

            // Create new birds
            for (int i = 0; i < numberOfBirds; i++)

                CreateBird(); //put a breakpoint here if you want individual player stats as opposed to team-random stats
        }

        public static explicit operator Flock(GameObject v)
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region Fields/Properties

        /// <summary>
        /// A scriptable object instance that contains the flock's settings.
        /// </summary>
        [Tooltip("A scriptable object instance that contains the flock's settings.")]
        [SerializeField]
        private FlockSettingScriptable _FlockSettings;

        /// <summary>
        /// A scriptable object instance that contains the flock's settings.
        /// </summary>
        public FlockSettingScriptable FlockSettings { get { return _FlockSettings; } }



        [Header("Center")]

        /// <summary>
        /// The sphere representing the center of the flock.
        /// </summary>
        [SerializeField]
        [Tooltip("The sphere representing the center of the flock.")]
        private GameObject Center;

        /// <summary>
        /// The current center (local position) of the flock.
        /// </summary>
        [SerializeField]
        [Tooltip("The current center (local position) of the flock.")]
        private Vector3 _CenterPosition;

        /// <summary>
        /// The current center (local position) of the flock.
        /// </summary>
        public Vector3 CenterPosition { get { return _CenterPosition; } }



        [Header("Birds")]

        /// <summary>
        /// The prefab template used to generate birds in this flock.
        /// </summary>
        [SerializeField]
        [Tooltip("The prefab template used to generate birds in this flock.")]
        private GameObject BirdTemplate;

        /// <summary>
        /// The parent holding all the generated birds.
        /// </summary>
        [SerializeField]
        [Tooltip("The parent holding all the generated birds.")]
        private GameObject BirdsParent;

        /// <summary>
        /// List of all the birds in this flock.
        /// </summary>
        [SerializeField]
        [Tooltip("List of all the birds in this flock.")]
        private List<Bird> _Birds;

        /// <summary>
        /// List of all the birds in this flock.
        /// </summary>
        public List<Bird> Birds { get { return _Birds; } }


        public float Weight_mean;
        public float Weight_std;

        public float Max_Velocity_mean;
        public float Max_Velocity_std;

        public float Aggressiveness_mean;
        public float Aggressiveness_std;

        public float Max_Exhaustion_mean;
        public float Max_Exhaustion_std;

        public Color Team_Color;
        public Color Team_Color2;
        public Transform Starting_Pos;

        public GameObject manager;

        public int score;

        #endregion

        #region Methods

        /// <summary>
        /// Continuously compute the position of the center of the flock.
        /// </summary>
        private void Update()
        {
            // Compute the center
            float centerX = 0, centerY = 0, centerZ = 0;
            foreach (Bird bird in _Birds)
            {
                centerX += bird.transform.localPosition.x;
                centerY += bird.transform.localPosition.y;
                centerZ += bird.transform.localPosition.z;
            }
            _CenterPosition = new Vector3(centerX, centerY, centerZ) / _Birds.Count();

            // Move the sphere to the center
            Center.transform.localPosition = _CenterPosition;

            // Update sphere visibility
            Center.gameObject.SetActive(_FlockSettings.IsCenterVisible);
        }

        /// <summary>
        /// Deletes all generated birds.
        /// </summary>
        private void Clear()
        {
            _Birds = new List<Bird>();
            foreach (Transform bird in BirdsParent.transform)
                GameObject.Destroy(bird.transform);
        }

        /// <summary>
        /// Adds a new bird to the flock.
        /// </summary>
        private void CreateBird()
        {
            // Initialize list
            if (_Birds == null)
                _Birds = new List<Bird>();

            // Create new bird
            GameObject bird = GameObject.Instantiate(BirdTemplate, BirdsParent.transform);

            //assign team colors
            MeshRenderer[] renderers = bird.GetComponentsInChildren<MeshRenderer>();
            //renderers[0].material.color = Team_Color;
            renderers[0].materials[0].color = Team_Color;
            renderers[0].materials[1].color = Team_Color2;
            /*
            MeshRenderer[] renderers = bird.GetComponentsInParent<MeshRenderer>();
            renderers[0].material.color = Team_Color;
            renderers[1].material.color = Team_Color2;
            */
            // Extract its script
            Bird birdScript = bird.GetComponent<Bird>();
            _Birds.Add(birdScript);

            // Set random location
            bird.transform.localPosition = new Vector3
            (
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f),
                Random.Range(-2f, 2f)
            );

            // Set random rotation
            bird.transform.localEulerAngles = new Vector3
            (
                Random.Range(0f, 360f),
                Random.Range(0f, 360f),
                Random.Range(0f, 360f)
            );

            // Add a velocity
            birdScript.Initialize(this, manager);

            System.Random r = new System.Random();
            birdScript.Weight = SampleValue(Weight_mean, Weight_std, r);
            birdScript.Max_Velocity = SampleValue(Max_Velocity_mean, Max_Velocity_std, r);
            birdScript.Aggressiveness = SampleValue(Aggressiveness_mean, Aggressiveness_std, r);
            birdScript.Max_Exhaustion = SampleValue(Max_Exhaustion_mean, Max_Exhaustion_std, r);
            bird.tag = "Player";

        }

        //Uses only the cos form of the box-muller transform to produce a random gaussian number
        //from a given mean and std. dev.
        private float SampleValue(float mean, float std_dev, System.Random r)
        {
            
            double U1 = r.NextDouble();
            double U2 = r.NextDouble();

            float x = Mathf.Sqrt(-2 * Mathf.Log((float)U1)) * Mathf.Cos((float)(2 * Mathf.PI * U2));

            return mean + (std_dev * x);
        }


        #endregion

    }
}
