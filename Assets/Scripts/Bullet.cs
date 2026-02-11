    using UnityEngine;

    public class Bullet : MonoBehaviour
    {
        public float speed = 60f;
        public float lifeTime = 2f;
        public float maxDistance = 100f;

        private Vector3 startPos;

        void Start()
        {
            startPos = transform.position;
            Destroy(gameObject, lifeTime);
        }

        void Update()
        {
            float move = speed * Time.deltaTime;
            transform.Translate(Vector3.forward * move);

            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, 0.5f))
            {
                EnemyBVH enemy = hit.collider.GetComponent<EnemyBVH>();
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }

                Destroy(gameObject);
            }

            if (Vector3.Distance(startPos, transform.position) > maxDistance)
            {
                Destroy(gameObject);
            }
        }
    }
