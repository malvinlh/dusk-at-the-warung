// A minimal unlit sprite shader = Unity's built-in "Sprites/Default" plus a solid-colour
// flash. Because it is built on the same UnitySprites.cginc path as the default sprite
// shader, it honours sprite tint, flip, atlases and transparency exactly. Drive _FlashAmount
// per-battler with a MaterialPropertyBlock (no per-instance material copies) for the hit flash.
Shader "DuskWarung/SpriteFlash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFlashFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            // Extra flash uniforms (everything else comes from UnitySprites.cginc).
            fixed4 _FlashColor;
            float _FlashAmount;

            fixed4 SpriteFlashFrag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                // Push the colour toward the (absolute) flash colour so the flash is pure
                // white regardless of the sprite's tint — avoids the "tint by renderer colour" gotcha.
                c.rgb = lerp(c.rgb, _FlashColor.rgb, _FlashAmount);
                c.rgb *= c.a; // Premultiplied alpha to match the Blend One OneMinusSrcAlpha mode.
                return c;
            }
        ENDCG
        }
    }
}
