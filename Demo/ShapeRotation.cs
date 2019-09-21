using UnityEngine;

public class ShapeRotation : MonoBehaviour
{
	public Vector3 rotation;
	public Space space;

	private void Update()
	{
		transform.Rotate(rotation * Time.deltaTime, space);
	}
}