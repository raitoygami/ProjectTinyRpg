#ifndef _VFX_FUNCTION_HLSL_
#define _VFX_FUNCTION_HLSL_

#if defined(_CUSTOMDATA)
    #define VFX_GET_CUSTOMDATA(customData01, customData02, type, def) GetCustomData(customData01, customData02, type, def)
#else
    #define VFX_GET_CUSTOMDATA(customData01, customData02, type, def) def
#endif


half GetCustomData(float4 customData01, float4 customData02, int type, half def)
{
    if (type == 0)
    {
        return def;
    }
    if (type <= 4)
    {
        return customData01[type - 1];
    }
    return customData02[type - 5];
}

//xy , yz , zw
half2 GetCustomData(float4 customData01, float4 customData02, int type, half2 def)
{
    if (type == 0)
    {
        return def;
    }
    if (type <= 3)
    {
        return half2(customData01[type - 1], customData01[type]);
    }
    return half2(customData02[type - 4], customData02[type - 3]);
}

//xyz , yzw
half3 GetCustomData(float4 customData01, float4 customData02, int type, half3 def)
{
    if (type == 0)
    {
        return def;
    }
    if (type <= 2)
    {
        return half3(customData01[type - 1], customData01[type], customData01[type + 1]);
    }
    return half3(customData02[type - 3], customData02[type - 2], customData02[type - 1]);
}

//xyzw
half4 GetCustomData(float4 customData01, float4 customData02, int type, half4 def)
{
    if (type == 0)
    {
        return def;
    }
    if (type == 1)
    {
        return customData01;
    }
    return customData02;
}

void GetCustomData1_float(float4 customData01, float4 customData02, int type, half def, out float data)
{
    data = VFX_GET_CUSTOMDATA(customData01, customData02, type, def);
}


void GetCustomData1_half(half4 customData01, half4 customData02, int type, half def, out half data)
{
    data = VFX_GET_CUSTOMDATA(customData01, customData02, type, def);
}

void GetCustomData2_float(float4 customData01, float4 customData02, int type, half2 def, out float2 data)
{
    data = VFX_GET_CUSTOMDATA(customData01, customData02, type, def);
}

void GetCustomData2_half(half4 customData01, half4 customData02, int type, half2 def, out half2 data)
{
    data = VFX_GET_CUSTOMDATA(customData01, customData02, type, def);
}

void GetCustomData3_float(float4 customData01, float4 customData02, int type, half3 def, out float3 data)
{
    data = VFX_GET_CUSTOMDATA(customData01, customData02, type, def);
}


void GetCustomData3_half(half4 customData01, half4 customData02, int type, half3 def, out half3 data)
{
    data = VFX_GET_CUSTOMDATA(customData01, customData02, type, def);
}

void GetCustomData4_float(float4 customData01, float4 customData02, int type, half4 def, out float4 data)
{
    data = VFX_GET_CUSTOMDATA(customData01, customData02, type, def);
}


void GetCustomData4_half(half4 customData01, half4 customData02, int type, half4 def, out half4 data)
{
    data = VFX_GET_CUSTOMDATA(customData01, customData02, type, def);
}

#endif
