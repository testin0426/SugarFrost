using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SimpleMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotateSpeed = 120f;

    private Animator _anim;

    void Start()
    {
        _anim = GetComponent<Animator>();
    }

    void Update()
    {
        float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down
        float horizontal = Input.GetAxis("Horizontal");  // A/D or Left/Right

        bool walking = Mathf.Abs(vertical) > 0.1f;
        _anim.SetBool("isWalking", walking);


        transform.Rotate(Vector3.up * horizontal * rotateSpeed * Time.deltaTime);
    }
       /* void OnAnimatorMove()
    {
        AnimatorStateInfo state = _anim.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Start") || state.IsName("Stop"))
        {
            // Root motion — let the animation drive position
            transform.position += _anim.deltaPosition;
            transform.rotation *= _anim.deltaRotation;
        }
        else if (state.IsName("WalkLoop"))
        {
            // Script-driven — you control speed
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
    }*/
}