using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerAnimationController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody rb;
    private CapsuleCollider playerCollider;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;
    public float crouchSpeedMultiplier = 0.5f;

    [Header("Crouch Settings")]
    public KeyCode crouchKey = KeyCode.C;
    public float crouchColliderHeight = 1.0f;
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private bool isCrouched = false;

    [Header("Jump Settings")]
    public float jumpForce = 5.5f;

    [Header("Ground Check")]
    public Transform groundCheckTransform;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask;
    public float groundCheckDistance = 0.4f;

    [Header("IK Settings")]
    public bool useIK = true;
    public Transform lookAtTarget;

    [Header("Hand IK")]
    public Transform rightHandTarget;
    [Range(0, 1)] public float rightHandWeight = 1f;

    [Header("Foot IK")]
    public bool useFootIK = true;
    public LayerMask footGroundMask;
    public float footRayDistance = 1.2f;
    public float footOffsetY = 0.02f;

    [Header("Ragdoll")]
    public Collider[] ragdollColliders;
    public Rigidbody[] ragdollRigidbodies;
    public Transform hipsBone;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();

        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;

        if (groundCheckTransform == null)
        {
            GameObject groundObj = new GameObject("GroundCheck");
            groundObj.transform.SetParent(transform);
            groundCheckTransform = groundObj.transform;
            groundCheckTransform.localPosition = new Vector3(0, 0.1f, 0);
        }

        foreach (Rigidbody r in ragdollRigidbodies) r.isKinematic = true;
        foreach (Collider c in ragdollColliders) c.enabled = false;
    }

    void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;

        if (input.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(input, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);

            float baseSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            float currentSpeed = isCrouched ? baseSpeed * crouchSpeedMultiplier : baseSpeed;

            Vector3 move = transform.forward * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + move);
        }
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;
        bool running = Input.GetKey(KeyCode.LeftShift);

        // === CROUCH TOGGLE ===
        if (Input.GetKeyDown(crouchKey))
            isCrouched = !isCrouched;

        anim.SetBool("Crouch", isCrouched);
        anim.SetBool("Crouch2", isCrouched);

        // Collider crouch
        if (isCrouched)
        {
            playerCollider.height = crouchColliderHeight;
            playerCollider.center = new Vector3(0, crouchColliderHeight / 2f, 0);
        }
        else
        {
            playerCollider.height = originalColliderHeight;
            playerCollider.center = originalColliderCenter;
        }

        // Locomotion
        float speedParam = 0f;
        if (input.magnitude > 0.1f)
        {
            if (isCrouched) speedParam = 0.5f;
            else speedParam = running ? 1f : 0.5f;
        }
        anim.SetFloat("Speed", speedParam, 0.1f, Time.deltaTime);

        bool grounded = IsGrounded();

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && grounded && !isCrouched)
        {
            anim.SetTrigger("Jump");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Attack
        if (Input.GetMouseButtonDown(0))
            anim.SetTrigger("Attack");

        // Ragdoll
        if (Input.GetKeyDown(KeyCode.K)) EnableRagdoll();
        if (Input.GetKeyDown(KeyCode.L)) DisableRagdoll();

        // Toggle IK
        if (Input.GetKeyDown(KeyCode.I)) useIK = !useIK;
    }

    bool IsGrounded()
    {
        Collider[] colliders = Physics.OverlapSphere(groundCheckTransform.position, groundCheckRadius, groundMask);
        foreach (Collider col in colliders)
            if (col != playerCollider && !col.isTrigger)
                return true;

        if (Physics.Raycast(transform.position + Vector3.up * 0.1f,
            Vector3.down, out RaycastHit hit, groundCheckDistance, groundMask))
            return true;

        return false;
    }

    public void EnableRagdoll()
    {
        anim.enabled = false;
        foreach (Rigidbody r in ragdollRigidbodies) r.isKinematic = false;
        foreach (Collider c in ragdollColliders) c.enabled = true;
    }

    public void DisableRagdoll()
    {
        transform.position = hipsBone.position;
        transform.rotation = hipsBone.rotation;

        foreach (Rigidbody r in ragdollRigidbodies) r.isKinematic = true;
        foreach (Collider c in ragdollColliders) c.enabled = false;

        anim.enabled = true;
        anim.Play("Idle", 0, 0f);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (!useIK || anim == null) return;

        // LOOK AT
        if (lookAtTarget != null)
        {
            anim.SetLookAtWeight(1f);
            anim.SetLookAtPosition(lookAtTarget.position);
        }

        // RIGHT HAND IK
        if (rightHandTarget != null)
        {
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandWeight);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandWeight);

            anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }
        else
        {
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        }

        // FOOT IK
        if (useFootIK)
        {
            HandleFootIK(AvatarIKGoal.LeftFoot);
            HandleFootIK(AvatarIKGoal.RightFoot);
        }
    }

    void HandleFootIK(AvatarIKGoal foot)
    {
        anim.SetIKPositionWeight(foot, 1f);
        anim.SetIKRotationWeight(foot, 1f);

        Vector3 footPos = anim.GetIKPosition(foot);
        Ray ray = new Ray(footPos + Vector3.up, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, footRayDistance, footGroundMask))
        {
            Vector3 targetPos = hit.point;
            targetPos.y += footOffsetY;

            anim.SetIKPosition(foot, targetPos);

            Quaternion rot = Quaternion.LookRotation(transform.forward, hit.normal);
            anim.SetIKRotation(foot, rot);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckTransform != null)
        {
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawSphere(groundCheckTransform.position, groundCheckRadius);
        }
    }
}
