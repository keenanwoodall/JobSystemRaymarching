using UnityEngine;

public class LightRotation : MonoBehaviour
{
	public float radius = 3f;
	public float speed = 1f;
	private Vector3 startPosition;

	private void Start()
	{
		startPosition = transform.position;
	}

	private void Update()
	{
		transform.position = startPosition + new Vector3(Mathf.Cos(Time.time * speed), 0f, Mathf.Sin(Time.time * speed)) * radius;
	}
}
