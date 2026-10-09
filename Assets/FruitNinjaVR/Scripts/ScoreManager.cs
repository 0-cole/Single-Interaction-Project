using System.Collections;
using UnityEngine;

namespace FruitNinjaVR
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("UI / Display References")]
        [SerializeField] private TextMesh m_ScoreTextMesh;
        [SerializeField] private TextMesh m_ComboTextMesh;

        private int m_Score = 0;
        private int m_FruitsSliced = 0;
        private int m_CurrentCombo = 0;
        private float m_ComboTimer = 0f;
        private const float COMBO_WINDOW = 0.6f;

        public int Score => m_Score;
        public int FruitsSliced => m_FruitsSliced;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            UpdateDisplays();
        }

        private void Update()
        {
            if (m_ComboTimer > 0f)
            {
                m_ComboTimer -= Time.deltaTime;
                if (m_ComboTimer <= 0f)
                {
                    m_CurrentCombo = 0;
                    UpdateDisplays();
                }
            }
        }

        public void AddScore(int points, Vector3 worldPosition)
        {
            m_FruitsSliced++;

            if (m_ComboTimer > 0f)
            {
                m_CurrentCombo++;
            }
            else
            {
                m_CurrentCombo = 1;
            }
            m_ComboTimer = COMBO_WINDOW;

            int comboBonus = (m_CurrentCombo - 1) * 5;
            int totalGain = points + comboBonus;
            m_Score += totalGain;

            UpdateDisplays();
            SpawnFloatingText(worldPosition, m_CurrentCombo > 1 ? $"+{totalGain} COMBO x{m_CurrentCombo}!" : $"+{totalGain}");
        }

        public void ResetGame()
        {
            m_Score = 0;
            m_FruitsSliced = 0;
            m_CurrentCombo = 0;
            m_ComboTimer = 0f;
            UpdateDisplays();
        }

        private void UpdateDisplays()
        {
            if (m_ScoreTextMesh != null)
            {
                m_ScoreTextMesh.text = $"SCORE: {m_Score}\nSLICES: {m_FruitsSliced}";
            }

            if (m_ComboTextMesh != null)
            {
                if (m_CurrentCombo > 1)
                {
                    m_ComboTextMesh.text = $"COMBO x{m_CurrentCombo}!";
                    m_ComboTextMesh.color = Color.yellow;
                }
                else
                {
                    m_ComboTextMesh.text = "READY TO SLICE!";
                    m_ComboTextMesh.color = new Color(0.8f, 0.8f, 0.8f);
                }
            }
        }

        private void SpawnFloatingText(Vector3 pos, string text)
        {
            GameObject textGo = new GameObject("ScorePopup");
            textGo.transform.position = pos + Vector3.up * 0.2f;

            // Face the player at origin
            textGo.transform.rotation = Quaternion.LookRotation(textGo.transform.position - Vector3.up * 1.5f);

            TextMesh tm = textGo.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = 28;
            tm.characterSize = 0.05f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.yellow;

            StartCoroutine(AnimateFloatingText(textGo));
        }

        private IEnumerator AnimateFloatingText(GameObject go)
        {
            float duration = 1.0f;
            float elapsed = 0f;
            Vector3 startPos = go.transform.position;
            TextMesh tm = go.GetComponent<TextMesh>();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (go == null) yield break;

                go.transform.position = startPos + Vector3.up * (t * 0.5f);
                if (tm != null)
                {
                    Color c = tm.color;
                    c.a = 1f - t;
                    tm.color = c;
                }

                yield return null;
            }

            if (go != null)
            {
                Destroy(go);
            }
        }
    }
}
