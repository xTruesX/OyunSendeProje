using UnityEngine;

namespace Boruhan.Story
{
    /// <summary>A named arrival point. SceneDoors in other scenes refer to it by id.</summary>
    public class SpawnPoint : MonoBehaviour
    {
        public string id;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
