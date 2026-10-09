using System.Collections;
using UnityEngine;

namespace FruitNinjaVR
{
    public class FruitHalf : MonoBehaviour
    {
        private Rigidbody m_Rb;
        private Color m_FleshColor;
        private float m_HangTime = 1.3f;
        private bool m_IsFizzling = false;

        public void Initialize(Mesh mesh, Material rindMat, Material fleshMat, Color fleshColor, Vector3 initialVelocity, Vector3 impulseForce, Vector3 torque)
        {
            m_FleshColor = fleshColor;

            MeshFilter mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new Material[] { rindMat, fleshMat };

            // Trigger collider so halves do not physically bash into each other or the katana
            SphereCollider col = gameObject.AddComponent<SphereCollider>();
            col.radius = 0.4f;
            col.isTrigger = true;

            m_Rb = gameObject.AddComponent<Rigidbody>();
            m_Rb.mass = 0.35f;
            m_Rb.linearVelocity = initialVelocity + impulseForce;
            m_Rb.angularVelocity = torque;

            StartCoroutine(FizzleRoutine());
        }

        private IEnumerator FizzleRoutine()
        {
            // Tumble and separate in the air
            yield return new WaitForSeconds(m_HangTime);

            m_IsFizzling = true;

            // Audio Feedback: play sizzling fizzle sound
            AudioClip fizzleClip = ProceduralAudio.FizzleClip;
            if (fizzleClip != null)
            {
                AudioSource.PlayClipAtPoint(fizzleClip, transform.position, 0.75f);
            }

            // Particle Feedback: spawn steam/sparkle fizzle vapor puff
            FruitJuiceParticle.SpawnFizzlePuff(transform.position, m_FleshColor);

            // Visual Feedback: smooth scale shrink / fizzle dissolve
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            const float duration = 0.45f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void Update()
        {
            if (!m_IsFizzling && transform.position.y < -1.5f)
            {
                Destroy(gameObject);
            }
        }
    }
}
