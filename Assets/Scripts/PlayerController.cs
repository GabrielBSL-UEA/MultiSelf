﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform spawnPlayer;

    public float speed;
    public float jumpForce;

    private Animator anim;
    private Rigidbody2D rig;
    private GameObject playerAbove;
    private bool jumping;
    private bool withPlayer;
    private bool wallColliding;
    private bool isFacingRight;
    private bool isDead;
    private int currentMovement;

    public Transform frontUp;
    public Transform frontDown;
    public LayerMask layer;
    public GameObject deathEffect;

    List <RecordValues> movementRecord;

    [HideInInspector]
    public bool isGhost;

    // Start is called before the first frame update
    void Awake()
    {
        movementRecord = new List<RecordValues>();
        anim = GetComponent<Animator>();
        rig = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        jumping = false;
        isFacingRight = true;
        withPlayer = false;
        isDead = false;
        currentMovement = 0;

        if (isGhost)
        {
            gameObject.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead)
            return;

        if (!isGhost)
        {
            RecordMovement();
            Movement(Input.GetAxis("Horizontal"));
            Jump(Input.GetButtonDown("Jump"));
        }
        else
        {
            if(movementRecord.Count > 0 && currentMovement < movementRecord.Count)
            {
                if (movementRecord[currentMovement].isDead)
                    StartDeath();

                else
                {
                    Movement(movementRecord[currentMovement].horizontalAxis);
                    Jump(movementRecord[currentMovement].jumpButtonDown);
                    currentMovement++;
                }
            }
            else
            {
                StartDeath();
            }
        }
    }

    private void FixedUpdate()
    {
        CheckJump();
    }

    private void DoJumpFromBelow()
    {
        rig.velocity = new Vector2(0f, jumpForce);

        if (withPlayer)
        {
            playerAbove.GetComponent<PlayerController>().DoJumpFromBelow();
        }
    }

    private void DoMoveFromBelow(float _horizontal)
    {
        Vector3 movement = new Vector3(_horizontal, 0f, 0f);
        transform.position += movement * Time.fixedDeltaTime * speed;

        if (withPlayer)
        {
            playerAbove.GetComponent<PlayerController>().DoMoveFromBelow(_horizontal);
        }
    }

    private void RecordMovement()
    {
        movementRecord.Add(new RecordValues(Input.GetAxis("Horizontal"), Input.GetButtonDown("Jump"), false));
    }
    
    private void Movement(float horizontal)
    {
        wallColliding = Physics2D.Linecast(frontUp.position, frontDown.position, layer);

        if (!wallColliding ||
            (horizontal > 0 && isFacingRight == false) ||
            (horizontal < 0 && isFacingRight == true))
        {
            Vector3 movement = new Vector3(horizontal, 0f, 0f);
            transform.position += movement * Time.fixedDeltaTime * speed;

            if (withPlayer)
            {
                playerAbove.GetComponent<PlayerController>().DoMoveFromBelow(horizontal);
            }
        }

        if (horizontal > 0)
        {
            isFacingRight = true;
            anim.SetBool("walking", true);
            transform.eulerAngles = new Vector3(0f, 0f, 0f);
        }
        else if (horizontal < 0)
        {
            isFacingRight = false;
            anim.SetBool("walking", true);
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }
        else
        {
            anim.SetBool("walking", false);
        }
    }

    private void Jump(bool jump)
    {
        if (jump && !jumping)
        {
            jumping = true;
            rig.velocity = new Vector2(0f, jumpForce);

            if (withPlayer)
            {
                playerAbove.GetComponent<PlayerController>().DoJumpFromBelow();
            }
        }
    }

    private void CheckJump()
    {
        if (rig.velocity.y < -0.1)
        {
            anim.SetBool("falling", true);
            anim.SetBool("jumping", false);
            rig.velocity = Vector3.ClampMagnitude(rig.velocity, jumpForce * 2f);
        }

        else if (rig.velocity.y > 0.1)
        {
            anim.SetBool("falling", false);
            anim.SetBool("jumping", true);
            rig.velocity = Vector3.ClampMagnitude(rig.velocity, jumpForce);
        }

        else
        {
            anim.SetBool("jumping", false);
            anim.SetBool("falling", false);
        }
    }

    public void IsGrounded()
    {
        jumping = false;
    }

    public void GetRecordedMovements(List<RecordValues> recordedMovements)
    {
        movementRecord = recordedMovements;
    }

    public void CreateNewGhost(Transform spawnPosition)
    {
        var newGhost = Instantiate(gameObject, spawnPlayer.position, Quaternion.identity);
        newGhost.transform.parent = FindObjectOfType<GhostsManager>().transform;
        newGhost.GetComponent<PlayerController>().isGhost = true;
        newGhost.GetComponent<PlayerController>().GetRecordedMovements(movementRecord);

        transform.position = spawnPosition.position;
        movementRecord = new List<RecordValues>();

        if(isDead)
            StartRespawn();
    }

    public void ResetPlayerObject(Transform spawnPosition)
    {
        StartRespawn();
        currentMovement = 0;
        transform.position = spawnPosition.position;
    }

    public void SetWithPlayer(bool _withPlayer, GameObject otherPlayer)
    { 
        withPlayer = _withPlayer;
        playerAbove = otherPlayer;
    }

    public void SetDeath()
    {
        FindObjectOfType<GameManager>().StartRestart();
    }

    public void StartDeath()
    {
        if(!isGhost)
            movementRecord.Add(new RecordValues(0, false, true));

        isDead = true;
        deathEffect.SetActive(true);
        transform.Find("GroundCheck").GetComponent<BoxCollider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<BoxCollider2D>().enabled = false;
        GetComponent<CircleCollider2D>().enabled = false;
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
    }

    private void StartRespawn()
    {
        isDead = false;
        deathEffect.SetActive(false);
        transform.Find("GroundCheck").GetComponent<BoxCollider2D>().enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<BoxCollider2D>().enabled = true;
        GetComponent<CircleCollider2D>().enabled = true;
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
    }

    public void SelfDestroy()
    {
        Destroy(gameObject);
    }
}
