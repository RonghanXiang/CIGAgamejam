using UnityEngine;

[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(200)]
public class CameraFollow : MonoBehaviour
{
    const float ReferenceMapWidth = 18f;
    const float ReferenceMapHeight = 10f;
    const float ViewPadding = 1f;

    [SerializeField] float borderThickness = 0.6f;
    [SerializeField] float rotationLerpSpeed = 12f;

    Camera cam;
    Transform target;
    PlayAreaBounds playArea;
    bool alignRotationToGravity;

    public bool AlignRotationToGravity => alignRotationToGravity;
    public bool IsFollowingPlayer => target != null;

    void Awake()
    {
        cam = GetComponent<Camera>();
        playArea = FindObjectOfType<PlayAreaBounds>();
    }

    void Start()
    {
        EnsureReferences();
        ApplyViewSize();
        cam.backgroundColor = new Color(0.15f, 0.17f, 0.22f);

        if (target != null)
            FollowTarget();
    }

    void EnsureReferences()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (playArea == null)
            playArea = FindObjectOfType<PlayAreaBounds>();

        if (target == null)
        {
            var player = FindObjectOfType<PlayerMove>();
            if (player != null)
                target = player.transform;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
            alignRotationToGravity = !alignRotationToGravity;
    }

    void LateUpdate()
    {
        EnsureReferences();

        if (target == null || playArea == null)
            return;

        ApplyViewSize();
        ApplyCameraRotation();
        FollowTarget();
    }

    void ApplyCameraRotation()
    {
        float targetAngle = 0f;
        if (alignRotationToGravity)
        {
            Vector2 gravityDir = WorldGravity.Direction.normalized;
            targetAngle = Mathf.Atan2(-gravityDir.x, -gravityDir.y) * Mathf.Rad2Deg;
        }

        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationLerpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    void ApplyViewSize()
    {
        cam.orthographic = true;

        float refHalfH = ReferenceMapHeight * 0.5f + borderThickness + ViewPadding;
        float refHalfW = ReferenceMapWidth * 0.5f + borderThickness + ViewPadding;
        float aspect = (float)Screen.width / Screen.height;
        cam.orthographicSize = aspect >= ReferenceMapWidth / ReferenceMapHeight
            ? refHalfH
            : refHalfW / aspect;
    }

    void FollowTarget()
    {
        Vector2 mapCenter = playArea.transform.position;
        Vector2 halfMap = playArea.InnerSize * 0.5f;
        float halfViewH = cam.orthographicSize;
        float halfViewW = halfViewH * cam.aspect;

        Vector2 camRight = transform.right;
        Vector2 camUp = transform.up;
        float mapExtentX = Mathf.Abs(camRight.x) * halfMap.x + Mathf.Abs(camRight.y) * halfMap.y;
        float mapExtentY = Mathf.Abs(camUp.x) * halfMap.x + Mathf.Abs(camUp.y) * halfMap.y;

        Vector2 offsetFromMap = (Vector2)target.position - mapCenter;
        float camLocalX = Vector2.Dot(offsetFromMap, camRight);
        float camLocalY = Vector2.Dot(offsetFromMap, camUp);

        float minCamLocalX = -mapExtentX + halfViewW;
        float maxCamLocalX = mapExtentX - halfViewW;
        float minCamLocalY = -mapExtentY + halfViewH;
        float maxCamLocalY = mapExtentY - halfViewH;

        if (minCamLocalX > maxCamLocalX)
            camLocalX = 0f;
        else
            camLocalX = Mathf.Clamp(camLocalX, minCamLocalX, maxCamLocalX);

        if (minCamLocalY > maxCamLocalY)
            camLocalY = 0f;
        else
            camLocalY = Mathf.Clamp(camLocalY, minCamLocalY, maxCamLocalY);

        Vector2 clampedOffset = camRight * camLocalX + camUp * camLocalY;
        Vector3 pos = transform.position;
        pos.x = mapCenter.x + clampedOffset.x;
        pos.y = mapCenter.y + clampedOffset.y;
        pos.z = -10f;
        transform.position = pos;
    }
}
