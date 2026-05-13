using UnityEngine;

public class Player2D : MonoBehaviour {

    [SerializeField] private float _speed = 10f;

    Rigidbody2D rb;
    Vector2 velocity;

    private void Start() {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        velocity = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized * _speed;
    }

    private void FixedUpdate() {
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }


}
