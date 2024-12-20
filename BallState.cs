using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BallState : MonoBehaviour
{
    // Topun düşüp düşmediğini belirten bir boolean değişken. Varsayılan olarak false.
    public bool dropped = false;
    // Bu fonksiyon, top bir nesneye çarptığında çağrılır.
    void OnCollisionEnter(Collision col)
    {
        // Çarpılan nesnenin etiketi "drop" ise, dropped değişkeni true olarak ayarlanır.
        if (col.gameObject.tag == "drop")
        {
            dropped = true;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}