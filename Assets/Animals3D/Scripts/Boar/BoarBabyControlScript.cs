using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Smaple {
public class BoarBabyControlScript : MonoBehaviour
{
    // chick
    private Animator _Animator;
    private CharacterController _Ctrl;
    // Boar
    private GameObject _Boar;
    private Animator _Boar_Animator;
    // move
    private Vector3 _MoveDirection = Vector3.zero;
    private bool _Move = false;

    private Dictionary<string, bool> _Status = new Dictionary<string, bool>
    {
        {"Jump", false },
        {"Damage", false },
        {"Stop", false },
        {"Sniff", false },
        {"Attack", false },
        {"Attack_Run", false },
        {"Meat", false },
    };

    void Start()
    {
        _Animator = this.GetComponent<Animator>();
        _Boar = GameObject.Find("boar_highpoly");
        _Boar_Animator = _Boar.GetComponent<Animator>();
        _Ctrl = this.GetComponent<CharacterController>();
        _Animator.SetFloat("MotionSpeed",2);
    }

    void Update()
    {
        GRAVITY();
        STATUS();

        if(!_Status.ContainsValue( true ))
        {
            MOVE();
            JUMP();
            DAMAGE();
            SNIFF();
            ATTACK();
            ATTACK_RUN();
            STOP();
            MEAT();
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
            else if(status_name == "Sniff")
            {
                SNIFF();
            }
            else if(status_name == "Attack")
            {
                ATTACK();
            }
            else if(status_name == "Attack_Run")
            {
                ATTACK_RUN();
            }
            else if(status_name == "Meat")
            {
                MEAT();
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

        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Sniff"))
        {
            _Status["Sniff"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Sniff"))
        {
            _Status["Sniff"] = false;
        }

        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            _Status["Attack"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            _Status["Attack"] = false;
        }

        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack_Run"))
        {
            _Status["Attack_Run"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack_Run"))
        {
            _Status["Attack_Run"] = false;
        }

        if(_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Meat"))
        {
            _Status["Meat"] = true;
        }
        else if(!_Animator.GetCurrentAnimatorStateInfo(0).IsTag("Meat"))
        {
            _Status["Meat"] = false;
        }
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
    //--------------------------------------------------------------------- Move
    private void MOVE ()
    {
        float distance = Vector3.Distance(this.transform.position, _Boar.transform.position);
        _Animator.SetFloat("Speed",distance * 0.5f);
        // Look
        var aim = _Boar.transform.position - this.transform.position;
        var look = Quaternion.LookRotation(aim, Vector3.up);
        this.transform.localRotation = look;

        if(distance >= 1.0f)
        {
            Vector3 offset = _Boar.transform.rotation * new Vector3(0, 0, -1.0f);
            float speed= 5.0f;
            this.transform.position = Vector3.Lerp(this.transform.position, _Boar.transform.position + offset, speed * Time.deltaTime);
        }

        if(distance > 1.01f)
        {
            if(!_Move){
                _Animator.CrossFade("move", 0.1f, 0, 0);
            }
            _Move = true;
        }
        else
        {
            if(_Move){
                _Animator.CrossFade("idle", 0.1f, 0, 0);
            }
            _Move = false;
        }
    }
    //--------------------------------------------------------------------- Jump
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
                    _Animator.CrossFade("idle", 0.1f, 0, 0);
                    _Move = false;
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
            _Move = false;
        }
	}
    //--------------------------------------------------------------------- SNIFF
    private void SNIFF ()
	{
	    if (Input.GetKeyDown(KeyCode.D))
		{
	    	_Animator.CrossFade("sniff_shift1", 0.2f, 0, 0);
	    }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("sniff_shift1")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("sniff", 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.5f
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("sniff")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("sniff_shift2", 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("sniff_shift2")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("idle", 0.1f, 0, 0);
            _Move = false;
        }
    }
    //--------------------------------------------------------------------- ATTACK
    private void ATTACK ()
    {
        if (Input.GetKeyDown(KeyCode.A))
	    {
	    	_Animator.CrossFade("attack_shift", 0.1f, 0, 0);
		}
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("attack_shift")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("attack_charge", 0.6f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("attack_charge")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("attack", 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("attack")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("idle", 0.1f, 0, 0);
            _Move = false;
        }
    }
    //--------------------------------------------------------------------- ATTACK_RUN
    private void ATTACK_RUN ()
    {
        if (Input.GetKeyDown(KeyCode.X))
	    {
		    _Animator.CrossFade("charge", 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 2
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("charge")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("attack_run", 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 2
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("attack_run")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("brake", 0.1f, 0, 0);
        }
        if (_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1
            && _Animator.GetCurrentAnimatorStateInfo(0).IsName("brake")
            && !_Animator.IsInTransition(0))
        {
            _Animator.CrossFade("idle", 0.1f, 0, 0);
            _Move = false;
        }
        Vector3 velocity;
        if(_Animator.GetCurrentAnimatorStateInfo(0).IsName("attack_run"))
        {
            velocity = this.transform.rotation * new Vector3(0, 0, 5);
            this.transform.position += velocity * Time.deltaTime;
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
    //--------------------------------------------------------------------- MEAT
    private void MEAT ()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            _Animator.CrossFade("meat", 0.0f, 0, 0);
            this.transform.Rotate(Vector3.forward, 180);
        }
        if (Input.GetKeyDown(KeyCode.R)
            && _Animator.GetCurrentAnimatorStateInfo(0).IsTag("Meat")
            && !_Animator.IsInTransition(0))
	    {
            _Animator.CrossFade("jump", 0.1f, 0, 0);
            _MoveDirection.y = 3.0f;
            StartCoroutine(JumpPose());
            this.transform.rotation = Quaternion.Euler(0,0,0);
        }
    }
}
}