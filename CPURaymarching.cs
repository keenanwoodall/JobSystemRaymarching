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
	[Range(1, 100)]
	[SerializeField] private int maxSteps = 100;
	[Range(1f, 100f)]
	[SerializeField] private float maxDistance = 100f;
	[Range(0.005f, 1f)]
	[SerializeField] private float surfaceDistance = 0.01f;

	[Space]

	[SerializeField] private Color surfaceColor = Color.green;
	[SerializeField] private Color fogColor = Color.black;
	[Range(0.1f, 10f)]
	[SerializeField] private float fogExponent = 2f;

	[Space]

	[SerializeField] private Transform cameraTransform;
	[SerializeField] private Transform lightTransform;
	[SerializeField] private Transform shapeTransform;
	[SerializeField] private Transform torusTransform;
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
		texture.filterMode = FilterMode.Point;

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
			time = Time.time * 2f,
			surfaceColor = float3(surfaceColor.r, surfaceColor.g, surfaceColor.b),
			fogColor = float3(fogColor.r, fogColor.g, fogColor.g),
			fogExponent = fogExponent,
			worldToCamera = cameraTransform.worldToLocalMatrix,
			worldToLight = lightTransform.worldToLocalMatrix,
			worldToShape = shapeTransform.worldToLocalMatrix,
			worldToTorus = torusTransform.worldToLocalMatrix,
			worldToPlane = planeTransform.worldToLocalMatrix
		}.Schedule(pixels.Length, resolution / 12);

		handle.Complete();
		texture.Apply(false);
	}

	[BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
	private struct RenderJob : IJobParallelFor
	{
		[WriteOnly]
		public NativeArray<Color32> pixels;
		public int width, height;
		public int maxSteps;
		public float maxDistance;
		public float surfaceDistance;

		public float time;
		public float3 surfaceColor;
		public float3 fogColor;
		public float fogExponent;

		public float4x4 worldToCamera, worldToLight, worldToShape, worldToTorus, worldToPlane;

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

			var distance = Raymarch(cameraPosition, rayDirection);
			var point = cameraPosition + rayDirection * distance;
			
			var normal = GetNormal(point);
			var lighting = lerp(0f, 1f, GetLight(point, normal));

			var color = surfaceColor;
			color *= lighting;
			color = lerp(color, fogColor, pow(clamp(distance, 0f, maxDistance) / maxDistance, fogExponent));

			color = saturate(color);
			pixels[index] = new Color32((byte)(color.x * 255), (byte)(color.y * 255), (byte)(color.z * 255), 255);
		}

		private float Raymarch(float3 origin, float3 direction)
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

			return currentDistance;
		}

		// Custom shit here. This is where you define the scene.
		private float GetDistance(float3 point)
		{
			var shapeDistance = roundBox(transform(worldToShape, point), float3(0.5f), 0.3f);

			var torusDistance = torus(transform(worldToTorus, point), float2(1f, 0.25f));

			var planeDistance = plane(transform(worldToPlane, point), up());

			return smoothUnion(smoothUnion(torusDistance, planeDistance, 0.5f), shapeDistance, 0.5f);
		}

		private float GetLight(float3 point, float3 normal)
		{
			var lightToWorld = inverse(worldToLight);
			var lightPosition = transform(lightToWorld, float3(0));
			var lightDirection = normalize(lightPosition - point);
			var lighting = saturate(dot(normal, lightDirection));

			var d = Raymarch(point + normal * surfaceDistance * 2f, lightDirection);

			if (d < length(point - lightPosition))
				lighting = 0f;

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