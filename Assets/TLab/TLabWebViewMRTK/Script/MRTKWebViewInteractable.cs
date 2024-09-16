using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using TLab.Android.WebView;

namespace TLab.XR.MRTK
{
    public class MRTKWebViewInteractable :
        BaseFocusHandler,
        IMixedRealityTouchHandler,
        IMixedRealityPointerHandler,
        IMixedRealitySourceStateHandler
    {
        protected class HandInteractData
        {
            public bool IsActive = true;
            public bool IsSourceNear = false;
            public Vector2 touchingQuadCoord = Vector2.zero;
            public Vector3 touchingPoint = Vector3.zero;
            public Vector3 touchingPointSmoothed = Vector3.zero;
            public Vector3 touchingInitialPt = Vector3.zero;
            public Vector3 touchingRayOffset = Vector3.zero;
            public Vector3 initialProjectedOffset = Vector3.zero;
            public IMixedRealityInputSource touchingSource = null;
            public IMixedRealityController currentController = null;
            public IMixedRealityPointer currentPointer = null;
        }

        #region Serialized Fields

        [SerializeField]
        [FormerlySerializedAs("enabled")]
        private bool isEnabled = true;
        /// <summary>
        /// This Property sets and gets whether a the pan/zoom behavior is active.
        /// </summary>
        public bool Enabled { get => isEnabled; set => isEnabled = value; }

        [SerializeField]
        private TLabWebView tlabWebView;

        [SerializeField]
        private GameObject keyborad;

        [SerializeField]
        [Range(0.0f, 99.0f)]
        private float smoothing = 80.0f;

        [Header("Visual affordance")]
        [SerializeField]
        [Tooltip("If affordance geometry is desired to emphasize the touch points(leftPoint and rightPoint) and the center point between them (reticle), assign them here.")]
        [FormerlySerializedAs("reticle")]
        private GameObject centerPoint = null;

        [SerializeField]
        private GameObject leftPoint = null;

        [SerializeField]
        private GameObject rightPoint = null;

        [Tooltip("When the slate is touched, what color to change on the ProximityLight center color override to. (Assumes the target material uses a proximity light and proximity light color override)")]
        [SerializeField]
        private Color proximityLightCenterColor = new Color(0.25f, 0.25f, 0.25f, 0.0f);

        [Header("Events")]
        public PanUnityEvent PanStarted = new PanUnityEvent();
        public PanUnityEvent PanStopped = new PanUnityEvent();
        public PanUnityEvent PanUpdated = new PanUnityEvent();

        #endregion Serialized Fields

        #region Private Properties

        private int m_lastXPos = 0;
        private int m_lastYPos = 0;

        private const int TOUCH_DOWN = 0;
        private const int TOUCH_UP = 1;
        private const int TOUCH_MOVE = 2;

        private Mesh mesh;
        private MeshFilter meshFilter;
        private BoxCollider boxCollider;

        private bool TouchActive => handDataMap.Count > 0;

        private float initialTouchDistance = 0.0f;
        private bool affordancesVisible = false;
        private float runningAverageSmoothing = 0.0f;
        private const float percentToDecimal = 0.01f;
        private Material currentMaterial;
        private int proximityLightCenterColorID;
        private Color defaultProximityLightCenterColor;
        private Dictionary<uint, HandInteractData> handDataMap = new Dictionary<uint, HandInteractData>();
        private bool oldIsTargetPositionLockedOnFocusLock;

#if UNITY_2019_3_OR_NEWER
        // Quad meshes by default (in 2019 and higher) appear to follow the vertex order
        // specified here: https://docs.unity3d.com/Manual/Example-CreatingaBillboardPlane.html
        // That is, LowerLeft->LowerRight->UpperLeft->UpperRight
        // Note that even though the example on that page is one that creates a quad manually
        // using a specific order of vertices, this order seems to be what a quad mesh defaults to.
        // This was discovered when looking into an issue on the SlateTests, which depend on the
        // projection math within this to be using the correct right and up vectors.
        private const int UpperLeftQuadIndex = 2;
        private const int UpperRightQuadIndex = 3;
        private const int LowerLeftQuadIndex = 0;
#else // !UNITY_2019_3_OR_NEWER
        // Quad meshes in 2018 and lower appear to follow a vertex order that looks like this:
        // [0] "(-0.5, -0.5, 0.0)"
        // [1] "(0.5, 0.5, 0.0)"
        // [2] "(0.5, -0.5, 0.0)"
        // [3] "(-0.5, 0.5, 0.0)"
        // That is, LowerLeft->UpperRight->LowerRight->UpperLeft
        // Note that the ifdefs only cover +/- 2019.3 because that was the min tested version
        // for Unity 2019 - this could very well be needed for 2019.2 and 2019.1, but with 2019.4
        // out at this point, support is mainly on the LTS release.
        private const int UpperLeftQuadIndex = 3;
        private const int UpperRightQuadIndex = 1;
        private const int LowerLeftQuadIndex = 0;
#endif

        #endregion Private Properties

        /// <summary>
        /// This function sets the pan and zoom back to their starting settings.
        /// </summary>
        public void Reset()
        {
            initialTouchDistance = 0.0f;
        }

        #region MonoBehaviour Handlers

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (isEnabled)
            {
                if (TouchActive)
                {
                    foreach (uint key in handDataMap.Keys)
                    {
                        if (UpdateHandTouchingPoint(key))
                        {
                            MoveTouch(key);
                        }
                    }
                }

                if (!TouchActive && affordancesVisible)
                {
                    SetAffordancesActive(false);
                }

                if (affordancesVisible)
                {
                    if (centerPoint != null)
                    {
                        centerPoint.transform.position = GetContactCenter();
                    }
                    if (leftPoint != null)
                    {
                        leftPoint.transform.position = GetContactForHand(Handedness.Left);
                    }
                    if (rightPoint != null)
                    {
                        rightPoint.transform.position = GetContactForHand(Handedness.Right);
                    }
                }
            }
        }

        #endregion MonoBehaviour Handlers

        #region Private Methods

        private bool UpdateHandTouchingPoint(uint sourceId)
        {
            Vector3 tryHandPoint = Vector3.zero;
            bool tryGetSucceeded = false;
            if (handDataMap.ContainsKey(sourceId))
            {
                HandInteractData data = handDataMap[sourceId];

                if (data.IsActive)
                {
                    if (data.IsSourceNear)
                    {
                        tryGetSucceeded = TryGetHandPositionFromController(data.currentController, TrackedHandJoint.IndexTip, out tryHandPoint);
                    }
                    else
                    {
                        TryGetHandRayPoint(data.currentController, out tryHandPoint);

                        Vector3 planePoint = this.transform.TransformPoint(mesh.vertices[0]);
                        Vector3 planeNormal = gameObject.transform.forward;
                        Vector3 rayPos = data.currentPointer.Result.StartPoint;
                        Vector3 rayDir = (tryHandPoint - rayPos).normalized;

                        float enter = PointerDirectionToQuad(rayDir, rayPos, planeNormal, planePoint);

                        tryHandPoint = rayPos + rayDir * enter;

                        tryGetSucceeded = enter > 0;
                    }

                    if (tryGetSucceeded)
                    {
                        tryHandPoint = SnapFingerToQuad(tryHandPoint);
                        Vector3 unfilteredTouchPt = tryHandPoint;
                        runningAverageSmoothing = smoothing * percentToDecimal;
                        unfilteredTouchPt *= (1.0f - runningAverageSmoothing);
                        data.touchingPointSmoothed = (data.touchingPointSmoothed * runningAverageSmoothing) + unfilteredTouchPt;
                        data.touchingPoint = data.touchingPointSmoothed;
                    }
                }
            }

            return true;
        }

        private bool TryGetHandRayPoint(IMixedRealityController controller, out Vector3 handRayPoint)
        {
            if (controller != null &&
                 controller.InputSource != null &&
                 controller.InputSource.Pointers != null &&
                 controller.InputSource.Pointers.Length > 0 &&
                 controller.InputSource.Pointers[0].Result != null)
            {
                handRayPoint = controller.InputSource.Pointers[0].Result.Details.Point;
                return true;
            }

            handRayPoint = Vector3.zero;
            return false;
        }

        private void Initialize()
        {
            SetAffordancesActive(false);

            // Check for boxcollider
            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                Debug.Log("The GameObject that runs this script must have a BoxCollider attached.");
            }
            else
            {
                Renderer renderer = this.GetComponent<Renderer>();
                Material material = (renderer != null) ? renderer.material : null;
                if ((material != null) && (material.mainTexture != null))
                {
                    material.mainTexture.wrapMode = TextureWrapMode.Repeat;
                }
            }

            // Get material
            currentMaterial = this.gameObject.GetComponent<Renderer>().material;
            proximityLightCenterColorID = Shader.PropertyToID("_ProximityLightCenterColorOverride");
            bool materialValid = currentMaterial != null && currentMaterial.HasProperty(proximityLightCenterColorID);
            defaultProximityLightCenterColor = materialValid ? currentMaterial.GetColor(proximityLightCenterColorID) : new Color(0.0f, 0.0f, 0.0f, 0.0f);

            // Precache references
            meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.Log("The GameObject: " + this.gameObject.name + " " + "does not have a Mesh component.");
            }
            else
            {
                mesh = meshFilter.mesh;
            }

            keyborad.SetActive(false);
        }

        private void UpdateTouchCoord(uint sourceId)
        {
            HandInteractData data = handDataMap[sourceId];
            data.touchingQuadCoord = GetQuadCoordFromPoint(data.touchingPoint);
        }

        private Vector3 GetContactCenter()
        {
            Vector3 center = Vector3.zero;

            if (handDataMap.Keys.Count > 0)
            {
                foreach (uint key in handDataMap.Keys)
                {
                    center += handDataMap[key].touchingPoint;
                }

                center /= (float)handDataMap.Keys.Count;
            }

            return center;
        }

        private void SetAffordancesActive(bool active)
        {
            affordancesVisible = active;
            if (centerPoint != null)
            {
                centerPoint.SetActive(affordancesVisible);
            }
            if (leftPoint != null)
            {
                leftPoint.SetActive(affordancesVisible);
            }
            if (rightPoint != null)
            {
                rightPoint.SetActive(affordancesVisible);
            }

            if (currentMaterial != null)
            {
                currentMaterial.SetColor(proximityLightCenterColorID, active ? proximityLightCenterColor : defaultProximityLightCenterColor);
            }
        }

        private Vector3 GetContactForHand(Handedness hand)
        {
            Vector3 handPoint = Vector3.zero;
            if (handDataMap.Keys.Count > 0)
            {
                foreach (uint key in handDataMap.Keys)
                {
                    if (handDataMap[key].currentController.ControllerHandedness == hand)
                    {
                        return handDataMap[key].touchingPoint;
                    }
                }
            }

            return handPoint;
        }

        private bool AreSourcesCompatible()
        {
            int score = 0;
            foreach (uint key in handDataMap.Keys)
            {
                score += handDataMap[key].IsSourceNear ? 1 : 0;
            }
            return (score == 0 || score == handDataMap.Keys.Count);
        }

        private float GetContactDistance()
        {
            if (handDataMap.Keys.Count < 2)
            {
                return 0.0f;
            }

            int index = 0;
            Vector3 a = Vector3.zero;
            Vector3 b = Vector3.zero;
            foreach (uint key in handDataMap.Keys)
            {
                if (index == 0)
                {
                    a = handDataMap[key].touchingPoint;
                }
                else if (index == 1)
                {
                    b = handDataMap[key].touchingPoint;
                }

                index++;
            }

            return (b - a).magnitude;
        }

        private Vector2 GetQuadCoordFromPoint(Vector3 point)
        {
            Vector2 quadCoord = GetQuadCoord(point);
            return quadCoord;
        }

        private Vector2 GetQuadCoord(Vector3 point)
        {
            Vector2 quadCoord = Vector2.zero;
            Vector3[] vertices = mesh.vertices;
            Vector3 upperLeft = transform.TransformPoint(vertices[UpperLeftQuadIndex]);
            Vector3 upperRight = transform.TransformPoint(vertices[UpperRightQuadIndex]);
            Vector3 lowerLeft = transform.TransformPoint(vertices[LowerLeftQuadIndex]);

            float magVertical = (lowerLeft - upperLeft).magnitude;
            float magHorizontal = (upperRight - upperLeft).magnitude;

            if (!Mathf.Approximately(0, magVertical) && !Mathf.Approximately(0, magHorizontal))
            {
                // Get point projection on vertices coordinates then divide by length to get quad coord 0 to 1
                quadCoord.x = Vector3.Dot(point - upperLeft, upperRight - upperLeft) / (magHorizontal * magHorizontal);
                quadCoord.y = Vector3.Dot(point - upperLeft, lowerLeft - upperLeft) / (magVertical * magVertical);
            }

            return quadCoord;
        }

        private float PointerDirectionToQuad(Vector3 rayDir, Vector3 rayPos, Vector3 planeNormal, Vector3 planePos)
        {
            Plane plane = new Plane(planeNormal, planePos);
            Ray ray = new Ray(rayPos, rayDir);

            plane.Raycast(ray, out float enter);

            return enter;
        }

        private Vector3 SnapFingerToQuad(Vector3 pointToSnap)
        {
            Vector3 planePoint = this.transform.TransformPoint(mesh.vertices[0]);
            Vector3 planeNormal = gameObject.transform.forward;

            return Vector3.ProjectOnPlane(pointToSnap - planePoint, planeNormal) + planePoint;
        }

        private void SetHandDataFromController(IMixedRealityController controller, IMixedRealityPointer pointer, bool isNear)
        {
            HandInteractData data = new HandInteractData();
            data.IsSourceNear = isNear;
            data.IsActive = true;
            data.touchingSource = controller.InputSource;
            data.currentController = controller;
            data.currentPointer = pointer;

            if (isNear)
            {
                if (TryGetHandPositionFromController(data.currentController, TrackedHandJoint.IndexTip, out Vector3 touchPosition))
                {
                    data.touchingInitialPt = SnapFingerToQuad(touchPosition);
                    data.touchingPointSmoothed = data.touchingInitialPt;
                    data.touchingPoint = data.touchingInitialPt;
                }
            }
            else // Is far
            {
                if (data.currentPointer is GGVPointer)
                {
                    data.touchingInitialPt = SnapFingerToQuad(data.currentPointer.Position);
                    data.touchingPoint = data.touchingInitialPt;
                    data.touchingPointSmoothed = data.touchingInitialPt;
                }
                else if (TryGetHandRayPoint(controller, out Vector3 handRayPt))
                {
                    data.touchingInitialPt = SnapFingerToQuad(handRayPt);
                    data.touchingPoint = data.touchingInitialPt;
                    data.touchingPointSmoothed = data.touchingInitialPt;
                    if (TryGetHandPositionFromController(data.currentController, TrackedHandJoint.Palm, out Vector3 touchPosition))
                    {
                        data.touchingRayOffset = handRayPt - SnapFingerToQuad(touchPosition);
                    }
                }
            }

            // Store value in case of MRController
            if (data.currentPointer != null)
            {
                Vector3 pt = data.currentPointer.Position;
                data.initialProjectedOffset = SnapFingerToQuad(pt);
            }

            data.touchingQuadCoord = GetQuadCoordFromPoint(data.touchingPoint);
            handDataMap.Add(data.touchingSource.SourceId, data);
            initialTouchDistance = GetContactDistance();

            if (handDataMap.Keys.Count > 1)
            {
                if (initialTouchDistance == 0)
                {
                    initialTouchDistance = GetContactDistance();
                }
                else
                {
                    float contactDist = GetContactDistance();
                    initialTouchDistance = contactDist + (initialTouchDistance - contactDist);
                }
            }

            SetAffordancesActive(isNear);

            StartTouch(data.touchingSource.SourceId);
        }

        private bool TryGetHandPositionFromController(IMixedRealityController controller, TrackedHandJoint joint, out Vector3 position)
        {
            if (controller is IMixedRealityHand hand)
            {
                if (hand.TryGetJoint(joint, out MixedRealityPose pose))
                {
                    position = pose.Position;
                    return true;
                }
            }

            position = Vector3.zero;
            return false;
        }

        private void UpdateTouchPos(Vector2 coord)
        {
            m_lastXPos = (int)(coord.x * tlabWebView.webSize.x);
            m_lastYPos = (int)(coord.y * tlabWebView.webSize.y);
        }

        #endregion Private Methods

        #region Internal State Handlers

        private void StartTouch(uint sourceId)
        {
            UpdateTouchCoord(sourceId);
            RaisePanStarted(sourceId);
        }

        private void EndTouch(uint sourceId)
        {
            if (handDataMap.ContainsKey(sourceId))
            {
                handDataMap.Remove(sourceId);
                RaisePanEnded(0);
            }
        }

        private void EndAllTouches()
        {
            if (handDataMap.Count > 0)
            {
                handDataMap.Clear();
                RaisePanEnded(0);
            }
        }

        private void MoveTouch(uint sourceId)
        {
            UpdateTouchCoord(sourceId);
            RaisePanning(sourceId);
        }

        #endregion Internal State Handlers

        // Ç±Ç±Ç≈WebViewÇÃëÄçÏÇåƒÇ—èoÇ∑
        #region Fire Events to Listening Objects

        private void RaisePanStarted(uint sourceId)
        {
            // Manuplate WebView
            if (TouchActive && AreSourcesCompatible() && handDataMap.ContainsKey(sourceId))
            {
                UpdateTouchPos(handDataMap[sourceId].touchingQuadCoord);
            }

            tlabWebView.TouchEvent(m_lastXPos, m_lastYPos, TOUCH_DOWN);

            // Play Audio
            PanStarted?.Invoke(new HandPanEventData());
        }

        private void RaisePanEnded(uint sourceId)
        {
            if (TouchActive && AreSourcesCompatible() && handDataMap.ContainsKey(sourceId))
            {
                UpdateTouchPos(handDataMap[sourceId].touchingQuadCoord);
            }

            tlabWebView.TouchEvent(m_lastXPos, m_lastYPos, TOUCH_UP);

            PanStopped?.Invoke(new HandPanEventData());
        }

        private void RaisePanning(uint sourceId)
        {
            if (TouchActive && AreSourcesCompatible() && handDataMap.ContainsKey(sourceId))
            {
                UpdateTouchPos(handDataMap[sourceId].touchingQuadCoord);
            }

            tlabWebView.TouchEvent(m_lastXPos, m_lastYPos, TOUCH_MOVE);

            PanUpdated?.Invoke(new HandPanEventData());
        }

        #endregion Fire Events to Listening Objects

        #region Keyborad

        public void SwitchKeyborad()
        {
            keyborad.SetActive(!keyborad.activeSelf);
        }

        #endregion

        #region BaseFocusHandler Methods

        /// <inheritdoc />
        public override void OnFocusEnter(FocusEventData eventData) { }

        /// <inheritdoc />
        public override void OnFocusExit(FocusEventData eventData)
        {
            EndAllTouches();
        }

        #endregion

        #region IMixedRealityTouchHandler
        /// <summary>
        /// In order to receive Touch Events from the IMixedRealityTouchHandler
        /// remember to add a NearInteractionTouchable script to the object that has this script.
        /// </summary>
        public void OnTouchStarted(HandTrackingInputEventData eventData)
        {
            EndTouch(eventData.SourceId);
            SetHandDataFromController(eventData.Controller, null, true);
            eventData.Use();
        }

        public void OnTouchCompleted(HandTrackingInputEventData eventData)
        {
            EndTouch(eventData.SourceId);
            eventData.Use();
        }

        public void OnTouchUpdated(HandTrackingInputEventData eventData) { }

        #endregion IMixedRealityTouchHandler

        #region IMixedRealityInputHandler Methods

        /// <summary>
        /// The Input Event handlers receive Hand Ray events.
        /// </summary>
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            bool isNear = eventData.Pointer is IMixedRealityNearPointer;
            oldIsTargetPositionLockedOnFocusLock = eventData.Pointer.IsTargetPositionLockedOnFocusLock;
            if (!isNear && eventData.Pointer.Controller.IsRotationAvailable)
            {
                eventData.Pointer.IsTargetPositionLockedOnFocusLock = false;
            }
            SetAffordancesActive(false);
            EndTouch(eventData.SourceId);
            SetHandDataFromController(eventData.Pointer.Controller, eventData.Pointer, isNear);
            eventData.Use();
        }

        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            eventData.Pointer.IsTargetPositionLockedOnFocusLock = oldIsTargetPositionLockedOnFocusLock;
            EndTouch(eventData.SourceId);
            eventData.Use();
        }

        #endregion IMixedRealityInputHandler Methods

        #region IMixedRealitySourceStateHandler Methods

        public void OnSourceLost(SourceStateEventData eventData)
        {
            EndTouch(eventData.SourceId);
            eventData.Use();
        }

        #endregion IMixedRealitySourceStateHandler Methods

        #region Unused Methods

        public void OnSourceDetected(SourceStateEventData eventData) { }

        public void OnPointerDragged(MixedRealityPointerEventData eventData) { }

        public void OnPointerClicked(MixedRealityPointerEventData eventData) { }

        #endregion Unused Methods
    }
}
