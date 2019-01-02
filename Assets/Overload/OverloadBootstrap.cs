using UnityEngine;

namespace WNP78.Overload
{
    class OverloadBootstrap : MonoBehaviour
    {
        private void Start()
        {
            OverloadMain.Init();
            Destroy(gameObject);
        }
    }
}
