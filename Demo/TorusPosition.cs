using UnityEngine;

public class TorusPosition : MonoBehaviour
{
	public float magnitude = 0.5f;
	public float speed = 1f;

	private Vector3 startPosition;

	private void Start()
	{
		startPosition = transform.position;
	}

	private void Update()
	{
		transform.position = startPosition + Vector3.up * Mathf.Sin(Time.time * speed) * magnitude;
	}
}
