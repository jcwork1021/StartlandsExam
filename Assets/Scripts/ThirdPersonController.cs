using UnityEngine;
using Photon.Pun;
using TMPro;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Reference from Its Child")]
    public GameObject playerCanvas;
    public TMP_Text nameTxt;
    public GameObject chatBubblePanel;
    public TMP_Text chatBubbleTxt;

    [Header("Debug for Movements")]
    public float velocity = 5f;
    public float sprintAdittion = 3.5f;
    public float jumpForce = 18f;
    public float jumpTime = 0.85f;
    public float gravity = 9.8f;

    float jumpElapsedTime = 0;

    bool isJumping = false;
    bool isSprinting = false;
    bool isCrouching = false;

    float inputHorizontal;
    float inputVertical;
    bool inputJump;
    bool inputCrouch;
    bool inputSprint;

    [Header("Reference from Itself")]
    public Animator animator;
    public CharacterController cc;
    public PhotonView pv;

    [Header("Reference from Its Child")]
    public Transform target;

    [Header("Debugger for chat")]
    [SerializeField] float chatCloseTime;
    [SerializeField] bool startCloseChatCountdown;
    [SerializeField] string chatContentMessage;


    void Start()
    {

        //Only enable controls if this is OUR player
        if (!pv.IsMine)
        {
            this.enabled = false;
            return;
        }

        Cursor.visible = false;

        if (pv.IsMine)
        {
            DisplayName(); //only local player triggers the RPC
        }
    }


    void Update()
    {
        if (startCloseChatCountdown)
        {
            chatCloseTime -= 1 * Time.deltaTime;

            if(chatCloseTime <= 0)
            {
                DisableChatBubble();
                startCloseChatCountdown = false;
            }
        }

        if (MPManager.Instance.isPause || MPManager.Instance.isChatting) 
        {

            return; //if the character is chatting or on pause lets not move
        }

        inputHorizontal = Input.GetAxis("Horizontal");
        inputVertical = Input.GetAxis("Vertical");
        inputJump = Input.GetAxis("Jump") == 1f;
        inputSprint = Input.GetAxis("Fire3") == 1f;
        inputCrouch = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.JoystickButton1);

        if (inputCrouch)
            isCrouching = !isCrouching;

        if (cc.isGrounded && animator != null)
        {
            animator.SetBool("crouch", isCrouching);

            float minimumSpeed = 0.9f;
            animator.SetBool("run", cc.velocity.magnitude > minimumSpeed);

            isSprinting = cc.velocity.magnitude > minimumSpeed && inputSprint;
            animator.SetBool("sprint", isSprinting);
        }

        if (animator != null)
            animator.SetBool("air", cc.isGrounded == false);

        if (inputJump && cc.isGrounded)
            isJumping = true;

        HeadHittingDetect();
    }


    private void FixedUpdate()
    {
        float velocityAdittion = 0;
        if (isSprinting)
            velocityAdittion = sprintAdittion;
        if (isCrouching)
            velocityAdittion = -(velocity * 0.50f);

        float speed = velocity + velocityAdittion;
        float directionX = inputHorizontal * speed * Time.deltaTime;
        float directionZ = inputVertical * speed * Time.deltaTime;
        float directionY = 0;

        if (isJumping)
        {
            directionY = Mathf.SmoothStep(jumpForce, jumpForce * 0.30f, jumpElapsedTime / jumpTime) * Time.deltaTime;

            jumpElapsedTime += Time.deltaTime;
            if (jumpElapsedTime >= jumpTime)
            {
                isJumping = false;
                jumpElapsedTime = 0;
            }
        }

        directionY = directionY - gravity * Time.deltaTime;


        // Character rotation
        // Flatten the camera's forward and right vectors onto the XZ plane
        // so vertical camera angle doesn't affect movement direction
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        // Build the move direction fully relative to where the camera is looking
        Vector3 horizontalDirection = (camForward * directionZ) + (camRight * directionX);

        // Rotate the character to face the movement direction
        if (directionX != 0 || directionZ != 0)
        {
            float angle = Mathf.Atan2(horizontalDirection.x, horizontalDirection.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, angle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.15f);
        }

        //End rotation


        Vector3 verticalDirection = Vector3.up * directionY;
        Vector3 movement = verticalDirection + horizontalDirection;
        cc.Move(movement);
    }

    void HeadHittingDetect()
    {
        float headHitDistance = 1.1f;
        Vector3 ccCenter = transform.TransformPoint(cc.center);
        float hitCalc = cc.height / 2f * headHitDistance;

        if (Physics.Raycast(ccCenter, Vector3.up, hitCalc))
        {
            jumpElapsedTime = 0;
            isJumping = false;
        }
    }

    public void DisplayName()
    {
        pv.RPC("RPCDisplayName", RpcTarget.AllBuffered, RememberMe.Instance.playerName);
    }


    [PunRPC]
    public void RPCDisplayName(string name)
    {
        nameTxt.text = name;
        nameTxt.gameObject.SetActive(true);

    }

    public void DisplayChat(string chatMessage)
    {
        pv.RPC("RPCDisplayChat", RpcTarget.AllBuffered, chatMessage);
        chatContentMessage = chatMessage;

        chatCloseTime = 5;
        startCloseChatCountdown = true;
    }

    [PunRPC]
    public void RPCDisplayChat(string message)
    {
        chatBubblePanel.gameObject.SetActive(true);
        chatBubbleTxt.text = message;
    }


    public void DisableChatBubble()
    {
        pv.RPC("RPCDisableChatBubble", RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void RPCDisableChatBubble()
    {
        chatBubblePanel.gameObject.SetActive(false);
    }

    public void OnTriggerEnter(Collider other)
    {
        
    }


}