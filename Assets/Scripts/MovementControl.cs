using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementControl : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        const float speed = 5f;
        Vector2 velocity = new Vector2(0.0f, 0.0f);
        if (IsOwner)
        {
            var keyboard = Keyboard.current;
            if (keyboard.wKey.isPressed)
            {
                velocity.y += 1.0f;
            }
            if (keyboard.sKey.isPressed)
            {
                velocity.y -= 1.0f;
            }
            if (keyboard.aKey.isPressed)
            {
                velocity.x -= 1.0f;
            }
            if (keyboard.dKey.isPressed)
            {
                velocity.x += 1.0f;
            }

            gameObject.transform.position = new Vector3(
                gameObject.transform.position.x + velocity.x * Time.deltaTime * speed,
                0,
                gameObject.transform.position.z + velocity.y * Time.deltaTime * speed
                );
        }
    }
}
