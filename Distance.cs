using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

// Credit to Inigo Quilez for these distance functions. All I did was make them work with Unity.Mathematics.
// https://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm

public static class Distance
{
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
}