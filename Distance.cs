using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

// Credit to Inigo Quilez for these distance functions. All I did was make them work with Unity.Mathematics.
// https://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm

public static class Distance
{

	public static float smoothUnion(float d1, float d2, float k)
	{
		float h = saturate(0.5f + 0.5f * (d2 - d1) / k);
		return lerp(d2, d1, h) - k * h * (1f - h);
	}

	public static float smoothSubtraction(float d1, float d2, float k)
	{
		float h = saturate(0.5f - 0.5f * (d2 + d1) / k);
		return lerp(d2, -d1, h) + k * h * (1f - h);
	}

	public static float smoothIntersection(float d1, float d2, float k)
	{
		float h = saturate(0.5f - 0.5f * (d2 - d1) / k);
		return lerp(d2, d1, h) + k * h * (1f - h);
	}

	public static float sphere(float3 p, float s)
	{
		return length(p) - s;
	}

	public static float box(float3 p, float3 b)
	{
		float3 d = abs(p) - b;
		return length(max(d, 0f))
			   + min(max(d.x, max(d.y, d.z)), 0f);
	}

	public static float plane(float3 p, float3 n)
	{
		return dot(p, n);
	}

	public static float roundBox(float3 p, float3 b, float r)
	{
		float3 d = abs(p) - b;
		return length(max(d, 0f)) - r
			   + min(max(d.x, max(d.y, d.z)), 0f); // remove this line for an only partially signed sdf 
	}

	public static float torus(float3 p, float2 t)
	{
		float2 q = float2(length(p.xz) - t.x, p.y);
		return length(q) - t.y;
	}
}