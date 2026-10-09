using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FruitNinjaVR
{
    public class FruitSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct FruitDefinition
        {
            public FruitType type;
            public string name;
            public Color rindColor;
            public Color fleshColor;
            public float scale;
            public int points;
        }

        [Header("Spawn Settings")]
        [SerializeField] private float m_MinSpawnDelay = 1.8f;
        [SerializeField] private float m_MaxSpawnDelay = 2.8f;
        [SerializeField] private Transform m_SpawnOrigin;
        [SerializeField] private Transform m_TargetArea;

        private List<FruitDefinition> m_FruitDefs = new List<FruitDefinition>();
        private Dictionary<FruitType, Material> m_RindMaterials = new Dictionary<FruitType, Material>();
        private Dictionary<FruitType, Material> m_FleshMaterials = new Dictionary<FruitType, Material>();

        private bool m_IsSpawning = true;

        private void Awake()
        {
            InitializeFruitDefinitions();
            CreateMaterials();
        }

        private void Start()
        {
            StartCoroutine(SpawnLoop());
        }

        private void InitializeFruitDefinitions()
        {
            m_FruitDefs.Add(new FruitDefinition
            {
                type = FruitType.Watermelon,
                name = "Watermelon",
                rindColor = new Color(0.12f, 0.45f, 0.16f), // Deep green
                fleshColor = new Color(0.96f, 0.22f, 0.28f), // Vibrant red
                scale = 0.38f,
                points = 10
            });

            m_FruitDefs.Add(new FruitDefinition
            {
                type = FruitType.Orange,
                name = "Orange",
                rindColor = new Color(1.0f, 0.5f, 0.05f), // Rich orange
                fleshColor = new Color(1.0f, 0.65f, 0.2f),
                scale = 0.30f,
                points = 15
            });

            m_FruitDefs.Add(new FruitDefinition
            {
                type = FruitType.Apple,
                name = "Apple",
                rindColor = new Color(0.85f, 0.12f, 0.12f), // Bright red
                fleshColor = new Color(0.98f, 0.95f, 0.8f), // Creamy flesh
                scale = 0.28f,
                points = 20
            });

            m_FruitDefs.Add(new FruitDefinition
            {
                type = FruitType.Lemon,
                name = "Lemon",
                rindColor = new Color(0.98f, 0.88f, 0.12f), // Bright yellow
                fleshColor = new Color(0.95f, 0.92f, 0.65f),
                scale = 0.25f,
                points = 25
            });

            m_FruitDefs.Add(new FruitDefinition
            {
                type = FruitType.Lime,
                name = "Lime",
                rindColor = new Color(0.35f, 0.85f, 0.15f), // Lime green
                fleshColor = new Color(0.85f, 0.95f, 0.7f),
                scale = 0.23f,
                points = 30
            });
        }

        private void CreateMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            foreach (var def in m_FruitDefs)
            {
                Material rind = new Material(litShader);
                rind.name = $"{def.name}_Rind_Mat";
                rind.color = def.rindColor;
                if (rind.HasProperty("_Smoothness")) rind.SetFloat("_Smoothness", 0.55f);
                m_RindMaterials[def.type] = rind;

                Material flesh = new Material(litShader);
                flesh.name = $"{def.name}_Flesh_Mat";
                flesh.color = def.fleshColor;
                if (flesh.HasProperty("_Smoothness")) flesh.SetFloat("_Smoothness", 0.35f);
                m_FleshMaterials[def.type] = flesh;
            }
        }

        private IEnumerator SpawnLoop()
        {
            yield return new WaitForSeconds(1.5f); // Initial prep delay

            while (m_IsSpawning)
            {
                float delay = Random.Range(m_MinSpawnDelay, m_MaxSpawnDelay);
                yield return new WaitForSeconds(delay);

                // 70% 1 fruit, 30% 2 fruits burst
                int count = Random.value < 0.3f ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    SpawnSingleFruit();
                    if (count > 1) yield return new WaitForSeconds(0.18f);
                }
            }
        }

        private void SpawnSingleFruit()
        {
            FruitDefinition def = m_FruitDefs[Random.Range(0, m_FruitDefs.Count)];
            bool isPC = SwordBlade.IsPCOnly();

            Vector3 spawnPos;
            float targetZ;
            float targetX;
            float apexHeight;

            if (isPC)
            {
                // PC Mode: Classic Fruit Ninja view!
                // Spawn in a moderate x spread, then arc STRAIGHT UP through the z=1.85 slice plane.
                // targetX ≈ spawnX so velocity has near-zero horizontal drift — easy to intercept with mouse.
                float spawnX = Random.Range(-0.55f, 0.55f);
                spawnPos = new Vector3(spawnX, Random.Range(0.25f, 0.45f), Random.Range(2.4f, 2.6f));
                targetZ = Random.Range(1.82f, 1.90f); // Directly crosses the mouse slice plane (1.85f)
                targetX = spawnX + Random.Range(-0.08f, 0.08f); // Nearly straight up, tiny wobble
                apexHeight = Random.Range(2.00f, 2.40f); // Nice arc, fully visible in PC camera
            }
            else
            {
                // VR Mode: In front of player within reach of Quest controllers
                spawnPos = m_SpawnOrigin != null ? m_SpawnOrigin.position : new Vector3(0f, 0.45f, 2.2f);
                spawnPos.y = Mathf.Max(spawnPos.y, 0.4f);
                spawnPos.x += Random.Range(-0.55f, 0.55f);
                spawnPos.z += Random.Range(-0.1f, 0.1f);

                targetZ = Random.Range(0.95f, 1.25f);
                targetX = Random.Range(-0.5f, 0.5f);
                apexHeight = Random.Range(2.15f, 2.55f);
            }

            // Create whole fruit GameObject
            GameObject fruitGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fruitGo.name = def.name;
            fruitGo.transform.position = spawnPos;

            Fruit fruit = fruitGo.AddComponent<Fruit>();
            fruit.Setup(def.type, m_RindMaterials[def.type], m_FleshMaterials[def.type], def.fleshColor, def.scale);

            Rigidbody rb = fruitGo.GetComponent<Rigidbody>();
            if (rb == null) rb = fruitGo.AddComponent<Rigidbody>();
            rb.mass = 0.45f;

            // Calculate ballistic arc velocity to soar high into player's field of view
            float gravity = Mathf.Abs(Physics.gravity.y);
            float vy = Mathf.Sqrt(2f * gravity * (apexHeight - spawnPos.y));
            float timeToApex = vy / gravity;

            float vx = (targetX - spawnPos.x) / timeToApex;
            float vz = (targetZ - spawnPos.z) / timeToApex;

            rb.linearVelocity = new Vector3(vx, vy, vz);
            rb.angularVelocity = Random.insideUnitSphere * 8f;

            // Audio feedback
            AudioClip launchClip = ProceduralAudio.LaunchClip;
            if (launchClip != null)
            {
                AudioSource.PlayClipAtPoint(launchClip, spawnPos, 0.6f);
            }
        }
    }
}
