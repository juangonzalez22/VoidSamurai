using UnityEngine;

public class FightCamera : MonoBehaviour
{
    public Transform player1;
    public Transform player2;

    public Camera cam;

    public float smoothTime = 0.2f;

    public float minZoom = 5f;
    public float maxZoom = 10f;
    public float maxPlayerDistance = 20f;

    public float minX;
    public float maxX;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (!player1 || !player2)
            return;

        Vector3 center =
            (player1.position + player2.position) * 0.5f;

        Vector3 targetPos =
            new Vector3(
                center.x,
                transform.position.y,
                transform.position.z
            );

        targetPos.x = Mathf.Clamp(
            targetPos.x,
            minX,
            maxX
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );

        float distance = Vector3.Distance(
            player1.position,
            player2.position
        );

        float zoom =
            Mathf.Lerp(
                minZoom,
                maxZoom,
                distance / maxPlayerDistance
            );

        cam.orthographicSize = zoom;
    }
}