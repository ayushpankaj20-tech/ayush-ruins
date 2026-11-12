using UnityEngine;

/// <summary>Kills the player when they fall below a set height under the level.</summary>
public class KillZone : MonoBehaviour
{
    public Transform player;
    public GameManager gameManager;
    [Tooltip("World height below which the player dies.")]
    public float killHeight = -15f;

    void Update()
    {
        if (player != null && player.position.y < killHeight) gameManager.PlayerDied();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawCube(new Vector3(transform.position.x, killHeight, transform.position.z), new Vector3(300f, 0.05f, 300f));
    }
}
