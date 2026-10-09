using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace FruitNinjaVR
{
    public class SwordBlade : MonoBehaviour
    {
        [Header("Blade References")]
        [SerializeField] private Transform m_TipTransform;
        [SerializeField] private Transform m_BaseTransform;
        [SerializeField] private TrailRenderer m_TrailRenderer;

        [Header("Respawn Settings")]
        [SerializeField] private Vector3 m_SpawnPosition;
        [SerializeField] private Quaternion m_SpawnRotation;

        private XRGrabInteractable m_GrabInteractable;
        private Rigidbody m_Rb;
        private Vector3 m_PreviousTipPos;
        private Vector3 m_SwingVelocity;
        private float m_DroppedTimer = 0f;

        public Vector3 SwingVelocity => m_SwingVelocity;

        private void Awake()
        {
            m_GrabInteractable = GetComponentInParent<XRGrabInteractable>();
            m_Rb = GetComponentInParent<Rigidbody>();

            if (m_TipTransform == null) m_TipTransform = transform;
            if (m_BaseTransform == null) m_BaseTransform = transform;

            m_PreviousTipPos = m_TipTransform.position;
            m_SpawnPosition = transform.root.position;
            m_SpawnRotation = transform.root.rotation;

            if (m_GrabInteractable != null)
            {
                m_GrabInteractable.selectEntered.AddListener(OnGrabbed);
                m_GrabInteractable.selectExited.AddListener(OnReleased);
            }
        }

        private void Start()
        {
            if (IsPCOnly())
            {
                StartCoroutine(PCAutoGrabRoutine());
            }
        }

        public static bool IsPCOnly()
        {
            // If active physical VR headset is present and not mock/simulator
            bool hasRealVRHeadset = UnityEngine.XR.XRSettings.isDeviceActive && 
                !string.IsNullOrEmpty(UnityEngine.XR.XRSettings.loadedDeviceName) && 
                !UnityEngine.XR.XRSettings.loadedDeviceName.ToLower().Contains("mock") &&
                !UnityEngine.XR.XRSettings.loadedDeviceName.ToLower().Contains("simulator");

            if (hasRealVRHeadset)
            {
                var inputDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.HeadMounted, inputDevices);
                if (inputDevices.Count > 0 && inputDevices[0].isValid)
                {
                    return false; // Physical VR headset connected
                }
            }

            return true; // PC mode (Editor / Desktop / Simulator)
        }

        private System.Collections.IEnumerator PCAutoGrabRoutine()
        {
            yield return null;
            yield return new WaitForSeconds(0.25f);
            TryGrabKatanaOnPC();
        }

        public bool TryGrabKatanaOnPC()
        {
            if (m_GrabInteractable == null)
                m_GrabInteractable = GetComponentInParent<XRGrabInteractable>();

            if (m_GrabInteractable == null || m_GrabInteractable.isSelected)
                return false;

            var manager = m_GrabInteractable.interactionManager;
            if (manager == null)
            {
                manager = Object.FindFirstObjectByType<XRInteractionManager>();
            }

            if (manager == null) return false;

            // Locate right hand controller interactor
            IXRSelectInteractor chosenInteractor = null;
            var interactors = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var mb in interactors)
            {
                if (mb is IXRSelectInteractor selInt && mb.enabled && mb.gameObject.activeInHierarchy)
                {
                    string objName = mb.name.ToLower();
                    string parentName = mb.transform.parent != null ? mb.transform.parent.name.ToLower() : "";

                    if (objName.Contains("right") || parentName.Contains("right"))
                    {
                        chosenInteractor = selInt;
                        break;
                    }
                    if (chosenInteractor == null)
                    {
                        chosenInteractor = selInt;
                    }
                }
            }

            if (chosenInteractor != null)
            {
                if (chosenInteractor is MonoBehaviour interactorMb)
                {
                    Transform handT = interactorMb.transform;
                    transform.root.position = handT.position;
                    transform.root.rotation = handT.rotation;
                }

                manager.SelectEnter(chosenInteractor, m_GrabInteractable);
                return true;
            }

            return false;
        }

        public void SetSpawnTransform(Vector3 pos, Quaternion rot)
        {
            m_SpawnPosition = pos;
            m_SpawnRotation = rot;
        }

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            m_DroppedTimer = 0f;
            AudioClip grabClip = ProceduralAudio.GrabClip;
            if (grabClip != null)
            {
                AudioSource.PlayClipAtPoint(grabClip, transform.position, 0.8f);
            }
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            m_DroppedTimer = 0f;
        }

        private void Update()
        {
            if (IsPCOnly())
            {
                UpdatePCControls();
            }
            else
            {
                UpdateVRControls();
            }

            // Calculate blade tip swing velocity for slicing & particle physics
            Vector3 currentTipPos = m_TipTransform.position;
            if (Time.deltaTime > 0f)
            {
                m_SwingVelocity = (currentTipPos - m_PreviousTipPos) / Time.deltaTime;
            }
            m_PreviousTipPos = currentTipPos;
        }

        // Local X offset of the Blade child from the Katana_Sword root (as serialized in scene: x = 0.24)
        // Subtracted from target so the blade CENTER sits at the mouse cursor, not the handle root.
        private const float k_BladeLocalOffsetX = 0.24f;
        private Vector3 m_PreviousBladeCenter = Vector3.zero;
        private Quaternion m_TargetRotation = Quaternion.Euler(0f, 0f, 35f);

        private void UpdatePCControls()
        {
            if (m_Rb != null)
            {
                m_Rb.isKinematic = true;
                m_Rb.useGravity = false;
                m_Rb.detectCollisions = true;
            }

            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

            // Mouse-driven Fruit Ninja katana slicing on PC
            if (cam != null && Mouse.current != null)
            {
                Vector2 mouseScreen = Mouse.current.position.ReadValue();
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                Ray ray = cam.ScreenPointToRay(mouseScreen);

                // Slice plane at z = 1.85f aligned with flying fruits
                Plane slicePlane = new Plane(Vector3.back, new Vector3(0f, 0f, 1.85f));
                if (slicePlane.Raycast(ray, out float enter))
                {
                    Vector3 cursorWorldPos = ray.GetPoint(enter);
                    Transform rootT = transform.root;

                    // Align blade with mouse swipe motion if moving, or smoothly rest at ready angle
                    if (mouseDelta.sqrMagnitude > 9f)
                    {
                        float swipeAngle = Mathf.Atan2(mouseDelta.y, mouseDelta.x) * Mathf.Rad2Deg;
                        // Blade points along +X, so rotation around Z aligns blade tip with swipe
                        m_TargetRotation = Quaternion.Euler(10f, -10f, swipeAngle);
                    }
                    else
                    {
                        // Elegant ready stance pointing diagonally upward
                        m_TargetRotation = Quaternion.Slerp(m_TargetRotation, Quaternion.Euler(15f, -10f, 35f), Time.deltaTime * 6f);
                    }

                    // Shift root position so blade center (not handle root) is centered right at mouse cursor
                    Vector3 bladeWorldRight = m_TargetRotation * Vector3.right;
                    Vector3 targetRootPos = cursorWorldPos - bladeWorldRight * k_BladeLocalOffsetX;

                    // Update transform immediately so visuals, child transforms, and raycasts are 100% in sync
                    rootT.position = targetRootPos;
                    rootT.rotation = m_TargetRotation;

                    if (m_Rb != null)
                    {
                        m_Rb.position = targetRootPos;
                        m_Rb.rotation = m_TargetRotation;
                    }
                }

                // Generous multi-sample slice detection every frame (bypasses kinematic trigger limitations)
                DoOverlapSlice();
            }

            // New Input System hotkeys
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    ScoreManager.Instance?.ResetGame();
                }

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    StartCoroutine(PCTestSwingRoutine());
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartCoroutine(PCTestSwingRoutine());
            }
        }

        /// <summary>
        /// Manual slice detection using Physics.OverlapBox on the blade's bounds and sweep segment.
        /// This is 100% reliable regardless of kinematic Rigidbody state and eliminates tunneling.
        /// </summary>
        private void DoOverlapSlice()
        {
            Vector3 currentBladeCenter = transform.position;
            // Half-extents: 0.42m long (covers whole blade), 0.08m high, 0.35m deep (covers full fruit flight depth)
            Vector3 halfExtents = new Vector3(0.42f, 0.08f, 0.35f);

            // 1. Current frame box check
            Collider[] hits = Physics.OverlapBox(currentBladeCenter, halfExtents, transform.rotation);
            for (int i = 0; i < hits.Length; i++)
            {
                CheckSlice(hits[i]);
            }

            // 2. Continuous sweep interpolation between previous frame and current frame
            if (m_PreviousBladeCenter != Vector3.zero)
            {
                float dist = Vector3.Distance(m_PreviousBladeCenter, currentBladeCenter);
                if (dist > 0.05f)
                {
                    int steps = Mathf.Clamp(Mathf.CeilToInt(dist / 0.1f), 2, 8);
                    for (int s = 1; s < steps; s++)
                    {
                        Vector3 interpPos = Vector3.Lerp(m_PreviousBladeCenter, currentBladeCenter, (float)s / steps);
                        Collider[] sweepHits = Physics.OverlapBox(interpPos, halfExtents, transform.rotation);
                        for (int i = 0; i < sweepHits.Length; i++)
                        {
                            CheckSlice(sweepHits[i]);
                        }
                    }
                }
            }
            m_PreviousBladeCenter = currentBladeCenter;
        }

        private void UpdateVRControls()
        {
            bool isGrabbed = m_GrabInteractable != null && m_GrabInteractable.isSelected;
            if (!isGrabbed)
            {
                Transform rootT = transform.root;
                if (rootT.position.y < 0.3f || Vector3.Distance(rootT.position, m_SpawnPosition) > 4f)
                {
                    m_DroppedTimer += Time.deltaTime;
                    if (m_DroppedTimer > 2.5f)
                    {
                        RespawnBlade();
                    }
                }
            }
            else
            {
                m_DroppedTimer = 0f;
            }
        }

        private System.Collections.IEnumerator PCTestSwingRoutine()
        {
            Transform rootT = transform.root;
            Vector3 center = new Vector3(0f, 1.8f, 1.85f);
            Vector3 startPos = center + new Vector3(-0.95f, 0.55f, 0f);
            Vector3 endPos = center + new Vector3(0.95f, -0.45f, 0f);
            Quaternion startRot = Quaternion.Euler(-25f, 35f, 60f);
            Quaternion endRot = Quaternion.Euler(35f, -45f, -60f);

            if (m_Rb != null) m_Rb.isKinematic = true;

            float elapsed = 0f;
            float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rootT.position = Vector3.Lerp(startPos, endPos, t);
                rootT.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            yield return new WaitForSeconds(0.1f);
        }

        public void RespawnBlade()
        {
            m_DroppedTimer = 0f;
            Transform rootT = transform.root;
            if (m_Rb != null)
            {
                m_Rb.linearVelocity = Vector3.zero;
                m_Rb.angularVelocity = Vector3.zero;
            }
            rootT.position = m_SpawnPosition;
            rootT.rotation = m_SpawnRotation;
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckSlice(other);
        }

        private void OnTriggerStay(Collider other)
        {
            CheckSlice(other);
        }

        private void CheckSlice(Collider other)
        {
            if (other == null) return;
            Fruit fruit = other.GetComponentInParent<Fruit>();
            if (fruit == null) fruit = other.GetComponentInChildren<Fruit>();
            if (fruit != null && !fruit.IsSliced)
            {
                // Determine slice normal: cross product of blade edge and swing direction
                Vector3 bladeEdge = (m_TipTransform.position - m_BaseTransform.position).normalized;
                Vector3 swingDir = m_SwingVelocity.normalized;

                Vector3 sliceNormal = Vector3.zero;
                if (swingDir != Vector3.zero && swingDir.sqrMagnitude > 0.01f)
                {
                    sliceNormal = Vector3.Cross(bladeEdge, swingDir).normalized;
                }
                if (sliceNormal == Vector3.zero || sliceNormal.magnitude < 0.01f)
                {
                    // Clean perpendicular cut across blade in screen space (XY plane)
                    sliceNormal = Vector3.Cross(bladeEdge, Vector3.forward).normalized;
                    if (sliceNormal == Vector3.zero || sliceNormal.magnitude < 0.01f)
                    {
                        sliceNormal = transform.up;
                    }
                }

                // Send haptic impulse to the grabbing controller
                SendHapticFeedback();

                fruit.Slice(sliceNormal, other.ClosestPoint(transform.position));
            }
        }

        private void SendHapticFeedback()
        {
            if (m_GrabInteractable == null) return;

            foreach (var interactor in m_GrabInteractable.interactorsSelecting)
            {
                // Send haptic impulse via the XRBaseController on the interactor's GameObject
                var hapticProvider = interactor.transform.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics.IXRHapticImpulseProvider>();
                if (hapticProvider != null)
                {
                    hapticProvider.GetChannelGroup()?.GetChannel()?.SendHapticImpulse(0.7f, 0.12f);
                }
            }
        }
    }
}
