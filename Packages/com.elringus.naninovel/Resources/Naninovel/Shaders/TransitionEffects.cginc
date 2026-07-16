#ifndef NANINOVEL_TRANSITION_EFFECTS_INCLUDED
#define NANINOVEL_TRANSITION_EFFECTS_INCLUDED

#include "NaninovelCG.cginc"

#define SAMPLE_CLIP(texName, uv, clipColor) ( Clip01(UNITY_SAMPLE_TEX2D(texName, uv), uv, clipColor) )

inline float4 Crossfade(float2 mainUV, float2 transitionUV, float progress)
{
    const float4 CLIP_COLOR = float4(0, 0, 0, 0);
    float4 mainColor = PremultiplyAlpha(SAMPLE_CLIP(_MainTex, mainUV, CLIP_COLOR));
    float4 transitionColor = PremultiplyAlpha(SAMPLE_CLIP(_TransitionTex, transitionUV, CLIP_COLOR));
    return lerp(mainColor, transitionColor, progress);
}

inline float4 BandedSwirl(float2 mainUV, float2 transitionUV, float progress, float twistAmount, float frequency)
{
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float distanceFromCenter = length(toUV);
    float2 normToUV = toUV / distanceFromCenter;
    float angle = atan2(normToUV.y, normToUV.x);

    angle += sin(distanceFromCenter * frequency) * twistAmount * progress;
    float2 newUV;
    sincos(angle, newUV.y, newUV.x);
    newUV = newUV * distanceFromCenter + center;

    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, frac(newUV)));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));

    return lerp(mainColor, transitionColor, progress);
}

inline float4 Blinds(float2 mainUV, float2 transitionUV, float progress, float count)
{
    float4 color = frac(mainUV.y * count) < progress
                       ? UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV)
                       : UNITY_SAMPLE_TEX2D(_MainTex, mainUV);
    color = PremultiplyAlpha(color);
    return color;
}

inline float4 CircleReveal(float2 mainUV, float2 transitionUV, float progress, float fuzzyAmount)
{
    float radius = -fuzzyAmount + progress * (0.70710678 + 2.0 * fuzzyAmount);
    float fromCenter = length(mainUV - float2(0.5, 0.5));
    float distFromCircle = fromCenter - radius;

    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, mainUV));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));

    float p = saturate((distFromCircle + fuzzyAmount) / (2.0 * fuzzyAmount));
    return lerp(transitionColor, mainColor, p);
}

inline float4 CircleStretch(float2 mainUV, float2 transitionUV, float progress)
{
    float2 center = float2(0.5, 0.5);
    float radius = progress * 0.70710678;
    float2 toUV = mainUV - center;
    float len = length(toUV);
    float2 normToUV = toUV / len;

    if (len < radius)
    {
        float distFromCenterToEdge = DistanceFromCenterToSquareEdge(normToUV) / 2.0;
        float2 edgePoint = center + distFromCenterToEdge * normToUV;

        float minRadius = min(radius, distFromCenterToEdge);
        float percentFromCenterToRadius = len / minRadius;

        float2 newUV = lerp(center, edgePoint, percentFromCenterToRadius);
        return PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, newUV));
    }
    else
    {
        float distFromCenterToEdge = DistanceFromCenterToSquareEdge(normToUV);
        float2 edgePoint = center + distFromCenterToEdge * normToUV;
        float distFromRadiusToEdge = distFromCenterToEdge - radius;

        float2 radiusPoint = center + radius * normToUV;
        float2 radiusToUV = mainUV - radiusPoint;

        float percentFromRadiusToEdge = length(radiusToUV) / distFromRadiusToEdge;

        float2 newUV = lerp(center, edgePoint, percentFromRadiusToEdge);
        return PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, newUV));
    }
}

inline float4 CloudReveal(float2 mainUV, float2 transitionUV, float progress)
{
    float cloud = UNITY_SAMPLE_TEX2D(_CloudsTex, mainUV).r;
    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, mainUV));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));

    float a;

    if (progress < 0.5) a = lerp(0.0, cloud, progress / 0.5);
    else a = lerp(cloud, 1.0, (progress - 0.5) / 0.5);

    return (a < 0.5) ? mainColor : transitionColor;
}

inline float4 Crumble(float2 mainUV, float2 transitionUV, float progress, float randomSeed)
{
    float2 offset = UNITY_SAMPLE_TEX2D(_CloudsTex, float2(mainUV.x / 5, frac(mainUV.y / 5 + min(0.9, randomSeed)))).xy * 2.0 - 1.0;
    float p = progress * 2;
    if (p > 1.0) p = 1.0 - (p - 1.0);

    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, frac(mainUV + offset * p)));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, frac(transitionUV + offset * p)));

    return lerp(mainColor, transitionColor, progress);
}

inline float4 Dissolve(float2 mainUV, float2 transitionUV, float progress, float step)
{
    const float4 CLIP_COLOR = float4(0, 0, 0, 0);
    float noise = (PerlinNoise(mainUV * step) + 1.0) / 2.0;
    float4 color = noise > progress
                       ? SAMPLE_CLIP(_MainTex, mainUV, CLIP_COLOR)
                       : SAMPLE_CLIP(_TransitionTex, transitionUV, CLIP_COLOR);
    color = PremultiplyAlpha(color);
    return color;
}

inline float4 DropFade(float2 mainUV, float2 transitionUV, float progress, float randomSeed)
{
    const float4 CLIP_COLOR = float4(0, 0, 0, 0);
    float offset = UNITY_SAMPLE_TEX2D(_CloudsTex, float2(mainUV.x / 5, randomSeed)).x;
    float4 mainColor = PremultiplyAlpha(SAMPLE_CLIP(_MainTex, float2(mainUV.x, mainUV.y + offset * progress), CLIP_COLOR));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));

    if (mainColor.a <= 0.0) return transitionColor;
    return lerp(mainColor, transitionColor, progress);
}

inline float4 LineReveal(float2 mainUV, float2 transitionUV, float progress, float fuzzyAmount, float2 lineNormal, float reverse)
{
    float2 lineOrigin = float2(-fuzzyAmount, -fuzzyAmount);
    float2 lineOffset = float2(1.0 + fuzzyAmount, 1.0 + fuzzyAmount);

    float2 currentLineOrigin = lerp(lineOrigin, lineOffset, lerp(progress, 1 - progress, reverse));
    float2 normLineNormal = normalize(lineNormal);
    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, mainUV));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));

    float distFromLine = dot(normLineNormal, lerp(mainUV - currentLineOrigin, currentLineOrigin - mainUV, reverse));
    float p = saturate((distFromLine + fuzzyAmount) / (2.0 * fuzzyAmount));
    return lerp(transitionColor, mainColor, p);
}

inline float4 Pixelate(float2 mainUV, float2 transitionUV, float progress)
{
    float pixels;
    float segmentProgress;

    if (progress < 0.5) segmentProgress = 1 - progress * 2;
    else segmentProgress = (progress - 0.5) * 2;

    pixels = 5 + 1000 * segmentProgress * segmentProgress;
    float2 newMainUV = round(mainUV * pixels) / pixels;
    float2 newTransitionUV = round(transitionUV * pixels) / pixels;

    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, newMainUV));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, newTransitionUV));

    float lerpProgress = saturate((progress - 0.4) / 0.2);
    return lerp(mainColor, transitionColor, lerpProgress);
}

inline float4 RadialBlur(float2 mainUV, float2 transitionUV, float progress)
{
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float2 normToUV = toUV;

    float4 mainColor = float4(0, 0, 0, 0);
    int count = 24;
    float s = progress * 0.02;

    for (int i = 0; i < count; i++)
    {
        mainColor += PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, mainUV - normToUV * s * i));
    }

    mainColor /= count;
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));

    return lerp(mainColor, transitionColor, progress);
}

inline float4 RadialWiggle(float2 mainUV, float2 transitionUV, float progress, float randomSeed)
{
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float distanceFromCenter = length(mainUV);
    float2 normToUV = toUV / distanceFromCenter;
    float angle = (atan2(normToUV.y, normToUV.x) + 3.141592) / (2.0 * 3.141592);
    float offset1 = UNITY_SAMPLE_TEX2D(_CloudsTex, float2(angle, frac(progress / 3 + distanceFromCenter / 5 + randomSeed))).x * 2.0 - 1.0;
    float offset2 = offset1 * 2.0 * min(0.3, (1 - progress)) * distanceFromCenter;
    offset1 = offset1 * 2.0 * min(0.3, progress) * distanceFromCenter;

    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, frac(center + normToUV * (distanceFromCenter + offset1))));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, frac(center + normToUV * (distanceFromCenter + offset2))));

    return lerp(mainColor, transitionColor, progress);
}

inline float4 RandomCircleReveal(float2 mainUV, float2 transitionUV, float progress, float randomSeed)
{
    float radius = progress * 0.70710678;
    float2 fromCenter = mainUV - float2(0.5, 0.5);
    float len = length(fromCenter);

    float2 toUV = normalize(fromCenter);
    float angle = (atan2(toUV.y, toUV.x) + 3.141592) / (2.0 * 3.141592);
    radius += progress * UNITY_SAMPLE_TEX2D(_CloudsTex, float2(angle, frac(randomSeed + progress / 5.0))).r;

    float4 color = len < radius
                       ? UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV)
                       : UNITY_SAMPLE_TEX2D(_MainTex, mainUV);
    color = PremultiplyAlpha(color);
    return color;
}

inline float4 Ripple(float2 mainUV, float2 transitionUV, float progress, float frequency, float speed, float amplitude)
{
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float distanceFromCenter = length(toUV);
    float2 normToUV = toUV / distanceFromCenter;

    float wave = cos(frequency * distanceFromCenter - speed * progress);
    float offset1 = progress * wave * amplitude;
    float offset2 = (1.0 - progress) * wave * amplitude;

    float2 newUV1 = center + normToUV * (distanceFromCenter + offset1);
    float2 newUV2 = center + normToUV * (distanceFromCenter + offset2);

    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, newUV1));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, newUV2));

    return lerp(mainColor, transitionColor, progress);
}

inline float4 RotateCrumble(float2 mainUV, float2 transitionUV, float progress, float randomSeed)
{
    float2 offset = (UNITY_SAMPLE_TEX2D(_CloudsTex, float2(mainUV.x / 10, frac(mainUV.y / 10 + min(0.9, randomSeed)))).xy * 2.0 - 1.0);
    float2 center = mainUV + offset / 10.0;
    float2 toUV = mainUV - center;
    float len = length(toUV);
    float2 normToUV = toUV / len;
    float angle = atan2(normToUV.y, normToUV.x);

    angle += 3.141592 * 2.0 * progress;
    float2 newOffset;
    sincos(angle, newOffset.y, newOffset.x);
    newOffset *= len;

    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, frac(center + newOffset)));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, frac(center + newOffset)));

    return lerp(mainColor, transitionColor, progress);
}

inline float4 Saturate(float2 mainUV, float2 transitionUV, float progress)
{
    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, mainUV));
    mainColor = saturate(mainColor * (2 * progress + 1));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));

    if (progress > 0.8)
    {
        float p = (progress - 0.8) * 5.0;
        return lerp(mainColor, transitionColor, p);
    }
    return mainColor;
}

inline float4 Shrink(float2 mainUV, float2 transitionUV, float progress, float speed)
{
    const float4 CLIP_COLOR = float4(0, 0, 0, 0);
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float distanceFromCenter = length(toUV);
    float2 normToUV = toUV / distanceFromCenter;

    float2 newUV = center + normToUV * (distanceFromCenter * (progress * speed + 1));
    float4 mainColor = PremultiplyAlpha(SAMPLE_CLIP(_MainTex, newUV, CLIP_COLOR));
    if (mainColor.a <= 0) mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));

    return mainColor;
}

inline float4 SlideIn(float2 mainUV, float2 transitionUV, float progress, float2 slideAmount)
{
    mainUV += slideAmount * progress;
    float4 color = any(saturate(mainUV) - mainUV)
                       ? UNITY_SAMPLE_TEX2D(_TransitionTex, frac(mainUV))
                       : UNITY_SAMPLE_TEX2D(_MainTex, mainUV);
    color = PremultiplyAlpha(color);
    return color;
}

inline float4 SwirlGrid(float2 mainUV, float2 transitionUV, float progress, float twistAmount, float cellCount)
{
    float cellSize = 1.0 / cellCount;

    float2 cell = floor(mainUV * cellCount);
    float2 oddeven = fmod(cell, 2.0);
    float cellTwistAmount = twistAmount;
    if (oddeven.x < 1.0) cellTwistAmount *= -1;
    if (oddeven.y < 1.0) cellTwistAmount *= -1;

    float2 newUV = frac(mainUV * cellCount);

    float2 center = float2(0.5, 0.5);
    float2 toUV = newUV - center;
    float distanceFromCenter = length(toUV);
    float2 normToUV = toUV / distanceFromCenter;
    float angle = atan2(normToUV.y, normToUV.x);

    angle += max(0, 0.5 - distanceFromCenter) * cellTwistAmount * progress;
    float2 newUV2;
    sincos(angle, newUV2.y, newUV2.x);
    newUV2 *= distanceFromCenter;
    newUV2 += center;

    newUV2 *= cellSize;
    newUV2 += cell * cellSize;

    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, newUV2));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));

    return lerp(mainColor, transitionColor, progress);
}

inline float4 Swirl(float2 mainUV, float2 transitionUV, float progress, float twistAmount)
{
    const float4 CLIP_COLOR = float4(0, 0, 0, 0);
    float2 center = float2(0.5, 0.5);
    float2 toUV = mainUV - center;
    float distanceFromCenter = length(toUV);
    float2 normToUV = toUV / distanceFromCenter;
    float angle = atan2(normToUV.y, normToUV.x);

    angle += distanceFromCenter * distanceFromCenter * twistAmount * progress;
    float2 newUV;
    sincos(angle, newUV.y, newUV.x);
    newUV *= distanceFromCenter;
    newUV += center;

    float4 mainColor = PremultiplyAlpha(SAMPLE_CLIP(_MainTex, newUV, CLIP_COLOR));
    float4 transitionColor = PremultiplyAlpha(SAMPLE_CLIP(_TransitionTex, transitionUV, CLIP_COLOR));

    return lerp(mainColor, transitionColor, progress);
}

inline float4 Water(float2 mainUV, float2 transitionUV, float progress, float randomSeed)
{
    float2 offset = UNITY_SAMPLE_TEX2D(_CloudsTex, float2(mainUV.x / 10, frac(mainUV.y / 10 + min(0.9, randomSeed)))).xy * 2.0 - 1.0;
    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, frac(mainUV + offset * progress)));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));

    if (mainColor.a <= 0.0) return transitionColor;
    return lerp(mainColor, transitionColor, progress);
}

inline float4 Waterfall(float2 mainUV, float2 transitionUV, float progress, float randomSeed)
{
    float offset = 1 - min(progress + progress * UNITY_SAMPLE_TEX2D(_CloudsTex, float2(mainUV.x, randomSeed)).r, 1.0);
    mainUV.y -= offset;
    transitionUV.y -= offset;

    float4 color = mainUV.y > 0.0
                       ? UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV)
                       : UNITY_SAMPLE_TEX2D(_MainTex, frac(mainUV));
    color = PremultiplyAlpha(color);
    return color;
}

inline float4 Wave(float2 mainUV, float2 transitionUV, float progress, float magnitude, float phase, float frequency)
{
    const float4 CLIP_COLOR = float4(0, 0, 0, 0);
    float2 newUV = mainUV + float2(magnitude * progress * sin(frequency * mainUV.y + phase * progress), 0);

    float4 mainColor = PremultiplyAlpha(SAMPLE_CLIP(_MainTex, newUV, CLIP_COLOR));
    float4 transitionColor = PremultiplyAlpha(SAMPLE_CLIP(_TransitionTex, transitionUV, CLIP_COLOR));

    return lerp(mainColor, transitionColor, progress);
}

inline float4 Custom(float2 mainUV, float2 transitionUV, float progress, float fuzzy, float invert)
{
    float4 mainColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_MainTex, mainUV));
    float4 transitionColor = PremultiplyAlpha(UNITY_SAMPLE_TEX2D(_TransitionTex, transitionUV));
    float4 customColor = UNITY_SAMPLE_TEX2D(_DissolveTex, transitionUV);
    customColor = lerp(customColor, 1 - customColor, invert);
    fuzzy = 100 - max(0, min(100, fuzzy));
    float p = saturate((progress - customColor.r) * fuzzy + progress);
    return lerp(mainColor, transitionColor, p);
}

// Executes transition effect based on enabled keyword.
// Returns resulting color of the transition at the given texture coordinates.
inline float4 ApplyTransitionEffect(float2 mainUV, float2 transitionUV, float progress, float4 params, float2 randomSeed)
{
    #ifdef NANINOVEL_TRANSITION_BANDEDSWIRL
    return BandedSwirl(mainUV, transitionUV, progress, params.x, params.y);
    #endif

    #ifdef NANINOVEL_TRANSITION_BLINDS
    return Blinds(mainUV, transitionUV, progress, params.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_CIRCLEREVEAL
    return CircleReveal(mainUV, transitionUV, progress, params.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_CIRCLESTRETCH
    return CircleStretch(mainUV, transitionUV, progress);
    #endif

    #ifdef NANINOVEL_TRANSITION_CLOUDREVEAL
    return CloudReveal(mainUV, transitionUV, progress);
    #endif

    #ifdef NANINOVEL_TRANSITION_CRUMBLE
    return Crumble(mainUV, transitionUV, progress, randomSeed.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_DISSOLVE
    return Dissolve(mainUV, transitionUV, progress, params.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_DROPFADE
    return DropFade(mainUV, transitionUV, progress, randomSeed.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_LINEREVEAL
    return LineReveal(mainUV, transitionUV, progress, params.x, params.yz, params.w);
    #endif

    #ifdef NANINOVEL_TRANSITION_PIXELATE
    return Pixelate(mainUV, transitionUV, progress);
    #endif

    #ifdef NANINOVEL_TRANSITION_RADIALBLUR
    return RadialBlur(mainUV, transitionUV, progress);
    #endif

    #ifdef NANINOVEL_TRANSITION_RADIALWIGGLE
    return RadialWiggle(mainUV, transitionUV, progress, randomSeed.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_RANDOMCIRCLEREVEAL
    return RandomCircleReveal(mainUV, transitionUV, progress, randomSeed.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_RIPPLE
    return Ripple(mainUV, transitionUV, progress, params.x, params.y, params.z);
    #endif

    #ifdef NANINOVEL_TRANSITION_ROTATECRUMBLE
    return RotateCrumble(mainUV, transitionUV, progress, randomSeed.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_SATURATE
    return Saturate(mainUV, transitionUV, progress);
    #endif

    #ifdef NANINOVEL_TRANSITION_SHRINK
    return Shrink(mainUV, transitionUV, progress, params.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_SLIDEIN
    return SlideIn(mainUV, transitionUV, progress, params.xy);
    #endif

    #ifdef NANINOVEL_TRANSITION_SWIRLGRID
    return SwirlGrid(mainUV, transitionUV, progress, params.x, params.y);
    #endif

    #ifdef NANINOVEL_TRANSITION_SWIRL
    return Swirl(mainUV, transitionUV, progress, params.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_WATER
    return Water(mainUV, transitionUV, progress, randomSeed.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_WATERFALL
    return Waterfall(mainUV, transitionUV, progress, randomSeed.x);
    #endif

    #ifdef NANINOVEL_TRANSITION_WAVE
    return Wave(mainUV, transitionUV, progress, params.x, params.y, params.z);
    #endif

    #ifdef NANINOVEL_TRANSITION_CUSTOM
    return Custom(mainUV, transitionUV, progress, params.x, params.y);
    #endif

    // When no transition keywords enabled default to crossfade.
    return Crossfade(mainUV, transitionUV, progress);
}

#endif // NANINOVEL_TRANSITION_EFFECTS_INCLUDED
