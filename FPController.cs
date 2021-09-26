using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPController : MonoBehaviour
{
    public GameObject cam;
    
    public Animator anim;

    // Holds onto audio file
    //public AudioSource shot;      //We do this in other soundController Script source, otherwise wont be synchronized
    public AudioSource[] footsteps;
    public AudioSource jump;
    public AudioSource land;
    public AudioSource ammo;
    public AudioSource medkit;
    public AudioSource triggerSound;
    public AudioSource death;
    public AudioSource sizzle;
    public AudioSource reloadSound;

    float speed = 0.1f;
    float Xsensitivity = 2;
    float Ysensitivity = 2;
    float minimumX = -90;
    float maximumX = 90;

    float x;
    float z;
    bool cursorIsLocked = true;
    bool lockCursor = true;

    // Inventory
    int numAmmo = 0;
    int maxAmmo = 50;
    int health = 0;
    int maxHealth = 100;
    int ammoClip = 0;
    int ammoClipMax = 10;


    Rigidbody rb;
    CapsuleCollider capsule;

    Quaternion cameraRot;
    Quaternion characterRot;

    // Start is called before the first frame update
    void Start()
    {
        health = maxHealth;
        rb = this.GetComponent<Rigidbody>();
        capsule = this.GetComponent<CapsuleCollider>();

        // This is us getting our camera information
        cameraRot = cam.transform.localRotation;

        characterRot = this.transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        // Arm animation to go from idle to aiming
        if (Input.GetKeyDown(KeyCode.F))
            anim.SetBool("arm", !anim.GetBool("arm"));

        //Fire animator to fire gun using a trigger 
        if (Input.GetMouseButtonDown(0) && !anim.GetBool("fire"))
        {
            if (ammoClip > 0)
            {
                anim.SetTrigger("fire");

                //Deplete Ammo
                ammoClip--;
           
                // shot.Play();  // Dont need because we have another script file.
            }
            // Play empty trigger sound if no ammo
            else if (anim.GetBool("arm"))
            {
                triggerSound.Play();
            }
            Debug.Log("ammo clip left is: " + ammoClip);
        }


        // Reload Animator using trigger while gun is upwards
        if (Input.GetKeyDown(KeyCode.R) && anim.GetBool("arm"))
        {
            anim.SetTrigger("reload");
            reloadSound.Play();
            int amountNeed = ammoClipMax - ammoClip;
            int ammoAvailable = amountNeed < numAmmo ? amountNeed : numAmmo;
            numAmmo -= ammoAvailable;
            ammoClip += ammoAvailable;
            Debug.Log("Ammo Left is: " + numAmmo);
           // Debug.Log("ammo in clip is : " + ammoClip);
        }
        // Walking with rifle Animation
        if (Mathf.Abs(x) > 0 || Mathf.Abs(z) > 0)
        {
            if (!anim.GetBool("walk"))
            {
                anim.SetBool("walk", true);
                //Debug.Log("About to Play Footstep");
                // Invokes the sound to play ("what sound funct", When to start, How long of intervals)
                InvokeRepeating("PlayFootStepAudio", 0, 0.4f);
            }
        }
        else if (anim.GetBool("walk"))
        {
            anim.SetBool("walk", false);
            CancelInvoke("PlayFootStepAudio");
        }

        // Jumping (with sounds)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded())
        {
            rb.AddForce(0, 300, 0);
            jump.Play();
            if (anim.GetBool("walk"))
                CancelInvoke("PlayFootStepAudio");

        }


    }

    void PlayFootStepAudio()
    {
        AudioSource audioSource = new AudioSource();
        int n = Random.Range(1, footsteps.Length);

        audioSource = footsteps[n];
        audioSource.Play();
        //Swaps them around at random location
        footsteps[n] = footsteps[0];
        footsteps[0] = audioSource;
    }
   void FixedUpdate()
    {
        float yRot = Input.GetAxis("Mouse X") * Ysensitivity;
        float xRot = Input.GetAxis("Mouse Y")* Xsensitivity;

        // Turns Quaternion into Euler ---> This rotates the character
        cameraRot *= Quaternion.Euler(-xRot, 0, 0);
        characterRot *= Quaternion.Euler(0, yRot, 0);

        // Puts the updated rotation ^^ onto our elements
        this.transform.localRotation = characterRot;
        cam.transform.localRotation = cameraRot;

        // Performing clamp t prevent player from looking too far one way
        cameraRot = clampRotationAroundXAxis(cameraRot);

        

         x = Input.GetAxis("Horizontal")* speed;
         z = Input.GetAxis("Vertical")* speed;


        //Update position of capsule
        //
        transform.position += cam.transform.forward * z + cam.transform.right * x; //new Vector3(x* speed, 0, z*speed);
        updateCursorLock();
        
    }

    // Limiting our look rotation with the mouse
    Quaternion clampRotationAroundXAxis(Quaternion q)
    {
        // Normalizing by scaling it down and dividing by its final component
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        // Converting x value in Quat to an euler value(rotation around x axis)
        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
        angleX = Mathf.Clamp(angleX, minimumX, maximumX);

        // Takes euler angle and turns it back into a quat value to put back in x position
        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        return q;
    }
    bool isGrounded() {
        RaycastHit hitInfo;
        if(Physics.SphereCast(transform.position, capsule.radius, Vector3.down, out hitInfo, 
            (capsule.height/2f) - capsule.radius + 0.1f))
        {
            return true;
        }
        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Trying to detect running into AMMO
        if(collision.gameObject.tag == "Ammo" && numAmmo < maxAmmo) // Cant pickup if max ammo is achieved
        {
            // Sound
            Debug.Log("Ran into Ammo box");
            ammo.Play();
            // Adding ammo
            numAmmo = Mathf.Clamp(numAmmo + 10, 0 , maxAmmo);
            Debug.Log("Amount of ammo is: " + numAmmo);

            /**********DONT DESTROY IF OBJECT STILL NEEDS TO DO SOMETHING!!!*******/
            Destroy(collision.gameObject);
        }
        //Trying to detect running into MEDKIT
        else if (collision.gameObject.tag == "Medkit" && health < maxHealth)
        {
            Debug.Log("Ran into med Kit");
            medkit.Play();

            // Adding health
            health = Mathf.Clamp(health + 10, 0, maxHealth);
            Debug.Log("Health is: " + health);
            /**********DONT DESTROY IF OBJECT STILL NEEDS TO DO SOMETHING!!!******/
            Destroy(collision.gameObject);
        }
        // Damage if collided with Lava
        else if(collision.gameObject.tag == "lava" && health > 0)
        {
            health = Mathf.Clamp(health - 10, 0, maxHealth);
            sizzle.Play();
            Debug.Log("Health level is now: " + health);
            if( health <= 0)
            {
                death.Play();
            }

        }
       
         
        // Make sound when we are grounded.
        if (isGrounded())
        {
            land.Play();
            if (anim.GetBool("walk"))
                InvokeRepeating("PlayFootStepAudio", 0 , 0.4f);
        }

        
    }

    // LOCKING CURSOR FUNCtiONS

    public void SetCursorLock(bool value)
    {
        lockCursor = value;
        if (!lockCursor)
        {
            //Locking position of cursor
            Cursor.lockState = CursorLockMode.None;
            // Whether the cursor is visible or not
            Cursor.visible = true;
        }
    }

    public void updateCursorLock()
    {
        if (lockCursor)
            InternalLockUpdate();
    }
    public void InternalLockUpdate()
    {

        if (Input.GetKeyUp(KeyCode.Escape))
            cursorIsLocked = false;
        else if (Input.GetMouseButtonUp(0))
            cursorIsLocked = true;

        if (cursorIsLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

        }
        else if (!cursorIsLocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
