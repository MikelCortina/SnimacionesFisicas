using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerAnimationController : MonoBehaviour
{
    private Animator anim;
    private Rigidbody rb;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 6f;

    [Header("Ground Check")]
    public Transform groundCheckTransform;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask;

    [Header("IK Settings")]
    public bool useIK = true;
    public Transform rightHandTarget;   // asigna en el inspector si quieres IK de mano
    public Transform lookAtTarget;      // objeto al que mira
    public float footDistance = 0.1f;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        // Si no asignas groundCheckTransform, lo crea automáticamente bajo los pies
        if (groundCheckTransform == null)
        {
            GameObject groundObj = new GameObject("GroundCheck");
            groundObj.transform.SetParent(transform);
            groundObj.transform.localPosition = new Vector3(0, 0.1f, 0);
            groundCheckTransform = groundObj.transform;
        }
    }

    void Update()
    {
        // === INPUT ===
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;

        bool running = Input.GetKey(KeyCode.LeftShift);

        // === LOCOMOTION ===
        float speedParam = 0f;
        if (input.magnitude > 0.1f)
            speedParam = running ? 1f : 0.5f;

        anim.SetFloat("Speed", speedParam, 0.1f, Time.deltaTime);

        // === JUMP ===
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            anim.SetBool("IsJumping", true);
            rb.AddForce(Vector3.up * 5.5f, ForceMode.Impulse);
        }

        // === ATTACK (ejemplo combo) ===
        if (Input.GetMouseButtonDown(0))
            anim.SetTrigger("Attack");

        // === RAGDOLL TEST ===
        if (Input.GetKeyDown(KeyCode.K)) EnableRagdoll();
        if (Input.GetKeyDown(KeyCode.L)) DisableRagdoll();

        // === TOGGLE IK ===
        if (Input.GetKeyDown(KeyCode.I)) useIK = !useIK;
    }

    // ========================================
    // GROUND CHECK (esta es la función que faltaba)
    // ========================================
    private bool IsGrounded()
    {
        return Physics.CheckSphere(groundCheckTransform.position, groundCheckRadius, groundMask);
    }

    // ========================================
    // RAGDOLL
    // ========================================
    [Header("Ragdoll")]
    public Collider[] ragdollColliders;
    public Rigidbody[] ragdollRigidbodies;
    public Transform hipsBone; // arrastra el hueso "Hips" aquí

    public void EnableRagdoll()
    {
        anim.enabled = false;
        foreach (Rigidbody r in ragdollRigidbodies) r.isKinematic = false;
        foreach (Collider c in ragdollColliders) c.enabled = true;
    }

    public void DisableRagdoll()
    {
        // Guardamos la posición final del ragdoll (usamos las caderas)
        transform.position = hipsBone.position;
        transform.rotation = hipsBone.rotation;

        foreach (Rigidbody r in ragdollRigidbodies) r.isKinematic = true;
        foreach (Collider c in ragdollColliders) c.enabled = false;

        anim.enabled = true;
        anim.Play("Idle", 0, 0f); // vuelve al idle limpio
    }

    // ========================================
    // ANIMATION EVENTS (llamados desde las animaciones)
    // ========================================
    public void JumpEnded() => anim.SetBool("IsJumping", false);
    public void PlayFootstep() => Debug.Log("Footstep"); // aquí pones tu sonido
    public void EnableDamage() => Debug.Log("Damage ON");
    public void DisableDamage() => Debug.Log("Damage OFF");

    // ========================================
    // INVERSE KINEMATICS
    // ========================================
    private void OnAnimatorIK(int layerIndex)
    {
        if (!useIK) return;

        // 1. Look At IK
        if (lookAtTarget != null)
        {
            anim.SetLookAtWeight(1f);
            anim.SetLookAtPosition(lookAtTarget.position);
        }

        // 2. Right Hand IK (para coger objetos)
        if (rightHandTarget != null)
        {
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
            anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }

        // 3. Foot Placement IK (extra muy valorado)
        AdjustFoot(AvatarIKGoal.LeftFoot);
        AdjustFoot(AvatarIKGoal.RightFoot);
    }

    private void AdjustFoot(AvatarIKGoal foot)
    {
        Vector3 footPos = anim.GetIKPosition(foot);
        Ray ray = new Ray(footPos + Vector3.up * 0.5f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 1f, groundMask))
        {
            Vector3 targetPos = hit.point;
            Quaternion targetRot = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;

            anim.SetIKPosition(foot, targetPos);
            anim.SetIKRotation(foot, targetRot);
            anim.SetIKPositionWeight(foot, 1f);
            anim.SetIKRotationWeight(foot, 1f);
        }
    }

    // Para ver el ground check en el editor
    private void OnDrawGizmosSelected()
    {
        if (groundCheckTransform != null)
        {
            Gizmos.color = IsGrounded() ? Color.green : Color.red;
            Gizmos.DrawSphere(groundCheckTransform.position, groundCheckRadius);
        }
    }
}