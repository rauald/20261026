using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class SimplePlayerMotor : MonoBehaviour
{
    public Transform leftArm, rightArm, leftLeg, rightLeg;
    public float speed = 4f;
    [Min(1f)] public float turnSpeed = 180f; // 90 degrees in 0.5 seconds.
    [Range(0.1f, 5f)] public float jumpHeight = 2.5f;
    public float gravity = -12f;
    private CharacterController controller;
    private float verticalSpeed, phase;
    private Mesh frontMesh;
    private Material frontMaterial;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        AddFrontColor();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        Vector2 input = Vector2.zero;
        if (keyboard != null)
        {
            input.x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            input.y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
        }
        // A/D steer; W/S move along the current facing without turning around.
        transform.Rotate(0f, input.x * turnSpeed * Time.deltaTime, 0f);
        if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2f;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && controller.isGrounded)
            verticalSpeed = Mathf.Sqrt(2f * Mathf.Abs(gravity) * Mathf.Clamp(jumpHeight, 0.1f, 5f));
        verticalSpeed += gravity * Time.deltaTime;
        Vector3 before = transform.position;
        CollisionFlags flags = controller.Move((transform.forward * (speed * input.y)
            + Vector3.up * verticalSpeed) * Time.deltaTime);
        if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
        Vector3 delta = transform.position - before;
        delta.y = 0;
        bool walking = controller.isGrounded && delta.sqrMagnitude > 0.000001f;
        if (walking) phase += delta.magnitude * 3f;
        float angle = walking ? Mathf.Sin(phase * Mathf.PI) * 30f : 0;
        if (!controller.isGrounded)
        {
            // Raise both arms on ascent, then open the pose while descending.
            float ascent = Mathf.Clamp01(verticalSpeed / 6f);
            AnimatePose(leftArm, -55f - 65f * ascent, -12f);
            AnimatePose(rightArm, -55f - 65f * ascent, 12f);
            AnimatePose(leftLeg, -15f - 20f * ascent, -6f);
            AnimatePose(rightLeg, 15f + 15f * ascent, 6f);
        }
        else
        {
            AnimatePose(leftLeg, angle); AnimatePose(rightLeg, -angle);
            AnimatePose(leftArm, -angle); AnimatePose(rightArm, angle);
        }
        if (transform.position.y < -15)
        {
            controller.enabled = false;
            transform.position = new Vector3(0, 0.05f, 0);
            verticalSpeed = 0;
            controller.enabled = true;
        }
    }
    static void AnimatePose(Transform limb, float angle, float spread = 0f)
    {
        if (limb == null) return;
        // Smooth takeoff and landing transitions independently of frame rate.
        float blend = 1f - Mathf.Exp(-14f * Time.deltaTime);
        limb.localRotation = Quaternion.Slerp(limb.localRotation,
            Quaternion.Euler(angle, 0, spread), blend);
    }

    void AddFrontColor()
    {
        // Color the existing cube's +Z face; no extra body parts or textures.
        var body = transform.Find("Body");
        if (body == null) return;
        var filter = body.GetComponent<MeshFilter>();
        var renderer = body.GetComponent<Renderer>();
        if (filter == null || renderer == null || filter.sharedMesh == null
            || renderer.sharedMaterial == null) return;
        frontMesh = Instantiate(filter.sharedMesh);
        frontMesh.name = "Body With Front Face";
        var triangles = frontMesh.triangles;
        var normals = frontMesh.normals;
        var baseFaces = new System.Collections.Generic.List<int>();
        var frontFace = new System.Collections.Generic.List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var destination = normals[triangles[i]].z > 0.9f ? frontFace : baseFaces;
            destination.Add(triangles[i]);
            destination.Add(triangles[i + 1]);
            destination.Add(triangles[i + 2]);
        }
        frontMesh.subMeshCount = 2;
        frontMesh.SetTriangles(baseFaces, 0);
        frontMesh.SetTriangles(frontFace, 1);
        frontMaterial = new Material(renderer.sharedMaterial);
        frontMaterial.name = "Front Solid Yellow";
        frontMaterial.color = new Color(1f, 0.8f, 0.05f);
        renderer.sharedMaterials = new[] { renderer.sharedMaterial, frontMaterial };
        filter.sharedMesh = frontMesh;
    }

    void OnDestroy()
    {
        if (frontMesh != null) Destroy(frontMesh);
        if (frontMaterial != null) Destroy(frontMaterial);
    }
}
