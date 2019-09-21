using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using static Unity.Mathematics.math;

[RequireComponent(typeof(MeshRenderer))]
public class CPURaymarching : MonoBehaviour
{
	[SerializeField] private int resolution = 480;

	private Texture2D texture;

	private NativeArray<Color32> pixels;

	private void OnEnable()
	{
		texture = new Texture2D(resolution, resolution);
		texture.filterMode = FilterMode.Point;

		GetComponent<MeshRenderer>().material.mainTexture = texture;

		pixels = texture.GetRawTextureData<Color32>();
	}

	private void Update()
	{
		var handle = new ProcessPixels { pixels = pixels, width = resolution, height = resolution }.Schedule(pixels.Length, 64);
		handle.Complete();
		texture.Apply();
	}

	[BurstCompile(CompileSynchronously = true)]
	private struct ProcessPixels : IJobParallelFor
	{
		[WriteOnly]
		public NativeArray<Color32> pixels;
		public int width, height;

		public void Execute(int index)
		{
			var xy = int2(index % width, index / width);
			var uv = xy / float2(width, height);

			var color = float3(0);



			pixels[index] = new Color32((byte)(color.x * 255), (byte)(color.y * 255), (byte)(color.z * 255), 1);
		}
	}
}