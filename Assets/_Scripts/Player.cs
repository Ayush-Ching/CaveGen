using UnityEngine;

public class Player : MonoBehaviour {

    [SerializeField] private float _speed = 10f;

    Rigidbody rb;
    Vector3 velocity;

    private void Start() {
        rb = GetComponent<Rigidbody>();
    }

    private void Update() {
        velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * _speed;
    }

    private void FixedUpdate() {
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }


}
