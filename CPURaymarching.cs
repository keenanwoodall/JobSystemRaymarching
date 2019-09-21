using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using static Distance;

public class CPURaymarching : MonoBehaviour
{
	[SerializeField] private MeshRenderer meshRenderer;

	[Space]

	[SerializeField] private int resolution = 480;
	[Range(0, 100)]
	[SerializeField] private int maxSteps = 100;
	[Range(0f, 100f)]
	[SerializeField] private float maxDistance = 100f;
	[Range(0.005f, 1f)]
	[SerializeField] private float surfaceDistance = 0.01f;

	[Space]

	[Range(0.1f, 10f)]
	[SerializeField] private float fogExponent = 2f;
	[SerializeField] private Color fogColor = Color.black;

	[Space]

	[SerializeField] private Transform cameraTransform;
	[SerializeField] private Transform lightTransform;
	[SerializeField] private Transform sphereTransform;
	[SerializeField] private Transform planeTransform;

	private Texture2D texture;

	private NativeArray<Color32> pixels;

	private void OnEnable()
	{
		texture = new Texture2D
		(
			width: resolution, 
			height: resolution, 
			format: UnityEngine.Experimental.Rendering.DefaultFormat.LDR, 
			flags: UnityEngine.Experimental.Rendering.TextureCreationFlags.None
		);
		texture.filterMode = FilterMode.Trilinear;

		meshRenderer.material.mainTexture = texture;

		pixels = texture.GetRawTextureData<Color32>();
	}

	private void Update()
	{
		var handle = new RenderJob
		{
			pixels = pixels,
			width = texture.width,
			height = texture.height,
			maxSteps = maxSteps,
			maxDistance = maxDistance,
			surfaceDistance = surfaceDistance,
			fogExponent = fogExponent,
			fogColor = float3(fogColor.r, fogColor.g, fogColor.g),
			worldToCamera = cameraTransform.worldToLocalMatrix,
			worldToLight = lightTransform.worldToLocalMatrix,
			worldToSphere = sphereTransform.worldToLocalMatrix,
			worldToPlane = planeTransform.worldToLocalMatrix
		}.Schedule(pixels.Length, resolution / 12);

		handle.Complete();
		texture.Apply(false);
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

		public float fogExponent;
		public float3 fogColor;

		public float4x4 worldToCamera, worldToLight, worldToSphere, worldToPlane;

		public void Execute(int index)
		{
			// 0 -> resolution
			var xy = int2(index % width, index / width);
			// 0 -> 1
			var uv = xy / float2(width, height);
			// -1 -> 1
			var suv = (uv - 0.5f) * 2f;

			var cameraToWorld = inverse(worldToCamera);
			var cameraPosition = transform(cameraToWorld, float3(0));
			var rayDirection = rotate(cameraToWorld, normalize(float3(suv.x, suv.y, 1f)));

			var steps = 0;

			var distance = Raymarch(cameraPosition, rayDirection, out steps);
			var hitPoint = cameraPosition + rayDirection * distance;
			var lighting = GetLight(hitPoint);

			var color = float3(lighting);

			color = lerp(color, fogColor, pow(clamp(distance, 0f, maxDistance) / maxDistance, fogExponent));

			color = saturate(color);
			pixels[index] = new Color32((byte)(color.x * 255), (byte)(color.y * 255), (byte)(color.z * 255), 255);
		}

		private float Raymarch(float3 origin, float3 direction, out int steps)
		{
			float currentDistance = 0f;

			int i = 0;
			for (i = 0; i < maxSteps; i++)
			{
				var newPoint = origin + direction * currentDistance;
				var newDistance = GetDistance(newPoint);
				currentDistance += newDistance;

				if (currentDistance > maxDistance || newDistance < surfaceDistance)
					break;
			}

			steps = i;

			return currentDistance;
		}

		private float GetDistance(float3 point)
		{
			var sphereDistance = sphere(transform(worldToSphere, point), 1f);
			var planeDistance = plane(transform(worldToPlane, point), up());

			return min(sphereDistance, planeDistance);
		}

		private float GetLight(float3 point)
		{
			var lightToWorld = inverse(worldToLight);
			var lightPosition = transform(lightToWorld, float3(0));
			var lightDirection = normalize(lightPosition - point);
			var normal = GetNormal(point);
			var lighting = saturate(dot(normal, lightDirection));

			var steps = 0;
			var d = Raymarch(point + normal * surfaceDistance * 2f, lightDirection, out steps);
			if (d < length(lightPosition - point))
				lighting *= 0.1f;
			lighting *= 1f - (steps / maxSteps);
			return lighting;
		}

		private float3 GetNormal(float3 point)
		{
			var distance = GetDistance(point);
			var e = float2(0.01f, 0f);
			var n = distance - float3(GetDistance(point - e.xyy), GetDistance(point - e.yxy), GetDistance(point - e.yyx));

			return normalize(n);
		}
	}
}