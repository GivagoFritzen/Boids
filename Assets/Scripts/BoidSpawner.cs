using System.Collections.Generic;
using UnityEngine;

namespace Boids
{
    public class BoidSpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        [SerializeField]
        private float spawnRadius = 10;
        [SerializeField]
        private int spawnCount = 10;
        [SerializeField]
        private GizmoType showSpawnRegion = GizmoType.SelectedOnly;

        [Header("Boids Settings")]
        [SerializeField]
        private Boid prefab = null;
        [SerializeField]
        private Transform target = null;
        private List<Boid> boids = new List<Boid>();

        public void Init(BoidManager boidManager)
        {
            SpawnPrefabs();
            boidManager.Add(boids, target);
        }

        private void SpawnPrefabs()
        {
            for (int i = 0; i < spawnCount; i++)
            {
                var pos = transform.position + Random.insideUnitSphere * spawnRadius;

                var boid = Instantiate(prefab);
                boid.transform.position = pos;
                boid.transform.forward = Random.insideUnitSphere;
                boid.transform.parent = transform;

                boids.Add(boid);
            }
        }

        #region DrawGizmos
        private void OnDrawGizmos()
        {
            if (showSpawnRegion == GizmoType.Always)
                DrawGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            if (showSpawnRegion == GizmoType.SelectedOnly)
                DrawGizmos();
        }

        private void DrawGizmos()
        {
            Gizmos.color = new Color(1, 1, 1, .5f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
        #endregion
    }
}