using UnityEngine;

public class BackToIdle : MonoBehaviour
{
    public Animator animator;
    public AnimationClip idleAnimation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BackToIdleState()
    {
        animator.SetBool("Crouch2", false);
    }
}
