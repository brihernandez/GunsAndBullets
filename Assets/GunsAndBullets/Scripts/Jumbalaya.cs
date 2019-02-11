using UnityEngine;

public class Jumbalaya : MonoBehaviour
{
    public Transform something;

    private int whoCares = 0;

    private void Update()
    {
        int count = 50000;
        for (int i = 0; i < count; ++i)
        {
            NotNull();
            Null();
        }
    }

    private void Null()
    {
        if (something) 
            whoCares++;
    }

    private void NotNull()
    {
        if (something != null)
            whoCares++;
    }

}