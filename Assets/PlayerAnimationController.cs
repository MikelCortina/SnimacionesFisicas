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
    private bool isCrouched = false; // NUEVO: Variable para toggle crouch

    [Header("Jump Settings")]
    public float jumpForce = 5.5f;

    [Header("Ground Check - AJUSTES IMPORTANTES")]
    public Transform groundCheckTransform;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask;
    public float groundCheckDistance = 0.4f;

    [Header("IK Settings")]
    public bool useIK = true;
    public Transform lookAtTarget;

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

    private void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;

        if (input.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(input, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);

            float baseSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            bool isCrouching = isCrouched; // Usa la variable toggle
            float currentSpeed = isCrouching ? baseSpeed * crouchSpeedMultiplier : baseSpeed;

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
        bool isCrouching = isCrouched; // Usa la variable toggle

        // === CROUCH TOGGLE ===
        if (Input.GetKeyDown(crouchKey))
        {
            isCrouched = !isCrouched; // Alterna el estado
            Debug.Log(isCrouched ? "¡CROUCH ACTIVADO!" : "Crouch desactivado");
        }

        anim.SetBool("Crouch", isCrouching);
        anim.SetBool("Crouch2",isCrouching);

        // Ajustar collider
        if (isCrouching)
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
            if (isCrouching)
                speedParam = 0.5f;
            else
                speedParam = running ? 1f : 0.5f;
        }
        anim.SetFloat("Speed", speedParam, 0.1f, Time.deltaTime);

        // GROUND CHECK
        bool grounded = IsGrounded();

        // JUMP (no mientras agachado)
        if (Input.GetKeyDown(KeyCode.Space) && grounded && !isCrouching)
        {
            anim.SetTrigger("Jump");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Ataque
        if (Input.GetMouseButtonDown(0))
            anim.SetTrigger("Attack");

        // Ragdoll
        if (Input.GetKeyDown(KeyCode.K)) EnableRagdoll();
        if (Input.GetKeyDown(KeyCode.L)) DisableRagdoll();

        // Toggle IK
        if (Input.GetKeyDown(KeyCode.I)) useIK = !useIK;
    }

    private bool IsGrounded()
    {
        Collider[] colliders = Physics.OverlapSphere(groundCheckTransform.position, groundCheckRadius, groundMask);
        foreach (Collider col in colliders)
        {
            if (col != playerCollider && !col.isTrigger)
                return true;
        }

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, groundCheckDistance, groundMask))
        {
            if (hit.collider != playerCollider && !hit.collider.isTrigger)
                return true;
        }
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

    public void PlayFootstep() => Debug.Log("Footstep");
    public void EnableDamage() => Debug.Log("Damage ON");
    public void DisableDamage() => Debug.Log("Damage OFF");

    private void OnAnimatorIK(int layerIndex)
    {
        if (!useIK) return;

        if (lookAtTarget != null)
        {
            anim.SetLookAtWeight(1f);
            anim.SetLookAtPosition(lookAtTarget.position);
        }

    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckTransform != null)
        {
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawSphere(groundCheckTransform.position, groundCheckRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, Vector3.down * groundCheckDistance);
        }
    }
}