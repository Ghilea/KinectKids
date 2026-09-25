Shader "KinectKids/Greve Volumetric Fog Resource"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0,0,0,1)
        _Density ("Density", Range(0,8)) = 2
        _NoiseScale ("Noise Scale", Range(0.05,4)) = 0.55
        _NoiseSpeed ("Noise Speed", Vector) = (0.05,0.02,0.03,0)
        _EdgeSoftness ("Edge Softness", Range(0.02,0.8)) = 0.22
        _FloorMode ("Floor Mode", Range(0,1)) = 0
    }
    Fallback "KinectKids/Greve Volumetric Fog"
}
