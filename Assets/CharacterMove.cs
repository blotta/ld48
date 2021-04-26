using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMove : MonoBehaviour
{
    [SerializeField] float _moveSpeed = 10f;
    [SerializeField] float _jumpSpeed = 0.5f;
    [SerializeField] float _gravity = 2f;

    CharacterController _characterController;
    Vector3 _moveDirection;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    void FixedUpdate()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical);
        Vector3 transformDirection = transform.TransformDirection(inputDirection); // get vec3 "translated" from local to global

        Vector3 flatMovement = _moveSpeed * Time.deltaTime * transformDirection;

        _moveDirection = new Vector3(flatMovement.x, _moveDirection.y, flatMovement.z);

        if (PlayerJumped)
            _moveDirection.y = _jumpSpeed;
        else if (_characterController.isGrounded)
            _moveDirection.y = 0f;
        else
            _moveDirection.y -= _gravity * Time.deltaTime;

        _characterController.Move(_moveDirection);
    }

    bool PlayerJumped => _characterController.isGrounded && Input.GetKey(KeyCode.Space);
}
