static const float PI = 3.14159265f;

float invLerp(float from, float to, float value) {
	return (value - from) / (to - from);
}

float remap(float origFrom, float origTo, float targetFrom, float targetTo, float value) {
	float rel = invLerp(origFrom, origTo, value);
	return lerp(targetFrom, targetTo, rel);
}


float calculateMarchDistance(float3 rayOrigin, float3 rayDir) {

	// Assume origin & direction given in world space
	float3 transformedOrigin = mul(unity_WorldToObject, float4(rayOrigin, 1.0)).xyz;
	float3 transformedDirection = mul(unity_WorldToObject, float4(rayDir, 0.0)).xyz;

	// Assume a cube shape
	float3 boundsMin = float3(-0.5, -0.5, -0.5);
	float3 boundsMax = float3(0.5, 0.5, 0.5);

	float3 invRaydir = 1.0 / transformedDirection;
	float3 t0 = (boundsMin - transformedOrigin) * invRaydir;
	float3 t1 = (boundsMax - transformedOrigin) * invRaydir;
	float3 tmin = min(t0, t1);
	float3 tmax = max(t0, t1);

	float dstA = max(max(tmin.x, tmin.y), tmin.z);
	float dstB = min(tmax.x, min(tmax.y, tmax.z));

	// CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
	// dstA is dst to nearest intersection, dstB dst to far intersection

	// CASE 2: ray intersects box from inside (dstA < 0 < dstB)
	// dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

	// CASE 3: ray misses box (dstA > dstB)

	float dstToBox = max(0, dstA);
	float dstInsideBox = max(0, dstB - dstToBox);
	return dstInsideBox;
}