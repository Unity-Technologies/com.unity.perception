using UnityEngine;

public class ConveyorForward : MonoBehaviour
{
    public float speed;
    Rigidbody m_RBody;

    void Start()
    {
        m_RBody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        var pos = m_RBody.position;
        m_RBody.position = pos - transform.forward * (speed * Time.fixedDeltaTime);
        m_RBody.MovePosition(pos);
    }
}
