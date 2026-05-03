using UnityEngine;

namespace ARtiGraf.AR
{
    public class DemoSpinObject : MonoBehaviour
    {
        [SerializeField] Vector3 eulerDegreesPerSecond = new Vector3(0f, 42f, 0f);

        void Update()
        {
            transform.Rotate(eulerDegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
