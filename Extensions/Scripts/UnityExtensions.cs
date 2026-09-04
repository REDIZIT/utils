using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public static class UnityExtensions
{
	public static bool IsDestroyed(this UnityEngine.Object target)
	{
		return !ReferenceEquals(target, null) && target == null;
	}
	
	public static Texture2D ConvertToTexture2D(this RenderTexture rt)
	{
		// 1. Создаем дескриптор для временной RT, но с пометкой sRGB
		RenderTextureDescriptor descriptor = rt.descriptor;
		descriptor.sRGB = true; // Важно: принудительно устанавливаем sRGB

		RenderTexture tempRT = RenderTexture.GetTemporary(descriptor);

		// 2. Копируем из исходной RT во временную. 
		// Если исходная была Linear, Unity сконвертирует её в sRGB при Blit.
		Graphics.Blit(rt, tempRT);

		// 3. Читаем пиксели уже из sRGB текстуры
		RenderTexture.active = tempRT;
    
		// Используем TextureFormat.RGBA32 или аналогичный, подходящий для JPG
		Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
		tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
		tex.Apply();

		RenderTexture.active = null;
		RenderTexture.ReleaseTemporary(tempRT);

		return tex;
	}

	public static RenderTexture ConvertToRenderTexture(this Texture2D tex)
	{
		RenderTexture rendTex = new(tex.width, tex.height, 0, GraphicsFormat.R8G8B8A8_SRGB);
		rendTex.enableRandomWrite = true;
		Graphics.Blit(tex, rendTex);
		return rendTex;
	}

	public static void CopyFrom(this Texture2D tex, RenderTexture rt)
	{
		RenderTexture.active = rt;
		tex.ReadPixels(new(0, 0, rt.width, rt.height), 0, 0);
		tex.Apply();
		RenderTexture.active = null;
	}
	
	public static void Dispatch2D(this ComputeShader shader, int kernelHandle, int numthreads, int sideSize)
	{
		int2 groupsCount = BaseMathUtils.GetKernelGroupsCount(new int2(numthreads, numthreads), sideSize);
		shader.Dispatch(kernelHandle, groupsCount.x, groupsCount.y, 1);
	}

	public static void Dispatch(this ComputeShader shader, int kernelHandle, int3 numthreads, int3 sideSize)
	{
		int3 groupsCount = BaseMathUtils.GetKernelGroupsCount(numthreads, sideSize);
		shader.Dispatch(kernelHandle, groupsCount.x, groupsCount.y, groupsCount.z);
	}

	public static void Dispatch(this ComputeShader shader, int kernelHandle, int2 numthreads, int2 sideSize)
	{
		int2 groupsCount = BaseMathUtils.GetKernelGroupsCount(numthreads, sideSize);
		shader.Dispatch(kernelHandle, groupsCount.x, groupsCount.y, 1);
	}
	
	public static void SetBool(this Material mat, string name, bool flag) => mat.SetInt(name, flag ? 1 : 0);
	public static void SetInt(this ComputeShader shader, string name, int2 v) => shader.SetInts(name, v.x, v.y);
	public static void SetInt(this ComputeShader shader, string name, int3 v) => shader.SetInts(name, v.x, v.y, v.z);
	public static void SetFloat(this ComputeShader shader, string name, float2 v) => shader.SetFloats(name, v.x, v.y);
	public static void SetFloat(this ComputeShader shader, string name, float3 v) => shader.SetFloats(name, v.x, v.y, v.z);
	public static void SetFloat(this ComputeShader shader, string name, float4 v) => shader.SetFloats(name, v.x, v.y, v.z, v.w);
	
	public static int2 SizeInt(this Texture tex) => new(tex.width, tex.height);
}