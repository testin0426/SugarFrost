#ifndef LAYER_FX_INCLUDED
#define LAYER_FX_INCLUDED

// Shared per-layer FX for RT plane shaders (RTfilter, Drawing_Toon_Plane, ...).
// Provides: procedural feathered masks (rect / ellipse / band) evaluated in
// local-UV or screen space, an optional screen-space *global* mask shared by
// every material that opts in, and a mip-LOD "layer blur" sampler.
//
// All parameters are scalar floats so they can be animated one-by-one from
// Naninovel via @rtfx (see RTFXCommand.cs). The including shader must list
// them in its Properties block; they are declared (once) here.
//
// Coordinate conventions:
//  - Local space: quad UV, (0,0) bottom-left .. (1,1) top-right.
//  - Screen space: normalized screen UV; X is aspect-corrected around the
//    mask center, so sizes are in *screen-height* units and circles stay round.
//  - _MaskWidth/_MaskHeight are HALF extents. _MaskFeather straddles the
//    shape edge symmetrically (AE-style), in the same units.
//  - Band: visible on the -X side of the line through the center, rotated by
//    _MaskAngle (degrees, CCW). Animate _MaskCenterX to wipe horizontally.

// ---- Per-material mask ----
float _MaskMode;      // 0 = off, 1 = rect, 2 = ellipse, 3 = band
float _MaskSpace;     // 0 = local UV, 1 = screen
float _MaskCenterX;
float _MaskCenterY;
float _MaskWidth;     // half extent
float _MaskHeight;    // half extent
float _MaskFeather;
float _MaskAngle;     // degrees
float _MaskInvert;    // 0 / 1

// Opt-in to the shared global mask (set as a material [Toggle]).
float _UseGlobalMask;

// ---- Global mask (Shader.SetGlobalFloat, always screen space) ----
// Defaults to mode 0 == no effect, so materials are safe before any @rtfx.
float _GMaskMode;
float _GMaskCenterX;
float _GMaskCenterY;
float _GMaskWidth;
float _GMaskHeight;
float _GMaskFeather;
float _GMaskAngle;
float _GMaskInvert;

// ---- Layer blur ----
float _BlurLod;       // 0 = sharp; >0 = mip LOD of _MainTex (RT needs mipmaps!)

float2 LayerFX_Rotate (float2 p, float degrees)
{
    float r = radians(degrees);
    float s = sin(r), c = cos(r);
    return float2(c * p.x + s * p.y, -s * p.x + c * p.y);
}

// Evaluates one mask shape. Returns 1 = fully visible, 0 = fully masked.
float LayerFX_EvalShape (float mode, float2 uv, float2 center, float2 halfSize,
                         float feather, float angleDeg, float invert, float aspect)
{
    if (mode < 0.5) return 1.0;

    float2 p = uv - center;
    p.x *= aspect;                       // 1 in local space; W/H on screen
    p = LayerFX_Rotate(p, angleDeg);

    float f = max(feather, 1e-4) * 0.5;  // feather straddles the edge
    float2 hs = max(halfSize, 1e-5);
    float m;

    if (mode < 1.5)                      // rect
    {
        float2 d = abs(p) - hs;
        m = 1.0 - smoothstep(-f, f, max(d.x, d.y));
    }
    else if (mode < 2.5)                 // ellipse
    {
        // Radial distance converted back to ~UV units so feather feels
        // consistent with the rect shape.
        float dist = (length(p / hs) - 1.0) * min(hs.x, hs.y);
        m = 1.0 - smoothstep(-f, f, dist);
    }
    else                                 // band (travelling edge)
    {
        m = 1.0 - smoothstep(-f, f, p.x);
    }

    return lerp(m, 1.0 - m, saturate(invert));
}

// Combined mask factor for the fragment: local/per-material mask times the
// shared global one (if this material opted in). Multiply into final alpha.
float LayerFX_MaskAlpha (float2 uv, float2 screenUV)
{
    float screenAspect = _ScreenParams.x / _ScreenParams.y;

    float2 p     = _MaskSpace > 0.5 ? screenUV : uv;
    float aspect = _MaskSpace > 0.5 ? screenAspect : 1.0;

    float m = LayerFX_EvalShape(_MaskMode, p,
        float2(_MaskCenterX, _MaskCenterY), float2(_MaskWidth, _MaskHeight),
        _MaskFeather, _MaskAngle, _MaskInvert, aspect);

    if (_UseGlobalMask > 0.5)
    {
        m *= LayerFX_EvalShape(_GMaskMode, screenUV,
            float2(_GMaskCenterX, _GMaskCenterY), float2(_GMaskWidth, _GMaskHeight),
            _GMaskFeather, _GMaskAngle, _GMaskInvert, screenAspect);
    }

    return m;
}

// Layer blur: mip-LOD sample plus a 4-tap diagonal spread to soften the
// box-filter look of raw mips. Alpha blurs with color, which is what we want
// for soft layer edges. No-op (and mip-0 exact) when _BlurLod is 0.
float4 LayerFX_SampleBlurred (TEXTURE2D_PARAM(tex, samp), float2 uv,
                              float lod, float2 texelSize)
{
    if (lod <= 0.01)
        return SAMPLE_TEXTURE2D(tex, samp, uv);

    float2 spread = texelSize * exp2(lod) * 0.5;
    float4 c = SAMPLE_TEXTURE2D_LOD(tex, samp, uv, lod) * 0.4;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2( spread.x,  spread.y), lod) * 0.15;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2(-spread.x,  spread.y), lod) * 0.15;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2( spread.x, -spread.y), lod) * 0.15;
    c += SAMPLE_TEXTURE2D_LOD(tex, samp, uv + float2(-spread.x, -spread.y), lod) * 0.15;
    return c;
}

#endif // LAYER_FX_INCLUDED
