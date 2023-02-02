using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample {
public class TurtleControlScript : MonoBehaviour
{
    private Animator _Animator;
    private CharacterController _Ctrl;
    private Vector3 _MoveDirection = Vector3.zero;
    private float _Speed;
    private bool _Shell = false;
    private GameObject _View_Camera;
    private Dictionary<string, bool> _Status = new Dictionary<string, bool>
    {
        {"Jump", false },
        {"Damage", false },
        {"Stop", false },
        {"Attack", false },
        {"Eat", false },
        {"LookAround", false },
        {"Shell", false },
    };

    void Start()
    {
        _Animator = this.GetComponent<Animator>();
        _Ctrl = this.GetComponent<CharacterController>();
        _View_Camera = GameObject.Find("Main Camera");
    }

    void Update()
    {
        CAMERA();
        GRAVITY();
        STATUS();

        if(!_Status.ContainsValue( true ))
        {
            MOVE();
            JUMP();
            DAMAGE();
            ATTACK();
            STOP();
            EAT();
            LOOKAROUND();
            SHELL();
        }
        else if(_Status.ContainsValue( true ))
        {
            string status_name = "";
            foreach(var i in _Status)
            {
                if(i.Value == true)
                {
                    status_name = i.Key;
                    break;
                }
            }
            if(status_name == "Jump")
            {
                MOVE();
                JUMP();
            }
            else if(status_name == "Damage")
            {
                DAMAGE();
            }
            else if(status_name == "Stop")
            {
                STOP();
            }
            else if(status_name == "Attack")
            {
                ATTACK();
            }
            else if(status_name == "Eat")
            {
                EAT();
            }
            else if(status_name == "LookAround")
            {
                LOOKAROUND();
            }
            else if(status_name == "Shell")
            {
                SHELL();
            }
        }
    }
    //--------------------------------------------------------------------- STATUS
    private void STATUS ()
    {
        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Jump"))
        {
            _Status["Jump"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Jump"))
        {
            _Status["Jump"] = false;
        }

        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Damage"))
        {
            _Status["Damage"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Damage"))
        {
            _Status["Damage"] = false;
        }

        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Stop"))
        {
            _Status["Stop"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Stop"))
        {
            _Status["Stop"] = false;
        }

        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            _Status["Attack"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            _Status["Attack"] = false;
        }
        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Eat"))
        {
            _Status["Eat"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Eat"))
        {
            _Status["Eat"] = false;
        }
        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Look"))
        {
            _Status["LookAround"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Look"))
        {
            _Status["LookAround"] = false;
        }
        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Shell"))
        {
            _Status["Shell"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Shell"))
        {
            _Status["Shell"] = false;
        }
    }
    //--------------------------------------------------------------------- CAMERA
    private void CAMERA ()
	{
		_View_Camera.transform.position = this.transform.position + new Vector3(0, 0.5f, 3);
	}
    //--------------------------------------------------------------------- GRAVITY
	private void GRAVITY ()
	{
		if (_Ctrl.isGrounded)
		{
			if(_MoveDirection.y < -0.5f)
            {
                _MoveDirection.y = -0.5f;
            }
		}
		else if (!_Ctrl.isGrounded)
		{
            _MoveDirection.y -= 5 * Time.deltaTime;
		}
		_Ctrl.Move(_MoveDirection * Time.deltaTime);
	}
    //--------------------------------------------------------------------- MOVE
	private void MOVE ()
    {
        float speed = 1;
		//------------------------------------------------------------ Speed
        if(Input.GetKey(KeyCode.Z))
        {
    	    speed = 1;
            _Animator.SetFloat("Speed", 1);
        }
	    else {
		    speed = 0.5f;
            _Animator.SetFloat("Speed", 0);
        }
        //------------------------------------------------------------ Foreward
        if (Input.GetKey(KeyCode.UpArrow))
        {
            // velocity
            if(_Animator.GetCurrentAnimatorStateInfo(0).IsName("move") || !_Ctrl.isGrounded)
            {
                Vector3 velocity = this.transform.rotation * new Vector3(0, 0, speed);
                MOVE_XZ(velocity);
                MOVE_RESET();
            }
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (_Ctrl.isGrounded)
		    {
                if(!_Animator.GetCurrentAnimatorStateInfo(0).IsName("jump"))
                {
                    _Animator.CrossFade("move", 0.1f, 0, 0);
                }
            }
        }
        
        //------------------------------------------------------------ character rotation
        if (Input.GetKey(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftArrow))
        {
            this.transform.Rotate(Vector3.up, 0.5f);
        }
        else if (Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
        {
            this.transform.Rotate(Vector3.up, -0.5f);
        }
        if (!Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
        {
            if(_Ctrl.isGrounded)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow) && !Input.GetKey(KeyCode.LeftArrow))
                {
            	    _Animator.CrossFade("move", 0.1f, 0, 0);
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
                {
            	    _Animator.CrossFade("move", 0.1f, 0, 0);
                }
            }
            // rotate stop
            else if (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.LeftArrow))
            {
                if(!_Animator.GetCurrentAnimatorStateInfo(0).IsName("jump"))
                {
            	    _Animator.CrossFade("idle", 0.1f, 0, 0);
                }
            }
        }
        KEY_UP();
	}
    //--------------------------------------------------------------------- MOVE_SUB
	private void MOVE_XZ (Vector3 velocity)
	{
        _MoveDirection = new Vector3 (velocity.x, _MoveDirection.y, velocity.z);
        _Ctrl.Move(_MoveDirection * Time.deltaTime);
    }
    private void MOVE_RESET()
    {
        _MoveDirection.x = 0;
        _MoveDirection.z = 0;
    }
    //--------------------------------------------------------------------- KEY_UP
	private void KEY_UP ()
	{
	    if (_Ctrl.isGrounded)
		{
	        if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                if(!Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
                {
                    _Animator.CrossFade("idle", 0.1f, 0, 0);
                }
            }
            else if (!Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow))
            {
        	    if (Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.LeftArrow))
        	    {
                    if(Input.GetKey(KeyCode.LeftArrow))
                    {
                        _Animator.CrossFade("move", 0.1f, 0, 0);
                    }
                    else if(Input.GetKey(KeyCode.RightArrow))
                    {
                        _Animator.CrossFade("move", 0.1f, 0, 0);
                    }
                    else{
            		    _Animator.CrossFade("idle", 0.1f, 0, 0);
                    }
                }
            }
        }
	}
    //--------------------------------------------------------------------- JUMP
	private void JUMP ()
	{
        if(_Ctrl.isGrounded)
        {
		    if(!_Animator.IsInTransition(0))
		    {
				if (Input.GetKeyDown(KeyCode.S))
				{
                    _Animator.CrossFade("jump", 0.1f, 0, 0);
					_MoveDirection.y = 3.0f;
                    StartCoroutine(JumpPose());
				}
                if(_Animator.GetCurrentAnimatorStateInfo(0).IsName("jump"))
                {
                    if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
                    {
                        _Animator.CrossFade("move", 0.1f, 0, 0);
                    }
                    else{
                        _Animator.CrossFade("idle", 0.1f, 0, 0);
                    }
                }
			}
		}
	}
    //--------------------------------------------------------------------- JumpPose
    private IEnumerator JumpPose ()
    {
        float t = 0;
        while(true)
        {
            if(t >= 1)
            {
                yield break;
            }
            t += 1 * Time.deltaTime;
            _Animator.SetFloat("JumpPose", t);
            yield return null;
        }
    }
    //--------------------------------------------------------------------- DAMAGE
	private void DAMAGE ()
	{
		if (Input.GetKeyDown(KeyCode.Q))
		{
			_Animator.CrossFade("damage", 0.1f, 0, 0);
		}
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsTag("Damage")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("idle", 0.1f, 0, 0);
        }
	}
    //--------------------------------------------------------------------- ATTACK
    private void ATTACK ()
    {
        if (Input.GetKeyDown(KeyCode.A))
	    {
	    	_Animator.CrossFade("attack_charge", 0.1f, 0, 0);
		}
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("attack_charge")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("attack", 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("attack")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("attack_back", 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("attack_back")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("idle", 0.1f, 0, 0);
        }
    }
    //--------------------------------------------------------------------- EAT
    private void EAT ()
	{
	    if (Input.GetKeyDown(KeyCode.D))
		{
	    	_Animator.CrossFade("eat", 0.2f, 0, 0);
	    }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 2
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("eat")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("idle", 0.3f, 0, 0);
        }
    }
    //--------------------------------------------------------------------- LOOK AROUND
    private void LOOKAROUND ()
	{
	    if (Input.GetKeyDown(KeyCode.X))
		{
	    	_Animator.CrossFade("idle_lookaround", 0.1f, 0, 0);
	    }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("idle_lookaround")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("idle", 0.02f, 0, 0);
        }
    }
    //--------------------------------------------------------------------- SHELL
    private void SHELL ()
	{
	    if (Input.GetKeyDown(KeyCode.W) && !_Shell)
		{
	    	_Animator.CrossFade("shell_inside", 0.1f, 0, 0);
	    }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("shell_inside")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("shell", 0.1f, 0, 0);
            _Shell = true;
        }

        if (Input.GetKeyDown(KeyCode.W) && _Shell)
		{
	    	_Animator.CrossFade("shell_outside", 0.1f, 0, 0);
            _Shell = false;
	    }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("shell_outside")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("idle", 0.1f, 0, 0);
        }
    }
    //--------------------------------------------------------------------- STOP
    private void STOP ()
    {
        if (Input.GetKeyDown(KeyCode.E)
            && !_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Stop"))
	    {
            _Animator.CrossFade("down", 0.1f, 0, 0);
        }
        if (Input.GetKeyDown(KeyCode.E)
            && _Animator.GetCurrentAnimatorStateInfo(0).IsTag("Stop")
            && !_Animator.IsInTransition(0))
	    {
            _Animator.CrossFade("jump", 0.1f, 0, 0);
            _MoveDirection.y = 3.0f;
            StartCoroutine(JumpPose());
        }
    }
}
}