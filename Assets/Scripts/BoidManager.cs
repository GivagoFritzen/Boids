using System.Collections.Generic;
using UnityEngine;

namespace Boids
{
    public class BoidManager : MonoBehaviour
    {
        [Header("Settings")]
        private const int THREADGROUPSIZE = 1024;
        public BoidSettings settings = null;
        [SerializeField]
        private ComputeShader compute = null;
        [SerializeField]
        private BoidSpawner[] spawners = null;

        [SerializeField]
        private List<Boid> boids = new List<Boid>();
        private BoidData[] boidsData = null;
        private ComputeBuffer boidBuffer = null;

        [Header("Limit Settings")]
        [SerializeField]
        [Tooltip("Config Layer Collision Matrix to Boids Ignore Limit, it's necessery edit to settings-obstacleMask")]
        private bool hasLimit = true;
        [SerializeField]
        private float radius = 10;
        [SerializeField]
        private LayerMask layerMaskToLimit = 0;

        private void Start()
        {
            AddLimits();

            foreach (var spawner in spawners)
                spawner.Init(this);
        }

        public void Add(List<Boid> boids, Transform target = null)
        {
            this.boids.AddRange(boids);

            foreach (Boid b in boids)
                b.Initialize(settings, target);
        }

        #region Limits
        private void AddLimits()
        {
            if (hasLimit)
            {
                var walls = new GameObject("Walls");
                walls.transform.SetParent(transform);
                walls.transform.localPosition = Vector3.zero;

                var halfRadius = radius / 2;

                CreateWall(walls, new Vector3(0, 0, halfRadius), new Vector3(90, 0, 0));
                CreateWall(walls, new Vector3(0, 0, -halfRadius), new Vector3(90, 0, 0));

                CreateWall(walls, new Vector3(0, halfRadius, 0), Vector3.zero);
                CreateWall(walls, new Vector3(0, -halfRadius, 0), Vector3.zero);

                CreateWall(walls, new Vector3(halfRadius, 0, 0), new Vector3(90, 0, 90));
                CreateWall(walls, new Vector3(-halfRadius, 0, 0), new Vector3(90, 0, 90));
            }
        }

        private void CreateWall(GameObject parent, Vector3 position, Vector3 rotation)
        {
            var wall = new GameObject("Wall");
            wall.layer = (int)Mathf.Log(layerMaskToLimit.value, 2);
            wall.transform.SetParent(parent.transform);

            wall.transform.localPosition = position;
            wall.transform.eulerAngles = rotation;
            wall.transform.localScale = new Vector3(radius, .2f, radius);

            wall.AddComponent<BoxCollider>();
        }
        #endregion

        private void Update()
        {
            if (boids == null || boids.Count == 0)
                return;

            boidsData = new BoidData[boids.Count];
            SetPositionAndDirectionToBoidsData();

            boidBuffer = new ComputeBuffer(boids.Count, BoidData.Size);
            boidBuffer.SetData(boidsData);

            ConfigComputeShader();

            boidBuffer.GetData(boidsData);

            UpdateBoids();

            boidBuffer.Release();
        }

        private void ConfigComputeShader()
        {
            compute.SetBuffer(0, "boids", boidBuffer);
            compute.SetInt("numBoids", boids.Count);
            compute.SetFloat("viewRadius", settings.perceptionRadius);
            compute.SetFloat("avoidRadius", settings.avoidanceRadius);

            int threadGroups = Mathf.CeilToInt(boids.Count / (float)THREADGROUPSIZE);
            compute.Dispatch(0, threadGroups, 1, 1);
        }

        private void SetPositionAndDirectionToBoidsData()
        {
            for (int i = 0; i < boids.Count; i++)
            {
                boidsData[i].position = boids[i].position;
                boidsData[i].direction = boids[i].forward;
            }
        }

        private void UpdateBoids()
        {
            for (int i = 0; i < boids.Count; i++)
            {
                boids[i].avgFlockHeading = boidsData[i].flockHeading;
                boids[i].centreOfFlockmates = boidsData[i].flockCentre;
                boids[i].avgAvoidanceHeading = boidsData[i].avoidanceHeading;
                boids[i].numPerceivedFlockmates = boidsData[i].numFlockmates;

                boids[i].UpdateBoid();
            }
        }
    }
}