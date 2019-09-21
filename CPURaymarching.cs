using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class CPURaymarching : MonoBehaviour
{
	[SerializeField] private MeshRenderer meshRenderer;
	[Space]
	[SerializeField] private int resolution = 480;
	[SerializeField, Range(0, 100)] private int maxSteps = 100;
	[SerializeField, Range(0f, 100f)] private float maxDistance = 100f;
	[SerializeField, Range(0.005f, 1f)] private float surfaceDistance = 0.01f;
	[Space]
	[SerializeField] private Transform cameraPositioner;
	[SerializeField] private Transform spherePositioner;
	[SerializeField] private Transform planePositioner;

	private Texture2D texture;

	private NativeArray<Color32> pixels;

	private void OnEnable()
	{
		texture = new Texture2D(resolution, resolution);
		texture.filterMode = FilterMode.Point;

		meshRenderer.material.mainTexture = texture;

		pixels = texture.GetRawTextureData<Color32>();
	}

	private void Update()
	{
		var handle = new RenderJob
		{
			pixels = pixels,
			width = resolution,
			height = resolution,
			maxSteps = maxSteps,
			maxDistance = maxDistance,
			surfaceDistance = surfaceDistance,
			cameraPosition = cameraPositioner.position,
			cameraRotation = cameraPositioner.rotation,
			spherePosition = spherePositioner.position,
			planePosition = planePositioner.position
		}.Schedule(pixels.Length, resolution / 12);

		handle.Complete();
		texture.Apply();
	}

	[BurstCompile(CompileSynchronously = true)]
	private struct RenderJob : IJobParallelFor
	{
		[WriteOnly]
		public NativeArray<Color32> pixels;
		public int width, height;
		public int maxSteps;
		public float maxDistance;
		public float surfaceDistance;

		public float3 cameraPosition;
		public quaternion cameraRotation;
		public float3 spherePosition;
		public float3 planePosition;

		public void Execute(int index)
		{
			// 0 -> resolution
			var xy = int2(index % width, index / width);
			// 0 -> 1
			var uv = xy / float2(width, height);
			// -1 -> 1
			var suv = (uv - 0.5f) * 2f;

			var rayDirection = rotate(cameraRotation, normalize(float3(suv.x, suv.y, 1f)));

			var distance = Raymarch(cameraPosition, rayDirection);
			var color = float3(distance / maxDistance);

			color = saturate(color);
			pixels[index] = new Color32((byte)(color.x * 255), (byte)(color.y * 255), (byte)(color.z * 255), 255);
		}

		private float Raymarch(float3 origin, float3 direction)
		{
			float currentDistance = 0f;

			for (int i = 0; i < maxSteps; i++)
			{
				var newPoint = origin + direction * currentDistance;
				var newDistance = GetDistance(newPoint);
				currentDistance += newDistance;

				if (currentDistance > maxDistance || newDistance < surfaceDistance)
					break;
			}

			return currentDistance;
		}

		private float GetDistance(float3 point)
		{
			var sphereRadius = 1f;

			var sphereDistance = length(point - spherePosition) - sphereRadius;
			var planeDistance = point.y - planePosition.y;

			return min(sphereDistance, planeDistance);
		}
	}
}