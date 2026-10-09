using UnityEngine;

namespace FruitNinjaVR
{
    public enum FruitType
    {
        Watermelon,
        Orange,
        Apple,
        Lemon,
        Lime
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class Fruit : MonoBehaviour
    {
        [Header("Fruit Configuration")]
        [SerializeField] private FruitType m_Type = FruitType.Watermelon;
        [SerializeField] private int m_ScorePoints = 10;
        [SerializeField] private Material m_RindMaterial;
        [SerializeField] private Material m_FleshMaterial;
        [SerializeField] private Color m_FleshColor = new Color(0.95f, 0.2f, 0.25f);

        private Rigidbody m_Rb;
        private bool m_IsSliced = false;

        public bool IsSliced => m_IsSliced;

        public void Setup(FruitType type, Material rindMat, Material fleshMat, Color fleshColor, float scale = 0.35f)
        {
            m_Type = type;
            m_RindMaterial = rindMat;
            m_FleshMaterial = fleshMat;
            m_FleshColor = fleshColor;
            transform.localScale = Vector3.one * scale;

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = rindMat;
            }
        }

        private void Awake()
        {
            m_Rb = GetComponent<Rigidbody>();
        }

        public void Slice(Vector3 sliceDirection, Vector3 slicePoint)
        {
            if (m_IsSliced) return;
            m_IsSliced = true;

            // Ensure slice normal is horizontal/perpendicular to swing
            Vector3 normal = sliceDirection.normalized;
            if (normal == Vector3.zero) normal = transform.right;

            // 1. Audio Feedback
            AudioClip sliceSound = ProceduralAudio.SliceClip;
            if (sliceSound != null)
            {
                AudioSource.PlayClipAtPoint(sliceSound, transform.position, 1f);
            }

            // 2. Visual Particle Feedback (Juice Splatter)
            FruitJuiceParticle.SpawnSplash(transform.position, m_FleshColor);

            // 3. Sliced Halves Spawning
            SpawnHalves(normal);

            // 4. Score Management
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(m_ScorePoints, transform.position);
            }

            // 5. Destroy Whole Fruit
            Destroy(gameObject);
        }

        private void SpawnHalves(Vector3 sliceNormal)
        {
            Quaternion cutRotation = Quaternion.FromToRotation(Vector3.right, sliceNormal);
            Vector3 currentVel = m_Rb != null ? m_Rb.linearVelocity : Vector3.zero;

            float impulseSpeed = Random.Range(2.0f, 3.5f);
            Vector3 impulsePos = sliceNormal * impulseSpeed + Vector3.up * 1.2f;
            Vector3 impulseNeg = -sliceNormal * impulseSpeed + Vector3.up * 1.2f;

            Vector3 torquePos = Random.insideUnitSphere * 15f;
            Vector3 torqueNeg = Random.insideUnitSphere * 15f;

            // Positive Half
            GameObject halfPosGo = new GameObject($"{name}_Half_R");
            halfPosGo.transform.position = transform.position + sliceNormal * 0.05f;
            halfPosGo.transform.rotation = cutRotation;
            halfPosGo.transform.localScale = transform.localScale;
            FruitHalf halfPos = halfPosGo.AddComponent<FruitHalf>();
            halfPos.Initialize(FruitMeshUtility.HemispherePositive, m_RindMaterial, m_FleshMaterial, m_FleshColor, currentVel, impulsePos, torquePos);

            // Negative Half
            GameObject halfNegGo = new GameObject($"{name}_Half_L");
            halfNegGo.transform.position = transform.position - sliceNormal * 0.05f;
            halfNegGo.transform.rotation = cutRotation;
            halfNegGo.transform.localScale = transform.localScale;
            FruitHalf halfNeg = halfNegGo.AddComponent<FruitHalf>();
            halfNeg.Initialize(FruitMeshUtility.HemisphereNegative, m_RindMaterial, m_FleshMaterial, m_FleshColor, currentVel, impulseNeg, torqueNeg);
        }

        private void Update()
        {
            if (transform.position.y < -3f)
            {
                Destroy(gameObject);
            }
        }
    }
}
